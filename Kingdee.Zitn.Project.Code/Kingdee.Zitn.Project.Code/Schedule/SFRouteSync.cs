using Kingdee.BOS;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.Zitn.Project.Code.conf;
using Kingdee.Zitn.Project.Code.plugin.SFForm;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Kingdee.Zitn.Project.Code.Schedule
{
    /// <summary>
    /// 【定时任务】顺丰物流轨迹同步
    /// 定期查询"已下单"状态的顺丰面单，同步最新路由信息。
    ///
    /// === 顺丰 API 限制 ===
    /// - 路由查询接口限频: 30 次/秒
    /// - 单次最多查 10 个运单号
    /// - 可在 K3 Cloud 任务计划中设置运行间隔
    /// </summary>
    [Description("【定时任务】顺丰物流轨迹同步"), HotUpdate]
    public class SFRouteSync : IScheduleService
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("顺丰路由同步");

        /// <summary>
        /// 定时任务入口，由 K3 Cloud 调度框架调用
        /// </summary>
        public void Run(Context ctx, BOS.Core.Schedule schedule)
        {
            _log.Section("路由同步定时任务开始");

            try
            {
                // 查询需要同步的运单：
                // - 状态为"已下单"
                // - 有运单号
                // - 最后同步时间超过配置的间隔（或从未同步）
                var sql = $@"/*dialect*/
SELECT
    FID,
    FBILLNO,
    FSFORDERID,
    FSFWAYBILLNO,
    FLASTSYNCTIME
FROM T_SF_WAYBILL  -- 替换为实际表名
WHERE FORDERSTATUS = '已下单'
  AND FSFWAYBILLNO IS NOT NULL
  AND FSFWAYBILLNO != ''
  AND (
      FLASTSYNCTIME IS NULL
      OR FLASTSYNCTIME < DATEADD(HOUR, -1, GETDATE())
  )
ORDER BY FLASTSYNCTIME ASC";

                DynamicObjectCollection waybills;
                try
                {
                    waybills = DBServiceHelper.ExecuteDynamicObject(ctx, sql);
                }
                catch (Exception ex)
                {
                    _log.Error($"查询待同步运单失败，请确认表名是否正确: {ex.Message}");
                    _log.WriteLog("提示: 将 sql 中的 T_SF_WAYBILL 替换为顺丰面单的实际数据库表名");
                    return;
                }

                if (waybills == null || waybills.Count == 0)
                {
                    _log.WriteLog("无待同步运单");
                    return;
                }

                _log.WriteLog($"待同步运单数: {waybills.Count}");

                var client = CreateSFClient();
                if (client == null)
                {
                    _log.Error("顺丰配置不完整，无法同步");
                    return;
                }

                int successCount = 0;
                int failCount = 0;
                int deliveredCount = 0;
                var updateSqls = new List<string>();

                foreach (DynamicObject wb in waybills)
                {
                    try
                    {
                        var fid = Convert.ToInt64(wb["FID"]);
                        var billNo = Convert.ToString(wb["FBILLNO"]);
                        var waybillNo = Convert.ToString(wb["FSFWAYBILLNO"]);

                        if (string.IsNullOrEmpty(waybillNo))
                            continue;

                        _log.WriteLog($"查询路由: {billNo}, 运单号: {waybillNo}");

                        var result = client.SearchRoutesByWaybill(waybillNo);

                        if (!result.Success)
                        {
                            failCount++;
                            _log.WriteLog($"  查询失败: [{result.ErrorCode}] {result.ErrorMsg}");
                            continue;
                        }

                        var routes = result.Data?.routeResps
                            ?.SelectMany(r => r.routes ?? new List<SFRouteNode>())
                            .OrderBy(r => r.acceptTime)
                            .ToList();

                        if (routes == null || routes.Count == 0)
                        {
                            _log.WriteLog($"  暂无路由信息");
                            // 更新同步时间，避免频繁无效查询
                            updateSqls.Add(
                                $@"UPDATE T_SF_WAYBILL SET FLASTSYNCTIME = GETDATE() WHERE FID = {fid}");
                            continue;
                        }

                        successCount++;

                        // 检查是否已签收（opCode = "80" 或 remark 包含"签收"）
                        var lastNode = routes.Last();
                        bool isDelivered = lastNode.opCode == "80"
                            || (lastNode.remark ?? "").Contains("签收");

                        if (isDelivered)
                        {
                            deliveredCount++;
                            _log.WriteLog($"  已签收, 运单号: {waybillNo}, 轨迹数: {routes.Count}");

                            updateSqls.Add(
                                $@"UPDATE T_SF_WAYBILL
SET FROUTEDATA = '{JsonConvert.SerializeObject(routes).Replace("'", "''")}',
    FLASTSYNCTIME = GETDATE(),
    FROUTESTATUS = '已签收'
WHERE FID = {fid}");
                        }
                        else
                        {
                            _log.WriteLog($"  同步成功, 运单号: {waybillNo}, 轨迹数: {routes.Count}");

                            updateSqls.Add(
                                $@"UPDATE T_SF_WAYBILL
SET FROUTEDATA = '{JsonConvert.SerializeObject(routes).Replace("'", "''")}',
    FLASTSYNCTIME = GETDATE()
WHERE FID = {fid}");
                        }

                        // 批量写库（每 10 条执行一次），避免单条SQL过大
                        if (updateSqls.Count >= 10)
                        {
                            FlushUpdates(ctx, updateSqls);
                            updateSqls.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        _log.Error($"同步异常: {ex.Message}");
                    }
                }

                // 剩余的执行
                FlushUpdates(ctx, updateSqls);

                _log.WriteLog(
                    $"同步完成: 成功 {successCount}, 失败 {failCount}, 已签收 {deliveredCount}, 总数 {waybills.Count}");
            }
            catch (Exception ex)
            {
                _log.Error("定时任务异常");
                _log.Error(ex);
            }

            _log.Section("路由同步定时任务结束");
        }

        private static void FlushUpdates(Context ctx, List<string> sqls)
        {
            if (sqls == null || sqls.Count == 0) return;
            try
            {
                var batch = string.Join(";", sqls);
                DBServiceHelper.Execute(ctx, batch);
            }
            catch (Exception ex)
            {
                _log.Error($"批量更新失败: {ex.Message}");
            }
        }

        private static SFExpressClient CreateSFClient()
        {
            var partnerID = SFConfig.PartnerID;
            var checkWord = SFConfig.CheckWord;
            var apiUrl = SFConfig.ApiUrl;

            if (string.IsNullOrEmpty(partnerID) || string.IsNullOrEmpty(checkWord))
                return null;

            return new SFExpressClient(partnerID, checkWord, apiUrl, timeoutMs: 15000, logger: _log);
        }

    }
}

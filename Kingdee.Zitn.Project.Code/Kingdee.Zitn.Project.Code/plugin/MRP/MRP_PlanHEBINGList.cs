using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Util;
using Kingdee.BOS.WebApi.Client;
using Newtonsoft.Json.Linq;
using Kingdee.Zitn.Project.Code.conf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;

namespace Kingdee.Zitn.Project.Code.plugin.MRP
{
    [Description("计划订单合并-按物料+计划跟踪号+云枢单据号+标记分组合并调用标准服务-列表"), HotUpdate]
    public class MRP_PlanHEBINGList : AbstractListPlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("计划订单合并");

        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);

            if (!e.BarItemKey.EqualsIgnoreCase("tbMerge1")) return;

            _log.WriteLog("========== 计划订单合并-按物料+跟踪号+云枢单据号+标记 开始 ==========");


            e.Cancel = true;



            var querySql = @"/*dialect*/SELECT
                A.FID,
                M.FNUMBER AS WLNUM,
                ISNULL(A.FMtoNo, '') AS JHGZH,
                ISNULL(A.F_PAEZ_YSDJH, '') AS YSDJH,
                 ISNULL(A.F_BJ, '') AS BJ
            FROM T_PLN_PLANORDER A
            JOIN T_PLN_PLANORDER_B B ON A.FID = B.FID
            JOIN T_BD_MATERIAL M ON A.FMATERIALID = M.FMATERIALID
            WHERE A.FDOCUMENTSTATUS IN ('A', 'D')
              AND A.FRELEASESTATUS = '0'
             AND A.FRELEASETYPE = '1'
            AND A.F_BJ ='1'";

            _log.WriteLog($"查询符合条件的计划订单: {querySql}");

            var queryResult = DBUtils.ExecuteDynamicObject(this.Context, querySql);
            if (queryResult == null || queryResult.Count < 2)
            {
                _log.WriteLog($"查询结果<2条(共{queryResult?.Count ?? 0}条)，无需合并，退出");
                return;
            }
            _log.WriteLog($"查询到 {queryResult.Count} 条符合条件的计划订单");


            var detailLimit = Math.Min(queryResult.Count, 20);
            _log.WriteLog($"查询明细(前{detailLimit}条):");
            for (int i = 0; i < detailLimit; i++)
            {
                var r = queryResult[i];
                _log.WriteLog($"  物料=[{r["WLNUM"]}], 计划跟踪号=[{r["JHGZH"]}], 云枢单据号=[{r["YSDJH"]}], 标记=[{r["BJ"]}], FID={r["FID"]}");
            }
            if (queryResult.Count > 20)
                _log.WriteLog($"  ... 共{queryResult.Count}条，其余省略");


            var groups = new Dictionary<string, List<string>>();
            foreach (DynamicObject row in queryResult)
            {
                var fid = row["FID"]?.ToString();
                var wlNum = row["WLNUM"]?.ToString() ?? "";
                var jhgzh = row["JHGZH"]?.ToString() ?? "";
                var ysdjh = row["YSDJH"]?.ToString() ?? "";
                var bj = row["BJ"]?.ToString() ?? "";

                if (string.IsNullOrEmpty(fid)) continue;

                var key = $"{wlNum}|{jhgzh}|{ysdjh}|{bj}";
                if (!groups.ContainsKey(key))
                    groups[key] = new List<string>();
                groups[key].Add(fid);
            }

            _log.WriteLog($"分组结果: {groups.Count} 组");
            foreach (var g in groups)
                _log.WriteLog($"  组 [{g.Key}]: {g.Value.Count} 条");


            var mergeGroups = groups.Where(g => g.Value.Count >= 2).ToList();
            if (mergeGroups.Count == 0)
            {
                _log.WriteLog("没有≥2条的分组，无需合并，退出");
                return;
            }
            _log.WriteLog($"需合并分组: {mergeGroups.Count} 组，共 {mergeGroups.Sum(g => g.Value.Count)} 条");


            var client = new K3CloudApiClient(ErpLogin.K3CloudUrl);
            _log.WriteLog("登录K3Cloud...");
            var loginResult = client.ValidateLogin(
                ErpLogin.AppId,
                ErpLogin.UserName,
                ErpLogin.Password,
                ErpLogin.Lcid);

            if (JObject.Parse(loginResult)["LoginResultType"].Value<int>() != 1)
            {
                _log.WriteLog("登录失败，退出");
                return;
            }
            _log.WriteLog("登录成功");

            var successCount = 0;
            var failCount = 0;

            foreach (var group in mergeGroups)
            {
                var ids = group.Value;
                var parts = group.Key.Split('|');
                var wl = parts.Length > 0 ? parts[0] : "";
                var jh = parts.Length > 1 ? parts[1] : "";
                var ys = parts.Length > 2 ? parts[2] : "";
                var bj = parts.Length > 3 ? parts[3] : "";

                var idx = successCount + failCount + 1;
                _log.WriteLog($"---------- 第{idx}/{mergeGroups.Count}组合并 ----------");
                _log.WriteLog($"  物料=[{wl}], 计划跟踪号=[{jh}], 云枢单据号=[{ys}], 标记=[{bj}]");
                _log.WriteLog($"  合并行数={ids.Count}, FIDs=[{string.Join(",", ids)}]");

                var mergeInfo = new JObject
                {
                    ["CreateOrgId"] = 0,
                    ["Ids"] = string.Join(",", ids),
                    ["NetworkCtrl"] = new JObject()
                };

                try
                {
                    var result = client.ExcuteOperation("PLN_PLANORDER", "tbMerge", mergeInfo.ToString());
                    var resultObj = JObject.Parse(result);
                    var isSuccess = resultObj["Result"]?["ResponseStatus"]?["IsSuccess"]?.Value<bool>() ?? false;
                    if (isSuccess)
                    {
                        _log.WriteLog($"  合并成功");
                        successCount++;
                    }
                    else
                    {
                        var errors = resultObj["Result"]?["ResponseStatus"]?["Errors"] as JArray;
                        var errorMsg = errors != null && errors.Count > 0
                            ? errors[0]?["Message"]?.ToString()
                            : "未知错误";
                        _log.WriteLog($"  合并失败: {errorMsg}");
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    _log.WriteLog($"  合并异常: {ex.Message}");
                    failCount++;
                }
            }

            _log.WriteLog($"========== 合并汇总: 共{mergeGroups.Count}组, 成功{successCount}组, 失败{failCount}组 ==========");


            this.ListView.Refresh();

            _log.WriteLog("========== 计划订单合并-按物料+跟踪号+云枢单据号+标记 结束 ==========");
        }
    }
}

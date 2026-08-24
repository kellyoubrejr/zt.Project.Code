using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.Zitn.Project.Code.conf;
using Kingdee.Zitn.Project.Code.plugin.SFBill;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Globalization;

namespace Kingdee.Zitn.Project.Code.Schedule
{
    [Description("【跑批】：物流面单顺丰路由轨迹+运费自动同步")]
    [Kingdee.BOS.Util.HotUpdate]
    public class SFBillSync : IScheduleService
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("物流面单同步");

        private const string HEAD_TABLE = "ZMER_t_Cust100030";      // 单据头
        private const string ROUTE_TABLE = "ZMER_t_Cust_Entry100084"; // 路由轨迹单据体
        private const string FEE_TABLE = "ZMER_t_Cust_Entry100087";   // 费用单据体

        public void Run(Context ctx, Kingdee.BOS.Core.Schedule schedule)
        {
            _log.Section("开始同步物流面单顺丰路由+运费");

            try
            {
                // 查询「已下单(B)」的面单
                var bills = DBUtils.ExecuteDynamicObject(ctx,
                    $@"/*dialect*/SELECT FID, FBillNo, FSFYDH FROM {HEAD_TABLE}
                        WHERE FORDERSTATUS = 'B' AND FSFYDH IS NOT NULL AND FSFYDH <> ''");

                if (bills == null || bills.Count == 0)
                {
                    _log.WriteLog("没有需要同步的面单");
                    return;
                }

                _log.WriteLog($"共 {bills.Count} 条面单需要同步");

                for (int i = 0; i < bills.Count; i++)
                {
                    long fid = Convert.ToInt64(bills[i]["FID"]);
                    string billNo = Convert.ToString(bills[i]["FBillNo"]);
                    string waybillNo = Convert.ToString(bills[i]["FSFYDH"]);

                    try
                    {
                        _log.Section($"同步 [{i + 1}/{bills.Count}] {billNo}，运单号={waybillNo}");
                        SyncRoutes(ctx, fid, waybillNo);
                        SyncFreight(ctx, fid, waybillNo);
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"同步失败：{billNo}");
                        _log.Error(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }

            _log.Section("同步结束");
        }

        /// <summary>同步路由轨迹（EXP_RECE_SEARCH_ROUTES）</summary>
        private void SyncRoutes(Context ctx, long fid, string waybillNo)
        {
            var msg = new JObject
            {
                ["language"] = "zh-CN",
                ["trackingType"] = "1",
                ["trackingNumber"] = new JArray(waybillNo),
                ["methodType"] = "1"
            };

            var apiResult = SFExpressClient.Call(SFExpressClient.SERVICE_SEARCH_ROUTES, JsonConvert.SerializeObject(msg));
            if (!apiResult.Success || apiResult.MsgData == null)
            {
                _log.WriteLog($"路由查询失败：{apiResult.ErrorCode} {apiResult.ErrorMsg}");
                return;
            }

            var routeResps = apiResult.MsgData["routeResps"] as JArray;
            if (routeResps == null || routeResps.Count == 0)
            {
                _log.WriteLog("路由查询无返回数据");
                return;
            }

            var routes = routeResps[0]["routes"] as JArray;
            if (routes == null || routes.Count == 0)
            {
                _log.WriteLog($"运单号 {waybillNo} 暂无路由轨迹");
                return;
            }

            // 先清空旧轨迹，再插入最新
            DBUtils.Execute(ctx, $"/*dialect*/DELETE FROM {ROUTE_TABLE} WHERE FID = {fid}");

            foreach (var r in routes)
            {
                string acceptTime = r["acceptTime"]?.ToString() ?? "";
                string acceptAddress = r["acceptAddress"]?.ToString() ?? "";
                string remark = r["remark"]?.ToString() ?? "";
                string opCode = r["opCode"]?.ToString() ?? "";
                string statusName = r["secondaryStatusName"]?.ToString()
                                    ?? r["firstStatusName"]?.ToString() ?? "";

                long entryId = NextEntryId(ctx, ROUTE_TABLE);

                var sql = $@"/*dialect*/INSERT INTO {ROUTE_TABLE}
                        (FENTRYID, FID, FAcceptTime1, FAcceptAddress, FRemark, FOpCode, FStatusName)
                    VALUES
                        ({entryId}, {fid}, '{Sql(acceptTime)}', '{Sql(acceptAddress)}', '{Sql(remark)}', '{Sql(opCode)}', '{Sql(statusName)}')";
                DBUtils.Execute(ctx, sql);
            }

            _log.WriteLog($"路由同步完成：{routes.Count} 条轨迹");
        }

        /// <summary>同步运费（EXP_RECE_QUERY_SFWAYBILL）</summary>
        private void SyncFreight(Context ctx, long fid, string waybillNo)
        {
            var msg = new JObject
            {
                ["trackingType"] = "2",
                ["trackingNum"] = waybillNo
            };

            var apiResult = SFExpressClient.Call(SFExpressClient.SERVICE_QUERY_SFWAYBILL, JsonConvert.SerializeObject(msg));
            if (!apiResult.Success || apiResult.MsgData == null)
            {
                _log.WriteLog($"运费查询失败：{apiResult.ErrorCode} {apiResult.ErrorMsg}");
                return;
            }

            var feeList = apiResult.MsgData["waybillFeeList"] as JArray;
            if (feeList == null || feeList.Count == 0)
            {
                _log.WriteLog($"运单号 {waybillNo} 暂无运费信息");
                return;
            }

            DBUtils.Execute(ctx, $"/*dialect*/DELETE FROM {FEE_TABLE} WHERE FID = {fid}");

            decimal total = 0;
            foreach (var fee in feeList)
            {
                string feeType = FeeTypeName(fee["type"]?.ToString() ?? "");
                string feeName = fee["name"]?.ToString() ?? "";
                decimal feeValue = 0;
                decimal.TryParse(fee["value"]?.ToString(), out feeValue);
                string settlementType = SettlementTypeName(fee["settlementTypeCode"]?.ToString() ?? "");

                total += feeValue;

                long entryId = NextEntryId(ctx, FEE_TABLE);

                var sql = $@"/*dialect*/INSERT INTO {FEE_TABLE}
                        (FENTRYID, FID, FFeeType, FFeeName, FFeeValue, FSettlementType)
                    VALUES
                        ({entryId}, {fid}, '{Sql(feeType)}', '{Sql(feeName)}', {feeValue.ToString(CultureInfo.InvariantCulture)}, '{Sql(settlementType)}')";
                DBUtils.Execute(ctx, sql);
            }

            // 回写运费总额 + 同步时间
            DBUtils.Execute(ctx, $@"/*dialect*/UPDATE {HEAD_TABLE}
                    SET FFreightTotal = {total.ToString(CultureInfo.InvariantCulture)}, FTBSJ = GETDATE()
                    WHERE FID = {fid}");

            _log.WriteLog($"运费同步完成：{feeList.Count} 条费用，总额={total}");
        }

        private long NextEntryId(Context ctx, string table)
        {
            var r = DBUtils.ExecuteDynamicObject(ctx,
                $"/*dialect*/SELECT ISNULL(MAX(FENTRYID), 0) AS MX FROM {table}");
            return (r != null && r.Count > 0) ? Convert.ToInt64(r[0]["MX"]) + 1 : 1;
        }

        private static string Sql(string s)
        {
            return string.IsNullOrEmpty(s) ? "" : s.Replace("'", "''");
        }

        /// <summary>费用类型(type) 数字枚举 → 中文</summary>
        private static string FeeTypeName(string code)
        {
            switch (code)
            {
                case "1": return "运费";
                case "2": return "其它费用";
                case "3": return "基础保";
                case "8": return "签单返还-纸质回单";
                case "91": return "拍照回传";
                default: return code;
            }
        }

        /// <summary>结算类型(settlementTypeCode) 数字枚举 → 中文</summary>
        private static string SettlementTypeName(string code)
        {
            switch (code)
            {
                case "1": return "现结";
                case "2": return "月结";
                default: return code;
            }
        }
    }
}

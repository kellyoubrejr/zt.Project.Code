using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.Zitn.Project.Code.conf;
using Kingdee.Zitn.Project.Code.plugin.SFBill;
using Kingdee.Zitn.Project.Code.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

namespace Kingdee.Zitn.Project.Code.Schedule
{
    [Description("【跑批】：批量查询顺丰路由轨迹")]
    [Kingdee.BOS.Util.HotUpdate]
    public class SFBillRouteQuerySchedule : IScheduleService
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("物流面单路由查询");

        private const string HEAD_TABLE = "ZMER_t_Cust100030";       // 单据头
        private const string ROUTE_TABLE = "ZMER_t_Cust_Entry100084"; // 路由轨迹单据体
        private const string BW_TABLE = "ZMER_t_Cust_Entry100085";    // 报文单据体

        public void Run(Context ctx, Kingdee.BOS.Core.Schedule schedule)
        {
            _log.Section("开始批量查询顺丰路由轨迹");

            try
            {
                // 1. 查询所有下单成功且有运单号的物流面单
                string querySql = $@"/*dialect*/SELECT FID, FBillNo, FSFYDH 
                                     FROM {HEAD_TABLE} 
                                     WHERE FORDERSTATUS = 'B' 
                                       AND FSFYDH IS NOT NULL 
                                       AND FSFYDH <> ''";
                
                _log.WriteLog($"查询待处理单据SQL: {querySql}");
                var result = DBUtils.ExecuteDataSet(ctx, querySql);
                
                if (result.Tables.Count == 0 || result.Tables[0].Rows.Count == 0)
                {
                    _log.WriteLog("没有需要查询路由的物流面单");
                    return;
                }

                DataTable table = result.Tables[0];
                int totalCount = table.Rows.Count;
                _log.WriteLog($"查询到 {totalCount} 条物流面单，开始批量查询路由");

                int successCount = 0;
                int failCount = 0;
                int skipCount = 0;

                foreach (DataRow row in table.Rows)
                {
                    long fid = Convert.ToInt64(row["FID"]);
                    string billNo = row["FBillNo"]?.ToString() ?? "";
                    string waybillNo = row["FSFYDH"]?.ToString() ?? "";

                    try
                    {
                        _log.WriteLog($"[{successCount + failCount + skipCount + 1}/{totalCount}] 处理单据: {billNo}, 运单号: {waybillNo}");

                        // 2. 调用顺丰路由查询接口
                        var msg = new JObject
                        {
                            ["language"] = "zh-CN",
                            ["trackingType"] = "1",
                            ["trackingNumber"] = new JArray(waybillNo),
                            ["methodType"] = "1"
                        };

                        var apiResult = SFExpressClient.Call(SFExpressClient.SERVICE_SEARCH_ROUTES, JsonConvert.SerializeObject(msg));
                        
                        // 3. 记录报文单据体
                        InsertMessage(ctx, fid, SFExpressClient.SERVICE_SEARCH_ROUTES, apiResult);

                        if (!apiResult.Success || apiResult.MsgData == null)
                        {
                            _log.WriteLog($"路由查询失败: {billNo}, 错误: {apiResult.ErrorCode} {apiResult.ErrorMsg}");
                            failCount++;
                            continue;
                        }

                        var routeResps = apiResult.MsgData["routeResps"] as JArray;
                        if (routeResps == null || routeResps.Count == 0)
                        {
                            _log.WriteLog($"路由查询无返回数据: {billNo}");
                            skipCount++;
                            continue;
                        }

                        var routes = routeResps[0]["routes"] as JArray;
                        if (routes == null || routes.Count == 0)
                        {
                            _log.WriteLog($"该运单暂无路由轨迹: {billNo}");
                            skipCount++;
                            continue;
                        }

                        // 4. 写入路由轨迹到数据库
                        WriteRoutesToSql(ctx, fid, routes);
                        
                        _log.WriteLog($"路由查询成功: {billNo}, 共 {routes.Count} 条轨迹");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"处理单据异常: {billNo}, FID={fid}");
                        _log.Error(ex);
                        failCount++;
                    }
                }

                // 5. 汇总结果
                string summary = $"路由查询完成: 共{totalCount}条, 成功{successCount}条, 失败{failCount}条, 跳过{skipCount}条";
                _log.Section(summary);
                
                if (failCount > 0)
                {
                    SendMsg.Send($"[物流面单路由查询] {summary}");
                }
            }
            catch (Exception ex)
            {
                _log.Error("批量查询顺丰路由轨迹异常");
                _log.Error(ex);
                SendMsg.Send("[物流面单路由查询] 批量查询异常", ex);
            }
        }

        /// <summary>SQL 兜底写入路由</summary>
        private void WriteRoutesToSql(Context ctx, long fid, JArray routes)
        {
            // 先删除旧路由数据
            DBUtils.Execute(ctx, $"/*dialect*/DELETE FROM {ROUTE_TABLE} WHERE FID = {fid}");

            foreach (var r in routes)
            {
                long entryId = NextEntryId(ctx, ROUTE_TABLE);
                string acceptTime = r["acceptTime"]?.ToString() ?? "";
                string acceptAddress = r["acceptAddress"]?.ToString() ?? "";
                string remark = r["remark"]?.ToString() ?? "";
                string opCode = r["opCode"]?.ToString() ?? "";
                string statusName = StatusName(r);

                var sql = $@"/*dialect*/INSERT INTO {ROUTE_TABLE}
                        (FENTRYID, FID, FAcceptTime1, FAcceptAddress, FRemark, FOpCode, FStatusName)
                    VALUES
                        ({entryId}, {fid}, '{Sql(acceptTime)}', '{Sql(acceptAddress)}', '{Sql(remark)}', '{Sql(opCode)}', '{Sql(statusName)}')";
                DBUtils.Execute(ctx, sql);
            }
        }

        /// <summary>写入报文单据体</summary>
        private void InsertMessage(Context ctx, long fid, string serviceCode, SFApiResult apiResult)
        {
            try
            {
                long entryId = NextEntryId(ctx, BW_TABLE);
                string success = apiResult.Success ? "1" : "0";

                var sql = $@"/*dialect*/INSERT INTO {BW_TABLE}
                        (FENTRYID, FID, FServiceCode, FRequestId, FCallTime, FRequestMsg, FResponseMsg, FSuccess, FErrorMsg)
                    VALUES
                        ({entryId}, {fid}, '{Sql(Truncate(serviceCode, 50))}', '{Sql(Truncate(apiResult.RequestId, 50))}', GETDATE(),
                         '{Sql(Truncate(apiResult.RequestMsg, 255))}', '{Sql(Truncate(apiResult.ResponseMsg, 255))}', '{success}', '{Sql(Truncate(apiResult.ErrorMsg, 50))}')";
                DBUtils.Execute(ctx, sql);
            }
            catch (Exception ex)
            {
                _log.Error($"写入报文单据体失败，FID={fid}");
                _log.Error(ex);
            }
        }

        private long NextEntryId(Context ctx, string table)
        {
            var r = DBUtils.ExecuteDynamicObject(ctx,
                $"/*dialect*/SELECT ISNULL(MAX(FENTRYID), 0) AS MX FROM {table}");
            return (r != null && r.Count > 0) ? Convert.ToInt64(r[0]["MX"]) + 1 : 1;
        }

        private static string StatusName(JToken r)
        {
            string secondary = r["secondaryStatusName"]?.ToString();
            if (!string.IsNullOrEmpty(secondary))
                return secondary;
            return r["firstStatusName"]?.ToString() ?? "";
        }

        private static string Sql(string s)
        {
            return string.IsNullOrEmpty(s) ? "" : s.Replace("'", "''");
        }

        private static string Truncate(string s, int max = 2000)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}
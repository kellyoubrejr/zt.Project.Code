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
using System.Globalization;

namespace Kingdee.Zitn.Project.Code.Schedule
{
    [Description("【跑批】：批量查询顺丰费用")]
    [Kingdee.BOS.Util.HotUpdate]
    public class SFBillFeeQuerySchedule : IScheduleService
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("物流面单费用查询");

        private const string HEAD_TABLE = "ZMER_t_Cust100030";       // 单据头
        private const string FEE_TABLE = "ZMER_t_Cust_Entry100087";   // 费用单据体
        private const string BW_TABLE = "ZMER_t_Cust_Entry100085";    // 报文单据体

        public void Run(Context ctx, Kingdee.BOS.Core.Schedule schedule)
        {
            _log.Section("开始批量查询顺丰费用");

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
                    _log.WriteLog("没有需要查询费用的物流面单");
                    return;
                }

                DataTable table = result.Tables[0];
                int totalCount = table.Rows.Count;
                _log.WriteLog($"查询到 {totalCount} 条物流面单，开始批量查询费用");

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

                        // 2. 调用顺丰费用查询接口
                        var msg = new JObject
                        {
                            ["trackingType"] = "2",
                            ["trackingNum"] = waybillNo
                        };

                        var apiResult = SFExpressClient.Call(SFExpressClient.SERVICE_QUERY_SFWAYBILL, JsonConvert.SerializeObject(msg));
                        
                        // 3. 记录报文单据体
                        InsertMessage(ctx, fid, SFExpressClient.SERVICE_QUERY_SFWAYBILL, apiResult);

                        if (!apiResult.Success || apiResult.MsgData == null)
                        {
                            _log.WriteLog($"费用查询失败: {billNo}, 错误: {apiResult.ErrorCode} {apiResult.ErrorMsg}");
                            failCount++;
                            continue;
                        }

                        var feeList = apiResult.MsgData["waybillFeeList"] as JArray;
                        if (feeList == null || feeList.Count == 0)
                        {
                            _log.WriteLog($"该运单暂无费用信息: {billNo}");
                            skipCount++;
                            continue;
                        }

                        // 4. 写入费用数据到数据库
                        WriteFreightToSql(ctx, fid, feeList);
                        
                        // 5. 回写单据头运费总额
                        UpdateFreightTotal(ctx, fid, feeList);
                        
                        _log.WriteLog($"费用查询成功: {billNo}, 共 {feeList.Count} 条费用");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"处理单据异常: {billNo}, FID={fid}");
                        _log.Error(ex);
                        failCount++;
                    }
                }

                // 6. 汇总结果
                string summary = $"费用查询完成: 共{totalCount}条, 成功{successCount}条, 失败{failCount}条, 跳过{skipCount}条";
                _log.Section(summary);
                
                if (failCount > 0)
                {
                    SendMsg.Send($"[物流面单费用查询] {summary}");
                }
            }
            catch (Exception ex)
            {
                _log.Error("批量查询顺丰费用异常");
                _log.Error(ex);
                SendMsg.Send("[物流面单费用查询] 批量查询异常", ex);
            }
        }

        /// <summary>SQL 兜底写入费用</summary>
        private void WriteFreightToSql(Context ctx, long fid, JArray feeList)
        {
            // 先删除旧费用数据
            DBUtils.Execute(ctx, $"/*dialect*/DELETE FROM {FEE_TABLE} WHERE FID = {fid}");

            foreach (var fee in feeList)
            {
                decimal feeValue = 0;
                decimal.TryParse(fee["value"]?.ToString(), out feeValue);

                long entryId = NextEntryId(ctx, FEE_TABLE);
                string feeType = FeeTypeName(fee["type"]?.ToString() ?? "");
                string feeName = "自动" + (fee["name"]?.ToString() ?? "");
                string settlementType = SettlementTypeName(fee["settlementTypeCode"]?.ToString() ?? "");

                var sql = $@"/*dialect*/INSERT INTO {FEE_TABLE}
                        (FENTRYID, FID, FFeeType, FFeeName, FFeeValue, FSettlementType)
                    VALUES
                        ({entryId}, {fid}, '{Sql(feeType)}', '{Sql(feeName)}', {feeValue.ToString(CultureInfo.InvariantCulture)}, '{Sql(settlementType)}')";
                DBUtils.Execute(ctx, sql);
            }
        }

        /// <summary>回写单据头运费总额（FFreightTotal）</summary>
        private void UpdateFreightTotal(Context ctx, long fid, JArray feeList)
        {
            decimal total = 0;
            foreach (var fee in feeList)
            {
                decimal v = 0;
                decimal.TryParse(fee["value"]?.ToString(), out v);
                total += v;
            }

            try
            {
                DBUtils.Execute(ctx,
                    $@"/*dialect*/UPDATE {HEAD_TABLE} SET FFreightTotal = {total.ToString(CultureInfo.InvariantCulture)} WHERE FID = {fid}");
            }
            catch (Exception ex)
            {
                _log.Error($"回写运费总额失败，FID={fid}");
                _log.Error(ex);
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
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Kingdee.Zitn.Project.Code.plugin.MSPO
{
    [HotUpdate, Description("【免审采购订单审核服务】遍历单据体根据PO、GYS、WL更新颜色标志")]
    public class MSPOAuditUpsertColorFlag : AbstractOperationServicePlugIn
    {

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FPURNO");
            e.FieldKeys.Add("FSUPPLIER");
            e.FieldKeys.Add("FMATERIALID");
            e.FieldKeys.Add("FMATERIALID_Id");
            e.FieldKeys.Add("F_ZMER_TEXT_83G");
            e.FieldKeys.Add("F_ZMER_TEXT_QTR");
            e.FieldKeys.Add("FTAXPRICE");
        }

        // 日志目录：D盘下的“采购面审日志”文件夹，按天拆分日志文件
        private const string LogDirectory = @"D:\采购面审日志";
        // 写日志锁，避免并发审核/多线程同时写同一个日志文件
        private static readonly object LogLock = new object();

        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            WriteLog($"========== 开始执行免审采购订单颜色标志更新，单据数: {(e.DataEntitys == null ? 0 : e.DataEntitys.Length)} ==========");
            try
            {
                base.AfterExecuteOperationTransaction(e);
                int billIndex = 0;
                foreach (var billObj in e.DataEntitys)
                {
                    billIndex++;

                    var entrys = billObj["ZMER_Cust_Entry100101"] as DynamicObjectCollection;

                    string tag = $"[单据{billIndex}]";
                    //string billNo = Convert.ToString(billObj["FPURNO"]);
                    //string GYS = Convert.ToString(billObj["FSUPPLIER"]);
                    //string FMATERIAL = Convert.ToString(billObj["FMATERIAL"]);
                    //string XIAXIAN = Convert.ToString(billObj["F_ZMER_TEXT_83G"]);
                    //string SHANGXIAN = Convert.ToString(billObj["F_ZMER_TEXT_QTR"]);
                    //string HANSHUIDANJIA = Convert.ToString(billObj["FTAXPRICE"]);
                    //WriteLog($"{tag} 免审配置 - PO号: '{billNo}', 供应商: '{GYS}', 物料: '{FMATERIAL}', 下限: '{XIAXIAN}', 上限: '{SHANGXIAN}', 免审含税单价: '{HANSHUIDANJIA}'");

                    string billNo = string.Empty;
                    string GYS = string.Empty;
                    string FMATERIAL = string.Empty;
                    string XIAXIAN = string.Empty;
                    string SHANGXIAN = string.Empty;
                    string HANSHUIDANJIA = string.Empty;

                    foreach (var entry in entrys)
                    {
                         billNo = Convert.ToString(entry["FPURNO"]);
                         GYS = Convert.ToString(entry["FSUPPLIER"]);
                         FMATERIAL = Convert.ToString(entry["FMATERIALID_Id"]);
                         XIAXIAN = Convert.ToString(entry["F_ZMER_TEXT_83G"]);
                         SHANGXIAN = Convert.ToString(entry["F_ZMER_TEXT_QTR"]);
                         HANSHUIDANJIA = Convert.ToString(entry["FTAXPRICE"]);

                        WriteLog($"分录行 - PO号: '{billNo}', 供应商: '{GYS}', 物料: '{FMATERIAL}', 下限: '{XIAXIAN}', 上限: '{SHANGXIAN}', 免审含税单价: '{HANSHUIDANJIA}'");
                    }


                    // 单据编号有效条件：CGDD开头 + 后面全是数字；为空或纯文字均视为无有效编号
                    bool hasValidBillNo = !string.IsNullOrWhiteSpace(billNo) && Regex.IsMatch(billNo, @"^CGDD\d+$");
                    WriteLog($"{tag} 单据编号 '{billNo}' 是否有效: {hasValidBillNo}，{(hasValidBillNo ? "按编号+供应商+物料查询" : "编号无效，仅按供应商+物料查询")}");

                    string entitySql;
                    if (hasValidBillNo)
                    {
                        // 有有效单据编号：按编号+供应商+物料查询
                        entitySql = $@"
                                        /*dialect*/
                                        SELECT  C.FNAME AS FSUPPLIERNAME,FMATERIALID,FQTY,FTAXPRICE,B.FENTRYID
                                        FROM T_PUR_POORDER A
                                        JOIN T_PUR_POORDERENTRY B ON A.FID = B.FID
                                        JOIN T_PUR_POORDERENTRY_F B1 ON B.FENTRYID = B1.FENTRYID
                                        JOIN T_BD_SUPPLIER_L C ON A.FSUPPLIERID = C.FSUPPLIERID
                                        WHERE FBILLNO = '{billNo}' AND C.FNAME = '{GYS}' AND FMATERIALID = '{FMATERIAL}' AND A.FDOCUMENTSTATUS <> 'C'";
                    }
                    else
                    {
                        // 编号为空或纯文字：不加FBILLNO条件，直接按供应商+物料查询
                        entitySql = $@"
                                        /*dialect*/
                                        SELECT  C.FNAME AS FSUPPLIERNAME,FMATERIALID,FQTY,FTAXPRICE,B.FENTRYID
                                        FROM T_PUR_POORDER A
                                        JOIN T_PUR_POORDERENTRY B ON A.FID = B.FID
                                        JOIN T_PUR_POORDERENTRY_F B1 ON B.FENTRYID = B1.FENTRYID
                                        JOIN T_BD_SUPPLIER_L C ON A.FSUPPLIERID = C.FSUPPLIERID
                                        WHERE C.FNAME = '{GYS}' AND FMATERIALID = '{FMATERIAL}' AND A.FDOCUMENTSTATUS <> 'C'";
                    }
                    WriteLog($"{tag} 查询SQL: {Regex.Replace(entitySql, @"\s+", " ").Trim()}");
                    var result = DBUtils.ExecuteDynamicObject(this.Context, entitySql);
                    if (result == null || result.Count == 0)
                    {
                        WriteLog($"{tag} 查询结果: 0 条，未匹配到采购订单分录，跳过更新");
                        continue;
                    }
                    WriteLog($"{tag} 查询结果: {result.Count} 条采购订单分录");
                    foreach (var entity in result)
                    {
                        decimal qty = Convert.ToDecimal(entity["FQTY"]);
                        decimal taxPrice = Convert.ToDecimal(entity["FTAXPRICE"]);
                        long entryId = Convert.ToInt64(entity["FENTRYID"]);
                        WriteLog($"{tag}[分录FENTRYID={entryId}] 分录数据 - 供应商名称: '{Convert.ToString(entity["FSUPPLIERNAME"])}', 数量: {qty}, 采购订单含税单价: {taxPrice}");
                        //判断免审的上下限和含税单价与采购订单的数量和含税单价是否符合条件
                        //数量大于下限，小于等于上限，含税单价小于等于采购订单含税单价
                        string updateSql;
                        if (qty > Convert.ToDecimal(XIAXIAN) && qty <= Convert.ToDecimal(SHANGXIAN) && taxPrice <= Convert.ToDecimal(HANSHUIDANJIA))
                        {
                            // 🟢 满足免审
                            WriteLog($"{tag}[分录FENTRYID={entryId}] 条件判定: 满足免审 - {qty} > 下限{XIAXIAN} 且 {qty} <= 上限{SHANGXIAN} 且 {taxPrice} <= 免审含税单价{HANSHUIDANJIA}");
                            updateSql = $"/*dialect*/UPDATE T_PUR_POORDERENTRY SET FCOLORFLAG = N'存在免审记录' WHERE FENTRYID = {entryId}";
                        }
                        else
                        {
                            // 🔵 有配置但不满足
                            WriteLog($"{tag}[分录FENTRYID={entryId}] 条件判定: 存在免审配置但不满足条件 - 数量: {qty}, 下限: {XIAXIAN}, 上限: {SHANGXIAN}, 分录含税单价: {taxPrice}, 免审含税单价: {HANSHUIDANJIA}");
                            updateSql = $"/*dialect*/UPDATE T_PUR_POORDERENTRY SET FCOLORFLAG = N'存在免审但不满足条件' WHERE FENTRYID = {entryId}";
                        }
                        WriteLog($"{tag}[分录FENTRYID={entryId}] 执行更新SQL: {updateSql}");
                        int affectedRows = DBUtils.Execute(this.Context, updateSql);
                        WriteLog($"{tag}[分录FENTRYID={entryId}] 更新完成，影响行数: {affectedRows}");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"发生异常: {ex}");
                throw;
            }
            finally
            {
                WriteLog("========== 本次执行结束 ==========");
            }
        }

        /// <summary>
        /// 追加写日志到 D:\采购面审日志\采购面审日志_yyyyMMdd.log，日志失败不影响业务执行
        /// </summary>
        private static void WriteLog(string message)
        {
            try
            {
                lock (LogLock)
                {
                    if (!Directory.Exists(LogDirectory))
                    {
                        Directory.CreateDirectory(LogDirectory);
                    }
                    string logFile = Path.Combine(LogDirectory, $"采购面审日志_{DateTime.Now:yyyyMMdd}.log");
                    string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
                    File.AppendAllText(logFile, line, Encoding.UTF8);
                }
            }
            catch
            {
                // 日志写入失败（如D盘不存在、无权限）时静默吞掉，不影响业务逻辑
            }
        }
    }
}

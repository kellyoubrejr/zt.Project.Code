using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using Kingdee.BOS.WebApi.Client;
using Newtonsoft.Json.Linq;
using Kingdee.Zitn.Project.Code.conf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Kingdee.BOS.Orm.DataEntity;

namespace Kingdee.Zitn.Project.Code.plugin.MRP
{
    [Description("销售订单保存根据多投数量生成计划订单-表单"), HotUpdate]
    public class MRP_SalOrderSaveToPlanOrderForm : AbstractBillPlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("销售订单生成计划订单");


        private const string BILLTYPE_SAMPLE = "685101150db14c";
        private const string BILLTYPE_STANDARD = "eacb50844fc84a10b03d7b841f3a6278";
        private const string BILLTYPE_INTERNAL_TRIAL = "6902b7bb51f609";
        private const string BILLTYPE_PRE_CAST = "68d0fdc2ec8d84";
        private const string BILLTYPE_DISPATCH_PRE_CAST = "6a69467829511a";

        private DateTime GetWorkDay(DateTime date)
        {
            var result = date;
            for (int i = 0; i < 30; i++)
            {
                var dateStr = result.Date.ToString("yyyy-MM-dd");
                var sql = $@"SELECT B.FDATESTYLE FROM T_ENG_WORKCAL A
                             JOIN T_ENG_WORKCALDATA B ON A.FID = B.FID
                             WHERE B.FDAY = '{dateStr}'";
                var dt = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (dt == null || dt.Count == 0)
                    break;
                if (dt[0]["FDATESTYLE"].ToString() != "2")
                    break;
                result = result.AddDays(-1);
            }
            return result;
        }

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            if (e.BarItemKey.Equals("tbSplitSave", StringComparison.OrdinalIgnoreCase))
            {
                var client = new K3CloudApiClient(ErpLogin.K3CloudUrl);
                _log.WriteLog("开始登录K3Cloud...");
                var loginResult = client.ValidateLogin(
                    ErpLogin.AppId,
                    ErpLogin.UserName,
                    ErpLogin.Password,
                    ErpLogin.Lcid);
                _log.WriteLog($"登录返回: {loginResult}");

                if (JObject.Parse(loginResult)["LoginResultType"].Value<int>() != 1)
                {
                    _log.WriteLog("登录失败，退出");
                    return;
                }
                _log.WriteLog("登录成功");

                _log.WriteLog("========== 销售保存-生成计划订单按类型 开始 ==========");

                var fbillno = this.View.Model.GetValue("FBillNo")?.ToString();
                if (string.IsNullOrEmpty(fbillno))
                {
                    _log.WriteLog("获取单据号为空，退出");
                    return;
                }
                _log.WriteLog($"当前单据号: {fbillno}");


                var fidSql = $@"SELECT A.FID, A.FBILLTYPEID FROM T_SAL_ORDER A WHERE A.FBILLNO = '{fbillno}'";
                var fidDt = DBUtils.ExecuteDynamicObject(this.Context, fidSql);
                if (fidDt == null || fidDt.Count == 0)
                {
                    _log.WriteLog("未查到销售订单FID，退出");
                    return;
                }
                var fid = fidDt[0]["FID"]?.ToString();
                var billTypeId = fidDt[0]["FBILLTYPEID"]?.ToString();
                _log.WriteLog($"销售订单FID={fid}, 单据类型={billTypeId}");


                var validTypes = new[] { BILLTYPE_SAMPLE, BILLTYPE_STANDARD, BILLTYPE_INTERNAL_TRIAL, BILLTYPE_PRE_CAST, BILLTYPE_DISPATCH_PRE_CAST };
                if (Array.IndexOf(validTypes, billTypeId) < 0)
                {
                    _log.WriteLog($"单据类型不在处理范围内: {billTypeId}，退出");
                    return;
                }


                var allIds = new List<string> { fid };
                var idsByBillType = GetBillTypeGroups(allIds);
                foreach (var kv in idsByBillType)
                    _log.WriteLog($"单据类型分组: {kv.Key} = {kv.Value.Count}条");



                var groupAIds = new List<string>();
                AddIfExists(idsByBillType, BILLTYPE_PRE_CAST, groupAIds);
                AddIfExists(idsByBillType, BILLTYPE_SAMPLE, groupAIds);
                AddIfExists(idsByBillType, BILLTYPE_STANDARD, groupAIds);


                var groupBIds = new List<string>();
                AddIfExists(idsByBillType, BILLTYPE_DISPATCH_PRE_CAST, groupBIds);
                AddIfExists(idsByBillType, BILLTYPE_INTERNAL_TRIAL, groupBIds);

                _log.WriteLog($"A组(预投+样品+标准)共{groupAIds.Count}条, B组(调度预投+内部试制)共{groupBIds.Count}条");

                var allValidIds = new List<string>();
                allValidIds.AddRange(groupAIds);
                allValidIds.AddRange(groupBIds);

                if (allValidIds.Count == 0)
                {
                    _log.WriteLog("没有需要处理的销售订单类型，退出");
                    return;
                }


                var ids = string.Join(",", allValidIds);
                var sql = $@"SELECT B.FENTRYID,
                M.FNUMBER AS WLNUM,
                BOM.FNUMBER AS BOM,
                UN.FNUMBER AS DANWEI,
                M2.FNUMBER AS WLFL,
                TY.FNUMBER AS TFDJLX,
                M.F_PAEZ_SYB AS SYB,
                D.FNUMBER AS SCCJ,
                B.F_UNW_QTY_QTR AS DTSL,
                B.F_PAEZ_SJTCQTY AS SJTCSL,
                A.F_PAEZ_TEXT AS YSDJH,
                A.FBILLNO AS XSDDH,
                A.FBILLTYPEID AS BILLTYPEID,
                CASE
                    WHEN A.FBILLTYPEID = '685101150db14c' THEN '样品销售订单'
                    WHEN A.FBILLTYPEID = 'eacb50844fc84a10b03d7b841f3a6278' THEN '标准销售订单'
                    WHEN A.FBILLTYPEID = '6902b7bb51f609' THEN '内部试制销售订单'
                    WHEN A.FBILLTYPEID = '68d0fdc2ec8d84' THEN '预投销售订单'
                    WHEN A.FBILLTYPEID = '6a69467829511a' THEN '调度预投销售订单'
                END AS XSDDLX,
                B.FMTONO AS JHGZH,
                B.F_PAEZ_STATUS AS PSZT,
                ML.FNAME AS WLNAME,A.F_BJ,B.FSEQ,B1.FDELIVERYDATE,B.F_PAEZ_STATUS
                FROM T_SAL_ORDER A
                JOIN T_SAL_ORDERENTRY B ON A.FID = B.FID
                JOIN T_SAL_ORDERENTRY_D B1 ON B.FENTRYID = B1.FENTRYID
                JOIN T_BD_MATERIAL M ON B.FMATERIALID = M.FMATERIALID
                JOIN T_BD_MATERIALBASE M1 ON M.FMATERIALID = M1.FMATERIALID
                JOIN T_BD_MATERIALCATEGORY M2 ON M1.FCATEGORYID = M2.FCATEGORYID
                JOIN t_BD_MaterialProduce M3 ON M.FMATERIALID = M3.FMATERIALID
                JOIN T_BAS_BILLTYPE TY ON TY.FBILLTYPEID = M3.FPRODUCEBILLTYPE
                JOIN T_BD_UNIT UN ON B.FUNITID = UN.FUNITID
                JOIN T_BD_MATERIAL_L ML ON M.FMATERIALID = ML.FMATERIALID
                LEFT JOIN T_ENG_BOM BOM ON B.FBOMID = BOM.FID
                LEFT JOIN T_BD_DEPARTMENT D ON M3.FWORKSHOPID = D.FDEPTID
                WHERE A.FID IN ({ids})
                AND B.F_UNW_Integer_qtr = 0 AND (A.FDOCUMENTSTATUS = 'C' OR A.FDOCUMENTSTATUS = 'B')  AND A.F_BJ = 1";

                _log.WriteLog($"执行SQL: {sql}");

                var dt = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (dt == null || dt.Count == 0)
                {
                    _log.WriteLog("SQL查询结果为空，退出");
                    return;
                }
                _log.WriteLog($"SQL查询到 {dt.Count} 行数据");



                int idx = 0;
                bool bj = false;
                foreach (DynamicObject row in dt)
                {
                    long entryId = row["FENTRYID"]?.ToString() != null ? Convert.ToInt64(row["FENTRYID"]) : 0;
                    billTypeId = row["BILLTYPEID"]?.ToString() ?? "";
                    var wlNum = row["WLNUM"]?.ToString() ?? "";
                    var bom = row["BOM"]?.ToString() ?? "";
                    var danWei = row["DANWEI"]?.ToString() ?? "";
                    var wlfl = row["WLFL"]?.ToString() ?? "";
                    var syb = row["SYB"]?.ToString() ?? "";
                    var sccj = row["SCCJ"]?.ToString() ?? "";
                    var ysdjh = row["YSDJH"]?.ToString() ?? "";
                    var tfdjlx = row["TFDJLX"]?.ToString() ?? "";
                    var wlName = row["WLNAME"]?.ToString() ?? "";
                    var xsdDh = row["XSDDH"]?.ToString() ?? "";
                    var xsddLx = row["XSDDLX"]?.ToString() ?? "";
                    var jhgzh = row["JHGZH"]?.ToString() ?? "";
                    var fbj = row["F_BJ"]?.ToString() ?? "";
                    if (fbj == "0") continue;
                    if (fbj == "1")
                    {
                        bj = true;
                    }
                    var rawDate = row["FDELIVERYDATE"] != null ? Convert.ToDateTime(row["FDELIVERYDATE"]) : DateTime.Now;
                    var today = GetWorkDay(rawDate).Date;


                    decimal qty;
                    string jhddType = "";



                    var seq = Convert.ToInt32(row["FSEQ"]);
                    if (this.View.Model.GetValue("F_PAEZ_STATUS", (int)(seq - 1)).ToString() == "2") continue;
                    switch (billTypeId)
                    {
                        case BILLTYPE_SAMPLE:
                        case BILLTYPE_STANDARD:
                        case BILLTYPE_PRE_CAST:


                            qty = Convert.ToInt64(this.View.Model.GetValue("F_UNW_QTY_QTR", seq - 1));
                            break;
                        case BILLTYPE_INTERNAL_TRIAL:
                        case BILLTYPE_DISPATCH_PRE_CAST:


                            qty = Convert.ToInt64(this.View.Model.GetValue("F_PAEZ_SJTCQTY", seq - 1));
                            jhddType = "2";
                            break;
                        default:
                            _log.WriteLog($"未知单据类型: {billTypeId}, 跳过 物料={wlNum}");
                            continue;
                    }
                    idx++;
                    if (qty <= 0)
                    {
                        _log.WriteLog($"数量<=0, 跳过 物料={wlNum}, 单据类型={xsddLx}");
                        continue;
                    }

                    var releaseType = string.IsNullOrEmpty(bom) ? "2" : "1";

                    _log.WriteLog($"创建计划订单: 类型={xsddLx}, 物料={wlNum}, BOM={bom}, 数量={qty}, 单位={danWei}");

                    JObject modelObject = new JObject();
                    modelObject.Add("FID", 0);

                    JObject billTypeObj = new JObject();
                    billTypeObj.Add("FNUMBER", "JHDD02_SYS");
                    modelObject.Add("FBillTypeID", billTypeObj);

                    JObject materialObj = new JObject();
                    materialObj.Add("FNumber", wlNum);
                    modelObject.Add("FMaterialId", materialObj);

                    JObject bomObj = new JObject();
                    bomObj.Add("FNumber", bom);
                    modelObject.Add("FBomId", bomObj);

                    JObject unitObj = new JObject();
                    unitObj.Add("FNumber", danWei);
                    modelObject.Add("FUnitId", unitObj);

                    JObject baseUnitObj = new JObject();
                    baseUnitObj.Add("FNumber", danWei);
                    modelObject.Add("FBaseUnitId", baseUnitObj);

                    modelObject.Add("FSugQty", qty);
                    modelObject.Add("FFirmQty", qty);



                    modelObject.Add("FPlanStartDate", today);
                    modelObject.Add("FPlanFinishDate", today);
                    modelObject.Add("FFirmStartDate", today);
                    modelObject.Add("FFirmFinishDate", today);
                    modelObject.Add("F_BJ", bj);

                    if (!string.IsNullOrEmpty(sccj))
                    {
                        JObject prdDeptObj = new JObject();
                        prdDeptObj.Add("FNumber", sccj);
                        modelObject.Add("FPrdDeptId", prdDeptObj);
                    }

                    JObject demandOrgObj = new JObject();
                    demandOrgObj.Add("FNumber", "101");
                    modelObject.Add("FDemandOrgId", demandOrgObj);

                    JObject supplyOrgObj = new JObject();
                    supplyOrgObj.Add("FNumber", "101");
                    modelObject.Add("FSupplyOrgId", supplyOrgObj);

                    JObject inStockOrgObj = new JObject();
                    inStockOrgObj.Add("FNumber", "101");
                    modelObject.Add("FInStockOrgId", inStockOrgObj);

                    modelObject.Add("FOwnerTypeId", "BD_OwnerOrg");

                    JObject ownerObj = new JObject();
                    ownerObj.Add("FNumber", "101");
                    modelObject.Add("FOwnerId", ownerObj);

                    modelObject.Add("FReleaseType", releaseType);
                    modelObject.Add("FReleaseStatus", "0");

                    JObject faMaObj = new JObject();
                    faMaObj.Add("FNumber", wlNum);
                    modelObject.Add("F_FaMaNum", faMaObj);

                    JObject chTypeObj = new JObject();
                    chTypeObj.Add("FNUMBER", wlfl);
                    modelObject.Add("F_PAEZ_CHTYPE", chTypeObj);

                    JObject cpbmObj = new JObject();
                    cpbmObj.Add("FNUMBER", wlNum);
                    modelObject.Add("F_PAEZ_CPBM", cpbmObj);

                    JObject scTypeObj = new JObject();
                    scTypeObj.Add("FNUMBER", tfdjlx);
                    modelObject.Add("F_PAEZ_SCTYPE", scTypeObj);

                    modelObject.Add("F_PAEZ_YSDJH", ysdjh);

                    modelObject.Add("F_SYB", syb);

                    modelObject.Add("FMtoNo", jhgzh);


                    modelObject.Add("F_PAEZ_XXDDH", xsdDh);


                    modelObject.Add("F_PAEZ_XSDDTYPE", xsddLx);


                    if (!string.IsNullOrEmpty(jhddType))
                        modelObject.Add("F_PAEZ_JHDDTYPE", jhddType);

                    modelObject.Add("FEntity", new JArray());
                    modelObject.Add("FEntityCoby", new JArray());

                    JObject subEntity = new JObject();
                    subEntity.Add("FEntryId", 0);
                    JObject releaseBillTypeObj = new JObject();
                    releaseBillTypeObj.Add("FNUMBER", tfdjlx);
                    subEntity.Add("FReleaseBillType", releaseBillTypeObj);
                    modelObject.Add("FSubEntity", subEntity);

                    JObject saveObj = new JObject
                    {
                        ["Model"] = modelObject,
                        ["IsDeleteEntry"] = true
                    };

                    var jsonData = saveObj.ToString();
                    _log.WriteLog($"调用Save接口, JSON数据: {jsonData}");

                    try
                    {
                        var result = client.Save("PLN_PLANORDER", jsonData);
                        _log.WriteLog($"Save返回结果: {result}");

                        var saveResult = JObject.Parse(result);
                        var responseStatus = saveResult["Result"]?["ResponseStatus"];
                        if (responseStatus != null)
                        {
                            var isSuccess = responseStatus["IsSuccess"]?.Value<bool>() ?? false;
                            if (isSuccess)
                            {
                                var planId = saveResult["Result"]?["Id"]?.ToString() ?? "";
                                var planNumber = saveResult["Result"]?["Number"]?.ToString() ?? "";
                                _log.WriteLog($"计划订单创建成功: Id={planId}, Number={planNumber}");


                                this.View.Model.SetValue("F_UNW_Integer_qtr", planId, (int)(seq - 1));
                                this.View.UpdateView("F_UNW_Integer_qtr");
                                this.View.Model.Save();
                            }
                            else
                            {
                                var errors = responseStatus["Errors"] as JArray;
                                var errorMsg = errors != null && errors.Count > 0
                                    ? errors[0]["Message"]?.ToString()
                                    : "未知错误";
                                _log.WriteLog($"计划订单创建失败: {errorMsg}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.WriteLog($"调用Save接口异常: {ex.Message}");
                    }
                }

                _log.WriteLog("========== 销售保存-生成计划订单按类型 结束 ==========");
            }
        }
        private Dictionary<string, List<string>> GetBillTypeGroups(List<string> allIds)
        {
            var result = new Dictionary<string, List<string>>();
            if (allIds == null || allIds.Count == 0) return result;

            var ids = string.Join(",", allIds);
            var sql = $@"/*dialect*/SELECT A.FID, A.FBILLTYPEID FROM T_SAL_ORDER A WHERE A.FID IN ({ids})";
            var dt = DBUtils.ExecuteDynamicObject(this.Context, sql);
            foreach (DynamicObject row in dt)
            {
                var fid = row["FID"]?.ToString();
                var billType = row["FBILLTYPEID"]?.ToString();
                if (string.IsNullOrEmpty(fid) || string.IsNullOrEmpty(billType)) continue;
                if (!result.ContainsKey(billType))
                    result[billType] = new List<string>();
                result[billType].Add(fid);
            }
            return result;
        }

        private static void AddIfExists(Dictionary<string, List<string>> dict, string key, List<string> target)
        {
            if (dict.TryGetValue(key, out var list))
                target.AddRange(list);
        }
    }
}

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
    [Description("意向预测销售订单保存根据多投数量生成计划订单-表单"), HotUpdate]
    public class MRP_PlnForceCastSaveToPlanOrderForm : AbstractBillPlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("意向预测生成计划订单");

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

                _log.WriteLog("========== 预测单保存-生成计划订单 开始 ==========");

                var fbillno = this.View.Model.GetValue("FBillNo").ToString();


                var sql = $@"SELECT
                M.FNUMBER AS WLNUM,
                BOM.FNUMBER AS BOM,
                UN.FNUMBER AS DANWEI,
                M2.FNUMBER AS WLFL,
                M.F_PAEZ_SYB AS SYB,
                D.FNUMBER AS SCCJ,
                B.F_XQ_DTSL AS DTSL,
                ML.FNAME AS WLNAME,
                A.F_UNW_TEXT_QTR AS YSDJH , TY.FNUMBER AS TFDJLX,B.FENTRYID,B.FSEQ,A.F_BJ,B.FENDDATE,A.FBILLNO
                FROM T_PLN_FORECAST A
                JOIN T_PLN_FORECASTENTRY B ON A.FID = B.FID
                JOIN T_BD_MATERIAL M ON B.FMATERIALID = M.FMATERIALID
                JOIN T_BD_MATERIALBASE M1 ON M.FMATERIALID = M1.FMATERIALID
                JOIN T_BD_MATERIALCATEGORY M2 ON M1.FCATEGORYID = M2.FCATEGORYID
                JOIN t_BD_MaterialProduce M3 ON M.FMATERIALID = M3.FMATERIALID
				JOIN T_BAS_BILLTYPE TY ON TY.FBILLTYPEID = M3.FPRODUCEBILLTYPE
                JOIN T_BD_MATERIAL_L ML ON M.FMATERIALID = ML.FMATERIALID
                JOIN T_BD_UNIT UN ON B.FUNITID = UN.FUNITID
                LEFT JOIN T_ENG_BOM BOM ON B.FBOMID = BOM.FID
                LEFT JOIN T_BD_DEPARTMENT D ON M3.FWORKSHOPID = D.FDEPTID
                WHERE A.FBILLNO = '{fbillno}' AND A.FBILLTYPEID = '6a0d041e5879e6'
                AND B.F_XQ_DTSL > 0 AND B.F_XQ_DDNM = 0  AND A.F_BJ = 1";

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
                    var salNo = row["FBILLNO"]?.ToString() ?? "";
                    var fbj = row["F_BJ"]?.ToString() ?? "";
                    if (fbj == "0") return;
                    if (fbj == "1")
                    {
                        bj = true;
                    }
                    var wlNum = row["WLNUM"]?.ToString() ?? "";
                    var bom = row["BOM"]?.ToString() ?? "";
                    var danWei = row["DANWEI"]?.ToString() ?? "";
                    var wlfl = row["WLFL"]?.ToString() ?? "";
                    var syb = row["SYB"]?.ToString() ?? "";
                    var sccj = row["SCCJ"]?.ToString() ?? "";
                    long seq = Convert.ToInt64(row["FSEQ"]);

                    if (this.View.Model.GetValue("F_XQ_XLLB", (int)(seq - 1)).ToString() == "2") continue;

                    var dtsl = Convert.ToInt64(this.View.Model.GetValue("F_XQ_DTSL", (int)(seq - 1)));
                    if (dtsl <= 0) continue;


                    _log.WriteLog($"【获取表单多投数量】{dtsl}{seq}{seq - 1}");
                    idx++;
                    var wlName = row["WLNAME"]?.ToString() ?? "";
                    var ysdjh = row["YSDJH"]?.ToString() ?? "";
                    var tfdjlx = row["TFDJLX"]?.ToString() ?? "";
                    long entryId = row["FENTRYID"] != null ? Convert.ToInt64(row["FENTRYID"]) : 0;
                    var rawDate = row["FENDDATE"] != null ? Convert.ToDateTime(row["FENDDATE"]) : DateTime.Now;
                    var today = GetWorkDay(rawDate).Date;

                    var releaseType = string.IsNullOrEmpty(bom) ? "2" : "1";

                    _log.WriteLog($"创建计划订单: 物料={wlNum}, BOM={bom}, 数量={dtsl}, 单位={danWei}, 投放类型={releaseType}");

                    JObject modelObject = new JObject();
                    modelObject.Add("FID", 0);

                    JObject billTypeObj = new JObject();
                    billTypeObj.Add("FNUMBER", "JHDD03_SYS");
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

                    modelObject.Add("FSugQty", dtsl);
                    modelObject.Add("FFirmQty", dtsl);
                    modelObject.Add("F_PAEZ_XXDDH", salNo);


                    modelObject.Add("F_BJ", bj);


                    modelObject.Add("FPlanStartDate", today);
                    modelObject.Add("FPlanFinishDate", today);
                    modelObject.Add("FFirmStartDate", today);
                    modelObject.Add("FFirmFinishDate", today);

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


                                this.View.Model.SetValue("F_XQ_DDNM", planId, (int)(seq - 1));
                                this.View.UpdateView("F_XQ_DDNM");
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

                _log.WriteLog("========== 预测单审核-生成计划订单 结束 ==========");
            }

        }
    }
}

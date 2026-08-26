using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.WebApi.Client;
using Newtonsoft.Json.Linq;
using Kingdee.Zitn.Project.Code.conf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Kingdee.Zitn.Project.Code.plugin.MRP
{
    [Description("意向预测销售订单保存根据多投数量生成计划订单-服务"), HotUpdate]
    public class MRP_PlnForceCastSaveToPlanOrderOpe : AbstractOperationServicePlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("意向预测生成计划订单");

        private Dictionary<long, string> _entryPlanMap;

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("F_XQ_DDNM");
            e.FieldKeys.Add("F_XQ_DTSL");
        }

        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);

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

            var allIds = e.SelectedRows
                            .Select(row => row.DataEntity["Id"]?.ToString())
                            .ToList();
            _log.WriteLog($"选中行数: {allIds.Count}, FIDs: {string.Join(",", allIds)}");

            var idsByBillType = GetBillTypeGroups(allIds);
            idsByBillType.TryGetValue("6a0d041e5879e6", out var yxForecastList);
            _log.WriteLog($"按单据类型分组: 意向销售订单类型预测单={yxForecastList?.Count ?? 0}条");

            if (yxForecastList == null || yxForecastList.Count == 0)
            {
                _log.WriteLog("没有意向销售订单类型的预测单，退出");
                return;
            }

            var ids = string.Join(",", yxForecastList);

            var sql = $@"SELECT B.FENTRYID,
                M.FNUMBER AS WLNUM,
                BOM.FNUMBER AS BOM,
                UN.FNUMBER AS DANWEI,
                M2.FNUMBER AS WLFL,
                M.F_PAEZ_SYB AS SYB,
                D.FNUMBER AS SCCJ,
                B.F_XQ_DTSL AS DTSL,
                ML.FNAME AS WLNAME,
                A.F_UNW_TEXT_QTR AS YSDJH,
                TY.FNUMBER AS TFDJLX,
                A.FBILLNO AS YCDH
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
                WHERE A.FID IN ({ids})
                AND B.F_XQ_DTSL > 0 AND B.F_XQ_XLLB = '1' AND B.F_XQ_DDNM = 0  AND A.F_BJ = 1";

            _log.WriteLog($"执行SQL: {sql}");

            var dt = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if (dt == null || dt.Count == 0)
            {
                _log.WriteLog("SQL查询结果为空，退出");
                return;
            }
            _log.WriteLog($"SQL查询到 {dt.Count} 行数据");

            var today = DateTime.Now.ToString("yyyy-MM-dd");

            _entryPlanMap = new Dictionary<long, string>();

            var entryDataLookup = new Dictionary<long, DynamicObject>();
            foreach (var selectedRow in e.SelectedRows)
            {
                var entity = selectedRow.DataEntity;
                if (!(entity?["ForecastEntry"] is DynamicObjectCollection entries)) continue;
                foreach (var entry in entries)
                {
                    var entId = Convert.ToInt64(entry["Id"]);
                    if (entId > 0) entryDataLookup[entId] = entry;
                }
            }

            foreach (DynamicObject row in dt)
            {
                long entryId = row["FENTRYID"] != null ? Convert.ToInt64(row["FENTRYID"]) : 0;
                var wlNum = row["WLNUM"]?.ToString() ?? "";
                var bom = row["BOM"]?.ToString() ?? "";
                var danWei = row["DANWEI"]?.ToString() ?? "";
                var wlfl = row["WLFL"]?.ToString() ?? "";
                var syb = row["SYB"]?.ToString() ?? "";
                var sccj = row["SCCJ"]?.ToString() ?? "";
                var wlName = row["WLNAME"]?.ToString() ?? "";
                var ysdjh = row["YSDJH"]?.ToString() ?? "";
                var tfdjlx = row["TFDJLX"]?.ToString() ?? "";
                var ycdh = row["YCDH"]?.ToString() ?? "";

                var qty = entryDataLookup.TryGetValue(entryId, out var ent)
                    ? Convert.ToDecimal(ent["F_XQ_DTSL"] ?? 0)
                    : 0;
                var formEnt = entryDataLookup.TryGetValue(entryId, out var fe) ? fe : null;
                var formDtsl = formEnt?["F_XQ_DTSL"];
                _log.WriteLog($"DEBUG: SQL_DTSL={row["DTSL"]}, Form_DTSL={formDtsl}, final_qty={qty}");

                if (qty <= 0)
                {
                    _log.WriteLog($"数量<=0, 跳过 物料={wlNum}");
                    continue;
                }

                var releaseType = string.IsNullOrEmpty(bom) ? "2" : "1";

                _log.WriteLog($"创建计划订单: 物料={wlNum}, BOM={bom}, 数量={qty}, 单位={danWei}, 投放类型={releaseType}");

                JObject modelObject = new JObject();
                modelObject.Add("FID", 0);

                JObject billTypeObj = new JObject();
                billTypeObj.Add("FNUMBER", "JHDD02_SYS");
                modelObject.Add("FBillTypeID", billTypeObj);

                JObject supplyOrgObj = new JObject();
                supplyOrgObj.Add("FNumber", "101");
                modelObject.Add("FSupplyOrgId", supplyOrgObj);

                JObject inStockOrgObj = new JObject();
                inStockOrgObj.Add("FNumber", "101");
                modelObject.Add("FInStockOrgId", inStockOrgObj);

                if (!string.IsNullOrEmpty(sccj))
                {
                    JObject prdDeptObj = new JObject();
                    prdDeptObj.Add("FNumber", sccj);
                    modelObject.Add("FPrdDeptId", prdDeptObj);
                }

                JObject demandOrgObj = new JObject();
                demandOrgObj.Add("FNumber", "101");
                modelObject.Add("FDemandOrgId", demandOrgObj);

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

                modelObject.Add("F_PAEZ_XXDDH", ycdh);

                modelObject.Add("F_PAEZ_XSDDTYPE", "意向预测销售订单");

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
                            // _entryPlanMap[entryId] = planId;
                            var upsql = $@"UPDATE T_PLN_FORECASTENTRY SET F_XQ_DDNM = {planId} WHERE FENTRYID = {entryId}";
                            DBUtils.Execute(this.Context, upsql);
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

            _log.WriteLog("========== 预测单保存-生成计划订单 结束 ==========");
        }

        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);
            if (_entryPlanMap == null || _entryPlanMap.Count == 0) return;
            foreach (var dataEntity in e.DataEntitys)
            {
                if (!(dataEntity["ForecastEntry"] is DynamicObjectCollection entries)) continue;
                foreach (var entry in entries)
                {
                    var entId = Convert.ToInt64(entry["Id"]);
                    if (_entryPlanMap.TryGetValue(entId, out var planId))
                    {
                        entry["F_XQ_DDNM"] = planId;
                    }
                }
            }
        }

        private Dictionary<string, List<string>> GetBillTypeGroups(List<string> allIds)
        {
            var result = new Dictionary<string, List<string>>();
            if (allIds == null || allIds.Count == 0) return result;

            var ids = string.Join(",", allIds);
            var sql = $@"/*dialect*/SELECT A.FID, A.FBILLTYPEID FROM T_PLN_FORECAST A WHERE A.FID IN ({ids})";
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
    }
}

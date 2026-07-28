using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using Kingdee.BOS.WebApi.Client;
using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Kingdee.Zitn.Project.Code.plugin.DeliveryNotice
{
    [Description("【发货通知单下推服务】--[样品/意向]发货通知单审核根据标识自动下推直接调拨单"), HotUpdate]
    public class DeliveryNoticePushTransferDirect : AbstractOperationServicePlugIn
    {
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);

            var log = CustomLog.For("发货通知单");
            log.Section("下推直接调拨单");

            var client = new K3CloudApiClient(ErpLogin.K3CloudUrl);
            log.WriteLog("登录中...");
            var loginResult = client.ValidateLogin(
                ErpLogin.AppId,
                ErpLogin.UserName,
                ErpLogin.Password,
                ErpLogin.Lcid
            );
            if (JObject.Parse(loginResult)["LoginResultType"].Value<int>() != 1)
            {
                log.Error("登录失败");
                return;
            }
            log.WriteLog("登录成功");

            var allIds = e.SelectedRows
                            .Select(row => row.DataEntity["Id"]?.ToString())
                            .ToList();
            log.Section($"选中行数: {allIds.Count}, FIDs: {string.Join(",", allIds)}");

            if (allIds.Count == 0) return;

            var ids = string.Join(",", allIds);

            var judgeSql = $@"/*dialect*/SELECT A.FID, B.FENTRYID
FROM T_SAL_DELIVERYNOTICE A
JOIN T_SAL_DELIVERYNOTICEENTRY B ON A.FID = B.FID
WHERE A.FID IN ({ids}) AND A.FISBY = 1 AND A.FBILLTYPEID IN ('685120830dc927', '685120950dcadd')";

            var judgeDt = DBUtils.ExecuteDynamicObject(this.Context, judgeSql);
            if (judgeDt == null || judgeDt.Count == 0)
            {
                log.WriteLog("没有FISBY=1的分录，跳过下推");
                return;
            }

            var fidEntryMap = new Dictionary<long, List<long>>();
            for (int i = 0; i < judgeDt.Count; i++)
            {
                var fid = Convert.ToInt64(judgeDt[i]["FID"]);
                var entryId = Convert.ToInt64(judgeDt[i]["FENTRYID"]);

                if (!fidEntryMap.ContainsKey(fid))
                    fidEntryMap[fid] = new List<long>();
                fidEntryMap[fid].Add(entryId);
            }

            log.WriteLog($"共 {fidEntryMap.Count} 张单据满足下推条件");

            foreach (var kvp in fidEntryMap)
            {
                var fid = kvp.Key;
                var entryIds = kvp.Value;
                log.WriteLog($"FID={fid} 开始下推, 分录项: {string.Join(",", entryIds)}");

                try
                {
                    AutoPushTransferDirect(client, fid, entryIds, log);
                }
                catch (Exception ex)
                {
                    log.Error($"FID={fid} 下推异常");
                    log.Error(ex);
                }
            }

            log.Section("下推结束");
        }

        private void AutoPushTransferDirect(K3CloudApiClient client, long fid, List<long> entryIds, CustomLog.LogWriter log)
        {
            var entryIdsStr = string.Join(",", entryIds);

            // 提前查好所有源单分录的事业部（F_PAEZ_SYB），按FENTRYID建字典
            var sybMap = new Dictionary<long, string>();
            var sybSql = $@"/*dialect*/SELECT FENTRYID, F_PAEZ_SYB FROM T_SAL_DELIVERYNOTICEENTRY WHERE FENTRYID IN ({entryIdsStr})";
            var sybList = DBUtils.ExecuteDynamicObject(this.Context, sybSql);
            if (sybList != null)
            {
                for (int s = 0; s < sybList.Count; s++)
                {
                    var seId = Convert.ToInt64(sybList[s]["FENTRYID"]);
                    var seSyb = Convert.ToString(sybList[s]["F_PAEZ_SYB"] ?? "");
                    sybMap[seId] = seSyb;
                }
            }

            JObject pushObj = new JObject
            {
                ["Ids"] = "",
                ["Numbers"] = new JArray(),
                ["EntryIds"] = entryIdsStr,
                ["RuleId"] = "DeliveryNotice-StkTransferDirect",
                ["TargetBillTypeId"] = "",
                ["TargetOrgId"] = 0,
                ["TargetFormId"] = "STK_TransferDirect",
                ["IsEnableDefaultRule"] = false,
                ["IsDraftWhenSaveFail"] = true,
                ["CustomParams"] = new JObject()
            };

            log.WriteLog($"Push请求 EntryIds: {entryIdsStr}");
            var resultJson = client.Push("SAL_DELIVERYNOTICE", pushObj.ToString());
            log.WriteLog($"Push返回: {resultJson}");

            var pushResult = JObject.Parse(resultJson);
            var pushStatus = pushResult["Result"]["ResponseStatus"];
            if (!pushStatus["IsSuccess"].Value<bool>())
            {
                var errors = pushStatus["Errors"] as JArray;
                if (errors != null && errors.Count > 0)
                {
                    foreach (JObject err in errors)
                    {
                        log.Error($"FID={fid} Push失败: FieldName={err["FieldName"]}, Message={err["Message"]}, DIndex={err["DIndex"]}");
                    }
                }
                else
                {
                    log.Error($"FID={fid} Push失败, 完整返回: {resultJson}");
                }
                return;
            }

            var successList = pushStatus["SuccessEntitys"] as JArray;
            if (successList == null || successList.Count == 0)
            {
                log.Error($"FID={fid} Push成功但无SuccessEntitys返回: {resultJson}");
                return;
            }

            foreach (var item in successList)
            {
                long targetFid = item["Id"].Value<long>();
                var targetEntryIds = item["EntryIds"]["FBillEntry"] as JArray;
                log.WriteLog($"FID={fid} 下推成功 → 直接调拨单 FID={targetFid}, 分录数={targetEntryIds?.Count}");

                JObject modelObj = new JObject();
                modelObj.Add("FID", targetFid);

                JArray entryArray = new JArray();
                for (int i = 0; i < targetEntryIds.Count; i++)
                {
                    long targetEntryId = targetEntryIds[i].Value<long>();
                    long sourceEntryId = entryIds[i];

                    // 1. 拿物料ID
                    var entryInfoSql = $@"/*dialect*/SELECT FMATERIALID FROM T_STK_STKTRANSFERINENTRY WHERE FENTRYID = {targetEntryId}";
                    var entryInfoList = DBUtils.ExecuteDynamicObject(this.Context, entryInfoSql);
                    if (entryInfoList == null || entryInfoList.Count == 0) continue;
                    var materialId = Convert.ToInt64(entryInfoList[0]["FMATERIALID"]);

                    // 2. 拿事业部，映射调入仓库
                    var syb = sybMap.ContainsKey(sourceEntryId) ? sybMap[sourceEntryId] : "";
                    var destStockCode = GetDestStockBySYB(syb);

                    JObject entryObj = new JObject();
                    entryObj.Add("FEntryID", targetEntryId);

                    // 3. 序列号关联主档查调出仓库/仓位/批号
                    var serialSql = $@"/*dialect*/
                                            SELECT DISTINCT
                                                L.FNUMBER AS FLOT,
                                                S.FNUMBER AS FSTOCK,
                                                F1.FNUMBER AS SHELF,
                                                F2.FNUMBER AS LAYER,
                                                F3.FNUMBER AS POS
                                            FROM
                                            T_STK_STKTRANSFERINENTRY T
                                            LEFT JOIN T_STK_STKTRANSFERINSERIAL T1 ON T.FENTRYID = T1.FENTRYID
                                            LEFT JOIN T_BD_SERIALMASTER A ON T1.FSERIALID = A.FSERIALID
                                            LEFT JOIN T_BD_SERIALMASTEROTHER B ON A.FSERIALID = B.FSERIALID
                                            LEFT JOIN T_BD_STOCK S ON B.FSTOCKID = S.FSTOCKID
                                            LEFT JOIN T_BD_LOTMASTER L ON B.FLOT = L.FLOTID
                                            LEFT JOIN T_BAS_FLEXVALUESDETAIL F ON B.FSTOCKLOCID = F.FID
                                            LEFT JOIN T_BAS_FLEXVALUESENTRY F1 ON F.FF100001 = F1.FENTRYID
                                            LEFT JOIN T_BAS_FLEXVALUESENTRY F2 ON F.FF100002 = F2.FENTRYID
                                            LEFT JOIN T_BAS_FLEXVALUESENTRY F3 ON F.FF100003 = F3.FENTRYID
                                            WHERE T.FENTRYID = {targetEntryId} AND T.FMATERIALID = '{materialId}'
                                            ";
                    var serialList = DBUtils.ExecuteDynamicObject(this.Context, serialSql);
                    if (serialList != null && serialList.Count > 0)
                    {
                        var stockCode = Convert.ToString(serialList[0]["FSTOCK"] ?? "");
                        var shelfCode = Convert.ToString(serialList[0]["SHELF"] ?? "");
                        var layerCode = Convert.ToString(serialList[0]["LAYER"] ?? "");
                        var posCode = Convert.ToString(serialList[0]["POS"] ?? "");
                        var lot = Convert.ToString(serialList[0]["FLOT"] ?? "");

                        // 调出仓库
                        JObject srcStockObj = new JObject();
                        srcStockObj.Add("FNumber", stockCode);
                        entryObj.Add("FSrcStockId", srcStockObj);

                        // 调出仓位
                        JObject srcStockLocObj = new JObject();
                        JObject shelf = new JObject();
                        shelf.Add("FNumber", shelfCode);
                        srcStockLocObj.Add("FSRCSTOCKLOCID__FF100001", shelf);
                        JObject layer = new JObject();
                        layer.Add("FNumber", layerCode);
                        srcStockLocObj.Add("FSRCSTOCKLOCID__FF100002", layer);
                        JObject pos = new JObject();
                        pos.Add("FNumber", posCode);
                        srcStockLocObj.Add("FSRCSTOCKLOCID__FF100003", pos);
                        entryObj.Add("FSrcStockLocId", srcStockLocObj);

                        // 批号
                        JObject lotObj = new JObject();
                        lotObj.Add("FNumber", lot);
                        entryObj.Add("FLot", lotObj);
                    }

                    // 4. 调入仓库（根据事业部映射）
                    JObject destStockObj = new JObject();
                    destStockObj.Add("FNumber", destStockCode);
                    entryObj.Add("FDestStockId", destStockObj);

                    entryArray.Add(entryObj);
                }
                modelObj.Add("FBillEntry", entryArray);

                JObject saveObj = new JObject
                {
                    ["Model"] = modelObj,
                    ["IsDeleteEntry"] = false
                };

                log.WriteLog($"Save请求: {saveObj}");
                var saveResult = client.Save("STK_TransferDirect", saveObj.ToString());
                log.WriteLog($"Save返回: {saveResult}");

                var saveResultObj = JObject.Parse(saveResult);
                if (!saveResultObj["Result"]["ResponseStatus"]["IsSuccess"].Value<bool>())
                {
                    var saveErrors = saveResultObj["Result"]["ResponseStatus"]["Errors"] as JArray;
                    if (saveErrors != null && saveErrors.Count > 0)
                    {
                        foreach (JObject err in saveErrors)
                        {
                            log.Error($"FID={targetFid} Save失败: FieldName={err["FieldName"]}, Message={err["Message"]}");
                        }
                    }
                    else
                    {
                        log.Error($"FID={targetFid} Save失败, 完整返回: {saveResult}");
                    }
                    continue;
                }

                log.WriteLog($"FID={targetFid} 保存成功");

                /*var submitResult = client.Submit("STK_TransferDirect", new JObject { ["Ids"] = targetFid.ToString() }.ToString());
                log.WriteLog($"Submit返回: {submitResult}");

                var submitResultObj = JObject.Parse(submitResult);
                if (submitResultObj["Result"]["ResponseStatus"]["IsSuccess"].Value<bool>())
                {
                    log.WriteLog($"FID={fid} → 直接调拨单 FID={targetFid} 下推提交成功");
                }
                else
                {
                    log.Error($"FID={targetFid} Submit失败: {submitResult}");
                }*/
            }
        }

        private string GetDestStockBySYB(string syb)
        {
            switch (syb)
            {
                case "1": return "9201";
                case "2": return "9202";
                case "3": return "9203";
                case "4": return "9204";
                case "5": return "9205";
                default: return "";
            }
        }
    }
}

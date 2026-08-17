using Kingdee.BOS.App.Data;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.WebApi.Client;
using Kingdee.BOS.WebApi.ServicesStub;
using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kingdee.Zitn.Project.Code.Interface.ToBPM
{
    /// <summary>
    /// BPM服务接口
    /// </summary>
    public class BpmService : AbstractWebApiBusinessService
    {
        public BpmService(KDServiceContext context)
            : base(context)
        {
        }

        /// <summary>
        /// 销售项目 - BPM推送销售线索时同步到金蝶
        /// 根据CRMID查询是否存在，存在则更新，不存在则新建
        /// </summary>
        /// <param name="items">JSON字符串，格式：[{"crmId":"","fName":""},...]</param>
        public object SalesProject(string items)
        {
            var ctx = KDContext.Session.AppContext;
            if (ctx == null)
                return new { StatusCode = 401, Message = "超时，请重新登录" };

            if (string.IsNullOrWhiteSpace(items))
                return new { StatusCode = 400, Message = "数据不能为空" };

            try
            {
                JToken root = JToken.Parse(items);
                JArray itemsArr;

                if (root is JArray arr)
                    itemsArr = arr;
                else if (root is JObject obj && obj["parameters"] is JArray paramArr)
                    itemsArr = paramArr;
                else if (root is JObject singleObj)
                    itemsArr = new JArray { singleObj };
                else
                    return new { StatusCode = 400, Message = "数据格式不正确" };

                if (itemsArr.Count == 0)
                    return new { StatusCode = 400, Message = "数据不能为空" };

                // 解析并校验入参
                var validItems = new List<JObject>();
                var invalidItems = new List<object>();
                for (int i = 0; i < itemsArr.Count; i++)
                {
                    var item = itemsArr[i] as JObject;
                    if (item == null)
                    {
                        invalidItems.Add(new { Index = i, Message = "数据格式错误" });
                        continue;
                    }
                    var crmId = item["crmId"]?.ToString();
                    var fName = item["fName"]?.ToString();
                    if (string.IsNullOrWhiteSpace(crmId) || string.IsNullOrWhiteSpace(fName))
                    {
                        invalidItems.Add(new { Index = i, CrmId = crmId, Message = "crmId或fName不能为空" });
                        continue;
                    }
                    item["crmId"] = crmId;
                    item["fName"] = fName;
                    validItems.Add(item);
                }

                if (validItems.Count == 0)
                    return new { StatusCode = 400, Message = "无有效数据", InvalidItems = invalidItems };

                // 登录
                var client = new K3CloudApiClient(ErpLogin.K3CloudUrl);
                var loginResult = client.ValidateLogin(
                    ErpLogin.AppId,
                    ErpLogin.UserName,
                    ErpLogin.Password,
                    ErpLogin.Lcid
                );
                if (JObject.Parse(loginResult)["LoginResultType"].Value<int>() != 1)
                    return new { StatusCode = 500, Message = "K3Cloud 登录失败" };

                // 批量查询已存在的CRMID（IN查询，分200一组防超长）
                var existingFids = new Dictionary<string, long>();
                var crmIds = validItems.Select(o => o["crmId"].ToString()).Distinct().ToList();
                for (int i = 0; i < crmIds.Count; i += 200)
                {
                    var batch = crmIds.Skip(i).Take(200)
                        .Select(id => "'" + id.Replace("'", "''") + "'");
                    var batchSql = $"SELECT FID, FCRMID FROM QFJE_t_Cust100021 WHERE FCRMID IN ({string.Join(",", batch)})";
                    var batchResult = DBUtils.ExecuteDynamicObject(ctx, batchSql);
                    if (batchResult != null)
                    {
                        foreach (var row in batchResult)
                            existingFids[row["FCRMID"].ToString()] = Convert.ToInt64(row["FID"]);
                    }
                }

                // 逐条保存
                var saveObjTemplate = new JObject
                {
                    ["NeedUpDateFields"] = new JArray(),
                    ["NeedReturnFields"] = new JArray(),
                    ["IsDeleteEntry"] = "true",
                    ["SubSystemId"] = "",
                    ["IsVerifyBaseDataField"] = "false",
                    ["IsEntryBatchFill"] = "true",
                    ["ValidateFlag"] = "true",
                    ["NumberSearch"] = "true",
                    ["IsAutoAdjustField"] = "true",
                    ["InterationFlags"] = "",
                    ["IgnoreInterationFlag"] = "",
                    ["IsControlPrecision"] = "false",
                    ["ValidateRepeatJson"] = "false"
                };

                var successList = new List<object>();
                var failList = new List<object>();
                int updateCount = 0;
                int createCount = 0;

                foreach (var item in validItems)
                {
                    var crmId = item["crmId"].ToString();
                    var fName = item["fName"].ToString();

                    try
                    {
                        existingFids.TryGetValue(crmId, out long existingFid);
                        bool isUpdate = existingFid > 0;
                        if (isUpdate) updateCount++; else createCount++;

                        var modelObj = new JObject
                        {
                            ["FID"] = existingFid,
                            ["FName"] = fName,
                            ["FCRMID"] = crmId
                        };

                        var saveObj = new JObject(saveObjTemplate);
                        saveObj["Model"] = modelObj;

                        var saveResultJson = client.Save("QFJE_XM", saveObj.ToString());
                        var saveJObj = JObject.Parse(saveResultJson);
                        var responseStatus = saveJObj["Result"]["ResponseStatus"];

                        if (!responseStatus["IsSuccess"].Value<bool>())
                        {
                            var errMsg = responseStatus["Errors"]?[0]?["Message"]?.ToString() ?? "未知错误";
                            failList.Add(new { CrmId = crmId, IsUpdate = isUpdate, Message = "保存失败: " + errMsg });
                            continue;
                        }

                        var savedId = saveJObj["Result"]["Id"]?.Value<long>() ?? 0;
                        var savedNumber = saveJObj["Result"]["Number"]?.ToString() ?? "";

                        if (!isUpdate)
                        {
                            // 新建 → 提交
                            var submitObj = new JObject
                            {
                                ["CreateOrgId"] = 0,
                                ["Numbers"] = new JArray(),
                                ["Ids"] = savedId.ToString(),
                                ["SelectedPostId"] = 0,
                                ["UseOrgId"] = 0,
                                ["NetworkCtrl"] = "",
                                ["IgnoreInterationFlag"] = ""
                            };
                            var submitResult = client.Submit("QFJE_XM", submitObj.ToString());
                            var submitJObj = JObject.Parse(submitResult);
                            if (!submitJObj["Result"]["ResponseStatus"]["IsSuccess"].Value<bool>())
                            {
                                var submitErr = submitJObj["Result"]["ResponseStatus"]["Errors"]?[0]?["Message"]?.ToString() ?? "未知错误";
                                failList.Add(new { CrmId = crmId, IsUpdate = false, Id = savedId, Number = savedNumber, Message = "保存成功，提交失败: " + submitErr });
                                continue;
                            }

                            // 新建 → 审核
                            var auditObj = new JObject
                            {
                                ["CreateOrgId"] = 0,
                                ["Numbers"] = new JArray(),
                                ["Ids"] = savedId.ToString(),
                                ["InterationFlags"] = "",
                                ["UseOrgId"] = 0,
                                ["NetworkCtrl"] = "",
                                ["IsVerifyProcInst"] = "true",
                                ["IgnoreInterationFlag"] = "",
                                ["UseBatControlTimes"] = "false"
                            };
                            var auditResult = client.Audit("QFJE_XM", auditObj.ToString());
                            var auditJObj = JObject.Parse(auditResult);
                            if (!auditJObj["Result"]["ResponseStatus"]["IsSuccess"].Value<bool>())
                            {
                                var auditErr = auditJObj["Result"]["ResponseStatus"]["Errors"]?[0]?["Message"]?.ToString() ?? "未知错误";
                                failList.Add(new { CrmId = crmId, IsUpdate = false, Id = savedId, Number = savedNumber, Message = "保存提交成功，审核失败: " + auditErr });
                                continue;
                            }
                        }

                        successList.Add(new
                        {
                            CrmId = crmId,
                            IsUpdate = isUpdate,
                            Id = savedId,
                            Number = savedNumber
                        });
                    }
                    catch (Exception ex)
                    {
                        failList.Add(new { CrmId = crmId, Message = ex.Message });
                    }
                }

                return new
                {
                    StatusCode = 200,
                    Total = validItems.Count,
                    UpdateCount = updateCount,
                    CreateCount = createCount,
                    SuccessCount = successList.Count,
                    FailCount = failList.Count,
                    InvalidCount = invalidItems.Count,
                    SuccessList = successList,
                    FailList = failList,
                    InvalidItems = invalidItems
                };
            }
            catch (Exception ex)
            {
                return new { StatusCode = 500, Message = "服务器错误: " + ex.Message };
            }
        }
    }
}

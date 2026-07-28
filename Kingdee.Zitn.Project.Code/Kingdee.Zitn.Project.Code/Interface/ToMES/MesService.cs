using Kingdee.BOS.App.Data;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.WebApi.Client;
using Kingdee.BOS.WebApi.ServicesStub;
using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;

namespace Kingdee.Zitn.Project.Code.Interface.ToMES
{
    /// <summary>
    /// 【接口服务】对接MES系统的服务类
    /// </summary>
    public class MesService : AbstractWebApiBusinessService
    {
        public MesService(KDServiceContext context)
            : base(context)
        {
        }

        /// <summary>
        /// 获取生产订单信息
        /// </summary>
        /// <param name="queryDate">查询日期</param>
        /// <param name="onlyStart">是否只查询计划状态 1-7 0-所有</param>
        /// <param name="fseq">生产订单行号</param>
        /// <returns></returns>
        public object GetTodoInfo(DateTime queryDate, int onlyStart)
        {
            var ctx = KDContext.Session.AppContext;
            if (ctx == null)
            {
                return new
                {
                    StatusCode = 401,
                    Message = "超时，请重新登录"
                };
            }

            if (queryDate == default(DateTime))
            {
                return new
                {
                    StatusCode = 400,
                    Message = "查询日期参数无效"
                };
            }

            if (onlyStart > 7)
            {
                return new
                {
                    StatusCode = 400,
                    Message = "onlyStart 参数无效，只能为 0 到 7的状态值"
                };
            }

            try
            {
                string dateParam = queryDate.ToString("yyyy-MM-dd");

                var result = DBUtils.ExecuteDataSet(ctx, $"EXEC sp_GetProductionOrderInfo '{dateParam}',{onlyStart}");

                var dataList = new List<Dictionary<string, object>>();

                if (result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in result.Tables[0].Rows)
                    {
                        var rowDict = new Dictionary<string, object>();
                        foreach (DataColumn column in result.Tables[0].Columns)
                        {
                            rowDict[column.ColumnName] = row[column];
                        }
                        dataList.Add(rowDict);
                    }
                }

                if (dataList.Count == 0)
                {
                    return new
                    {
                        StatusCode = 404,
                        Message = "未找到相关单据信息"
                    };
                }

                return new
                {
                    StatusCode = 200,
                    Data = dataList,
                    ApiName = System.Reflection.MethodBase.GetCurrentMethod().Name,
                    OnlyStart = onlyStart
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    StatusCode = 500,
                    Message = $"服务器错误: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 生产订单下推生产汇报单，填写工时
        /// </summary>
        /// <param name="fentryIds">生产订单分录ID，逗号分隔，如 "100,101,102"</param>
        /// <param name="workHours">对应的工时，逗号分隔，如 "8.0,6.5,7.0"</param>
        public object PushMoToMorpt(string fentryIds, string workHours)
        {
            var ctx = KDContext.Session.AppContext;
            if (ctx == null)
                return new { StatusCode = 401, Message = "超时，请重新登录" };

            if (string.IsNullOrWhiteSpace(fentryIds) || string.IsNullOrWhiteSpace(workHours))
                return new { StatusCode = 400, Message = "参数不能为空" };

            try
            {
                var entryIdArr = fentryIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var hoursArr = workHours.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (entryIdArr.Length != hoursArr.Length)
                    return new { StatusCode = 400, Message = "分录ID个数与工时个数不一致" };

                var client = new K3CloudApiClient(ErpLogin.K3CloudUrl);
                var loginResult = client.ValidateLogin(
                    ErpLogin.AppId,
                    ErpLogin.UserName,
                    ErpLogin.Password,
                    ErpLogin.Lcid
                );
                if (JObject.Parse(loginResult)["LoginResultType"].Value<int>() != 1)
                    return new { StatusCode = 500, Message = "K3Cloud 登录失败" };

                var resultList = new List<object>();

                for (int i = 0; i < entryIdArr.Length; i++)
                {
                    var entryId = entryIdArr[i].Trim();
                    if (!decimal.TryParse(hoursArr[i].Trim(), out decimal hours))
                        continue;

                    // ---- 防重：查询已存在工时汇报单 ----
                    var checkSql = string.Format(
                        @"/*dialect*/SELECT 1 FROM T_PRD_MORPT A
                          JOIN T_PRD_MORPTENTRY B ON A.FID = B.FID
                          WHERE B.FSRCENTRYID = {0} AND A.F_PAEZ_GSHB = '1'", entryId);
                    var checkDt = DBUtils.ExecuteDynamicObject(ctx, checkSql);
                    if (checkDt != null && checkDt.Count > 0)
                    {
                        resultList.Add(new { OriginalEntryId = entryId, Skipped = true, Message = "已存在工时汇报单，跳过" });
                        continue;
                    }

                    // ---- 下推：单个分录 ----
                    var pushJson = new JObject
                    {
                        ["Ids"] = "",
                        ["Numbers"] = new JArray(),
                        ["EntryIds"] = entryId,
                        ["RuleId"] = "490c1753-b810-42e5-8633-9ac1ba185e38", // 工时走490c1753-b810-42e5-8633-9ac1ba185e38 不走 PRD_MO2MORPT
                        ["TargetBillTypeId"] = "",
                        ["TargetOrgId"] = 0,
                        ["TargetFormId"] = "PRD_MORPT",
                        ["IsEnableDefaultRule"] = false,
                        ["IsDraftWhenSaveFail"] = true,
                        ["CustomParams"] = new JObject()
                    }.ToString();

                    var pushResultJson = client.Push("PRD_MO", pushJson);
                    var pushJObj = JObject.Parse(pushResultJson);

                    if (!pushJObj["Result"]["ResponseStatus"]["IsSuccess"].Value<bool>())
                    {
                        var errMsg = pushJObj["Result"]["ResponseStatus"]["Errors"]?[0]?["Message"]?.ToString() ?? "未知错误";
                        resultList.Add(new { EntryId = entryId, Success = false, Message = "下推失败: " + errMsg });
                        continue;
                    }

                    var successList = pushJObj["Result"]["ResponseStatus"]["SuccessEntitys"] as JArray;
                    if (successList == null || successList.Count == 0)
                    {
                        resultList.Add(new { EntryId = entryId, Success = false, Message = "下推结果为空" });
                        continue;
                    }

                    var pushItem = successList[0];
                    long newFid = pushItem["Id"].Value<long>();
                    var newEntryIds = pushItem["EntryIds"]?["FEntity"] as JArray;
                    if (newEntryIds == null || newEntryIds.Count == 0)
                    {
                        resultList.Add(new { EntryId = entryId, Success = false, Message = "未获取到新分录ID" });
                        continue;
                    }

                    long newEntryId = newEntryIds[0].Value<long>();
                    JArray entityArray = new JArray();
                    JObject entityRow = new JObject
                    {
                        ["FEntryID"] = newEntryId,
                        ["FHrWorkTime"] = hours,
                        ["FFinishQty"] = 0,
                        ["FPassQty"] = 0,
                        ["FFailQty"] = 0,
                        ["FScrapQty"] = 0,
                        ["FPendingRepairQty"] = 0,
                        ["FReworkQty"] = 0,
                        ["FSampleDamageQty"] = 0
                    };
                    entityArray.Add(entityRow);

                    // ---- 保存 ----
                    JObject modelObj = new JObject
                    {
                        ["FID"] = newFid,
                        ["F_PAEZ_GSHB"] = "true",
                        ["FEntity"] = entityArray
                    };

                    var saveObj = new JObject
                    {
                        ["NeedUpDateFields"] = new JArray(),
                        ["NeedReturnFields"] = new JArray(),
                        ["IsDeleteEntry"] = "true",
                        ["SubSystemId"] = "",
                        ["IsVerifyBaseDataField"] = "false",
                        ["IsEntryBatchFill"] = "true",
                        ["ValidateFlag"] = "true",
                        ["NumberSearch"] = "true",
                        ["IsAutoAdjustField"] = "false",
                        ["InterationFlags"] = "",
                        ["IgnoreInterationFlag"] = "",
                        ["IsControlPrecision"] = "false",
                        ["ValidateRepeatJson"] = "false",
                        ["Model"] = modelObj
                    };

                    var saveResultJson = client.Save("PRD_MORPT", saveObj.ToString());
                    var saveJObj = JObject.Parse(saveResultJson);
                    bool saveOk = saveJObj["Result"]["ResponseStatus"]["IsSuccess"].Value<bool>();
                    string saveErrMsg = null;
                    if (!saveOk)
                    {
                        saveErrMsg = saveJObj["Result"]["ResponseStatus"]["Errors"]?[0]?["Message"]?.ToString() ?? "未知保存错误";
                    }

                    resultList.Add(new
                    {
                        OriginalEntryId = entryId,
                        NewFid = newFid,
                        NewEntryId = newEntryId,
                        WorkHours = hours,
                        SaveSuccess = saveOk,
                        SaveError = saveErrMsg
                    });
                }

                return new { StatusCode = 200, Message = "完成", Data = resultList };
            }
            catch (Exception ex)
            {
                return new { StatusCode = 500, Message = "服务器错误: " + ex.Message };
            }
        }

        /// <summary>
        /// 生产订单下推生产汇报单，填入序列号
        /// </summary>
        /// <param name="fentryIds">生产订单分录ID，逗号分隔，如 "100,101,102"</param>
        /// <param name="serialNosGroups">序列号组，| 分隔每组（对应每个分录），组内逗号分隔，如 "SN001,SN002|SN003|SN004"</param>
        public object PushMoToMorptXLH(string fentryIds, string serialNosGroups)
        {
            var ctx = KDContext.Session.AppContext;
            if (ctx == null)
                return new { StatusCode = 401, Message = "超时，请重新登录" };

            if (string.IsNullOrWhiteSpace(fentryIds) || string.IsNullOrWhiteSpace(serialNosGroups))
                return new { StatusCode = 400, Message = "参数不能为空" };

            try
            {
                var entryIdArr = fentryIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var serialGroupsArr = serialNosGroups.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                if (entryIdArr.Length != serialGroupsArr.Length)
                    return new { StatusCode = 400, Message = "分录ID个数与序列号组数不一致" };

                var client = new K3CloudApiClient(ErpLogin.K3CloudUrl);
                var loginResult = client.ValidateLogin(
                    ErpLogin.AppId,
                    ErpLogin.UserName,
                    ErpLogin.Password,
                    ErpLogin.Lcid
                );
                if (JObject.Parse(loginResult)["LoginResultType"].Value<int>() != 1)
                    return new { StatusCode = 500, Message = "K3Cloud 登录失败" };

                var resultList = new List<object>();

                for (int i = 0; i < entryIdArr.Length; i++)
                {
                    var entryId = entryIdArr[i].Trim();
                    var serialNosInGroup = serialGroupsArr[i].Trim();

                    // ---- 下推：单个分录 ----
                    var pushJson = new JObject
                    {
                        ["Ids"] = "",
                        ["Numbers"] = new JArray(),
                        ["EntryIds"] = entryId,
                        ["RuleId"] = "PRD_MO2MORPT",
                        ["TargetBillTypeId"] = "",
                        ["TargetOrgId"] = 0,
                        ["TargetFormId"] = "PRD_MORPT",
                        ["IsEnableDefaultRule"] = false,
                        ["IsDraftWhenSaveFail"] = true,
                        ["CustomParams"] = new JObject()
                    }.ToString();

                    var pushResultJson = client.Push("PRD_MO", pushJson);
                    var pushJObj = JObject.Parse(pushResultJson);

                    if (!pushJObj["Result"]["ResponseStatus"]["IsSuccess"].Value<bool>())
                    {
                        var errMsg = pushJObj["Result"]["ResponseStatus"]["Errors"]?[0]?["Message"]?.ToString() ?? "未知错误";
                        resultList.Add(new { EntryId = entryId, Success = false, Message = "下推失败: " + errMsg });
                        continue;
                    }

                    var successList = pushJObj["Result"]["ResponseStatus"]["SuccessEntitys"] as JArray;
                    if (successList == null || successList.Count == 0)
                    {
                        resultList.Add(new { EntryId = entryId, Success = false, Message = "下推结果为空" });
                        continue;
                    }

                    var pushItem = successList[0];
                    long newFid = pushItem["Id"].Value<long>();
                    var newEntryIds = pushItem["EntryIds"]?["FEntity"] as JArray;
                    if (newEntryIds == null || newEntryIds.Count == 0)
                    {
                        resultList.Add(new { EntryId = entryId, Success = false, Message = "未获取到新分录ID" });
                        continue;
                    }

                    long newEntryId = newEntryIds[0].Value<long>();

                    // ---- 构建序列号子单据体 ----
                    JArray serialArray = new JArray();
                    var serialNoArr = serialNosInGroup.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var sn in serialNoArr)
                    {
                        var trimmedSn = sn.Trim();
                        JObject serialItem = new JObject
                        {
                            ["FDetailID"] = 0,
                            ["FQCMaterialId"] = new JObject { ["FNUMBER"] = "" },
                            ["FInspectResult"] = "",
                            ["FQcAuxPropId"] = new JObject(),
                            ["FQCQty"] = 0,
                            ["FSerialNo"] = trimmedSn,
                            ["FSerialId"] = new JObject { ["FNumber"] = trimmedSn },
                            ["FQCStockInSelQty"] = 0,
                            ["FSerialNote"] = "",
                            ["FBaseQCQty"] = 0,
                            ["FBaseQCStockInSelQty"] = 0
                        };
                        serialArray.Add(serialItem);
                    }

                    JArray entityArray = new JArray();
                    JObject entityRow = new JObject
                    {
                        ["FEntryID"] = newEntryId,
                        ["FSerialSubEntity"] = serialArray
                    };
                    entityArray.Add(entityRow);

                    // ---- 保存 ----
                    JObject modelObj = new JObject
                    {
                        ["FID"] = newFid,
                        ["FEntity"] = entityArray
                    };

                    var saveObj = new JObject
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
                        ["ValidateRepeatJson"] = "false",
                        ["Model"] = modelObj
                    };

                    var saveResultJson = client.Save("PRD_MORPT", saveObj.ToString());
                    var saveJObj = JObject.Parse(saveResultJson);
                    bool saveOk = saveJObj["Result"]["ResponseStatus"]["IsSuccess"].Value<bool>();

                    resultList.Add(new
                    {
                        OriginalEntryId = entryId,
                        NewFid = newFid,
                        NewEntryId = newEntryId,
                        SerialNos = serialNosInGroup,
                        SaveSuccess = saveOk
                    });
                }

                return new { StatusCode = 200, Message = "完成", Data = resultList };
            }
            catch (Exception ex)
            {
                return new { StatusCode = 500, Message = "服务器错误: " + ex.Message };
            }
        }
    }
}

using Kingdee.BOS.App.Data;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.WebApi.Client;
using Kingdee.BOS.WebApi.ServicesStub;
using Kingdee.Zitn.Project.Code.conf;
using Kingdee.Zitn.Project.Code.models;
using Kingdee.Zitn.Project.Code.Util;
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
        private  string materialId = string.Empty;
        private static readonly CustomLog.LogWriter _log = CustomLog.For("MesService");
        public MesService(KDServiceContext context)
            : base(context)
        {
        }

        /// <summary>
        /// 获取生产订单信息
        /// </summary>
        /// <param name="queryDate">查询日期</param>
        /// <param name="onlyStart">是否只查询计划状态 1-7特定状态 0-所有</param>
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

        /// <summary>
        /// 查询生产用料清单（PRD_PPBOM）
        /// </summary>
        /// <param name="billNo">生产订单号 FMOBillNO</param>
        /// <param name="materialCode">物料编码 FMaterialID.FNumber</param>
        /// <param name="entrySeq">生产订单行号 FMOEntrySeq</param>
        public object Ppbom(string billNo, string materialCode, string entrySeq)
        {
            var ctx = KDContext.Session.AppContext;
            if (ctx == null)
                return new { StatusCode = 401, Message = "超时，请重新登录" };

            if (string.IsNullOrWhiteSpace(billNo) && string.IsNullOrWhiteSpace(materialCode) && string.IsNullOrWhiteSpace(entrySeq))
                return new { StatusCode = 400, Message = "生产订单号、物料编码、生产订单行号至少提供一个查询条件" };

            try
            {
                var client = new K3CloudApiClient(ErpLogin.K3CloudUrl);
                var loginResult = client.ValidateLogin(
                    ErpLogin.AppId,
                    ErpLogin.UserName,
                    ErpLogin.Password,
                    ErpLogin.Lcid
                );
                if (JObject.Parse(loginResult)["LoginResultType"].Value<int>() != 1)
                    return new { StatusCode = 500, Message = "K3Cloud 登录失败" };

                // 构建过滤条件（ExecuteBillQuery 用 SQL 风格字符串，单引号转义）
                var conditions = new List<string>();
                if (!string.IsNullOrWhiteSpace(billNo))
                    conditions.Add($"FMOBillNO = '{billNo.Trim().Replace("'", "''")}'");
                if (!string.IsNullOrWhiteSpace(materialCode))
                    conditions.Add($"FMaterialID.FNumber = '{materialCode.Trim().Replace("'", "''")}'");
                if (!string.IsNullOrWhiteSpace(entrySeq))
                    conditions.Add($"FMOEntrySeq = {entrySeq.Trim()}");

                var queryObj = new JObject
                {
                    ["FormId"] = "PRD_PPBOM",
                    ["FieldKeys"] = BuildPpbomFieldKeys(),
                    ["FilterString"] = string.Join(" and ", conditions),
                    ["OrderString"] = "",
                    ["TopRowCount"] = 0,
                    ["StartRow"] = 0,
                    ["Limit"] = 2000,
                    ["SubSystemId"] = ""
                };

                // ExecuteBillQuery 返回 List<List<object>>：第一行是列头（字段名），后续是数据行
                var queryResult = client.ExecuteBillQuery(queryObj.ToString());

                if (queryResult == null || queryResult.Count <= 1)
                    return new { StatusCode = 404, Message = "未找到生产用料清单数据" };

                var header = queryResult[0];

                // 按列头组装成 JObject 行列表
                var rows = new List<JObject>();
                for (int i = 1; i < queryResult.Count; i++)
                {
                    var row = new JObject();
                    var dataRow = queryResult[i];
                    for (int j = 0; j < header.Count && j < dataRow.Count; j++)
                    {
                        var key = header[j]?.ToString();
                        if (string.IsNullOrEmpty(key))
                            continue;

                        var val = dataRow[j];
                        row[key] = (val == null || val is DBNull) ? JValue.CreateNull() : JToken.FromObject(val);
                    }
                    rows.Add(row);
                }

                // 拆分成「单据头一条 + 单据体数组」的结构
                JObject billHead = null;
                var entityList = new JArray();

                foreach (var row in rows)
                {
                    if (billHead == null)
                    {
                        billHead = new JObject();
                        foreach (var prop in row.Properties())
                        {
                            if (!prop.Name.StartsWith("FEntity.", StringComparison.OrdinalIgnoreCase))
                                billHead[prop.Name] = prop.Value;
                        }
                    }

                    var entityRow = new JObject();
                    foreach (var prop in row.Properties())
                    {
                        if (prop.Name.StartsWith("FEntity.", StringComparison.OrdinalIgnoreCase))
                            entityRow[prop.Name.Substring("FEntity.".Length)] = prop.Value;
                    }
                    entityList.Add(entityRow);
                }

                var data = new JObject
                {
                    ["BillHead"] = billHead ?? new JObject(),
                    ["FEntity"] = entityList
                };

                return new
                {
                    StatusCode = 200,
                    Message = "查询成功",
                    Total = entityList.Count,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                return new { StatusCode = 500, Message = "服务器错误: " + ex.Message };
            }
        }

        /// <summary>
        /// 生产用料清单查询（数据包方式，可读基础资料属性等标准接口取不到的字段）
        /// </summary>
        /// <param name="items">JSON，格式：{"billNo":"生产订单号","materialCode":"物料编码","entrySeq":"生产订单行号"}</param>
        public object PpbomList(string items)
        {
            var ctx = KDContext.Session.AppContext;
            if (ctx == null)
                return new { StatusCode = 401, Message = "超时，请重新登录" };

            if (string.IsNullOrWhiteSpace(items))
                return new { StatusCode = 400, Message = "参数不能为空" };

            try
            {
                string billNo = null, materialCode = null, entrySeq = null;
                var root = JToken.Parse(items);
                if (root is JObject jobj)
                {
                    billNo = jobj["billNo"]?.ToString();
                    materialCode = jobj["materialCode"]?.ToString();
                    entrySeq = jobj["entrySeq"]?.ToString();
                }
                else if (root is JArray arr)
                {
                    if (arr.Count > 0) billNo = arr[0]?.ToString();
                    if (arr.Count > 1) materialCode = arr[1]?.ToString();
                    if (arr.Count > 2) entrySeq = arr[2]?.ToString();
                }

                if (string.IsNullOrWhiteSpace(billNo) && string.IsNullOrWhiteSpace(materialCode) && string.IsNullOrWhiteSpace(entrySeq))
                    return new { StatusCode = 400, Message = "生产订单号、物料编码、生产订单行号至少提供一个查询条件" };

                string materialId = "";
                if (!string.IsNullOrWhiteSpace(materialCode))
                {
                    var mtrlSql = $"/*dialect*/SELECT FMATERIALID FROM T_BD_MATERIAL WHERE FUSEORGID = 101006 AND FNUMBER = '{materialCode.Trim().Replace("'", "''")}' AND FDOCUMENTSTATUS = 'C'";
                    var mtrlDt = DBUtils.ExecuteDynamicObject(ctx, mtrlSql);
                    if (mtrlDt != null && mtrlDt.Count > 0)
                        materialId = mtrlDt[0]["FMATERIALID"].ToString();
                }

                // 生产订单状态（SQL 查询，不在数据包取）
                string moStatus = "";
                if (!string.IsNullOrWhiteSpace(billNo) && !string.IsNullOrWhiteSpace(materialId) && !string.IsNullOrWhiteSpace(entrySeq))
                {
                    var statusSql = $@"/*dialect*/SELECT CASE B1.FSTATUS
                                                WHEN 1 THEN '计划'
                                                WHEN 2 THEN '计划确认'
                                                WHEN 3 THEN '下达'
                                                WHEN 4 THEN '开工'
                                                WHEN 5 THEN '完工'
                                                WHEN 6 THEN '结案'
                                                WHEN 7 THEN '结算'
                                                ELSE '未知状态'
                                            END AS FSTATUS
                                            FROM T_PRD_MO A
                                            JOIN T_PRD_MOENTRY B ON A.FID = B.FID
                                            JOIN T_PRD_MOENTRY_A B1 ON B.FENTRYID = B1.FENTRYID
                                            WHERE FBILLNO = '{billNo.Trim().Replace("'", "''")}'
                                              AND FMATERIALID = '{materialId}'
                                              AND FSEQ = {entrySeq.Trim()}";
                    var statusDt = DBUtils.ExecuteDynamicObject(ctx, statusSql);
                    if (statusDt != null && statusDt.Count > 0)
                        moStatus = Convert.ToString(statusDt[0]["FSTATUS"]);
                }

                var conditions = new List<string>();
                if (!string.IsNullOrWhiteSpace(billNo))
                    conditions.Add($"FMOBillNO = '{billNo.Trim().Replace("'", "''")}'");
                if (!string.IsNullOrWhiteSpace(materialId))
                    conditions.Add($"FMaterialID = '{materialId}'");
                if (!string.IsNullOrWhiteSpace(entrySeq))
                    conditions.Add($"FMOEntrySeq = {entrySeq.Trim()}");

                var objs = Tools.GetItemArray(ctx, "PRD_PPBOM", string.Join(" and ", conditions));
                if (objs == null || objs.Length == 0)
                    return new { StatusCode = 404, Message = "未找到生产用料清单数据" };

                var list = new List<object>();
                foreach (var obj in objs)
                {
                    var bodyList = new List<object>();
                    var entitys = obj["PPBomEntry"] as DynamicObjectCollection;
                    if (entitys != null)
                    {
                        foreach (var entry in entitys)
                        {
                            bodyList.Add(new
                            {
                                //项次
                                FReplaceGroup = Str(entry["ReplaceGroup"]),
                                //子项物料编码
                                FmtrlNum = RefNumber(entry["MaterialID"]),
                                //子项物料名称
                                FmtrlName = RefName(entry["MaterialID"]),
                                //子项物料规格型号
                                FmtrlSpec = RefSpec(entry["MaterialID"]),
                                //应发数量
                                FMustQty = Dec(entry["MustQty"]),
                                //基本单位分子
                                FBaseNumerator = Dec(entry["BaseNumerator"]),
                                //已领数量
                                FPickedQty = Dec(entry["PickedQty"]),
                                //未领数量
                                FNoPickedQty = Dec(entry["NoPickedQty"]),
                                //子项类型  1 标准件 2 返还件 3 替代件
                                FMaterialType = ErpDataDict.Map(ErpDataDict.MaterialType, Str(entry["MaterialType"])),
                                //使用比列
                                FUseRate = Dec(entry["UseRate"], 3),
                                //分子
                                FNumerator = Dec(entry["Numerator"]),
                                //分母
                                FDenominator = Dec(entry["Denominator"]),
                                //单位
                                FUnit = RefNumber(entry["UnitID"]),
                                //可用库存
                                FInventoryQty = Dec(entry["InventoryQty"]),
                                //发料方式  1 直接领料 2 直接倒冲 3 调拨领料 4 调拨倒冲 7 不发料
                                FIssueType = ErpDataDict.Map(ErpDataDict.IssueType, RefNumber(entry["IssueType"])),
                                //货主 101 青岛智腾微电子有限公司  1  青岛智腾科技有限公司
                                FOwnerID = ErpDataDict.Map(ErpDataDict.Owner, RefNumber(entry["OwnerID"]))
                            });
                        }
                    }

                    list.Add(new
                    {
                        //生产用料编号
                        FBillNo = Str(obj["BillNo"]),
                        //父项物料编码
                        FMaterialID = RefNumber(obj["MaterialID"]),
                        //父项物料名称
                        FMaterialName = RefName(obj["MaterialID"]),
                        //父项物料规格型号
                        FMaterialSpec = RefSpec(obj["MaterialID"]),
                        //BOM版本
                        FBOM = RefNumber(obj["FBOMID"]),
                        //生产组织
                        FPrdOrgId = RefName(obj["PrdOrgId"]),
                        //生产车间
                        //FWorkshopID = RefNumber(obj["WorkshopID"]),
                        //生产车间名称
                        FWorkshopIDName = RefName(obj["WorkshopID"]),
                        //单位
                        FUnit = RefNumber(obj["UnitID"]),
                        //数量
                        FQty = Dec(obj["Qty"]),
                        //生产订单类型
                        //FMOType = RefNumber(obj["MOType"]),
                        FMOType = RefName(obj["MOType"]),
                        //生产订单号
                        FMOBillNO = Str(obj["MOBillNO"]),
                        //生产订单状态
                        FMOStatus = moStatus,
                        //生产订单行号
                        FMOEntrySeq = Str(obj["MOEntrySeq"]),
                        //单据状态  A 创建 B审核中 C 审核通过 D 重新审核
                        FDocumentStatus = ErpDataDict.Map(ErpDataDict.DocumentStatus, Str(obj["DocumentStatus"])),
                        //领料原因
                        F_PAEZ_LLYY = RefName(obj["F_PAEZ_LLYY"]),
                        //事业部  9 无, 1 事业一部, 2 事业二部, 3 事业三部, 4 事业四部, 11 事业五部, 10 生产中心, 6 电源子公司, 5 烽行事业部, 12 北京子公司, 13 系统事业部, 14 陀螺事业部
                        F_PAEZ_SYB = ErpDataDict.Map(ErpDataDict.BusinessDept, RefNumber(obj["F_PAEZ_SYB"])),
                        //备注
                        FDescription = Str(obj["Description"]),
                        FEntity = bodyList
                    });
                }

                return new { StatusCode = 200, Message = "查询成功", Total = list.Count, Data = list };
            }
            catch (Exception ex)
            {
                return new { StatusCode = 500, Message = "服务器错误: " + ex.Message };
            }
        }

        /// <summary>
        /// 查询其他订单信息
        /// </summary>
        /// <param name="order">订单号</param>
        /// <returns>查询结果</returns>
        public object GetOtherOrder(string order)
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

            if (string.IsNullOrWhiteSpace(order))
            {
                return new
                {
                    StatusCode = 400,
                    Message = "订单号参数不能为空"
                };
            }

            try
            {
                // 调用存储过程 SP_SelectionOrser
                string sql = $"EXEC SP_SelectionOrser '{order.Trim().Replace("'", "''")}'";
                var result = DBUtils.ExecuteDataSet(ctx, sql);

                if (result == null || result.Tables.Count == 0 || result.Tables[0].Rows.Count == 0)
                {
                    return new
                    {
                        StatusCode = 404,
                        Message = "未找到相关订单信息"
                    };
                }

                var dataList = new List<Dictionary<string, object>>();
                foreach (DataRow row in result.Tables[0].Rows)
                {
                    var rowDict = new Dictionary<string, object>();
                    foreach (DataColumn column in result.Tables[0].Columns)
                    {
                        rowDict[column.ColumnName] = row[column];
                    }
                    dataList.Add(rowDict);
                }

                return new
                {
                    StatusCode = 200,
                    Message = "查询成功",
                    Data = dataList,
                    ApiName = System.Reflection.MethodBase.GetCurrentMethod().Name
                };
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _log?.Error($"查询其他订单信息异常，订单号：{order}，错误：{ex.Message}");
                _log?.Error(ex);
                
                return new
                {
                    StatusCode = 500,
                    Message = $"服务器错误: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 保存包装箱标签（ZMER_BZXBQ），支持批量多箱
        /// </summary>
        /// <param name="data">
        /// JSON 字符串，箱号数组（或 {"Data":[...]}）。
        /// 单个包装箱结构：{"FBZXLH":"包装序列号","FEntity":[{"FWLNUM":"物料编码","FXLH":"序列号"}]}
        /// FEntity 为扁平明细，每个序列号一行（一箱对应多个物料、每个物料多个序列号时逐条铺平）。
        /// 示例：
        /// [{"FBZXLH":"BZX001","FEntity":[{"FWLNUM":"WL001","FXLH":"SN001"},{"FWLNUM":"WL001","FXLH":"SN002"}]},
        ///  {"FBZXLH":"BZX002","FEntity":[{"FWLNUM":"WL002","FXLH":"SN003"}]}]
        /// </param>
        public object SaveBZXBQ(string data)
        {
            var ctx = KDContext.Session.AppContext;
            if (ctx == null)
                return new { StatusCode = 401, Message = "超时，请重新登录" };

            if (string.IsNullOrWhiteSpace(data))
                return new { StatusCode = 400, Message = "参数不能为空" };

            try
            {
                // 入参支持 [箱号数组] 或 {"Data":[箱号数组]}
                var root = JToken.Parse(data);
                JArray boxList;
                if (root is JArray arr)
                    boxList = arr;
                else if (root is JObject obj && obj["Data"] is JArray arr2)
                    boxList = arr2;
                else
                    return new { StatusCode = 400, Message = "入参格式错误，应为箱号数组" };

                if (boxList.Count == 0)
                    return new { StatusCode = 400, Message = "箱号列表为空" };

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
                int successCount = 0, failCount = 0;

                foreach (var boxToken in boxList)
                {
                    if (!(boxToken is JObject box))
                        continue;

                    string fbzxLH = box["FBZXLH"]?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(fbzxLH))
                    {
                        failCount++;
                        resultList.Add(new { FBZXLH = "", Success = false, Message = "包装序列号为空" });
                        continue;
                    }

                    // 防重：包装序列号唯一
                    var exist = Tools.GetItemArray(ctx, "ZMER_BZXBQ",
                        $"FBZXLH = '{fbzxLH.Replace("'", "''")}'");
                    if (exist != null && exist.Length > 0)
                    {
                        failCount++;
                        resultList.Add(new { FBZXLH = fbzxLH, Success = false, Message = "该包装序列号已存在" });
                        continue;
                    }

                    var entries = box["FEntity"] as JArray;
                    if (entries == null || entries.Count == 0)
                    {
                        failCount++;
                        resultList.Add(new { FBZXLH = fbzxLH, Success = false, Message = "明细不能为空" });
                        continue;
                    }

                    // 构建单据体：每个序列号一行
                    var entityArray = new JArray();
                    foreach (var e in entries)
                    {
                        if (!(e is JObject entryObj)) continue;
                        entityArray.Add(new JObject
                        {
                            ["FEntryID"] = 0,
                            ["FWLNUM"] = entryObj["FWLNUM"]?.ToString() ?? "",
                            ["FXLH"] = entryObj["FXLH"]?.ToString() ?? ""
                        });
                    }

                    if (entityArray.Count == 0)
                    {
                        failCount++;
                        resultList.Add(new { FBZXLH = fbzxLH, Success = false, Message = "明细解析为空" });
                        continue;
                    }

                    var modelObj = new JObject
                    {
                        ["FID"] = 0,
                        ["FBillNo"] = "",
                        ["FBZXLH"] = fbzxLH,
                        ["F_ZMER_Entity_moz"] = entityArray
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
                        ["ValidateRepeatJson"] = "true",
                        ["Model"] = modelObj
                    };

                    var saveResultJson = client.Save("ZMER_BZXBQ", saveObj.ToString());
                    var saveJObj = JObject.Parse(saveResultJson);
                    bool saveOk = saveJObj["Result"]["ResponseStatus"]["IsSuccess"].Value<bool>();

                    string errMsg = null;
                    if (!saveOk)
                        errMsg = saveJObj["Result"]["ResponseStatus"]["Errors"]?[0]?["Message"]?.ToString() ?? "未知保存错误";

                    string newId = saveJObj["Result"]?["Id"]?.ToString();
                    string newNumber = saveJObj["Result"]?["Number"]?.ToString();

                    if (saveOk) successCount++; else failCount++;

                    resultList.Add(new
                    {
                        FBZXLH = fbzxLH,
                        Success = saveOk,
                        Id = newId,
                        Number = newNumber,
                        Message = saveOk ? "保存成功" : errMsg
                    });
                }

                return new
                {
                    StatusCode = 200,
                    Message = "完成",
                    SuccessCount = successCount,
                    FailCount = failCount,
                    Data = resultList
                };
            }
            catch (Exception ex)
            {
                return new { StatusCode = 500, Message = "服务器错误: " + ex.Message };
            }
        }

        private static string Str(object value)
        {
            return value == null ? "" : Convert.ToString(value);
        }

        private static decimal Dec(object value, int digits = 0)
        {
            if (value == null || value == DBNull.Value) return 0;
            return Math.Round(Convert.ToDecimal(value), digits, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// 读取基础资料字段：优先取编码 Number，其次取内码 Id，非基础资料则取原值
        /// </summary>
        private static string RefNumber(object value)
        {
            if (value == null) return "";
            var dyn = value as DynamicObject;
            if (dyn != null)
            {
                var number = dyn["Number"];
                if (number != null && !string.IsNullOrWhiteSpace(Convert.ToString(number)))
                    return Convert.ToString(number);
                var id = dyn["Id"];
                if (id != null)
                    return Convert.ToString(id);
                return "";
            }
            return Convert.ToString(value);
        }

        /// <summary>
        /// 读取基础资料字段的名称 Name
        /// </summary>
        private static string RefName(object value)
        {
            if (value == null) return "";
            var dyn = value as DynamicObject;
            if (dyn != null)
            {
                var name = dyn["Name"];
                if (name != null && !string.IsNullOrWhiteSpace(Convert.ToString(name)))
                    return Convert.ToString(name);
                return "";
            }
            return Convert.ToString(value);
        }

        /// <summary>
        /// 读取物料基础资料的规格型号 Specification
        /// </summary>
        private static string RefSpec(object value)
        {
            if (value == null) return "";
            var dyn = value as DynamicObject;
            if (dyn != null)
            {
                var spec = dyn["Specification"];
                if (spec != null && !string.IsNullOrWhiteSpace(Convert.ToString(spec)))
                    return Convert.ToString(spec);
                return "";
            }
            return "";
        }

        private static readonly string[] PpbomHeaderFields = new[]
        {
            "FID", "FBillNo", "FDocumentStatus", "FApproverId", "FApproveDate",
            "FModifierId", "FCreateDate", "FCreatorId", "FModifyDate", "FDescription",
            "FMaterialID.FNumber", "FMaterialName", "FMaterialModel", "FWorkshopID",
            "FBOMID.FNumber", "FQty", "FMOBillNO", "FMOEntrySeq", "FMOStatus", "FMOEntryID",
            "FBaseQty", "FMoId", "FBaseUnitID.FNumber", "FMOType.FNumber", "FUnitID.FNumber",
            "FPrdOrgId.FNumber", "FAuxPropIDHead", "FParentOwnerTypeId", "FParentOwnerId.FNumber",
            "FEntrustOrgId.FNumber", "FMoEntryMirror.FNumber", "FMoEntryStatus", "FSaleOrderId",
            "FSaleOrderEntryId", "FSALEORDERNO", "FSaleOrderEntrySeq", "FReqSrc",
            "FInventoryDate", "FGeneRateDate", "FIsQCMo", "F_PAEZ_LLYY.FNumber", "F_PAEZ_SYB"
        };

        private static readonly string[] PpbomBodyFields = new[]
        {
            "FEntryID", "FParentRowId", "FRowExpandType", "FRowId", "FBOMEntryID",
            "FReplaceGroup", "FMaterialID2.FNumber", "FMaterialName1", "FMaterialModel1",
            "FMaterialType", "FTimeUnit2", "FDosageType", "FUseRate", "FScrapRate",
            "FOperID", "FProcessID.FNumber", "FNeedDate2", "FStdQty", "FNeedQty2",
            "FMustQty", "FPickedQty", "FRePickedQty", "FScrapQty", "FGoodReturnQty",
            "FINCDefectReturnQty", "FProcessDefectReturnQty", "FConsumeQty", "FWipQty",
            "FIssueType", "FBackFlushType", "FOverRate", "FStockLOCID", "FStockID.FNumber",
            "FMTONO", "FProjectNO", "FPositionNO", "FSelPickedQty", "FSelRePickedQty",
            "FSelPrcdReturnQty", "FSrcTransOrgId.FNumber", "FSrcTransStockId.FNumber",
            "FSrcTransStockLocId.FNumber", "FSelTranslateQty", "FTranslateQty", "FIsGetScrap",
            "FAllowOver", "FBomId2.FNumber", "FSupplyOrg.FNumber", "FMEMO1", "FBaseStdQty",
            "FBaseNeedQty", "FBaseUnitID1.FNumber", "FBaseMustQty", "FBasePickedQty",
            "FBaseRepickedQty", "FBaseScrapQty", "FBaseGoodReturnQty", "FBaseIncDefectReturnQty",
            "FBasePrcDefectReturnQty", "FBaseConsumeQty", "FBaseWipQty", "FBaseSelGoodReturnQty",
            "FBaseSelIncDefectReturnQty", "FBaseSelPrcDefectReturnQty", "FBaseSelPickedQty",
            "FBaseSelRePickedQty", "FBaseSelPrcdReturnQty", "FBaseSelTranslateQty", "FBaseTranslateQty",
            "FBaseReturnNoOkQty", "FReturnNoOkQty", "FMoType1.FNumber", "FMoBillNo1", "FMoId1",
            "FMoEntryId1", "FMoEntrySeq1", "FUnitID2.FNumber", "FBOMNumerator", "FBOMDenominator",
            "FNumerator", "FDenominator", "FBaseBOMNumerator", "FBaseNumerator", "FAuxPropID",
            "FOwnerID.FNumber", "FOwnerTypeId", "FIsKeyItem", "FFixScrapQty", "FLot.FNumber",
            "FOffsetTime", "FBaseFixScrapQTY", "FBASEDENOMINATOR", "FBASEBOMDENOMINATOR",
            "FIsKeyComponent", "FBFLowId", "FWorkCalId2.FNumber", "FPriority", "FReserveType",
            "FReplacePolicy", "FReplaceType", "FReplacePriority", "FOverControlMode", "FSMId",
            "FSMEntryId", "FBaseStockReadyQty", "FStockReadyQty", "FChildSupplyOrgId.FNumber",
            "FOptQueue", "FStockStatusId", "FEntrustPickOrgId.FNumber", "FIsSkip", "FISMinIssueQty",
            "FPPBomEntryType", "FUPDATERID", "FUPDateDate", "FGroupByOwnerId.FNumber", "FSupplyMode",
            "FBaseNoPickedQty", "FNoPickedQty", "FSelIssueQty", "FBaseSelIssueQty", "FIssueQty",
            "FBaseIssueQty", "FIsMrpRun", "FSrcPPBOMID", "FSrcPPBOMEntryId", "FPathEntryID",
            "FBaseInventoryQty", "FInventoryQty", "FSupplyType", "FSrcPathEntryID", "FReturnQty",
            "FBaseReturnQty", "FIsExpand", "FCheckReturnMtrl", "FBaseReturnAppSelQty", "FReturnAppSelQty",
            "FBillAccuYieldRate", "FMinIssueQty", "FBaseMinIssueQty", "FALLOCATEQTY", "FBASEALLOCATEQTY",
            "FISMODIFYMQ", "FTOPMATERIALID", "FPARENTMATERIALID", "FTOPMTRLNUMBER", "FPARENTMTRLNUMBER",
            "FACTUALPICKQTY", "FBASEACTUALPICKQTY", "FPPBOMChangeFlag", "FNoReCalSubRate",
            "FSrcSpMoPpbomEntryId", "FSrcSpMoPpbomId", "FSrcSpRoPpbomEntryId", "FSrcSpRoPpbomId",
            "FCloseScrapQty", "FBaseCloseScrapQty", "FBasePickingQty", "FPickingQty",
            "F_PAEZ_WLSH", "F_WNQC_XSDDH"
        };

        private static string BuildPpbomFieldKeys()
        {
            var keys = new List<string>();
            keys.AddRange(PpbomHeaderFields);
            foreach (var f in PpbomBodyFields)
                keys.Add("FEntity." + f);
            return string.Join(",", keys);
        }
    }
}

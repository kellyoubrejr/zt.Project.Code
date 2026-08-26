using Kingdee.BOS.App.Data;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.WebApi.Client;
using Kingdee.BOS.WebApi.ServicesStub;
using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
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


        #region 采购需求看板N表同步BPM接口

        /// <summary>
        /// 查询采购需求数据（调用存储过程 pr_purapplicationV1_0）
        /// </summary>
        /// <param name="startDate">开始时间（格式：yyyy-MM）</param>
        /// <param name="endDate">结束时间（格式：yyyy-MM）</param>
        /// <returns>列表数据</returns>
        public object GetPurChaseInfo(string startDate, string endDate)
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

            try
            {
                if (string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate))
                {
                    return new
                    {
                        StatusCode = 400,
                        Message = "开始时间和结束时间不能为空，格式：yyyy-MM"
                    };
                }

                string sql = $"EXEC pr_purapplicationV1_0 '{startDate}','{endDate}'";
                var result = DBUtils.ExecuteDataSet(ctx, sql);

                var dataList = new List<Dictionary<string, object>>();

                if (result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                {
                    DataTable table = result.Tables[0];

                    foreach (DataRow row in table.Rows)
                    {
                        var rowDict = new Dictionary<string, object>();
                        rowDict["buyer"] = row["采购员"] == DBNull.Value ? null : row["采购员"];

                        rowDict["demandTotal"] = row["需求总条数"] == DBNull.Value ? 0 : row["需求总条数"];
                        rowDict["demandOk"] = row["需求满足条数"] == DBNull.Value ? 0 : row["需求满足条数"];
                        rowDict["demandNg"] = row["需求不满足条数"] == DBNull.Value ? 0 : row["需求不满足条数"];
                        rowDict["demandRate"] = row["需求满足率"] == DBNull.Value ? 0 : row["需求满足率"];

                        rowDict["orderOnTime"] = row["下单及时数"] == DBNull.Value ? 0 : row["下单及时数"];
                        rowDict["orderLate"] = row["下单不及时数"] == DBNull.Value ? 0 : row["下单不及时数"];
                        rowDict["orderRate"] = row["下单及时率"] == DBNull.Value ? 0 : row["下单及时率"];

                        rowDict["deliveryOnTime"] = row["交付及时数"] == DBNull.Value ? 0 : row["交付及时数"];
                        rowDict["deliveryLate"] = row["交付不及时数"] == DBNull.Value ? 0 : row["交付不及时数"];
                        rowDict["deliveryRate"] = row["交付及时率"] == DBNull.Value ? 0 : row["交付及时率"];


                        rowDict["purchaseAmountTotal"] = row["采购总金额"] == DBNull.Value ? 0 : row["采购总金额"];
                        rowDict["purchaseCount"] = row["采购条数"] == DBNull.Value ? 0 : row["采购条数"];
                        rowDict["totalAmountAll"] = row["全部总金额"] == DBNull.Value ? 0 : row["全部总金额"];
                        rowDict["totalCountAll"] = row["全部总条数"] == DBNull.Value ? 0 : row["全部总条数"];
                        rowDict["purchaseCountRatio"] = row["条数占比"] == DBNull.Value ? 0 : row["条数占比"];
                        rowDict["purchaseAmountRatio"] = row["金额占比"] == DBNull.Value ? 0 : row["金额占比"];
                        rowDict["returnTotalCount"] = row["采购退料总条数"] == DBNull.Value ? 0 : row["采购退料总条数"];
                        rowDict["returnOnTimeCount"] = row["采购退料及时条数"] == DBNull.Value ? 0 : row["采购退料及时条数"];
                        rowDict["returnDelayCount"] = row["采购退料不及时条数"] == DBNull.Value ? 0 : row["采购退料不及时条数"];
                        rowDict["returnOnTimeRate"] = row["采购退料及时率"] == DBNull.Value ? 0 : row["采购退料及时率"];
                        rowDict["prodReturnTotal"] = row["生产退料总单数"] == DBNull.Value ? 0 : row["生产退料总单数"];
                        rowDict["prodReturnHandled"] = row["生产退料已处理"] == DBNull.Value ? 0 : row["生产退料已处理"];
                        rowDict["prodReturnUnhandled"] = row["生产退料未处理"] == DBNull.Value ? 0 : row["生产退料未处理"];
                        rowDict["prodReturnRate"] = row["生产退料及时率"] == DBNull.Value ? 0 : row["生产退料及时率"];

                        dataList.Add(rowDict);
                    }
                }
                else
                {
                    return new
                    {
                        StatusCode = 200,
                        Data = new List<Dictionary<string, object>>(),
                        ApiName = System.Reflection.MethodBase.GetCurrentMethod().Name
                    };
                }

                return new
                {
                    StatusCode = 200,
                    Data = dataList,
                    ApiName = System.Reflection.MethodBase.GetCurrentMethod().Name
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
        /// 查询供应商付款数据
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public object SupplierFKTOBPMService(string startDate, string endDate)
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

            try
            {
                if (string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate))
                {
                    return new
                    {
                        StatusCode = 400,
                        Message = "开始时间和结束时间不能为空，格式：yyyy-MM"
                    };
                }

                string sql = string.Format($@"/*dialect*/SELECT     S.FNAME as 供应商名称,
                                                SUM(B.FAPPLYAMOUNTFOR)as 申请付款总金额, 
                                                SUM(SUM(B.FAPPLYAMOUNTFOR)) OVER()  as 全部申请付款金额
                                            FROM T_CN_PAYAPPLY A
                                            JOIN T_CN_PAYAPPLYENTRY B ON A.FID = B.FID
                                            JOIN T_BD_SUPPLIER_L S ON A.FRECTUNIT = S.FSUPPLIERID
                                            WHERE
                                                A.FRECTUNITTYPE = 'BD_Supplier'
                                                AND A.FCREATEDATE >= '{startDate}'
                                                AND A.FCREATEDATE < '{endDate}'
                                            GROUP BY
                                                S.FNAME
                                            ORDER BY
                                                申请付款总金额 DESC; ");
                var result = DBUtils.ExecuteDataSet(ctx, sql);

                var dataList = new List<Dictionary<string, object>>();

                if (result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                {
                    DataTable table = result.Tables[0];

                    foreach (DataRow row in table.Rows)
                    {
                        var rowDict = new Dictionary<string, object>();
                        rowDict["supplierName"] = row["供应商名称"] == DBNull.Value ? null : row["供应商名称"];

                        rowDict["amount"] = row["申请付款总金额"] == DBNull.Value ? 0 : row["申请付款总金额"];
                        rowDict["totalAmount"] = row["全部申请付款金额"] == DBNull.Value ? 0 : row["全部申请付款金额"];

                        dataList.Add(rowDict);
                    }
                }
                else
                {
                    return new
                    {
                        StatusCode = 200,
                        Data = new List<Dictionary<string, object>>(),
                        ApiName = System.Reflection.MethodBase.GetCurrentMethod().Name
                    };
                }

                return new
                {
                    StatusCode = 200,
                    Data = dataList,
                    ApiName = System.Reflection.MethodBase.GetCurrentMethod().Name
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
        /// 查询费用项目数据
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public object CostItemsTOBPMService(string startDate, string endDate)
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

            try
            {
                if (string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate))
                {
                    return new
                    {
                        StatusCode = 400,
                        Message = "开始时间和结束时间不能为空，格式：yyyy-MM"
                    };
                }

                string sql = string.Format($@"/*dialect*/SELECT
                                                  E.FNAME AS 费用项目,
                                                  SUM(CASE WHEN B.FSOURCETYPE = 'PUR_PurchaseOrder' THEN B.FAPPLYAMOUNTFOR ELSE 0 END) AS 采购订单,
                                                  SUM(CASE WHEN B.FSOURCETYPE = 'AP_Payable' THEN B.FAPPLYAMOUNTFOR ELSE 0 END) AS 应付单,
                                                  SUM(B.FAPPLYAMOUNTFOR) AS 合计,
                                                  ROUND(
                                                    CASE
                                                      WHEN SUM(B.FAPPLYAMOUNTFOR) = 0 THEN 0
                                                      ELSE(SUM(CASE WHEN B.FSOURCETYPE = 'PUR_PurchaseOrder' THEN B.FAPPLYAMOUNTFOR ELSE 0 END) * 100.0 / SUM(B.FAPPLYAMOUNTFOR))
                                                    END, 2
                                                  ) AS 比例
                                                FROM T_CN_PAYAPPLY A
                                                JOIN T_CN_PAYAPPLYENTRY B ON A.FID = B.FID
                                                JOIN T_BD_EXPENSE_L E ON B.FCOSTID = E.FEXPID
                                                WHERE
                                                  A.FRECTUNITTYPE = 'BD_Supplier'
                                                  AND B.FCOSTID IN(20045, 117490)
                                                  AND B.FSOURCETYPE IN('PUR_PurchaseOrder', 'AP_Payable')
                                                  AND A.FCREATEDATE >= '{startDate}'
                                                  AND A.FCREATEDATE < '{endDate}'
                                                  AND A.FDOCUMENTSTATUS = 'C'
                                                GROUP BY E.FNAME

                                                UNION ALL
                                                SELECT
                                                  '总计' AS 费用项目,
                                                  SUM(CASE WHEN B.FSOURCETYPE = 'PUR_PurchaseOrder' THEN B.FAPPLYAMOUNTFOR ELSE 0 END) AS 采购订单,
                                                  SUM(CASE WHEN B.FSOURCETYPE = 'AP_Payable' THEN B.FAPPLYAMOUNTFOR ELSE 0 END) AS 应付单,
                                                  SUM(B.FAPPLYAMOUNTFOR) AS 合计,
                                                  ROUND(
                                                    CASE
                                                      WHEN SUM(B.FAPPLYAMOUNTFOR) = 0 THEN 0
                                                      ELSE(SUM(CASE WHEN B.FSOURCETYPE = 'PUR_PurchaseOrder' THEN B.FAPPLYAMOUNTFOR ELSE 0 END) * 100.0 / SUM(B.FAPPLYAMOUNTFOR))
                                                    END, 2
                                                  ) AS 比例
                                                FROM T_CN_PAYAPPLY A
                                                JOIN T_CN_PAYAPPLYENTRY B ON A.FID = B.FID
                                                JOIN T_BD_EXPENSE_L E ON B.FCOSTID = E.FEXPID
                                                WHERE
                                                  A.FRECTUNITTYPE = 'BD_Supplier'
                                                  AND B.FCOSTID IN(20045, 117490)
                                                  AND B.FSOURCETYPE IN('PUR_PurchaseOrder', 'AP_Payable')
                                                  AND A.FCREATEDATE >= '{startDate}'
                                                  AND A.FCREATEDATE < '{endDate}'
                                                  AND A.FDOCUMENTSTATUS = 'C'   ");
                var result = DBUtils.ExecuteDataSet(ctx, sql);

                var dataList = new List<Dictionary<string, object>>();

                if (result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                {
                    DataTable table = result.Tables[0];

                    foreach (DataRow row in table.Rows)
                    {
                        var rowDict = new Dictionary<string, object>();
                        rowDict["feeItem"] = row["费用项目"] == DBNull.Value ? null : row["费用项目"];

                        rowDict["purchaseOrderAmount"] = row["采购订单"] == DBNull.Value ? 0 : row["采购订单"];
                        rowDict["apAmount"] = row["应付单"] == DBNull.Value ? 0 : row["应付单"];
                        rowDict["totalAmount"] = row["合计"] == DBNull.Value ? 0 : row["合计"];
                        rowDict["ratio"] = row["比例"] == DBNull.Value ? 0 : row["比例"];

                        dataList.Add(rowDict);
                    }
                }
                else
                {
                    return new
                    {
                        StatusCode = 200,
                        Data = new List<Dictionary<string, object>>(),
                        ApiName = System.Reflection.MethodBase.GetCurrentMethod().Name
                    };
                }

                return new
                {
                    StatusCode = 200,
                    Data = dataList,
                    ApiName = System.Reflection.MethodBase.GetCurrentMethod().Name
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
        /// 采购检验
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public object WXLLTOBPMService(string startDate, string endDate)
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

            try
            {
                if (string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate))
                {
                    return new
                    {
                        StatusCode = 400,
                        Message = "开始时间和结束时间不能为空，格式：yyyy-MM"
                    };
                }

                string sql = string.Format($@"/*dialect*/SELECT
                                                          S.FNAME AS 供应商,
                                                          SUM(B.FINSPECTQTY) AS 检验数量,
                                                          SUM(B.FQUALIFIEDQTY) AS 合格数,
                                                          SUM(B.FUNQUALIFIEDQTY) AS 不合格数,
                                                          (SUM(B.FQUALIFIEDQTY) / SUM(B.FINSPECTQTY)) * 100 AS 合格率,
                                                          (SUM(B.FQUALIFIEDQTY) + SUM(CASE WHEN T1.FDEFPROCESS = 'B' THEN T1.FDEFECTIVEQTY ELSE 0 END)) AS 接收,
                                                          SUM(CASE WHEN T1.FDEFPROCESS = 'B' THEN T1.FDEFECTIVEQTY ELSE 0 END) AS 让步接收数量,
                                                          SUM(CASE WHEN T1.FDEFPROCESS = 'F' THEN T1.FDEFECTIVEQTY ELSE 0 END) AS 判退数量,
                                                          (SUM(B.FQUALIFIEDQTY) + SUM(CASE WHEN T1.FDEFPROCESS = 'B' THEN T1.FDEFECTIVEQTY ELSE 0 END)) / SUM(B.FINSPECTQTY) * 100 AS 接受率
                                                        FROM
                                                          T_QM_INSPECTBILL A
                                                          JOIN T_QM_INSPECTBILLENTRY B ON A.FID = B.FID
                                                          JOIN T_BD_SUPPLIER_L S ON B.FSUPPLIERID = S.FSUPPLIERID
                                                          LEFT JOIN T_QM_DEFECTPROCESSENTRY T1 ON A.FBILLNO = T1.FSOURCEBILLNO
                                                          LEFT JOIN T_QM_DEFECTPROCESS T ON T.FID = T1.FID
                                                            WHERE
                                                         A.FCREATEDATE >= '{startDate}'
                                                  AND A.FCREATEDATE < '{endDate}'
                                                        GROUP BY
                                                          S.FNAME");
                var result = DBUtils.ExecuteDataSet(ctx, sql);

                var dataList = new List<Dictionary<string, object>>();

                if (result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                {
                    DataTable table = result.Tables[0];

                    foreach (DataRow row in table.Rows)
                    {
                        var rowDict = new Dictionary<string, object>();
                        rowDict["supplier"] = row["供应商"] == DBNull.Value ? null : row["供应商"];

                        rowDict["inspectionQty"] = row["检验数量"] == DBNull.Value ? 0 : row["检验数量"];
                        rowDict["qualifiedQty"] = row["合格数"] == DBNull.Value ? 0 : row["合格数"];
                        rowDict["unqualifiedQty"] = row["不合格数"] == DBNull.Value ? 0 : row["不合格数"];
                        rowDict["passRate"] = row["合格率"] == DBNull.Value ? 0 : row["合格率"];
                        rowDict["received"] = row["接收"] == DBNull.Value ? 0 : row["接收"];
                        rowDict["concession"] = row["让步接收数量"] == DBNull.Value ? 0 : row["让步接收数量"];
                        rowDict["rejected"] = row["判退数量"] == DBNull.Value ? 0 : row["判退数量"];
                        rowDict["acceptRate"] = row["接受率"] == DBNull.Value ? 0 : row["接受率"];

                        dataList.Add(rowDict);
                    }
                }
                else
                {
                    return new
                    {
                        StatusCode = 200,
                        Data = new List<Dictionary<string, object>>(),
                        ApiName = System.Reflection.MethodBase.GetCurrentMethod().Name
                    };
                }

                return new
                {
                    StatusCode = 200,
                    Data = dataList,
                    ApiName = System.Reflection.MethodBase.GetCurrentMethod().Name
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
        /// 未下单：采购申请数量-已下推采购订单数量
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public object NoDownOrderTOBPMService(string startDate, string endDate)
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

            try
            {
                if (string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate))
                {
                    return new
                    {
                        StatusCode = 400,
                        Message = "开始时间和结束时间不能为空，格式：yyyy-MM"
                    };
                }

                int reqQty = 0;
                int purQty = 0;

                // ================== 采购申请数量 ==================
                string sql = $@"/*dialect*/
            SELECT COUNT(*) AS QTY 
            FROM T_PUR_Requisition 
            WHERE FAPPLICATIONDATE >= '{startDate}'
              AND FAPPLICATIONDATE < '{endDate}'AND FDOCUMENTSTATUS = 'C'";

                var result = DBUtils.ExecuteDataSet(ctx, sql);

                if (result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                {
                    reqQty = result.Tables[0].Rows[0]["QTY"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(result.Tables[0].Rows[0]["QTY"]);
                }

                // ================== 已下推采购订单数量 ==================
                string sql2 = $@"/*dialect*/
            SELECT COUNT(*) AS cyqty
            FROM T_PUR_Requisition A
            JOIN T_PUR_POORDER P3 ON P3.FID = A.FID
            WHERE A.FAPPLICATIONDATE >= '{startDate}'
              AND A.FAPPLICATIONDATE < '{endDate}'";

                var result2 = DBUtils.ExecuteDataSet(ctx, sql2);

                if (result2.Tables.Count > 0 && result2.Tables[0].Rows.Count > 0)
                {
                    purQty = result2.Tables[0].Rows[0]["cyqty"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(result2.Tables[0].Rows[0]["cyqty"]);
                }

                // ================== 计算差值 ==================
                int diff = reqQty - purQty;

                if (diff < 0)
                {
                    diff = 0;
                }

                // ================== 统一列表结构返回 ==================
                var dataList = new List<Dictionary<string, object>>();

                var rowDict = new Dictionary<string, object>();

                rowDict["diffQty"] = diff;

                dataList.Add(rowDict);

                return new
                {
                    StatusCode = 200,
                    Data = dataList,
                    ApiName = System.Reflection.MethodBase.GetCurrentMethod().Name
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
        /// 未到货：采购订单数量-已下推收料单数量
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public object NoArriveOrderTOBPMService(string startDate, string endDate)
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

            try
            {
                if (string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate))
                {
                    return new
                    {
                        StatusCode = 400,
                        Message = "开始时间和结束时间不能为空，格式：yyyy-MM"
                    };
                }

                int cgQty = 0;
                int slQty = 0;

                // ================== 采购订单数量 ==================
                string sql = $@"/*dialect*/
            SELECT COUNT(*) AS QTY 
            FROM T_PUR_POORDER 
            WHERE FDATE >= '{startDate}'
              AND FDATE < '{endDate}'AND FDOCUMENTSTATUS = 'C'";

                var result = DBUtils.ExecuteDataSet(ctx, sql);

                if (result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                {
                    cgQty = result.Tables[0].Rows[0]["QTY"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(result.Tables[0].Rows[0]["QTY"]);
                }

                string sql2 = $@"/*dialect*/
                                        SELECT COUNT(DISTINCT a.fbillno) AS cyqty
                            FROM T_PUR_Receive a
                            JOIN T_PUR_ReceiveEntry b ON a.fid = b.FID
                            JOIN T_PUR_POORDER C ON B.FSRCBILLNO = C.FBILLNO
                            WHERE a.FDATE >= '{startDate}'
                              AND a.FDATE < '{endDate}'
                              AND C.FDATE >= '{startDate}'
                              AND C.FDATE < '{endDate}'
                              AND a.FDOCUMENTSTATUS <> 'C'
                              AND b.FSRCBILLNO IS NOT NULL
                              AND b.FSRCBILLNO <> ''";

                var result2 = DBUtils.ExecuteDataSet(ctx, sql2);

                if (result2.Tables.Count > 0 && result2.Tables[0].Rows.Count > 0)
                {
                    slQty = result2.Tables[0].Rows[0]["cyqty"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(result2.Tables[0].Rows[0]["cyqty"]);
                }

                // ================== 计算差值 ==================
                int diff = cgQty - slQty;

                if (diff < 0)
                {
                    diff = 0;
                }

                // ================== 统一列表结构返回 ==================
                var dataList = new List<Dictionary<string, object>>();

                var rowDict = new Dictionary<string, object>();

                rowDict["diffQty"] = diff;

                dataList.Add(rowDict);

                return new
                {
                    StatusCode = 200,
                    Data = dataList,
                    ApiName = System.Reflection.MethodBase.GetCurrentMethod().Name
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
        /// 未检验数量：采购订单数量-已下推检验审核数量
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public object ArriveNoQCTOBPMService(string startDate, string endDate)
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

            try
            {
                if (string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate))
                {
                    return new
                    {
                        StatusCode = 400,
                        Message = "开始时间和结束时间不能为空，格式：yyyy-MM"
                    };
                }

                int cgQty = 0;
                int slQty = 0;

                // ================== 收料数量 ==================
                string sql = $@"/*dialect*/
            SELECT COUNT(*) AS QTY 
            FROM T_PUR_Receive 
            WHERE FDATE >= '{startDate}'
              AND FDATE < '{endDate}'AND FDOCUMENTSTATUS = 'C'";

                var result = DBUtils.ExecuteDataSet(ctx, sql);

                if (result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                {
                    cgQty = result.Tables[0].Rows[0]["QTY"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(result.Tables[0].Rows[0]["QTY"]);
                }

                // ================== 已下推检验审核数量 ==================
                string sql2 = $@"/*dialect*/
                                        SELECT COUNT(DISTINCT a.fbillno) AS cyqty
                            FROM T_PUR_Receive a
                            JOIN T_PUR_ReceiveEntry b ON a.fid = b.FID
                            JOIN T_PUR_POORDER C ON B.FSRCBILLNO = C.FBILLNO
                            WHERE a.FDATE >= '{startDate}'
                              AND a.FDATE < '{endDate}'
                              AND C.FDATE >= '{startDate}'
                              AND C.FDATE < '{endDate}'
                              AND a.FDOCUMENTSTATUS = 'C'
                              AND b.FSRCBILLNO IS NOT NULL
                              AND b.FSRCBILLNO <> ''";

                var result2 = DBUtils.ExecuteDataSet(ctx, sql2);

                if (result2.Tables.Count > 0 && result2.Tables[0].Rows.Count > 0)
                {
                    slQty = result2.Tables[0].Rows[0]["cyqty"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(result2.Tables[0].Rows[0]["cyqty"]);
                }

                // ================== 计算差值 ==================
                int diff = cgQty - slQty;

                if (diff < 0)
                {
                    diff = 0;
                }

                // ================== 统一列表结构返回 ==================
                var dataList = new List<Dictionary<string, object>>();

                var rowDict = new Dictionary<string, object>();

                rowDict["diffQty"] = diff;

                dataList.Add(rowDict);

                return new
                {
                    StatusCode = 200,
                    Data = dataList,
                    ApiName = System.Reflection.MethodBase.GetCurrentMethod().Name
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
        /// 未入库数量：采购订单数量-已下推入库审核数量
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public object QCNoInStockTOBPMService(string startDate, string endDate)
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

            try
            {
                if (string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate))
                {
                    return new
                    {
                        StatusCode = 400,
                        Message = "开始时间和结束时间不能为空，格式：yyyy-MM"
                    };
                }

                int cgQty = 0;
                int slQty = 0;

                // ================== 检验数量 ==================
                string sql = $@"/*dialect*/
            SELECT COUNT(*) AS QTY 
            FROM T_PUR_Receive 
            WHERE FDATE >= '{startDate}'
              AND FDATE < '{endDate}'AND FDOCUMENTSTATUS = 'C'";

                var result = DBUtils.ExecuteDataSet(ctx, sql);

                if (result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                {
                    cgQty = result.Tables[0].Rows[0]["QTY"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(result.Tables[0].Rows[0]["QTY"]);
                }

                // ================== 已下推入库审核数量 ==================
                string sql2 = $@"/*dialect*/
                                             SELECT COUNT(DISTINCT a.fbillno) as cyqty
                                            FROM t_STK_InStock a
                                            JOIN T_STK_INSTOCKENTRY b ON a.fid = b.FID
                                            JOIN T_PUR_Receive C ON B.FSRCBILLNO = C.FBILLNO
                                            WHERE a.FDATE >= '{startDate}'
                                              AND a.FDATE < '{endDate}'
                                              AND C.FDATE >= '{startDate}'
                                              AND C.FDATE < '{endDate}'
                                              AND a.FDOCUMENTSTATUS = 'C'
                                              AND b.FSRCBILLNO IS NOT NULL
                                              AND b.FSRCBILLNO <> ''";

                var result2 = DBUtils.ExecuteDataSet(ctx, sql2);

                if (result2.Tables.Count > 0 && result2.Tables[0].Rows.Count > 0)
                {
                    slQty = result2.Tables[0].Rows[0]["cyqty"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(result2.Tables[0].Rows[0]["cyqty"]);
                }

                // ================== 计算差值 ==================
                int diff = cgQty - slQty;

                if (diff < 0)
                {
                    diff = 0;
                }

                // ================== 统一列表结构返回 ==================
                var dataList = new List<Dictionary<string, object>>();

                var rowDict = new Dictionary<string, object>();

                rowDict["diffQty"] = diff;

                dataList.Add(rowDict);


                return new
                {
                    StatusCode = 200,
                    Data = dataList,
                    ApiName = System.Reflection.MethodBase.GetCurrentMethod().Name
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

        #endregion
    }
}

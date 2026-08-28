using Kingdee.BOS.BizTipsInfo;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.WebApi.Client;
using Kingdee.Zitn.Project.Code.conf;
using Kingdee.Zitn.Project.Code.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.UI.WebControls;
using static Kingdee.K3.MFG.App.AppServiceContext;

namespace Kingdee.Zitn.Project.Code.plugin.PRDinstock
{
    [Description("【生产入库审核服务】--客返(KF)直接推送BpmApi；报检(BJD)校验最后一次入库后推送BpmApi")]
    [HotUpdate]
    public class PrdInstockAuditToBPM : AbstractOperationServicePlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("生产入库审核推送BpmApi");

        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            try
            {
                base.AfterExecuteOperationTransaction(e);

                var ids = string.Join(",",
                    e.DataEntitys.Select(o => o[0]));

                _log.Section($"审核开始，FIDs: {ids}");

                var client = new K3CloudApiClient(ErpLogin.K3CloudUrl);
                var loginResult = client.ValidateLogin(
                    ErpLogin.AppId,
                    ErpLogin.UserName,
                    ErpLogin.Password,
                    ErpLogin.Lcid
                );

                _log.WriteLog($"登录结果: {loginResult}");

                var resultType =
                    JObject.Parse(loginResult)["LoginResultType"]
                    .Value<int>();

                if (resultType != 1)
                {
                    _log.WriteLog($"登录失败，LoginResultType={resultType}，跳过推送");
                    return;
                }

                _log.WriteLog("登录成功");

                var query = string.Format(@"
                                            SELECT DISTINCT A.Fysdjh
                                            FROM T_PRD_INSTOCK A
                                            WHERE A.FID IN ({0}) AND A.FDOCUMENTSTATUS = 'C'", ids);

                DynamicObjectCollection collection =
                    DbUtils.ExecuteDynamicObject(
                        this.Context,
                        query);

                if (collection == null || collection.Count == 0)
                {
                    _log.WriteLog($"未查询到运单号，FIDs: {ids}");
                    return;
                }

                _log.WriteLog($"查询到 {collection.Count} 条运单号记录");

                var failedList = new List<string>();

                for (int i = 0; i < collection.Count; i++)
                {
                    string sequenceNo =
                        collection[i]["Fysdjh"]?.ToString();

                    if (string.IsNullOrEmpty(sequenceNo))
                    {
                        _log.WriteLog("运单号为空，跳过");
                        continue;
                    }

                    _log.WriteLog($"[{i + 1}/{collection.Count}] 运单号: {sequenceNo}");

                    if (sequenceNo.StartsWith("BJD"))
                    {
                        // 报检：先校验是否最后一次入库
                        if (!IsLastInstock(sequenceNo))
                        {
                            _log.WriteLog($"报检单 {sequenceNo} 非最后一次入库，跳过推送");
                            continue;
                        }
                        _log.WriteLog($"报检单 {sequenceNo} 为最后一次入库，准备推送");
                    }
                    else if (sequenceNo.StartsWith("KF"))
                    {
                        // 客返：直接推送
                        _log.WriteLog($"客返单 {sequenceNo} 直接推送");
                    }
                    else
                    {
                        _log.WriteLog($"运单号 {sequenceNo} 前缀非 KF/BJD，跳过");
                        continue;
                    }

                    var dataToSend = new
                    {
                        sequenceNo = sequenceNo
                    };

                    string jsonBody =
                        JsonConvert.SerializeObject(dataToSend);

                    string apiUrl =
                        "http://10.0.32.10:8769/api/public/aftersale/noticeSend";

                    string responseText;
                    bool success = CallApi(apiUrl, jsonBody, out responseText);

                    if (success)
                    {
                        _log.WriteLog($"推送成功，运单号: {sequenceNo}，URL:{apiUrl},,,响应: {responseText}");
                    }
                    else
                    {
                        _log.WriteLog($"推送失败，运单号: {sequenceNo}，URL:{apiUrl},,,响应: {responseText}");

                        string pluginUrl = "Kingdee.Zitn.Project.Code.plugin.PRDinstock.PrdInstockAuditToBPM";
                        string operationType = sequenceNo.StartsWith("KF") ? "客返单" : "报检单";
                        string exceptionMessage = string.IsNullOrWhiteSpace(responseText) ? "接口无响应" : responseText;
                        SendMsg.Send($@"🚨【紧急】【生产入库审核】BpmApi推送异常！

                                操作单据：{operationType}
                                单号：{sequenceNo}
                                时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}
                                异常信息：{exceptionMessage}
                                插件：{pluginUrl}
                                提示：请核查kingdeeLog");

                        failedList.Add(sequenceNo);
                    }
                }

                if (failedList.Count > 0)
                {
                    _log.WriteLog($"推送完成，失败 {failedList.Count} 条，运单号: {string.Join(",", failedList)}");

                    var result = new OperateResult();
                    result.Message = string.Format(
                        "生产入库单审核成功，但数据推送BpmApi失败！失败运单号：{0}，请检查日志。",
                        string.Join(",", failedList));
                    this.OperationResult.IsShowMessage = true;
                    this.OperationResult.OperateResult.Add(result);
                }
                else
                {
                    _log.WriteLog("全部推送成功");
                }
            }
            catch (Exception ex)
            {
                _log.Error("审核插件异常");
                _log.Error(ex);
                _log.Error($"完整异常: {ex}");
            }
        }

        /// <summary>
        /// 报检(BJD)：校验云枢单据号对应的生产订单（可能多个），该云枢单据号下所有已审核入库单的物料+实收数量（累加）与生产订单物料+数量完全一致才算最后一次
        /// </summary>
        private bool IsLastInstock(string fysdjh)
        {
            // 1. 云枢单据号 → 生产订单（一个云枢单据号可能对应多个生产订单）
            var moSql = string.Format(
                "SELECT FID, FBILLNO FROM T_PRD_MO WHERE F_PAEZ_TEXT = '{0}'",
                fysdjh);
            var moList = DbUtils.ExecuteDynamicObject(this.Context, moSql);

            if (moList == null || moList.Count == 0)
            {
                _log.WriteLog($"未找到云枢单据号 {fysdjh} 对应的生产订单");
                return false;
            }

            // 2. 遍历每个生产订单，汇总全部物料及数量
            var moMatQty = new Dictionary<long, decimal>();

            for (int m = 0; m < moList.Count; m++)
            {
                string fbillno = moList[m]["FBILLNO"]?.ToString();
                long moFid = Convert.ToInt64(moList[m]["FID"]);

                // 2.1 生产订单的全部物料及数量
                var matSql = string.Format(
                    "SELECT FMATERIALID, FQTY FROM T_PRD_MO A JOIN T_PRD_MOENTRY B ON A.FID = B.FID WHERE A.FID = {0}",
                    moFid);
                var moMats = DbUtils.ExecuteDynamicObject(this.Context, matSql);

                if (moMats == null || moMats.Count == 0)
                {
                    _log.WriteLog($"生产订单 {fbillno} 无物料分录");
                    return false;
                }

                for (int i = 0; i < moMats.Count; i++)
                {
                    long matId = Convert.ToInt64(moMats[i]["FMATERIALID"]);
                    decimal qty = Convert.ToDecimal(moMats[i]["FQTY"]);

                    if (moMatQty.ContainsKey(matId))
                    {
                        moMatQty[matId] += qty;
                    }
                    else
                    {
                        moMatQty[matId] = qty;
                    }
                }
            }

            // 3. 汇总该云枢单据号下所有已审核入库单的物料+实收数量（累加）
            var instockSql = string.Format(@"
SELECT B.FMATERIALID, SUM(B.FREALQTY) AS FREALQTY
FROM T_PRD_INSTOCK A JOIN T_PRD_INSTOCKENTRY B ON A.FID = B.FID
WHERE A.Fysdjh = '{0}' AND A.FDOCUMENTSTATUS = 'C'
GROUP BY B.FMATERIALID", fysdjh);
            var instockMats = DbUtils.ExecuteDynamicObject(this.Context, instockSql);

            var instockMatQty = new Dictionary<long, decimal>();
            if (instockMats != null)
            {
                for (int i = 0; i < instockMats.Count; i++)
                {
                    long matId = Convert.ToInt64(instockMats[i]["FMATERIALID"]);
                    decimal qty = Convert.ToDecimal(instockMats[i]["FREALQTY"]);
                    instockMatQty[matId] = qty;
                }
            }

            // 4. 比对：入库单物料+实收数量 与 生产订单物料+数量 完全一致才算最后一次
            if (instockMatQty.Count != moMatQty.Count)
            {
                _log.WriteLog($"物料种类数不一致：入库单累计 {instockMatQty.Count} 种，生产订单 {moMatQty.Count} 种，非最后一次");
                return false;
            }

            foreach (var kv in moMatQty)
            {
                decimal instockQty;
                if (!instockMatQty.TryGetValue(kv.Key, out instockQty) || instockQty != kv.Value)
                {
                    _log.WriteLog($"物料 {kv.Key} 数量不一致：入库单累计实收 {instockQty}，生产订单 {kv.Value}，非最后一次");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 调用API
        /// </summary>
        private bool CallApi(string url, string jsonData, out string responseText)
        {
            return CallPostApi(url, jsonData, out responseText);
        }

        private bool CallPostApi(
            string url,
            string jsonData,
            out string responseText)
        {
            responseText = "";

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.Headers[
                        HttpRequestHeader.ContentType] =
                        "application/json";

                    byte[] postBytes =
                        Encoding.UTF8.GetBytes(jsonData);

                    byte[] responseBytes =
                        webClient.UploadData(
                            url,
                            "POST",
                            postBytes);

                    responseText =
                        Encoding.UTF8.GetString(responseBytes);

                    return responseText.Contains("success")
                           || responseText.Contains("\"code\":200")
                           || responseText.Contains(@"""errcode"":0")
                           || responseText.Contains("操作成功");
                }
            }
            catch (Exception ex)
            {
                responseText = ex.Message;
                _log.Error($"API调用异常，URL: {url}");
                _log.Error(ex);
                return false;
            }
        }
    }
}

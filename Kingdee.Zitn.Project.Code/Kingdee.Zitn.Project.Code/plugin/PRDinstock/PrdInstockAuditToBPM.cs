using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.WebApi.Client;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using static Kingdee.K3.MFG.App.AppServiceContext;
using Kingdee.Zitn.Project.Code.conf;

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
SELECT Fysdjh
FROM T_PRD_INSTOCK
WHERE FID IN ({0})", ids);

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

                    _log.WriteLog($"[{i + 1}/{collection.Count}] 运单号: {sequenceNo}");

                    if (string.IsNullOrEmpty(sequenceNo))
                    {
                        _log.WriteLog("运单号为空，跳过");
                        continue;
                    }

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
                        //"http://10.0.128.10:8081/api/public/aftersale/noticeSend";

                    string responseText;
                    bool success = CallApi(apiUrl, jsonBody, out responseText);

                    if (success)
                    {
                        _log.WriteLog($"推送成功，运单号: {sequenceNo}，响应: {responseText}");
                    }
                    else
                    {
                        _log.WriteLog($"推送失败，运单号: {sequenceNo}，响应: {responseText}");
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
        /// 报检(BJD)：校验云枢单据号对应的生产订单（可能多个），全部生产订单的物料都已审核入库才算最后一次
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

            // 2. 遍历每个生产订单，任一生产订单未全部入库都不算最后一次
            for (int m = 0; m < moList.Count; m++)
            {
                string fbillno = moList[m]["FBILLNO"]?.ToString();
                long moFid = Convert.ToInt64(moList[m]["FID"]);

                // 2.1 生产订单的全部物料编码（一对多）
                var matSql = string.Format(
                    "SELECT DISTINCT FMATERIALID FROM T_PRD_MOENTRY WHERE FID = {0}",
                    moFid);
                var moMats = DbUtils.ExecuteDynamicObject(this.Context, matSql);

                if (moMats == null || moMats.Count == 0)
                {
                    _log.WriteLog($"生产订单 {fbillno} 无物料分录");
                    return false;
                }

                // 2.2 已审核入库单中，该生产订单已入库的物料
                var recvSql = string.Format(@"
SELECT DISTINCT B.FMATERIALID
FROM T_PRD_INSTOCK H
JOIN T_PRD_INSTOCKENTRY B ON H.FID = B.FID
WHERE H.FDOCUMENTSTATUS = 'C' AND B.FMOBILLNO = '{0}'", fbillno);
                var recvMats = DbUtils.ExecuteDynamicObject(this.Context, recvSql);

                var received = new HashSet<long>();
                if (recvMats != null)
                {
                    for (int i = 0; i < recvMats.Count; i++)
                    {
                        received.Add(Convert.ToInt64(recvMats[i]["FMATERIALID"]));
                    }
                }

                // 2.3 该生产订单物料是否全部已入库
                for (int i = 0; i < moMats.Count; i++)
                {
                    long matId = Convert.ToInt64(moMats[i]["FMATERIALID"]);
                    if (!received.Contains(matId))
                    {
                        _log.WriteLog($"生产订单 {fbillno} 物料 {matId} 尚未入库，非最后一次");
                        return false;
                    }
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

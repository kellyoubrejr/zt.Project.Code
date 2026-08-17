using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.WebApi.Client;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Linq;
using static Kingdee.K3.MFG.App.AppServiceContext;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Kingdee.BOS.Util;
using System.Data;
using System;
using System.IO;
using System.Collections.Generic;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.Zitn.Project.Code.conf;

namespace Kingdee.Zitn.Project.Code.plugin.PrdInstock
{
    [Description("【生产入库单服务】：审核调用BpmApi客返")]
    [HotUpdate]
    public class PrdInstockAuditToBPMKF : AbstractOperationServicePlugIn
    {
        private static readonly string LogPath = @"D:\金蝶自定义日志文件\生产入库单审核推送BpmApi.txt";

        private static void WriteLog(string msg)
        {
            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(LogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}{Environment.NewLine}",
                    Encoding.UTF8);
            }
            catch { }
        }

        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            try
            {
                base.AfterExecuteOperationTransaction(e);

                var ids = string.Join(",",
                    e.DataEntitys.Select(o => o[0]));

                WriteLog($"========== 审核开始，FIDs: {ids} ==========");

                var client = new K3CloudApiClient(ErpLogin.K3CloudUrl);
                var loginResult = client.ValidateLogin(
                    ErpLogin.AppId,
                    ErpLogin.UserName,
                    ErpLogin.Password,
                    ErpLogin.Lcid
                );

                WriteLog($"登录结果: {loginResult}");
                var resultType =
                    JObject.Parse(loginResult)["LoginResultType"]
                    .Value<int>();

                if (resultType != 1)
                {
                    WriteLog($"登录失败，LoginResultType={resultType}，跳过推送");
                    return;
                }

                WriteLog("登录成功");

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
                    WriteLog($"未查询到运单号，FIDs: {ids}");
                    return;
                }

                WriteLog($"查询到 {collection.Count} 条运单号记录");

                var failedList = new List<string>();

                for (int i = 0; i < collection.Count; i++)
                {
                    string sequenceNo =
                        collection[i]["Fysdjh"]?.ToString();

                    WriteLog($"[{i + 1}/{collection.Count}] 开始推送，运单号: {sequenceNo}");

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
                        WriteLog($"推送成功，运单号: {sequenceNo}，响应: {responseText}");
                    }
                    else
                    {
                        WriteLog($"推送失败，运单号: {sequenceNo}，响应: {responseText}");
                        failedList.Add(sequenceNo);
                    }
                }

                if (failedList.Count > 0)
                {
                    WriteLog($"推送完成，失败 {failedList.Count} 条，运单号: {string.Join(",", failedList)}");

                    var result = new OperateResult();
                    result.Message = string.Format(
                        "生产入库单审核成功，但数据推送BpmApi失败！失败运单号：{0}，请检查日志。",
                        string.Join(",", failedList));
                    this.OperationResult.IsShowMessage = true;
                    this.OperationResult.OperateResult.Add(result);
                }
                else
                {
                    WriteLog("全部推送成功");
                }
            }
            catch (Exception ex)
            {
                WriteLog($"审核插件异常: {ex.Message}\r\n{ex.StackTrace}");
            }
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
                WriteLog($"API调用异常，URL: {url}，异常: {ex.Message}");
                return false;
            }
        }
    }
}

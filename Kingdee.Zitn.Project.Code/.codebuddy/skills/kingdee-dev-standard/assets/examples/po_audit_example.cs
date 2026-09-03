using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.Zitn.Project.Code.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Kingdee.Zitn.Project.Code.plugin.PO
{
    /// <summary>
    /// 采购订单审核服务插件示例
    /// 基于实际项目代码模式，展示标准的审核服务实现
    /// </summary>
    [Description("【采购订单审核服务】：采购订单审核，调用bpm接口传值")]
    [HotUpdate]
    public class PoAuditToBPMHTExample : AbstractOperationServicePlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("PoAuditToBPMHTExample");
        
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            
            // 获取选中的单据ID
            var ids = string.Join(",", e.SelectedRows
                .Select(row => row.DataEntity["Id"]?.ToString()));
            
            _log.WriteLog($"开始处理采购订单审核，单据ID：{ids}");
            
            // 检查是否已存在合同号
            bool hasContract = CheckContractNumber(ids);
            if (hasContract)
            {
                throw new KDBusinessException("采购订单已存在合同号，不能推送BPM合同审核签章接口，请检查！", 
                    "采购订单已存在合同号，不能推送BPM合同审核签章接口，请检查！");
            }
            
            // 获取采购订单数据
            var requestList = GetPurchaseOrderData(ids);
            string jsonBody = JsonConvert.SerializeObject(requestList);
            
            // 配置API接口
            string apiUrl = "http://10.0.32.10:8769/api/public/contractAuditSeal/generatePurchaseOrder";
            
            // 记录日志
            WriteLog("========== 开始推送新版 ==========");
            WriteLog($"单据ID: {ids}");
            WriteLog($"请求URL: {apiUrl}");
            WriteLog($"请求数据: {jsonBody}");
            
            // 记录空值字段
            LogEmptyFields(requestList);
            
            // 调用API接口
            bool success = CallPostApi(apiUrl, jsonBody, out string response);
            
            if (!success)
            {
                // 失败处理
                WriteLog($"推送完成！返回信息: {response}");
                WriteLog("========== 推送结束 ==========");
                
                // 发送企微消息通知失败
                try
                {
                    string billNos = GetBillNos(ids);
                    SendMsg.Send($@"?【采购订单】推送BPM合同审核失败！

? 操作单据：采购订单审核
? 单据编号：{billNos}
? 时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}
? 错误信息：{response}
? 接口地址：{apiUrl}

提示：请检查BPM接口状态或联系管理员处理");
                }
                catch (Exception sendEx)
                {
                    WriteLog($"发送企微消息失败: {sendEx.Message}");
                }
            }
            else
            {
                // 成功处理
                WriteLog($"推送完成！返回结果: {response}");
                WriteLog("========== 推送结束 ==========");
                
                // 发送成功消息
                try
                {
                    string billNos = GetBillNos(ids);
                    SendMsg.Send($"?【采购订单】推送BPM合同审核成功！单据编号：{billNos}");
                }
                catch (Exception sendEx)
                {
                    WriteLog($"发送企微消息失败: {sendEx.Message}");
                }
            }
        }
        
        /// <summary>
        /// 检查是否已存在合同号
        /// </summary>
        /// <param name="ids">单据ID</param>
        /// <returns>是否存在合同号</returns>
        private bool CheckContractNumber(string ids)
        {
            try
            {
                var sql = $"SELECT FCGHTH FROM T_PUR_POORDER WHERE FID IN ({ids}) AND FCGHTH IS NOT NULL AND FCGHTH <> ''";
                DynamicObjectCollection result = DBUtils.ExecuteDynamicObject(this.Context, sql);
                return result != null && result.Count > 0;
            }
            catch (Exception ex)
            {
                _log.Error($"检查合同号异常：{ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 获取单据编号
        /// </summary>
        /// <param name="ids">单据ID</param>
        /// <returns>单据编号</returns>
        private string GetBillNos(string ids)
        {
            try
            {
                var sql = $"SELECT FBILLNO FROM T_PUR_POORDER WHERE FID IN ({ids})";
                var result = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (result != null && result.Count > 0)
                {
                    var billNos = result.Select(r => r["FBILLNO"]?.ToString()).Where(n => !string.IsNullOrEmpty(n));
                    return string.Join("、", billNos);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"获取单据编号异常：{ex.Message}", ex);
            }
            return ids;
        }
        
        /// <summary>
        /// 获取采购订单数据
        /// </summary>
        /// <param name="ids">单据ID</param>
        /// <returns>采购订单数据列表</returns>
        private List<Dictionary<string, object>> GetPurchaseOrderData(string ids)
        {
            // 实际实现中，这里会查询数据库获取详细的采购订单数据
            // 包括主表、明细表、付款计划等信息
            
            // 这里返回示例数据
            return new List<Dictionary<string, object>>();
        }
        
        /// <summary>
        /// 安全获取字符串值
        /// </summary>
        private static string SafeStr(DynamicObject obj, string field)
        {
            var val = obj[field];
            if (val == null || val == DBNull.Value) return "";
            return val.ToString();
        }
        
        /// <summary>
        /// 记录日志
        /// </summary>
        private void WriteLog(string message)
        {
            try
            {
                string logPath = @"D:\金蝶自定义日志文件\采购订单审核推送BPM合同.txt";
                string dir = Path.GetDirectoryName(logPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}");
            }
            catch
            {
                // 日志写入失败不抛异常，避免影响主流程
            }
        }
        
        /// <summary>
        /// 记录空值字段
        /// </summary>
        private static void LogEmptyFields(List<Dictionary<string, object>> requestList)
        {
            for (int i = 0; i < requestList.Count; i++)
            {
                var order = requestList[i];
                // 实际实现中会检查各个字段的空值情况
            }
        }
        
        /// <summary>
        /// 调用POST接口
        /// </summary>
        private bool CallPostApi(string url, string jsonData, out string responseText)
        {
            responseText = "";
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = 30000;
                
                byte[] postBytes = Encoding.UTF8.GetBytes(jsonData);
                request.ContentLength = postBytes.Length;
                using (Stream reqStream = request.GetRequestStream())
                {
                    reqStream.Write(postBytes, 0, postBytes.Length);
                }
                
                HttpWebResponse response;
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException webEx)
                {
                    response = (HttpWebResponse)webEx.Response;
                }
                
                if (response == null)
                {
                    responseText = "接口无响应";
                    return false;
                }
                
                int statusCode = (int)response.StatusCode;
                string body;
                using (Stream respStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(respStream, Encoding.UTF8))
                {
                    body = reader.ReadToEnd();
                }
                response.Dispose();
                
                responseText = body;
                return statusCode == 200 && (body.Contains("success") || body.Contains("\"code\":200") || body.Contains("\"code\":0"));
            }
            catch (Exception ex)
            {
                responseText = ex.Message;
                return false;
            }
        }
    }
}
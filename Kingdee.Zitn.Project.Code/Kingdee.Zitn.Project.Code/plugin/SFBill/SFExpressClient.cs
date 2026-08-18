using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Kingdee.Zitn.Project.Code.plugin.SFBill
{
    /// <summary>
    /// 顺丰开放平台 BSP 接口客户端
    /// 通用调用：签名 + form-urlencoded POST，返回统一结果
    /// </summary>
    public static class SFExpressClient
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("顺丰接口");

        /// <summary>下单接口</summary>
        public const string SERVICE_CREATE_ORDER = "EXP_RECE_CREATE_ORDER";
        /// <summary>路由查询接口</summary>
        public const string SERVICE_SEARCH_ROUTES = "EXP_RECE_SEARCH_ROUTES";
        /// <summary>清单运费查询接口</summary>
        public const string SERVICE_QUERY_SFWAYBILL = "EXP_RECE_QUERY_SFWAYBILL";

        /// <summary>
        /// 调用顺丰接口
        /// </summary>
        /// <param name="serviceCode">接口服务代码</param>
        /// <param name="msgDataJson">业务数据报文（JSON 字符串）</param>
        public static SFApiResult Call(string serviceCode, string msgDataJson)
        {
            var result = new SFApiResult();
            try
            {
                string partnerId = SFConfig.PartnerID;
                string checkWord = SFConfig.CheckWord;
                string apiUrl = SFConfig.ApiUrl;
                string requestId = Guid.NewGuid().ToString("N");
                string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();

                // 1. URL 编码 msgData（与签名、传输保持一致）
                string msgDataEncoded = UrlEncode(msgDataJson);

                // 2. 签名：Base64(MD5(urlEncode(msgData) + timestamp + checkWord))
                string toVerify = msgDataEncoded + timestamp + checkWord;
                string md5Hex = Md5Hex(toVerify);
                string msgDigest = UrlEncode(Convert.ToBase64String(Encoding.UTF8.GetBytes(md5Hex)));

                // 3. 组装 form-urlencoded 表单
                var form = new StringBuilder();
                form.Append("partnerID=").Append(UrlEncode(partnerId));
                form.Append("&requestID=").Append(UrlEncode(requestId));
                form.Append("&serviceCode=").Append(UrlEncode(serviceCode));
                form.Append("&timestamp=").Append(timestamp);
                form.Append("&msgDigest=").Append(msgDigest);
                form.Append("&msgData=").Append(msgDataEncoded);

                _log.WriteLog($"调用服务 [{serviceCode}]，requestID={requestId}");
                _log.WriteLog($"请求 msgData：{msgDataJson}");

                string responseText = Post(apiUrl, form.ToString());
                _log.WriteLog($"响应原文：{responseText}");

                result.Raw = responseText;
                result.RequestId = requestId;
                result.RequestMsg = msgDataJson;
                result.ResponseMsg = responseText;

                ParseResponse(responseText, result);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                result.Success = false;
                result.ErrorCode = "";
                result.ErrorMsg = "调用顺丰接口异常：" + ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 解析公共响应。顺丰可能返回两种结构：
        /// 1) 直接 { success, errorCode, errorMsg, msgData }
        /// 2) { apiResultCode, apiResultData: "json字符串" }，其中 apiResultData 里是 1) 的结构
        /// </summary>
        private static void ParseResponse(string responseText, SFApiResult result)
        {
            var root = JObject.Parse(responseText);

            JToken data = root;
            if (root["apiResultData"] != null)
            {
                data = root["apiResultData"].Type == JTokenType.String
                    ? JObject.Parse(root["apiResultData"].Value<string>())
                    : root["apiResultData"];
            }

            result.Success = data["success"]?.Value<bool>() ?? false;
            result.ErrorCode = data["errorCode"]?.Value<string>() ?? "";
            result.ErrorMsg = data["errorMsg"]?.Value<string>() ?? "";
            result.MsgData = data["msgData"] as JObject;
        }

        private static string Post(string url, string formBody)
        {
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                byte[] postBytes = Encoding.UTF8.GetBytes(formBody);
                byte[] respBytes = client.UploadData(url, "POST", postBytes);
                return Encoding.UTF8.GetString(respBytes);
            }
        }

        private static string Md5Hex(string input)
        {
            using (var md5 = MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>
        /// URL 编码（对齐 Java URLEncoder：空格转 +，安全字符 -_.*，其余按 UTF-8 转 %XX 大写）
        /// </summary>
        private static string UrlEncode(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            var sb = new StringBuilder();
            foreach (char c in s)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                    || c == '-' || c == '_' || c == '.' || c == '*')
                {
                    sb.Append(c);
                }
                else if (c == ' ')
                {
                    sb.Append('+');
                }
                else
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(new[] { c });
                    foreach (byte b in bytes)
                        sb.Append('%').Append(b.ToString("X2"));
                }
            }
            return sb.ToString();
        }
    }

    /// <summary>顺丰接口统一返回结果</summary>
    public class SFApiResult
    {
        /// <summary>业务是否成功（success=true 且 errorCode=S0000 时成功）</summary>
        public bool Success { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMsg { get; set; }
        /// <summary>返回的业务数据（msgData）</summary>
        public JObject MsgData { get; set; }
        /// <summary>响应原文</summary>
        public string Raw { get; set; }
        /// <summary>请求唯一号</summary>
        public string RequestId { get; set; }
        /// <summary>请求报文</summary>
        public string RequestMsg { get; set; }
        /// <summary>响应报文</summary>
        public string ResponseMsg { get; set; }
    }
}

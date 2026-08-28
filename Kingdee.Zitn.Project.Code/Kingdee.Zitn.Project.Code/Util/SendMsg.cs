using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;

namespace Kingdee.Zitn.Project.Code.Util
{
    /// <summary>
    /// 企业微信消息发送工具类（通用）。
    /// 用法：SendMsg.Send("要发送的内容");
    /// 配置在 K3 Cloud AppSettings（web.config）：WeCom_CorpId / WeCom_Secret / WeCom_AgentId / WeCom_ToUser
    /// 发送失败仅记录日志，不抛异常，不影响调用方主流程。
    /// </summary>
    public static class SendMsg
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("企业微信消息");

        // access_token 缓存（有效期 7200 秒，提前 200 秒过期）
        private static readonly object _lock = new object();
        private static string _token;
        private static DateTime _tokenExpire = DateTime.MinValue;

        /// <summary>发送文本消息给默认收件人</summary>
        public static void Send(string content)
        {
            Send(content, WeComConfig.ToUser);
        }

        /// <summary>发送文本消息给指定收件人（多个用 | 分隔）</summary>
        public static void Send(string content, string toUser)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                    return;

                string token = GetAccessToken();
                if (string.IsNullOrEmpty(token))
                {
                    _log.Error("获取 access_token 失败，消息未发送");
                    return;
                }

                string url = $"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={token}";

                var postData = new
                {
                    touser = toUser,
                    msgtype = "text",
                    agentid = WeComConfig.AgentId,
                    text = new { content = content },
                    safe = 0
                };
                string jsonContent = JsonConvert.SerializeObject(postData);

                using (var client = new HttpClient())
                using (var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                {
                    var response = client.PostAsync(url, httpContent).Result;
                    string result = response.Content.ReadAsStringAsync().Result;

                    var obj = JObject.Parse(result);
                    int errcode = obj["errcode"]?.Value<int>() ?? -1;
                    if (errcode != 0)
                    {
                        _log.Error($"发送失败，errcode={errcode}，返回：{result}");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("发送企业微信消息异常");
                _log.Error(ex);
            }
        }

        /// <summary>发送“内容 + 异常信息”给默认收件人，用于失败/异常场景</summary>
        public static void Send(string content, Exception ex)
        {
            string msg = content;
            if (ex != null)
            {
                msg += "\r\n异常：" + ex.Message;
            }
            Send(msg);
        }

        private static string GetAccessToken()
        {
            lock (_lock)
            {
                if (!string.IsNullOrEmpty(_token) && DateTime.Now < _tokenExpire)
                    return _token;

                string url = $"https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={WeComConfig.CorpId}&corpsecret={WeComConfig.Secret}";

                using (var client = new HttpClient())
                {
                    string result = client.GetStringAsync(url).Result;
                    var obj = JObject.Parse(result);

                    string token = obj["access_token"]?.ToString();
                    if (string.IsNullOrEmpty(token))
                    {
                        _log.Error($"获取 access_token 失败，返回：{result}");
                        return string.Empty;
                    }

                    _token = token;
                    _tokenExpire = DateTime.Now.AddSeconds(7000);
                    return token;
                }
            }
        }
    }
}

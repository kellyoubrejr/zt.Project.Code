using Kingdee.Zitn.Project.Code.conf;
using Kingdee.Zitn.Project.Code.models;
using Kingdee.Zitn.Project.Code.Util;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Web;

namespace Kingdee.Zitn.Project.Code.Interface.ToOther
{
    /// <summary>
    /// 企业微信消息发送HTTP处理程序 - 无状态，外部系统可直接调用
    /// </summary>
    public class WeComHttpHandler : IHttpHandler
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("企业微信HTTP接口");

        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json;charset=utf-8";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.StatusCode = 200;

            string returnCode = "0000";
            string returnMsg = "成功";
            object data = null;

            try
            {
                // 1. 读取请求体
                string body = ReadBody(context);
                if (string.IsNullOrWhiteSpace(body))
                {
                    returnCode = "1000";
                    returnMsg = "请求报文为空";
                }
                else
                {
                    var root = JObject.Parse(body);
                    string content = (string)root["content"] ?? "";
                    string toUserChinese = (string)root["toUserChinese"] ?? "";

                    // 2. 参数验证
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        returnCode = "1001";
                        returnMsg = "消息内容不能为空";
                    }
                    else if (string.IsNullOrWhiteSpace(toUserChinese))
                    {
                        returnCode = "1002";
                        returnMsg = "收件人不能为空";
                    }
                    else
                    {
                        // 3. 金蝶登录鉴权
                        try
                        {
                            var client = new Kingdee.BOS.WebApi.Client.K3CloudApiClient(ErpLogin.K3CloudUrl);
                            var loginResult = client.ValidateLogin(
                                ErpLogin.AppId,
                                ErpLogin.UserName,
                                ErpLogin.Password,
                                ErpLogin.Lcid
                            );

                            var loginJson = JObject.Parse(loginResult);
                            if (loginJson["LoginResultType"].Value<int>() != 1)
                            {
                                returnCode = "2000";
                                returnMsg = "K3Cloud 登录失败";
                                _log?.Error($"K3Cloud登录失败: {loginResult}");
                            }
                            else
                            {
                                // 4. 获取企微用户名
                                string toUser = WeComUserMapping.GetWeComUser(toUserChinese);
                                if (string.IsNullOrEmpty(toUser))
                                {
                                    returnCode = "3000";
                                    returnMsg = $"未找到收件人 '{toUserChinese}' 的企微用户映射";
                                    data = new
                                    {
                                        SupportedUsers = string.Join("、", WeComUserMapping.UserMap.Keys)
                                    };
                                }
                                else
                                {
                                    // 5. 发送消息
                                    SendMsg.Send(content, toUser);
                                    _log?.WriteLog($"企业微信消息发送成功: {toUserChinese} -> {toUser}");
                                    
                                    returnCode = "0000";
                                    returnMsg = "消息发送成功";
                                    data = new
                                    {
                                        ToUser = toUserChinese,
                                        WeComUser = toUser,
                                        Content = content,
                                        SendTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                                    };
                                }
                            }
                        }
                        catch (Exception loginEx)
                        {
                            returnCode = "2001";
                            returnMsg = $"登录异常: {loginEx.Message}";
                            _log?.Error(loginEx);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                returnCode = "9999";
                returnMsg = $"处理异常: {ex.Message}";
                _log?.Error(ex);
            }

            // 6. 返回结果
            var result = new
            {
                return_code = returnCode,
                return_msg = returnMsg,
                data = data
            };

            context.Response.Write(JObject.FromObject(result).ToString());
            context.Response.End();
        }

        private static string ReadBody(HttpContext context)
        {
            context.Request.InputStream.Position = 0;
            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                return reader.ReadToEnd();
        }
    }
}
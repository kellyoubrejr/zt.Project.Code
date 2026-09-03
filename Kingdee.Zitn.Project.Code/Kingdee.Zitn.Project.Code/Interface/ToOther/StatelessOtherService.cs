using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.WebApi.Client;
using Kingdee.BOS.WebApi.ServicesStub;
using Kingdee.Zitn.Project.Code.conf;
using Kingdee.Zitn.Project.Code.models;
using Kingdee.Zitn.Project.Code.Util;
using Newtonsoft.Json.Linq;
using System;

namespace Kingdee.Zitn.Project.Code.Interface.ToOther
{
    /// <summary>
    /// 无状态其他系统服务接口 - 不依赖金蝶会话，外部系统可直接调用
    /// </summary>
    public class StatelessOtherService : AbstractWebApiBusinessService
    {
        public StatelessOtherService(KDServiceContext context)
            : base(context)
        {
        }

        private static readonly CustomLog.LogWriter _log = CustomLog.For("StatelessOtherService");

        /// <summary>
        /// 发送企业微信消息（无状态版本）
        /// </summary>
        /// <param name="content">消息内容</param>
        /// <param name="toUserChinese">收件人中文姓名（多个用"|"分隔）</param>
        /// <returns>发送结果</returns>
        public object SendWeComMessage(string content, string toUserChinese)
        {
            try
            {
                // 1. 参数验证
                if (string.IsNullOrWhiteSpace(content))
                {
                    return new
                    {
                        StatusCode = 400,
                        Message = "消息内容不能为空"
                    };
                }

                if (string.IsNullOrWhiteSpace(toUserChinese))
                {
                    return new
                    {
                        StatusCode = 400,
                        Message = "收件人不能为空"
                    };
                }

                // 2. 检查金蝶会话上下文
                var ctx = KDContext.Session.AppContext;
                if (ctx == null)
                {
                    // 会话为空，尝试使用K3CloudApiClient登录
                    _log?.WriteLog("金蝶会话为空，尝试K3CloudApiClient登录");
                    try
                    {
                        var client = new K3CloudApiClient(ErpLogin.K3CloudUrl);
                        var loginResult = client.ValidateLogin(
                            ErpLogin.AppId,
                            ErpLogin.UserName,
                            ErpLogin.Password,
                            ErpLogin.Lcid
                        );

                        var loginJson = JObject.Parse(loginResult);
                        if (loginJson["LoginResultType"].Value<int>() != 1)
                        {
                            _log?.Error($"K3Cloud登录失败: {loginResult}");
                            return new
                            {
                                StatusCode = 500,
                                Message = "K3Cloud 登录失败",
                                Data = new { LoginResult = loginResult }
                            };
                        }
                        _log?.WriteLog("K3Cloud登录成功");
                    }
                    catch (Exception loginEx)
                    {
                        _log?.Error($"登录异常: {loginEx.Message}");
                        return new
                        {
                            StatusCode = 500,
                            Message = $"登录异常: {loginEx.Message}"
                        };
                    }
                }

                // 3. 获取企微用户名
                string toUser = WeComUserMapping.GetWeComUser(toUserChinese);
                if (string.IsNullOrEmpty(toUser))
                {
                    return new
                    {
                        StatusCode = 404,
                        Message = $"未找到收件人 '{toUserChinese}' 的企微用户映射",
                        Data = new
                        {
                            SupportedUsers = string.Join("、", WeComUserMapping.UserMap.Keys)
                        }
                    };
                }

                // 4. 发送消息
                SendMsg.Send(content, toUser);
                _log?.WriteLog($"企业微信消息发送成功: {toUserChinese} -> {toUser}");

                return new
                {
                    StatusCode = 200,
                    Message = "消息发送成功",
                    Data = new
                    {
                        ToUser = toUserChinese,
                        WeComUser = toUser,
                        Content = content,
                        SendTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                };
            }
            catch (Exception ex)
            {
                _log?.Error($"发送失败: {ex.Message}");
                return new
                {
                    StatusCode = 500,
                    Message = $"发送失败: {ex.Message}"
                };
            }
        }
    }
}
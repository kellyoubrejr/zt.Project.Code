using System.Configuration;

namespace Kingdee.Zitn.Project.Code.conf
{
    /// <summary>
    /// 企业微信消息配置
    /// 通过 K3 Cloud AppSettings 配置（web.config 的 appSettings 节点）
    /// </summary>
    public static class WeComConfig
    {
        public static string CorpId => Get("WeCom_CorpId", "ww1e34e278cf82b78b");
        public static string Secret => Get("WeCom_Secret", "TMUPsrShG9nEOIQtJtXwm9x_MSHsGTW2XSPTAGLjc2o");
        public static int AgentId   => int.TryParse(Get("WeCom_AgentId", "1000027"), out var v) ? v : 1000027;
        public static string ToUser => Get("WeCom_ToUser", "yangwei|chenxiao");

        private static string Get(string key, string defaultValue)
        {
            return ConfigurationManager.AppSettings[key] ?? defaultValue;
        }
    }
}

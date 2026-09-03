using System.Configuration;

namespace Kingdee.Zitn.Project.Code.conf
{
    /// <summary>
    /// 企业微信消息配置类 - 提供企业微信API所需的配置参数
    /// </summary>
    /// <remarks>
    /// 通过 K3 Cloud AppSettings 配置（web.config 的 appSettings 节点）
    /// 包含企业微信CorpId、Secret、AgentId和接收用户等配置
    /// 使用静态类设计，无需实例化，全局可访问
    /// </remarks>
    public static class WeComConfig
    {
        /// <summary>
        /// 企业微信CorpId - 企业唯一标识
        /// </summary>
        /// <value>
        /// 从配置读取的企业微信CorpId，默认值为"ww1e34e278cf82b78b"
        /// </value>
        public static string CorpId => Get("WeCom_CorpId", "ww1e34e278cf82b78b");
        
        /// <summary>
        /// 企业微信Secret - 应用密钥
        /// </summary>
        /// <value>
        /// 从配置读取的企业微信应用密钥，默认值为"TMUPsrShG9nEOIQtJtXwm9x_MSHsGTW2XSPTAGLjc2o"
        /// </value>
        public static string Secret => Get("WeCom_Secret", "TMUPsrShG9nEOIQtJtXwm9x_MSHsGTW2XSPTAGLjc2o");
        
        /// <summary>
        /// 企业微信AgentId - 应用ID
        /// </summary>
        /// <value>
        /// 从配置读取的企业微信应用ID，默认值为1000027
        /// </value>
        public static int AgentId   => int.TryParse(Get("WeCom_AgentId", "1000027"), out var v) ? v : 1000027;
        
        /// <summary>
        /// 企业微信消息接收用户 - 多个用户用"|"分隔
        /// </summary>
        /// <value>
        /// 从配置读取的消息接收用户列表，默认值为"yangwei|chenxiao"
        /// </value>
        public static string ToUser => Get("WeCom_ToUser", "yangwei|chenxiao");

        /// <summary>
        /// 从配置文件读取配置值
        /// </summary>
        /// <param name="key">配置键名</param>
        /// <param name="defaultValue">默认值，当配置不存在时返回</param>
        /// <returns>
        /// 配置值，如果配置不存在则返回默认值
        /// </returns>
        private static string Get(string key, string defaultValue)
        {
            return ConfigurationManager.AppSettings[key] ?? defaultValue;
        }
    }
}

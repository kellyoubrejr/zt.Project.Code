using System;
using System.Configuration;

namespace Kingdee.Zitn.Project.Code.conf
{
    public static class ErpLogin
    {
        private enum EnvironmentType { DEV, PROD }

        private static EnvironmentType Environment
        {
            get
            {
                #if PROD
                return EnvironmentType.PROD;
                #else
                return EnvironmentType.DEV;
                #endif
            }
        }

        public static string EnvironmentName =>
            Environment == EnvironmentType.DEV ? "DEV" : "PROD";

        /* 开发环境 */
        private static class Dev
        {
            //public static string K3CloudUrl => Get("K3CloudApiUrl", "http://127.0.0.1/k3cloud/");
            //public static string AppId      => Get("K3CloudApiAppId", "6a54a3e5e05b91");
            public static string K3CloudUrl => Get("K3CloudApiUrl", "http://10.0.128.18/k3cloud/");
            public static string AppId      => Get("K3CloudApiAppId", "6a7be2788b72d9");
            public static string UserName   => Get("K3CloudApiUserName", "admin");
            public static string Password   => Get("K3CloudApiPassword", "Flzx3qc!");
            public static int Lcid          => int.TryParse(Get("K3CloudApiLcid", "2052"), out var v) ? v : 2052;
        }

        /* 生产环境 */
        private static class Prod
        {
            public static string K3CloudUrl => Get("K3CloudApiUrl", "http://10.0.32.18/k3cloud/");
            public static string AppId      => Get("K3CloudApiAppId", "688399bec6449e");
            public static string UserName   => Get("K3CloudApiUserName", "admin");
            public static string Password   => Get("K3CloudApiPassword", "Flzx3qc!");
            public static int Lcid          => int.TryParse(Get("K3CloudApiLcid", "2052"), out var v) ? v : 2052;
        }

        /* 对外暴露 */
        public static string K3CloudUrl => Pick(Dev.K3CloudUrl, Prod.K3CloudUrl);
        public static string AppId      => Pick(Dev.AppId, Prod.AppId);
        public static string UserName   => Pick(Dev.UserName, Prod.UserName);
        public static string Password   => Pick(Dev.Password, Prod.Password);
        public static int Lcid          => Pick(Dev.Lcid, Prod.Lcid);

        public static bool IsConfigured =>
            !string.IsNullOrWhiteSpace(K3CloudUrl) &&
            !string.IsNullOrWhiteSpace(AppId) &&
            !string.IsNullOrWhiteSpace(UserName) &&
            !string.IsNullOrWhiteSpace(Password) &&
            Lcid > 0;

        private static T Pick<T>(T dev, T prod) =>
            Environment == EnvironmentType.DEV ? dev : prod;

        private static string Get(string key, string defaultValue)
        {
            return ConfigurationManager.AppSettings[key] ?? defaultValue;
        }
    }
}

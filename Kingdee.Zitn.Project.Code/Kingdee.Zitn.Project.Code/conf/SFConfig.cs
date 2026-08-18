using System;
using System.Configuration;

namespace Kingdee.Zitn.Project.Code.conf
{
    /// <summary>
    /// 顺丰开放平台配置
    /// 通过 K3 Cloud AppSettings 配置，支持 DEV/PROD 环境切换
    /// </summary>
    public static class SFConfig
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

        /* 开发环境（沙箱） */
        private static class Dev
        {
            public static string PartnerID     => Get("SF_PartnerID", "ZTWDZUUBNMV7");
            public static string CheckWord     => Get("SF_CheckWord", "xKwHkfkyuI7QxIsh1mSHQbMTH2VfKz4T");
            public static string ApiUrl        => Get("SF_ApiUrl", "https://sfapi-sbox.sf-express.com/std/service");
            public static string MonthlyCard   => Get("SF_MonthlyCard", "7551234567");

            // 默认寄件人信息
            public static string SenderName     => Get("SF_SenderName", "青岛智腾微电子有限公司");
            public static string SenderPhone    => Get("SF_SenderPhone", "0532-58717259");
            public static string SenderProvince => Get("SF_SenderProvince", "山东省");
            public static string SenderCity     => Get("SF_SenderCity", "青岛市");
            public static string SenderDistrict => Get("SF_SenderDistrict", "城阳高新区");
            public static string SenderAddress  => Get("SF_SenderAddress", "名园路11号");
        }

        /* 生产环境 */
        private static class Prod
        {
            public static string PartnerID     => Get("SF_PartnerID", "ZTWDZUUBNMV7");
            public static string CheckWord     => Get("SF_CheckWord", "Hd42vy0fKqeXSHql3p2teiDSqp7py2eW");
            public static string ApiUrl        => Get("SF_ApiUrl", "https://bspgw.sf-express.com/std/service");
            public static string MonthlyCard   => Get("SF_MonthlyCard", "5325070013");

            public static string SenderName     => Get("SF_SenderName", "青岛智腾微电子有限公司");
            public static string SenderPhone    => Get("SF_SenderPhone", "0532-58717259");
            public static string SenderProvince => Get("SF_SenderProvince", "山东省");
            public static string SenderCity     => Get("SF_SenderCity", "青岛市");
            public static string SenderDistrict => Get("SF_SenderDistrict", "城阳高新区");
            public static string SenderAddress  => Get("SF_SenderAddress", "名园路11号");
        }

        /* 对外暴露 */
        public static string PartnerID     => Pick(Dev.PartnerID, Prod.PartnerID);
        public static string CheckWord     => Pick(Dev.CheckWord, Prod.CheckWord);
        public static string ApiUrl        => Pick(Dev.ApiUrl, Prod.ApiUrl);
        public static string MonthlyCard   => Pick(Dev.MonthlyCard, Prod.MonthlyCard);

        public static string SenderName     => Pick(Dev.SenderName, Prod.SenderName);
        public static string SenderPhone    => Pick(Dev.SenderPhone, Prod.SenderPhone);
        public static string SenderProvince => Pick(Dev.SenderProvince, Prod.SenderProvince);
        public static string SenderCity     => Pick(Dev.SenderCity, Prod.SenderCity);
        public static string SenderDistrict => Pick(Dev.SenderDistrict, Prod.SenderDistrict);
        public static string SenderAddress  => Pick(Dev.SenderAddress, Prod.SenderAddress);

        /// <summary>寄件人完整地址（省+市+区+详细地址，可直接传入顺丰 address 字段）</summary>
        public static string SenderFullAddress => SenderProvince + SenderCity + SenderDistrict + SenderAddress;

        private static T Pick<T>(T dev, T prod) =>
            Environment == EnvironmentType.DEV ? dev : prod;

        private static string Get(string key, string defaultValue)
        {
            return ConfigurationManager.AppSettings[key] ?? defaultValue;
        }
    }
}

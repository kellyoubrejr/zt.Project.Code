using System.Collections.Generic;

namespace Kingdee.Zitn.Project.Code.models
{
    /// <summary>
    /// ERP 数据字典（编码/枚举 → 中文，可复用）
    /// </summary>
    public static class ErpDataDict
    {
        /// <summary>子项类型</summary>
        public static readonly IReadOnlyDictionary<string, string> MaterialType = new Dictionary<string, string>
        {
            { "1", "标准件" },
            { "2", "返还件" },
            { "3", "替代件" }
        };

        /// <summary>发料方式</summary>
        public static readonly IReadOnlyDictionary<string, string> IssueType = new Dictionary<string, string>
        {
            { "1", "直接领料" },
            { "2", "直接倒冲" },
            { "3", "调拨领料" },
            { "4", "调拨倒冲" },
            { "7", "不发料" }
        };

        /// <summary>货主</summary>
        public static readonly IReadOnlyDictionary<string, string> Owner = new Dictionary<string, string>
        {
            { "101", "青岛智腾微电子有限公司" },
            { "1", "青岛智腾科技有限公司" }
        };

        /// <summary>单据状态</summary>
        public static readonly IReadOnlyDictionary<string, string> DocumentStatus = new Dictionary<string, string>
        {
            { "A", "创建" },
            { "B", "审核中" },
            { "C", "审核通过" },
            { "D", "重新审核" }
        };

        /// <summary>事业部</summary>
        public static readonly IReadOnlyDictionary<string, string> BusinessDept = new Dictionary<string, string>
        {
            { "9", "无" },
            { "1", "事业一部" },
            { "2", "事业二部" },
            { "3", "事业三部" },
            { "4", "事业四部" },
            { "11", "事业五部" },
            { "10", "生产中心" },
            { "6", "电源子公司" },
            { "5", "烽行事业部" },
            { "12", "北京子公司" },
            { "13", "系统事业部" },
            { "14", "陀螺事业部" }
        };

        /// <summary>生产订单状态</summary>
        public static readonly IReadOnlyDictionary<string, string> MoStatus = new Dictionary<string, string>
        {
            { "1", "计划" },
            { "2", "计划确认" },
            { "3", "下达" },
            { "4", "开工" },
            { "5", "完工" },
            { "6", "结案" },
            { "7", "结算" }
        };

        /// <summary>
        /// 按字典取中文，取不到返回原值
        /// </summary>
        public static string Map(IReadOnlyDictionary<string, string> map, string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return "";
            return map.TryGetValue(key, out var text) ? text : key;
        }
    }
}

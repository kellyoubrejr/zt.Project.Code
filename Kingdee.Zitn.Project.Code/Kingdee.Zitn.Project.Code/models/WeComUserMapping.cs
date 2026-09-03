using System.Collections.Generic;

namespace Kingdee.Zitn.Project.Code.models
{
    /// <summary>
    /// 企业微信用户映射（中文姓名 → 企微用户名）
    /// </summary>
    public static class WeComUserMapping
    {
        /// <summary>
        /// 用户映射字典（中文姓名 → 企微用户名）
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> UserMap = new Dictionary<string, string>
        {
            { "杨威", "yangwei" },
            { "陈晓", "chenxiao" },
            { "李全", "liquan" }
            // TODO: 后续从金蝶表单读取映射关系，实现动态配置
        };

        /// <summary>
        /// 根据中文姓名获取企微用户名
        /// </summary>
        /// <param name="chineseName">中文姓名</param>
        /// <returns>
        /// 企微用户名，如果映射不存在则返回null
        /// </returns>
        public static string GetWeComUser(string chineseName)
        {
            if (string.IsNullOrWhiteSpace(chineseName))
                return null;

            // 支持多个用户用"|"分隔
            if (chineseName.Contains("|"))
            {
                var names = chineseName.Split('|');
                var users = new List<string>();
                foreach (var name in names)
                {
                    var user = GetSingleUser(name.Trim());
                    if (user == null)
                        return null; // 任何一个用户映射失败则整体失败
                    users.Add(user);
                }
                return string.Join("|", users);
            }

            return GetSingleUser(chineseName);
        }

        /// <summary>
        /// 获取单个用户的企微用户名
        /// </summary>
        /// <param name="chineseName">中文姓名</param>
        /// <returns>企微用户名，如果不存在则返回null</returns>
        private static string GetSingleUser(string chineseName)
        {
            if (string.IsNullOrWhiteSpace(chineseName))
                return null;

            return UserMap.TryGetValue(chineseName, out var wecomUser) ? wecomUser : null;
        }

        /// <summary>
        /// 检查用户映射是否存在
        /// </summary>
        /// <param name="chineseName">中文姓名</param>
        /// <returns>是否存在映射</returns>
        public static bool HasMapping(string chineseName)
        {
            return GetWeComUser(chineseName) != null;
        }
    }
}
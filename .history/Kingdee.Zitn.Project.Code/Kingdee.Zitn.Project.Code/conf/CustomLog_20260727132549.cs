using System;
using System.IO;
using System.Text;

namespace Kingdee.Zitn.Project.Code.conf
{
    public static class CustomLog
    {
        private static readonly object _lock = new object();

        public static string LogPath { get; set; } =
            @"D:\kingdeeLog.txt";

        /// <summary>
        /// 写入普通日志：WriteLog("日志内容")
        /// </summary>
        public static void WriteLog(string message)
        {
            Write(null, message);
        }

        /// <summary>
        /// 写入带标识的日志：WriteLog("销售订单", "审核通过")
        /// </summary>
        public static void WriteLog(string tag, string message)
        {
            Write(tag, message);
        }

        /// <summary>
        /// 写入分段日志（带分隔线）：
        /// ======================== 销售订单 批量审核 ========================
        /// </summary>
        public static void Section(string tag, string message)
        {
            Write(tag, $"======================== {message ?? string.Empty} ========================");
        }

        /// <summary>
        /// 写入异常日志：Error("销售订单", ex)
        /// </summary>
        public static void Error(string tag, string message)
        {
            Write($"ERROR/{tag}", message);
        }

        public static void Error(string tag, Exception ex)
        {
            if (ex == null)
            {
                Write($"ERROR/{tag}", "未知异常");
                return;
            }

            Write($"ERROR/{tag}",
                $"异常：{ex.GetType().FullName}\r\n" +
                $"信息：{ex.Message}\r\n" +
                $"堆栈：{ex.StackTrace}");
        }

        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        /// <summary>
        /// 创建带固定标识的日志实例：var log = CustomLog.For("发货通知单");
        /// </summary>
        public static LogWriter For(string tag)
        {
            return new LogWriter(tag);
        }

        // ? 对外暴露
        public class LogWriter
        {
            private readonly string _tag;

            internal LogWriter(string tag)
            {
                _tag = tag;
            }

            public void WriteLog(string message)
            {
                CustomLog.WriteLog(_tag, message);
            }

            public void Section(string message)
            {
                CustomLog.Section(_tag, message);
            }

            public void Error(string message)
            {
                CustomLog.Error(_tag, message);
            }

            public void Error(Exception ex)
            {
                CustomLog.Error(_tag, ex);
            }

            /// <summary>切换标识，返回新实例（原实例不变）</summary>
            public LogWriter For(string tag)
            {
                return new LogWriter(tag);
            }
        }

        private static void Write(string tag, string message)
        {
            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var tagPart = string.IsNullOrWhiteSpace(tag) ? "" : $"[{tag.Trim()}] ";
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{ErpLogin.EnvironmentName}] {tagPart}{message ?? string.Empty}\r\n";

                lock (_lock)
                    File.AppendAllText(LogPath, line, Encoding.UTF8);
            }
            catch { }
        }
    }
}

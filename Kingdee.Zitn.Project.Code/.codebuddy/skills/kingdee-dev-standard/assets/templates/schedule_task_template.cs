using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.DynamicModel;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.DataAccess;
using Kingdee.BOS.Schedule;

namespace Kingdee.Zitn.Project.Code.Schedule
{
    /// <summary>
    /// 调度任务示例
    /// 继承 IRepairService 实现定时任务
    /// </summary>
    [Description("调度任务描述")]
    [HotUpdate]
    public class MyScheduleTaskTemplate : IRepairService
    {
        /// <summary>
        /// 任务执行入口
        /// </summary>
        /// <param name="ctx">上下文</param>
        public void Execute(Context ctx)
        {
            try
            {
                // 1. 记录开始日志
                WriteLog($"开始执行调度任务：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                // 2. 执行业务逻辑
                ExecuteBusinessLogic(ctx);

                // 3. 记录完成日志
                WriteLog($"调度任务执行完成：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                // 4. 记录异常日志
                WriteError($"调度任务执行异常：{ex.Message}");
                WriteError(ex);

                // 5. 发送异常通知
                SendNotification($"调度任务执行异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行业务逻辑
        /// </summary>
        /// <param name="ctx">上下文</param>
        private void ExecuteBusinessLogic(Context ctx)
        {
            // 实现具体的业务逻辑
            // 示例：处理待审核的单据
            ProcessPendingBills(ctx);
        }

        /// <summary>
        /// 处理待审核的单据
        /// </summary>
        /// <param name="ctx">上下文</param>
        private void ProcessPendingBills(Context ctx)
        {
            // 1. 查询待处理的数据
            string sql = $@"/*dialect*/SELECT FID, FBILLNO, FDOCUMENTSTATUS 
                          FROM T_PURORDER 
                          WHERE FDOCUMENTSTATUS = 'C'  -- 暂存状态
                          AND FDATE < GETDATE() - 7";  -- 超过7天的单据

            var data = DBUtils.ExecuteDynamicObject(ctx, sql);

            if (data != null && data.Count > 0)
            {
                WriteLog($"找到 {data.Count} 条待处理记录");

                foreach (var item in data)
                {
                    try
                    {
                        // 处理每条记录
                        ProcessSingleBill(ctx, item);
                    }
                    catch (Exception ex)
                    {
                        WriteError($"处理单据 {item["FBILLNO"]} 失败：{ex.Message}");
                    }
                }
            }
            else
            {
                WriteLog("没有找到待处理的记录");
            }
        }

        /// <summary>
        /// 处理单个单据
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="billData">单据数据</param>
        private void ProcessSingleBill(Context ctx, DynamicObject billData)
        {
            long fid = Convert.ToInt64(billData["FID"]);
            string billNo = billData["FBILLNO"]?.ToString() ?? "";

            // 1. 获取单据完整信息
            var billInfo = GetBillInfo(ctx, fid);
            if (billInfo == null)
            {
                WriteLog($"单据 {billNo} 不存在，跳过处理");
                return;
            }

            // 2. 执行业务处理
            // 这里实现具体的业务逻辑

            // 3. 记录处理日志
            WriteLog($"成功处理单据：{billNo}");
        }

        /// <summary>
        /// 获取单据信息
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="fid">单据ID</param>
        /// <returns>单据动态对象</returns>
        private DynamicObject GetBillInfo(Context ctx, long fid)
        {
            string sql = $@"/*dialect*/SELECT * FROM T_PURORDER WHERE FID = {fid}";
            var data = DBUtils.ExecuteDynamicObject(ctx, sql);
            return data?.FirstOrDefault();
        }

        #region 日志记录方法

        /// <summary>
        /// 写入普通日志
        /// </summary>
        /// <param name="message">日志消息</param>
        private void WriteLog(string message)
        {
            // 使用项目统一的日志记录方式
            // 这里可以根据实际情况调整
            Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        /// <summary>
        /// 写入错误日志
        /// </summary>
        /// <param name="message">错误消息</param>
        private void WriteError(string message)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        /// <summary>
        /// 写入错误日志（包含异常信息）
        /// </summary>
        /// <param name="ex">异常对象</param>
        private void WriteError(Exception ex)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - 异常：{ex.Message}");
            Console.WriteLine($"[ERROR] 堆栈：{ex.StackTrace}");
        }

        #endregion

        #region 通知方法

        /// <summary>
        /// 发送通知
        /// </summary>
        /// <param name="message">通知消息</param>
        private void SendNotification(string message)
        {
            // 使用项目统一的 SendMsg 发送企业微信通知
            // SendMsg.Send($"【调度任务】{message}");
        }

        /// <summary>
        /// 发送异常通知
        /// </summary>
        /// <param name="message">异常消息</param>
        /// <param name="ex">异常对象</param>
        private void SendNotification(string message, Exception ex)
        {
            // SendMsg.Send($"【调度任务异常】{message}", ex);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 安全获取字符串值
        /// </summary>
        /// <param name="obj">动态对象</param>
        /// <param name="field">字段名</param>
        /// <returns>字符串值</returns>
        private static string SafeStr(DynamicObject obj, string field)
        {
            var val = obj[field];
            if (val == null || val == DBNull.Value) return "";
            return val.ToString();
        }

        /// <summary>
        /// SQL字符串安全处理
        /// </summary>
        /// <param name="s">原始字符串</param>
        /// <returns>安全字符串</returns>
        private static string Sql(string s)
        {
            return string.IsNullOrEmpty(s) ? "" : s.Replace("'", "''");
        }

        #endregion
    }
}
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.Zitn.Project.Code.Util;
using System;
using System.ComponentModel;
using System.Linq;

namespace Kingdee.Zitn.Project.Code.plugin.SFBill
{
    /// <summary>
    /// 销售单审核操作插件示例
    /// 基于实际项目代码模式，展示标准的审核操作实现
    /// </summary>
    [Description("【销售单审核操作】：销售单审核处理")]
    [HotUpdate]
    public class SFBillAuditOpeExample : AbstractOperationServicePlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("SFBillAuditOpeExample");
        
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            
            try
            {
                _log.WriteLog("开始执行销售单审核操作");
                
                // 获取选中的单据ID
                var ids = string.Join(",", e.SelectedRows
                    .Select(row => row.DataEntity["Id"]?.ToString()));
                
                _log.WriteLog($"选中的单据ID：{ids}");
                
                // 执行审核前处理
                ProcessBeforeAudit(ids, e.SelectedRows);
                
                _log.WriteLog("销售单审核前处理完成");
            }
            catch (KDBusinessException ex)
            {
                _log.Error($"业务异常：{ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _log.Error($"系统异常：{ex.Message}", ex);
                throw new KDBusinessException("SYSTEM_ERROR", $"系统异常：{ex.Message}");
            }
        }
        
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            
            try
            {
                _log.WriteLog("开始执行销售单审核后处理");
                
                // 获取选中的单据ID
                var ids = string.Join(",", e.SelectedRows
                    .Select(row => row.DataEntity["Id"]?.ToString()));
                
                // 执行审核后处理
                ProcessAfterAudit(ids, e.SelectedRows);
                
                _log.WriteLog("销售单审核后处理完成");
            }
            catch (Exception ex)
            {
                _log.Error($"审核后处理异常：{ex.Message}", ex);
                // 审核后处理异常不影响主流程
            }
        }
        
        /// <summary>
        /// 处理审核前逻辑
        /// </summary>
        /// <param name="ids">单据ID</param>
        /// <param name="selectedRows">选中的行</param>
        private void ProcessBeforeAudit(string ids, SelectedRowCollection selectedRows)
        {
            try
            {
                _log.WriteLog($"开始审核前处理，单据ID：{ids}");
                
                // 获取销售单信息
                var billInfo = GetSFBillInfo(ids);
                
                // 验证销售单状态
                if (!ValidateSFBillStatus(billInfo))
                {
                    throw new KDBusinessException("销售单状态验证失败", "销售单状态不允许审核");
                }
                
                // 验证销售单数据完整性
                if (!ValidateSFBillData(billInfo))
                {
                    throw new KDBusinessException("销售单数据验证失败", "销售单数据不完整");
                }
                
                // 执行审核前业务逻辑
                ExecuteBeforeAuditLogic(billInfo);
                
                _log.WriteLog("审核前处理完成");
            }
            catch (KDBusinessException ex)
            {
                _log.Error($"审核前处理业务异常：{ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _log.Error($"审核前处理系统异常：{ex.Message}", ex);
                throw new KDBusinessException("SYSTEM_ERROR", $"系统异常：{ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理审核后逻辑
        /// </summary>
        /// <param name="ids">单据ID</param>
        /// <param name="selectedRows">选中的行</param>
        private void ProcessAfterAudit(string ids, SelectedRowCollection selectedRows)
        {
            try
            {
                _log.WriteLog($"开始审核后处理，单据ID：{ids}");
                
                // 获取销售单信息
                var billInfo = GetSFBillInfo(ids);
                
                // 执行审核后业务逻辑
                ExecuteAfterAuditLogic(billInfo);
                
                // 发送审核成功消息
                SendAuditSuccessMessage(billInfo);
                
                _log.WriteLog("审核后处理完成");
            }
            catch (Exception ex)
            {
                _log.Error($"审核后处理异常：{ex.Message}", ex);
                // 审核后处理异常不影响主流程
            }
        }
        
        /// <summary>
        /// 获取销售单信息
        /// </summary>
        /// <param name="ids">单据ID</param>
        /// <returns>销售单信息</returns>
        private DynamicObjectCollection GetSFBillInfo(string ids)
        {
            try
            {
                string sql = $@"/*dialect*/SELECT 
                    FID,
                    FBILLNO,
                    FSTATUS,
                    FSALEORGID,
                    FCUSTID,
                    FDATE
                FROM T_SAL_OUTBOUND
                WHERE FID IN ({ids})";
                
                var result = DBUtils.ExecuteDynamicObject(this.Context, sql);
                _log.WriteLog($"获取到 {result.Count} 条销售单信息");
                
                return result;
            }
            catch (Exception ex)
            {
                _log.Error($"获取销售单信息异常：{ex.Message}", ex);
                throw;
            }
        }
        
        /// <summary>
        /// 验证销售单状态
        /// </summary>
        /// <param name="billInfo">销售单信息</param>
        /// <returns>验证结果</returns>
        private bool ValidateSFBillStatus(DynamicObjectCollection billInfo)
        {
            try
            {
                foreach (var bill in billInfo)
                {
                    string status = bill["FSTATUS"]?.ToString();
                    
                    // 检查状态是否为可审核状态
                    // 假设状态值：0-暂存，1-已保存，2-已审核，3-已关闭
                    if (status != "1") // 只有已保存状态可以审核
                    {
                        _log.WriteLog($"销售单状态不允许审核，当前状态：{status}");
                        return false;
                    }
                }
                
                _log.WriteLog("销售单状态验证通过");
                return true;
            }
            catch (Exception ex)
            {
                _log.Error($"验证销售单状态异常：{ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 验证销售单数据完整性
        /// </summary>
        /// <param name="billInfo">销售单信息</param>
        /// <returns>验证结果</returns>
        private bool ValidateSFBillData(DynamicObjectCollection billInfo)
        {
            try
            {
                foreach (var bill in billInfo)
                {
                    string billId = bill["FID"]?.ToString();
                    string billNo = bill["FBILLNO"]?.ToString();
                    string orgId = bill["FSALEORGID"]?.ToString();
                    string custId = bill["FCUSTID"]?.ToString();
                    string date = bill["FDATE"]?.ToString();
                    
                    // 检查必填字段
                    if (string.IsNullOrEmpty(billId) || 
                        string.IsNullOrEmpty(billNo) ||
                        string.IsNullOrEmpty(orgId) ||
                        string.IsNullOrEmpty(custId) ||
                        string.IsNullOrEmpty(date))
                    {
                        _log.WriteLog($"销售单数据不完整：FID={billId}, FBILLNO={billNo}");
                        return false;
                    }
                    
                    // TODO: 添加更多数据完整性验证
                }
                
                _log.WriteLog("销售单数据验证通过");
                return true;
            }
            catch (Exception ex)
            {
                _log.Error($"验证销售单数据异常：{ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 执行审核前业务逻辑
        /// </summary>
        /// <param name="billInfo">销售单信息</param>
        private void ExecuteBeforeAuditLogic(DynamicObjectCollection billInfo)
        {
            try
            {
                foreach (var bill in billInfo)
                {
                    string billId = bill["FID"]?.ToString();
                    string billNo = bill["FBILLNO"]?.ToString();
                    
                    _log.WriteLog($"执行审核前业务逻辑，销售单：{billNo}");
                    
                    // TODO: 实现具体的审核前业务逻辑
                    // 例如：库存检查、信用检查、价格检查等
                    
                    _log.WriteLog($"审核前业务逻辑执行完成，销售单：{billNo}");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"执行审核前业务逻辑异常：{ex.Message}", ex);
                throw;
            }
        }
        
        /// <summary>
        /// 执行审核后业务逻辑
        /// </summary>
        /// <param name="billInfo">销售单信息</param>
        private void ExecuteAfterAuditLogic(DynamicObjectCollection billInfo)
        {
            try
            {
                foreach (var bill in billInfo)
                {
                    string billId = bill["FID"]?.ToString();
                    string billNo = bill["FBILLNO"]?.ToString();
                    
                    _log.WriteLog($"执行审核后业务逻辑，销售单：{billNo}");
                    
                    // TODO: 实现具体的审核后业务逻辑
                    // 例如：更新库存、生成应收单、发送通知等
                    
                    _log.WriteLog($"审核后业务逻辑执行完成，销售单：{billNo}");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"执行审核后业务逻辑异常：{ex.Message}", ex);
                throw;
            }
        }
        
        /// <summary>
        /// 发送审核成功消息
        /// </summary>
        /// <param name="billInfo">销售单信息</param>
        private void SendAuditSuccessMessage(DynamicObjectCollection billInfo)
        {
            try
            {
                var billNos = billInfo.Select(b => b["FBILLNO"]?.ToString())
                    .Where(n => !string.IsNullOrEmpty(n));
                
                string billNosStr = string.Join("、", billNos);
                
                SendMsg.Send($@"?【销售单】审核成功！

? 操作单据：销售单审核
? 单据编号：{billNosStr}
? 时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}
? 操作人：{this.Context.MKDUserContext.UserName}

提示：销售单审核处理已完成");
                
                _log.WriteLog($"发送审核成功消息完成，单据编号：{billNosStr}");
            }
            catch (Exception ex)
            {
                _log.Error($"发送审核成功消息异常：{ex.Message}", ex);
                // 消息发送失败不影响主流程
            }
        }
        
        /// <summary>
        /// 安全获取字符串值
        /// </summary>
        private static string SafeStr(DynamicObject obj, string field)
        {
            var val = obj[field];
            if (val == null || val == DBNull.Value) return "";
            return val.ToString();
        }
        
        /// <summary>
        /// SQL字符串安全处理
        /// </summary>
        private static string Sql(string s)
        {
            return string.IsNullOrEmpty(s) ? "" : s.Replace("'", "''");
        }
    }
}
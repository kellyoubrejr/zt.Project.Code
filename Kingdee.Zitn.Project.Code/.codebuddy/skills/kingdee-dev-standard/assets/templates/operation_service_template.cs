using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.Zitn.Project.Code.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Kingdee.Zitn.Project.Code.plugin.{MODULE_NAME}
{
    [Description("{DESCRIPTION}")]
    [HotUpdate]
    public class {CLASS_NAME} : AbstractOperationServicePlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("{CLASS_NAME}");
        
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            
            try
            {
                _log.WriteLog("开始执行操作服务");
                
                // 获取选中的单据ID
                var ids = string.Join(",", e.SelectedRows
                    .Select(row => row.DataEntity["Id"]?.ToString()));
                
                _log.WriteLog($"选中的单据ID：{ids}");
                
                // 执行业务逻辑
                ProcessOperation(ids, e.SelectedRows);
                
                _log.WriteLog("操作服务执行完成");
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
            _log.WriteLog("操作事务执行完成");
        }
        
        /// <summary>
        /// 处理操作逻辑
        /// </summary>
        /// <param name="ids">单据ID列表</param>
        /// <param name="selectedRows">选中的行</param>
        private void ProcessOperation(string ids, SelectedRowCollection selectedRows)
        {
            try
            {
                _log.WriteLog($"开始处理单据：{ids}");
                
                // 获取单据信息
                var billInfo = GetBillInfo(ids);
                
                // 数据验证
                if (!ValidateBillInfo(billInfo))
                {
                    throw new KDBusinessException("VALIDATION_ERROR", "单据数据验证失败");
                }
                
                // 执行业务处理
                ExecuteBusinessLogic(billInfo);
                
                // 发送消息通知
                SendMsg.Send($"【{MODULE_NAME}】操作处理完成：{ids}");
                
                _log.WriteLog($"单据处理完成：{ids}");
            }
            catch (KDBusinessException ex)
            {
                _log.Error($"业务处理异常：{ex.Message}");
                SendMsg.Send($"【{MODULE_NAME}】操作处理失败：{ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _log.Error($"处理单据异常：{ids}，错误：{ex.Message}", ex);
                SendMsg.Send($"【{MODULE_NAME}】操作处理失败：{ex.Message}");
                throw new KDBusinessException("SYSTEM_ERROR", $"系统异常：{ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取单据信息
        /// </summary>
        /// <param name="ids">单据ID列表</param>
        /// <returns>单据信息</returns>
        private DynamicObjectCollection GetBillInfo(string ids)
        {
            try
            {
                string sql = $@"/*dialect*/SELECT 
                    FID,
                    FBILLNO,
                    FSTATUS,
                    FCREATEORGID
                FROM T_{MODULE_NAME.ToUpper()}_ORDER
                WHERE FID IN ({ids})";
                
                var result = DBUtils.ExecuteDynamicObject(this.Context, sql);
                _log.WriteLog($"获取到 {result.Count} 条单据信息");
                
                return result;
            }
            catch (Exception ex)
            {
                _log.Error($"获取单据信息异常：{ex.Message}", ex);
                throw;
            }
        }
        
        /// <summary>
        /// 验证单据信息
        /// </summary>
        /// <param name="billInfo">单据信息</param>
        /// <returns>验证结果</returns>
        private bool ValidateBillInfo(DynamicObjectCollection billInfo)
        {
            try
            {
                if (billInfo == null || billInfo.Count == 0)
                {
                    _log.WriteLog("单据信息为空");
                    return false;
                }
                
                foreach (var bill in billInfo)
                {
                    string billId = bill["FID"]?.ToString();
                    string billNo = bill["FBILLNO"]?.ToString();
                    
                    if (string.IsNullOrEmpty(billId) || string.IsNullOrEmpty(billNo))
                    {
                        _log.WriteLog($"单据信息不完整：FID={billId}, FBILLNO={billNo}");
                        return false;
                    }
                    
                    // TODO: 添加更多业务验证逻辑
                }
                
                _log.WriteLog("单据信息验证通过");
                return true;
            }
            catch (Exception ex)
            {
                _log.Error($"验证单据信息异常：{ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 执行业务逻辑
        /// </summary>
        /// <param name="billInfo">单据信息</param>
        private void ExecuteBusinessLogic(DynamicObjectCollection billInfo)
        {
            try
            {
                foreach (var bill in billInfo)
                {
                    string billId = bill["FID"]?.ToString();
                    string billNo = bill["FBILLNO"]?.ToString();
                    
                    _log.WriteLog($"处理单据：{billNo}");
                    
                    // TODO: 实现具体的业务逻辑
                    ProcessSingleBill(billId, billNo, bill);
                    
                    _log.WriteLog($"单据处理完成：{billNo}");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"执行业务逻辑异常：{ex.Message}", ex);
                throw;
            }
        }
        
        /// <summary>
        /// 处理单个单据
        /// </summary>
        /// <param name="billId">单据ID</param>
        /// <param name="billNo">单据编号</param>
        /// <param name="bill">单据数据</param>
        private void ProcessSingleBill(string billId, string billNo, DynamicObject bill)
        {
            try
            {
                _log.WriteLog($"开始处理单个单据：{billNo}");
                
                // TODO: 实现单个单据的处理逻辑
                
                _log.WriteLog($"单个单据处理完成：{billNo}");
            }
            catch (Exception ex)
            {
                _log.Error($"处理单个单据异常：{billNo}，错误：{ex.Message}", ex);
                throw;
            }
        }
        
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
        /// <param name="s">输入字符串</param>
        /// <returns>安全字符串</returns>
        private static string Sql(string s)
        {
            return string.IsNullOrEmpty(s) ? "" : s.Replace("'", "''");
        }
    }
}
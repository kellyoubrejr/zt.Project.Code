using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.Zitn.Project.Code.conf;
using System;
using System.ComponentModel;
using System.Linq;

namespace Kingdee.Zitn.Project.Code.plugin.SFForm
{
    /// <summary>
    /// 【反审核服务Before】顺丰面单反审核时处理。
    ///
    /// 手工录单（状态=已录单）：直接清除状态，无需调顺丰取消接口。
    /// API下单（状态=已下单）：调顺丰取消订单接口 EXP_RECE_UPDATE_ORDER。
    /// </summary>
    [Description("【反审核服务Before】--顺丰面单反审核时取消顺丰订单"), HotUpdate]
    public class SFApiUnAudit : AbstractOperationServicePlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("顺丰面单反审核");

        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);

            var ids = string.Join(",",
                e.DataEntitys.Select(o => o[0]?.ToString() ?? "?"));
            _log.Section($"反审核开始，FIDs: {ids}");

            var client = CreateSFClient();

            foreach (DynamicObject entity in e.DataEntitys)
            {
                ProcessUnAudit(entity, client);
            }
        }

        private void ProcessUnAudit(DynamicObject entity, SFExpressClient client)
        {
            var billNo     = GetStr(entity, "FBillNo");
            var orderId    = GetStr(entity, "FSFOrderId");
            var waybillNo  = GetStr(entity, "FSFWaybillNo");
            var orderStatus = GetStr(entity, "FOrderStatus");

            _log.WriteLog($"处理反审核: {billNo}, 状态: {orderStatus}, waybillNo: {waybillNo}");

            // 手工录单模式：没有 API 下单，直接清除状态即可
            if (orderStatus == "已录单")
            {
                SetVal(entity, "FOrderStatus", "待录单");
                SetVal(entity, "FErrorMessage", "");
                _log.WriteLog($"手工录单，无需调取消接口，状态已重置");
                return;
            }

            // 未下单或下单失败等 → 跳过
            if (orderStatus != "已下单")
            {
                _log.WriteLog($"状态为 {orderStatus}，无需取消，跳过");
                return;
            }

            // API下单模式 → 调取消接口
            if (client == null)
            {
                _log.Error("顺丰配置不完整，跳过取消订单");
                return;
            }

            if (string.IsNullOrEmpty(orderId))
            {
                _log.WriteLog("orderId为空，无法取消，跳过");
                return;
            }

            _log.WriteLog($"开始取消订单, orderId: {orderId}, 运单号: {waybillNo}");

            var result = client.CancelOrder(orderId);

            SetVal(entity, "FLastSyncTime", DateTime.Now);
            SetVal(entity, "FSFResponse", result.RawResponse ?? "");

            if (result.Success)
            {
                SetVal(entity, "FOrderStatus", "已取消");
                SetVal(entity, "FErrorMessage", "");
                _log.WriteLog($"取消成功, orderId: {orderId}");
            }
            else if (result.IsTimeout)
            {
                SetVal(entity, "FOrderStatus", "取消失败");
                SetVal(entity, "FErrorMessage",
                    $"取消请求超时，请手动确认。requestID: {result.RequestID}");
                _log.WriteLog($"取消超时, orderId: {orderId}");
            }
            else
            {
                SetVal(entity, "FOrderStatus", "取消失败");
                SetVal(entity, "FErrorMessage", $"[{result.ErrorCode}] {result.ErrorMsg}");
                _log.Error($"取消失败, orderId: {orderId}, [{result.ErrorCode}] {result.ErrorMsg}");

                var msgResult = new OperateResult();
                msgResult.Message = $"顺丰订单取消失败: [{result.ErrorCode}] {result.ErrorMsg}\r\n请在顺丰平台手动取消";
                this.OperationResult.IsShowMessage = true;
                this.OperationResult.OperateResult.Add(msgResult);
            }
        }

        private SFExpressClient CreateSFClient()
        {
            var partnerID = SFConfig.PartnerID;
            var checkWord = SFConfig.CheckWord;
            var apiUrl   = SFConfig.ApiUrl;

            if (string.IsNullOrEmpty(partnerID) || string.IsNullOrEmpty(checkWord))
                return null;

            return new SFExpressClient(partnerID, checkWord, apiUrl, timeoutMs: 15000, logger: _log);
        }

        private static string GetStr(DynamicObject entity, string field)
        {
            try
            {
                var val = entity[field];
                if (val == null) return null;
                if (val is DynamicObject dynVal)
                    return dynVal["Name"]?.ToString()
                        ?? dynVal["Number"]?.ToString()
                        ?? dynVal.ToString();
                return val.ToString();
            }
            catch { return null; }
        }

        private static void SetVal(DynamicObject entity, string field, object value)
        {
            try { entity[field] = value; }
            catch (Exception ex) { _log.Error($"SetVal失败 field:{field} value:{value} err:{ex.Message}"); }
        }
    }
}

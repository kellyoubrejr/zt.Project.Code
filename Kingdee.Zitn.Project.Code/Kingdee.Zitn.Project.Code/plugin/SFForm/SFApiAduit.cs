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
    /// 【审核服务Before】顺丰面单审核时与顺丰交互。
    ///
    /// 两种模式：
    ///   手工录单 — 表单上已填写运单号（顺丰预给的标签），审核时直接标记为"已录单"。
    ///   API下单 — 表单上无运单号，审核时调顺丰下单接口获取新运单号。
    ///
    /// 判断逻辑：FSFWaybillNo 有值 → 手工录单；无值 → API下单。
    /// </summary>
    [Description("【审核服务Before】--顺丰面单审核之后推送顺丰快递信息"), HotUpdate]
    public class SFApiAduit : AbstractOperationServicePlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("顺丰面单审核");

        /// <summary>
        /// 在执行操作事务前调用，用于处理审核开始前的逻辑
        /// </summary>
        /// <param name="e">BeforeExecuteOperationTransaction 事件参数，包含数据实体等信息</param>
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e); // 调用基类的BeforeExecuteOperationTransaction方法

            var ids = string.Join(",",
                e.DataEntitys.Select(o => o[0]?.ToString() ?? "?"));
            _log.Section($"审核开始，FIDs: {ids}");

            var client = CreateSFClient();
            if (client == null)
            {
                BlockAudit("顺丰配置未完成，请联系管理员配置partnerID和校验码");
                return;
            }

            foreach (DynamicObject entity in e.DataEntitys)
            {
                ProcessEntity(entity, client);
            }
        }

        private void ProcessEntity(DynamicObject entity, SFExpressClient client)
        {
            var billNo    = GetStr(entity, "FBillNo");
            var waybillNo = GetStr(entity, "FSFWaybillNo");
            var orderId   = GetStr(entity, "FSFOrderId");

            _log.WriteLog($"处理单据: {billNo}, waybillNo: {waybillNo}, orderId: {orderId}");

            // 根据运单号是否有值，判断模式
            if (!string.IsNullOrEmpty(waybillNo))
            {
                ProcessManualMode(entity, client, billNo, waybillNo);
            }
            else
            {
                ProcessApiMode(entity, client, billNo, orderId);
            }
        }

        #region 手工录单模式 — 直接标记已录单

        private void ProcessManualMode(DynamicObject entity, SFExpressClient client,
            string billNo, string waybillNo)
        {
            _log.WriteLog($"手工录单模式: 运单号 {waybillNo}，直接标记已录单");

            // 手工录单不调API（无运单号校验接口），直接标记已录单
            SetVal(entity, "FOrderStatus", "已录单");
            SetVal(entity, "FErrorMessage", "");
            SetVal(entity, "FLastSyncTime", DateTime.Now);

            if (string.IsNullOrEmpty(GetStr(entity, "FSFOrderId")))
                SetVal(entity, "FSFOrderId", waybillNo);

            _log.WriteLog($"手工录单完成: {waybillNo}, 状态置为【已录单】");
        }

        #endregion

        #region API下单模式 — 调顺丰创建订单

        private void ProcessApiMode(DynamicObject entity, SFExpressClient client,
            string billNo, string orderId)
        {
            _log.WriteLog($"API下单模式: 创建顺丰订单");

            var sourceBillNo = GetStr(entity, "FSourceBillNo");

            // 生成 orderId
            if (string.IsNullOrEmpty(orderId))
            {
                orderId = GenerateOrderId(billNo);
                SetVal(entity, "FSFOrderId", orderId);
            }

            // 构建请求
            var request = BuildOrderRequest(entity, orderId, sourceBillNo);
            if (request == null)
            {
                BlockAudit($"单据 {billNo}: 下单数据不完整，请检查必填字段");
                return;
            }

            // 调用顺丰下单
            _log.WriteLog($"开始下单, orderId: {orderId}");
            var result = client.CreateOrder(request);

            SetVal(entity, "FRequestId", result.RequestID);
            SetVal(entity, "FSFResponse", result.RawResponse ?? "");
            SetVal(entity, "FLastSyncTime", DateTime.Now);

            if (result.Success && result.Data != null)
            {
                var waybillNo = result.Data.waybillNoInfoList
                    ?.FirstOrDefault()?.waybillNo;

                if (!string.IsNullOrEmpty(waybillNo))
                {
                    SetVal(entity, "FSFWaybillNo", waybillNo);
                    SetVal(entity, "FOrderStatus", "已下单");
                    SetVal(entity, "FErrorMessage", "");
                    _log.WriteLog($"下单成功, orderId: {orderId}, 运单号: {waybillNo}");
                }
                else
                {
                    SetVal(entity, "FOrderStatus", "下单失败");
                    SetVal(entity, "FErrorMessage", "顺丰返回成功但未获取到运单号");
                    _log.Error($"下单异常-无运单号, 返回: {result.RawResponse}");
                    BlockAudit($"单据 {billNo}: 顺丰下单未返回运单号，请检查返回报文");
                }
            }
            else if (result.IsTimeout)
            {
                SetVal(entity, "FOrderStatus", "待确认");
                SetVal(entity, "FErrorMessage",
                    $"请求超时，需手动查询确认。requestID: {result.RequestID}");
                _log.WriteLog($"下单超时, orderId: {orderId}, 状态置为【待确认】");
            }
            else
            {
                SetVal(entity, "FOrderStatus", "下单失败");
                SetVal(entity, "FErrorMessage", $"[{result.ErrorCode}] {result.ErrorMsg}");
                _log.Error($"下单失败, orderId: {orderId}, [{result.ErrorCode}] {result.ErrorMsg}");
                BlockAudit($"单据 {billNo}: 顺丰下单失败\r\n错误代码: {result.ErrorCode}\r\n错误信息: {result.ErrorMsg}\r\n请检查后重新审核");
            }
        }

        #endregion

        #region 构建下单请求（仅 API 模式使用）

        private SFOrderRequest BuildOrderRequest(DynamicObject entity, string orderId, string sourceBillNo)
        {
            var request = new SFOrderRequest
            {
                orderId = orderId,
                language = "zh-CN",
                isDoCall = 1,
                isReturnRoutelabel = 0
            };

            request.monthlyCard = GetStr(entity, "FMonthlyCard")
                ?? SFConfig.MonthlyCard;
            request.expressTypeId = GetInt(entity, "FExpressTypeId", 1);
            request.payMethod = GetInt(entity, "FPayMethod", 1);

            if (string.IsNullOrEmpty(request.monthlyCard))
            {
                SetVal(entity, "FOrderStatus", "下单失败");
                SetVal(entity, "FErrorMessage", "月结卡号为空");
                return null;
            }

            // 寄件人
            request.contactInfoList.Add(new SFContactInfo
            {
                contactType = 1,
                country     = "CN",
                contact     = GetStr(entity, "FSenderName")     ?? SFConfig.SenderName,
                tel         = GetStr(entity, "FSenderPhone")    ?? SFConfig.SenderPhone,
                province    = GetStr(entity, "FSenderProvince") ?? SFConfig.SenderProvince,
                city        = GetStr(entity, "FSenderCity")     ?? SFConfig.SenderCity,
                county      = GetStr(entity, "FSenderDistrict") ?? SFConfig.SenderDistrict,
                address     = GetStr(entity, "FSenderAddress")  ?? SFConfig.SenderAddress
            });

            // 收件人
            var receiverName  = GetStr(entity, "FReceiverName");
            var receiverPhone = GetStr(entity, "FReceiverPhone");
            if (string.IsNullOrEmpty(receiverName) || string.IsNullOrEmpty(receiverPhone))
            {
                SetVal(entity, "FOrderStatus", "下单失败");
                SetVal(entity, "FErrorMessage", "收件人姓名或电话为空");
                return null;
            }

            request.contactInfoList.Add(new SFContactInfo
            {
                contactType = 2,
                country     = "CN",
                contact     = receiverName,
                tel         = receiverPhone,
                province    = GetStr(entity, "FReceiverProvince") ?? "",
                city        = GetStr(entity, "FReceiverCity")     ?? "",
                county      = GetStr(entity, "FReceiverDistrict") ?? "",
                address     = GetStr(entity, "FReceiverAddress")  ?? ""
            });

            // 托寄物
            var cargoName = GetStr(entity, "FCargoName") ?? "商品";
            var quantity  = GetDecimal(entity, "FQuantity", 1);
            var weight    = GetDecimal(entity, "FWeight", 1);
            var unit      = GetStr(entity, "FUnit") ?? "件";

            request.cargoDetails.Add(new SFCargoDetail
            {
                name   = cargoName,
                count  = (int)Math.Ceiling(quantity),
                unit   = unit,
                weight = (double)weight
            });

            request.remark = string.IsNullOrEmpty(sourceBillNo)
                ? "金蝶 K3 Cloud 发货"
                : $"金蝶销售出库单号：{sourceBillNo}";

            return request;
        }

        #endregion

        #region 辅助

        private static string GenerateOrderId(string billNo)
        {
            var ts = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var safeBillNo = (billNo ?? "").Replace(" ", "").Replace("/", "").Replace("\\", "");
            return $"SF{ts}_{safeBillNo}";
        }

        private SFExpressClient CreateSFClient()
        {
            var partnerID = SFConfig.PartnerID;
            var checkWord = SFConfig.CheckWord;
            var apiUrl = SFConfig.ApiUrl;

            if (string.IsNullOrEmpty(partnerID) || string.IsNullOrEmpty(checkWord))
            {
                _log.Error("顺丰配置不完整");
                return null;
            }

            _log.WriteLog($"API地址: {apiUrl}, partnerID: {partnerID}");
            return new SFExpressClient(partnerID, checkWord, apiUrl, timeoutMs: 15000, logger: _log);
        }

        private void BlockAudit(string message)
        {
            var result = new OperateResult();
            result.Message = message;
            this.OperationResult.IsSuccess = false;
            this.OperationResult.IsShowMessage = true;
            this.OperationResult.OperateResult.Add(result);
            _log.Error($"阻塞审核: {message}");
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

        private static int GetInt(DynamicObject entity, string field, int defaultVal)
        {
            return int.TryParse(GetStr(entity, field), out var v) ? v : defaultVal;
        }

        private static decimal GetDecimal(DynamicObject entity, string field, decimal defaultVal)
        {
            return decimal.TryParse(GetStr(entity, field), out var v) ? v : defaultVal;
        }

        private static void SetVal(DynamicObject entity, string field, object value)
        {
            try { entity[field] = value; }
            catch (Exception ex) { _log.Error($"SetVal失败 field:{field} value:{value} err:{ex.Message}"); }
        }

        #endregion
    }
}

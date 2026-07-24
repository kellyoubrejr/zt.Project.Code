using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kingdee.Zitn.Project.Code.plugin.SFForm
{
    /// <summary>
    /// 【单据插件-工具栏按钮】顺丰面单工具栏按钮
    ///
    /// 按钮标识：
    ///   tbSFReOrder    — 重新录单/重新下单
    ///   tbSFQueryOrder — 查询订单状态（API下单超时补偿用）
    ///   tbSFQueryRoute — 查询路由
    ///
    /// 双模式行为：
    ///   手工录单 → 不调用下单/取消API，只做校验和路由查询。
    ///   API下单 → 调用全套下单/取消API。
    /// </summary>
    [Description("【单据插件-工具栏按钮】顺丰面单工具栏操作按钮"), HotUpdate]
    public class SFBillPlugin : AbstractBillPlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("顺丰面单按钮");

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            var key = e.BarItemKey ?? "";
            _log.Section($"按钮点击: {key}");

            try
            {
                if (key.Equals("tbSFReOrder", StringComparison.OrdinalIgnoreCase))
                    DoReOrder();
                else if (key.Equals("tbSFQueryOrder", StringComparison.OrdinalIgnoreCase))
                    DoQueryOrderStatus();
                else if (key.Equals("tbSFQueryRoute", StringComparison.OrdinalIgnoreCase))
                    DoQueryRoute();
            }
            catch (Exception ex)
            {
                _log.Error($"按钮 {key} 异常");
                _log.Error(ex);
                ShowMessage($"操作异常: {ex.Message}");
            }

            _log.Section($"按钮 {key} 结束");
        }

        #region 重新录单/重新下单

        /// <summary>
        /// 清空顺丰数据，重置到初始状态。
        ///   手工录单 → 清空运单号，状态重置为"待录单"
        ///   API下单 → 清空运单号+orderId，状态重置为"待下单"
        /// </summary>
        private void DoReOrder()
        {
            var header = this.View.Model.DataObject;
            var billNo = GetStr(header, "BillNo");
            var oldStatus = GetStr(header, "FOrderStatus");

            _log.WriteLog($"重新录单: {billNo}, 当前状态: {oldStatus}");

            // 清空运单号和错误
            SetVal(header, "FSFWaybillNo", "");
            SetVal(header, "FSFResponse", "");
            SetVal(header, "FErrorMessage", "");
            SetVal(header, "FRequestId", "");
            SetVal(header, "FLastSyncTime", DateTime.Now);

            // 判断原模式
            if (oldStatus == "已录单")
            {
                // 手工录单 → 重置
                SetVal(header, "FOrderStatus", "待录单");
                _log.WriteLog("手工录单模式 → 重置为待录单");
                this.View.UpdateView();
                ShowMessage("已重置，请输入新的运单号后重新审核。");
            }
            else
            {
                // API下单 → 重新生成 orderId
                var newOrderId = GenerateOrderId(billNo);
                SetVal(header, "FSFOrderId", newOrderId);
                SetVal(header, "FOrderStatus", "待下单");
                _log.WriteLog($"API下单模式 → 新orderId: {newOrderId}");
                this.View.UpdateView();
                ShowMessage($"已重置为待下单状态。\r\n新订单号: {newOrderId}\r\n审核时将自动调顺丰下单。");
            }
        }

        #endregion

        #region 查询订单状态（仅 API 模式超时补偿）

        private void DoQueryOrderStatus()
        {
            var header = this.View.Model.DataObject;
            var billNo = GetStr(header, "BillNo");
            var orderId = GetStr(header, "FSFOrderId");
            var orderStatus = GetStr(header, "FOrderStatus");

            _log.WriteLog($"查询订单状态: {billNo}, orderId: {orderId}, 状态: {orderStatus}");

            if (orderStatus == "已录单")
            {
                ShowMessage("当前为手工录单模式，运单号已存在，无需查询订单状态。");
                return;
            }

            if (string.IsNullOrEmpty(orderId))
            {
                ShowMessage("orderId 为空，无法查询。请先审核生成订单。");
                return;
            }

            var client = CreateSFClient();
            if (client == null)
            {
                ShowMessage("顺丰配置不完整");
                return;
            }

            _log.WriteLog($"调用 EXP_RECE_SEARCH_ORDER_RESP, orderId: {orderId}");
            var result = client.SearchOrderResp(orderId);

            SetVal(header, "FSFResponse", result.RawResponse ?? "");
            SetVal(header, "FLastSyncTime", DateTime.Now);

            if (result.Success && result.Data?.waybillNoInfoList != null
                && result.Data.waybillNoInfoList.Count > 0)
            {
                var waybillNo = result.Data.waybillNoInfoList[0].waybillNo;
                SetVal(header, "FSFWaybillNo", waybillNo);
                SetVal(header, "FOrderStatus", "已下单");
                SetVal(header, "FErrorMessage", "");
                _log.WriteLog($"订单已生成, 运单号: {waybillNo}");
                this.View.UpdateView();
                ShowMessage($"订单已生成！\r\n运单号: {waybillNo}\r\n订单状态: {result.Data.orderStateName}");
            }
            else if (result.IsTimeout)
            {
                ShowMessage($"查询超时，请稍后再试。\r\nrequestID: {result.RequestID}");
            }
            else
            {
                ShowMessage($"订单未生成: [{result.ErrorCode}] {result.ErrorMsg}\r\n如需重试请点击【重新下单】。");
            }
        }

        #endregion

        #region 查询路由（双模式通用）

        private void DoQueryRoute()
        {
            var header = this.View.Model.DataObject;
            var billNo = GetStr(header, "BillNo");
            var waybillNo = GetStr(header, "FSFWaybillNo");
            var orderStatus = GetStr(header, "FOrderStatus");

            _log.WriteLog($"查询路由: {billNo}, 运单号: {waybillNo}");

            if (string.IsNullOrEmpty(waybillNo))
            {
                ShowMessage("暂无运单号，无法查询路由。请先录入运单号或下单。");
                return;
            }

            var client = CreateSFClient();
            if (client == null)
            {
                ShowMessage("顺丰配置不完整");
                return;
            }

            var result = client.SearchRoutesByWaybill(waybillNo);

            SetVal(header, "FLastSyncTime", DateTime.Now);

            if (result.Success && result.Data?.routeResps != null
                && result.Data.routeResps.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"运单号: {waybillNo}");

                var allRoutes = result.Data.routeResps
                    .SelectMany(r => r.routes ?? new System.Collections.Generic.List<SFRouteNode>())
                    .OrderBy(r => r.acceptTime)
                    .ToList();

                if (allRoutes.Count == 0)
                {
                    sb.AppendLine("暂无路由信息，包裹可能尚未揽收。");
                }
                else
                {
                    sb.AppendLine();
                    foreach (var node in allRoutes)
                    {
                        sb.AppendLine($"[{node.acceptTime}] {node.remark}");
                        sb.AppendLine($"  ({node.opName}) {node.acceptAddress}");
                        sb.AppendLine();
                    }
                }

                SetVal(header, "FSFResponse",
                    JsonConvert.SerializeObject(result.Data));
                this.View.UpdateView();
                _log.WriteLog($"路由查询成功, 轨迹数: {allRoutes.Count}");
                ShowMessage(sb.ToString());
            }
            else if (result.IsTimeout)
            {
                ShowMessage("路由查询超时，请稍后再试");
            }
            else
            {
                ShowMessage($"路由查询失败: [{result.ErrorCode}] {result.ErrorMsg}");
            }
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
                return null;

            return new SFExpressClient(partnerID, checkWord, apiUrl, timeoutMs: 15000, logger: _log);
        }

        private void ShowMessage(string msg)
        {
            this.View.ShowMessage(msg);
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

        #endregion
    }
}

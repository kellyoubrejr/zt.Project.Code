using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text;

namespace Kingdee.Zitn.Project.Code.plugin.PRDinstock
{
    [Description("【生产入库表单服务】【按钮】--客返(KF)直接推送BpmApi；报检(BJD)校验最后一次入库后推送BpmApi")]
    [HotUpdate]
    public class PrdInstockBtnToBPM : AbstractDynamicFormPlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("生产入库按钮补偿推送BpmApi");

        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);

            if (!e.BarItemKey.Equals("bpmapi_btn", StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                string sequenceNo = this.View.Model.GetValue("Fysdjh")?.ToString();

                if (string.IsNullOrEmpty(sequenceNo))
                {
                    this.View.ShowErrMessage("未获取到有效的云枢单据号，操作已中止。");
                    return;
                }

                _log.Section($"按钮补偿开始，运单号: {sequenceNo}");

                if (sequenceNo.StartsWith("BJD"))
                {
                    // 报检：先校验是否最后一次入库
                    if (!IsLastInstock(sequenceNo))
                    {
                        _log.WriteLog($"报检单 {sequenceNo} 非最后一次入库，跳过推送");
                        this.View.ShowMessage("报检单非最后一次入库，未推送BpmApi。");
                        return;
                    }
                    _log.WriteLog($"报检单 {sequenceNo} 为最后一次入库，准备推送");
                }
                else if (sequenceNo.StartsWith("KF"))
                {
                    // 客返：直接推送
                    _log.WriteLog($"客返单 {sequenceNo} 直接推送");
                }
                else
                {
                    _log.WriteLog($"运单号 {sequenceNo} 前缀非 KF/BJD，跳过推送");
                    this.View.ShowMessage($"运单号 {sequenceNo} 前缀非 KF/BJD，未推送BpmApi。");
                    return;
                }

                var dataToSend = new
                {
                    sequenceNo = sequenceNo
                };

                string jsonBody = JsonConvert.SerializeObject(dataToSend);

                string apiUrl =
                        //"http://10.0.32.10:8769/api/public/aftersale/noticeSend";
                        "http://10.0.128.10:8081/api/public/aftersale/noticeSend";

                string responseText;
                bool success = CallApi(apiUrl, jsonBody, out responseText);

                if (success)
                {
                    _log.WriteLog($"推送成功，运单号: {sequenceNo}，响应: {responseText}");
                    this.View.ShowMessage("推送BpmApi成功！");
                }
                else
                {
                    _log.WriteLog($"推送失败，运单号: {sequenceNo}，响应: {responseText}");
                    this.View.ShowErrMessage($"接口调用失败！\n返回信息：{responseText}");
                }
            }
            catch (Exception ex)
            {
                _log.Error("按钮补偿插件异常");
                _log.Error(ex);
                this.View.ShowErrMessage($"系统异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 报检(BJD)：校验云枢单据号对应的生产订单（可能多个），该云枢单据号下所有已审核入库单的物料+实收数量（累加）与生产订单物料+数量完全一致才算最后一次
        /// </summary>
        private bool IsLastInstock(string fysdjh)
        {
            // 1. 云枢单据号 → 生产订单（一个云枢单据号可能对应多个生产订单）
            var moSql = string.Format(
                "SELECT FID, FBILLNO FROM T_PRD_MO WHERE F_PAEZ_TEXT = '{0}'",
                fysdjh);
            var moList = DBUtils.ExecuteDynamicObject(this.Context, moSql);

            if (moList == null || moList.Count == 0)
            {
                _log.WriteLog($"未找到云枢单据号 {fysdjh} 对应的生产订单");
                return false;
            }

            // 2. 遍历每个生产订单，汇总全部物料及数量
            var moMatQty = new Dictionary<long, decimal>();

            for (int m = 0; m < moList.Count; m++)
            {
                string fbillno = moList[m]["FBILLNO"]?.ToString();
                long moFid = Convert.ToInt64(moList[m]["FID"]);

                // 2.1 生产订单的全部物料及数量
                var matSql = string.Format(
                    "SELECT FMATERIALID, FQTY FROM T_PRD_MO A JOIN T_PRD_MOENTRY B ON A.FID = B.FID WHERE A.FID = {0}",
                    moFid);
                var moMats = DBUtils.ExecuteDynamicObject(this.Context, matSql);

                if (moMats == null || moMats.Count == 0)
                {
                    _log.WriteLog($"生产订单 {fbillno} 无物料分录");
                    return false;
                }

                for (int i = 0; i < moMats.Count; i++)
                {
                    long matId = Convert.ToInt64(moMats[i]["FMATERIALID"]);
                    decimal qty = Convert.ToDecimal(moMats[i]["FQTY"]);

                    if (moMatQty.ContainsKey(matId))
                    {
                        moMatQty[matId] += qty;
                    }
                    else
                    {
                        moMatQty[matId] = qty;
                    }
                }
            }

            // 3. 汇总该云枢单据号下所有已审核入库单的物料+实收数量（累加）
            var instockSql = string.Format(@"
SELECT B.FMATERIALID, SUM(B.FREALQTY) AS FREALQTY
FROM T_PRD_INSTOCK A JOIN T_PRD_INSTOCKENTRY B ON A.FID = B.FID
WHERE A.Fysdjh = '{0}' AND A.FDOCUMENTSTATUS = 'C'
GROUP BY B.FMATERIALID", fysdjh);
            var instockMats = DBUtils.ExecuteDynamicObject(this.Context, instockSql);

            var instockMatQty = new Dictionary<long, decimal>();
            if (instockMats != null)
            {
                for (int i = 0; i < instockMats.Count; i++)
                {
                    long matId = Convert.ToInt64(instockMats[i]["FMATERIALID"]);
                    decimal qty = Convert.ToDecimal(instockMats[i]["FREALQTY"]);
                    instockMatQty[matId] = qty;
                }
            }

            // 4. 比对：入库单物料+实收数量 与 生产订单物料+数量 完全一致才算最后一次
            if (instockMatQty.Count != moMatQty.Count)
            {
                _log.WriteLog($"物料种类数不一致：入库单累计 {instockMatQty.Count} 种，生产订单 {moMatQty.Count} 种，非最后一次");
                return false;
            }

            foreach (var kv in moMatQty)
            {
                decimal instockQty;
                if (!instockMatQty.TryGetValue(kv.Key, out instockQty) || instockQty != kv.Value)
                {
                    _log.WriteLog($"物料 {kv.Key} 数量不一致：入库单累计实收 {instockQty}，生产订单 {kv.Value}，非最后一次");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 调用API
        /// </summary>
        private bool CallApi(string url, string jsonData, out string responseText)
        {
            return CallPostApi(url, jsonData, out responseText);
        }

        private bool CallPostApi(
            string url,
            string jsonData,
            out string responseText)
        {
            responseText = "";

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.Headers[
                        HttpRequestHeader.ContentType] =
                        "application/json";

                    byte[] postBytes =
                        Encoding.UTF8.GetBytes(jsonData);

                    byte[] responseBytes =
                        webClient.UploadData(
                            url,
                            "POST",
                            postBytes);

                    responseText =
                        Encoding.UTF8.GetString(responseBytes);

                    return responseText.Contains("success")
                           || responseText.Contains("\"code\":200")
                           || responseText.Contains(@"""errcode"":0")
                           || responseText.Contains("操作成功");
                }
            }
            catch (Exception ex)
            {
                responseText = ex.Message;
                _log.Error($"API调用异常，URL: {url}");
                _log.Error(ex);
                return false;
            }
        }
    }
}

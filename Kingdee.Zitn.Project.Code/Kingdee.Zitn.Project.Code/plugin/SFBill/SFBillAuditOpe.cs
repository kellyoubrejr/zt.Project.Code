using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.plugin.SFBill
{
    [Description("【审核服务After】--物流面单审核之后推送顺丰下单（带单号下单）"), HotUpdate]
    public class SFBillAuditOpe : AbstractOperationServicePlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("物流面单审核");

        // 单据物理表（BOS 自定义单据）
        private const string HEAD_TABLE = "ZMER_t_Cust100030";      // 单据头
        private const string BW_TABLE = "ZMER_t_Cust_Entry100085";  // 报文单据体

        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);

            if (e.DataEntitys == null || e.DataEntitys.Length == 0)
                return;

            foreach (DynamicObject dataEntity in e.DataEntitys)
            {
                try
                {
                    long fid = Convert.ToInt64(dataEntity["Id"]);
                    ProcessCreateOrder(fid);
                }
                catch (Exception ex)
                {
                    _log.Error($"处理单据异常，FID={dataEntity?["Id"]}");
                    _log.Error(ex);
                }
            }
        }

        /// <summary>对单个面单执行顺丰下单（带单号下单）</summary>
        private void ProcessCreateOrder(long fid)
        {
            // 1. 查询单据头（审核前已提交，数据已落库）
            var heads = DBUtils.ExecuteDynamicObject(this.Context,
                $@"/*dialect*/SELECT FBillNo, FSFYDH, FSJR, FSJRDH, FSJRDZ, FJJR, FJJRDH, FJJRDZ, FKHDDH,
                            FPZHC, FZZHD, FPhotoNum, FPhotoContent, FPhotoRemark,
                            FSignBackContent, FSignBackNum, FSignBackRemark, FCPLX
                     FROM {HEAD_TABLE} WHERE FID = {fid}");

            if (heads == null || heads.Count == 0)
            {
                _log.WriteLog($"未查询到单据头，FID={fid}");
                return;
            }

            var row = heads[0];
            string billNo = Str(row, "FBillNo");
            string waybillNo = Str(row, "FSFYDH");
            string receiver = Str(row, "FSJR");
            string receiverPhone = Str(row, "FSJRDH");
            string receiverAddr = Str(row, "FSJRDZ");

            _log.Section($"开始顺丰下单：{billNo} (FID={fid})");

            // 2. 必填校验
            //【自动分配运单号】isGenWaybillNo=1 时 FSFYDH 由顺丰返回、无需录入，故跳过此项校验。
            //【切回带单号下单】isGenWaybillNo=0 时，放开下面这段校验即可。
            //if (string.IsNullOrEmpty(waybillNo))
            //{
            //    _log.WriteLog("顺丰运单号(FSFYDH)为空，跳过下单");
            //    UpdateHeadStatus(fid, "C", "", "", "", DateTime.Now, "顺丰运单号为空");
            //    return;
            //}
            if (string.IsNullOrEmpty(receiver))
            {
                _log.WriteLog("收件人(FSJR)为空，跳过下单");
                UpdateHeadStatus(fid, "C", "", "", "", DateTime.Now, "收件人为空");
                return;
            }
            if (string.IsNullOrEmpty(receiverPhone))
            {
                _log.WriteLog("收件人电话(FSJRDH)为空，跳过下单");
                UpdateHeadStatus(fid, "C", "", "", "", DateTime.Now, "收件人电话为空");
                return;
            }
            if (string.IsNullOrEmpty(receiverAddr))
            {
                _log.WriteLog("收件人地址(FSJRDZ)为空，跳过下单");
                UpdateHeadStatus(fid, "C", "", "", "", DateTime.Now, "收件人地址为空");
                return;
            }

            // 3. 寄件人：公户固定，单据为空时取配置
            string sender = string.IsNullOrEmpty(Str(row, "FJJR")) ? SFConfig.SenderName : Str(row, "FJJR");
            string senderPhone = string.IsNullOrEmpty(Str(row, "FJJRDH")) ? SFConfig.SenderPhone : Str(row, "FJJRDH");
            string senderAddr = string.IsNullOrEmpty(Str(row, "FJJRDZ")) ? SFConfig.SenderFullAddress : Str(row, "FJJRDZ");

            // 4. 客户订单号：为空则用单据编号代替
            string orderId = Str(row, "FKHDDH");
            if (string.IsNullOrEmpty(orderId))
                orderId = billNo;

            // 5. 产品类型 expressTypeId（默认 1 顺丰标快）
            int expressTypeId = 1;
            string cplx = Str(row, "FCPLX");
            if (!string.IsNullOrEmpty(cplx))
                int.TryParse(cplx, out expressTypeId);
            if (expressTypeId <= 0)
                expressTypeId = 1;

            // 6. 增值服务
            bool pzhc = BoolVal(Str(row, "FPZHC")); // 拍照回传 IN91
            bool zzhd = BoolVal(Str(row, "FZZHD")); // 纸质回单 IN03

            _log.WriteLog($"参数：orderId={orderId}，运单号={waybillNo}，产品类型={expressTypeId}，拍照回传={pzhc}，纸质回单={zzhd}");

            // 7. 构建报文并下单
            var msgData = BuildCreateOrderMsgData(orderId, waybillNo, sender, senderPhone, senderAddr,
                receiver, receiverPhone, receiverAddr, pzhc, zzhd, expressTypeId, row);
            string msgDataJson = JsonConvert.SerializeObject(msgData);

            var apiResult = SFExpressClient.Call(SFExpressClient.SERVICE_CREATE_ORDER, msgDataJson);

            // 8. 记录报文单据体
            InsertMessage(fid, SFExpressClient.SERVICE_CREATE_ORDER, apiResult);

            // 9. 回写结果
            if (apiResult.Success)
            {
                string filterResult = "";
                string routeLabel = "";
                string assignedWaybillNo = "";
                var md = apiResult.MsgData;
                if (md != null)
                {
                    filterResult = md["filterResult"]?.ToString() ?? "";
                    var routeLabelInfo = md["routeLabelInfo"] as JArray;
                    if (routeLabelInfo != null && routeLabelInfo.Count > 0)
                    {
                        var rld = routeLabelInfo[0]["routeLabelData"];
                        if (rld != null)
                            routeLabel = rld["destRouteLabel"]?.ToString() ?? "";
                    }
                    //【自动分配运单号】从返回里取出顺丰分配的运单号
                    var waybillInfo = md["waybillNoInfoList"] as JArray;
                    if (waybillInfo != null && waybillInfo.Count > 0)
                        assignedWaybillNo = waybillInfo[0]["waybillNo"]?.ToString() ?? "";
                }

                //【自动分配运单号】把顺丰分配的运单号回写到 FSFYDH，供后续路由/运费同步使用
                if (!string.IsNullOrEmpty(assignedWaybillNo))
                    UpdateWaybillNo(fid, assignedWaybillNo);

                string resultWaybillNo = string.IsNullOrEmpty(assignedWaybillNo) ? waybillNo : assignedWaybillNo;
                _log.WriteLog($"下单成功：运单号={resultWaybillNo}，筛单结果={filterResult}，路由标签={routeLabel}");
                UpdateHeadStatus(fid, "B", filterResult, routeLabel, apiResult.RequestId, DateTime.Now, "");
            }
            else
            {
                _log.WriteLog($"下单失败：{apiResult.ErrorCode} {apiResult.ErrorMsg}");
                UpdateHeadStatus(fid, "C", "", "", apiResult.RequestId, DateTime.Now, apiResult.ErrorMsg);
            }
        }

        /// <summary>构建下单 msgData（带单号下单）</summary>
        private JObject BuildCreateOrderMsgData(string orderId, string waybillNo,
            string sender, string senderPhone, string senderAddr,
            string receiver, string receiverPhone, string receiverAddr,
            bool pzhc, bool zzhd, int expressTypeId, DynamicObject row)
        {
            var msg = new JObject();
            msg["language"] = "zh-CN";
            msg["orderId"] = orderId;
            //【自动分配运单号】isGenWaybillNo=1：由顺丰分配运单号，不传 waybillNoInfoList。
            msg["isGenWaybillNo"] = 1;

            //【切回带单号下单】isGenWaybillNo=0 时，放开下面两段：isGenWaybillNo=0 + waybillNoInfoList。
            //msg["isGenWaybillNo"] = 0;

            // 顺丰运单号列表（带单号下单时传预印面单号）
            //var waybillList = new JArray();
            //waybillList.Add(new JObject { ["waybillType"] = 1, ["waybillNo"] = waybillNo });
            //msg["waybillNoInfoList"] = waybillList;

            // 托寄物（货物名称暂时写死）
            var cargoList = new JArray();
            cargoList.Add(new JObject { ["name"] = "测试托寄物品" });
            msg["cargoDetails"] = cargoList;

            // 收寄双方
            var contactList = new JArray();
            contactList.Add(new JObject
            {
                ["contactType"] = 1,
                ["company"] = sender,
                ["contact"] = sender,
                ["tel"] = senderPhone,
                ["address"] = senderAddr,
                ["country"] = "CN"
            });
            contactList.Add(new JObject
            {
                ["contactType"] = 2,
                ["company"] = receiver,
                ["contact"] = receiver,
                ["tel"] = receiverPhone,
                ["address"] = receiverAddr,
                ["country"] = "CN"
            });
            msg["contactInfoList"] = contactList;

            // 寄付月结
            msg["monthlyCard"] = SFConfig.MonthlyCard;
            msg["payMethod"] = 1;
            msg["expressTypeId"] = expressTypeId;
            msg["parcelQty"] = 1;
            msg["isReturnRoutelabel"] = 1;

            // 纸质回单 IN03：isSignBack=1 + extraInfoList
            if (zzhd)
            {
                msg["isSignBack"] = 1;
                var extraList = new JArray();
                extraList.Add(new JObject { ["attrName"] = "attr014", ["attrVal"] = "singBackInfo" });
                extraList.Add(new JObject { ["attrName"] = "attr015", ["attrVal"] = ToEnglishComma(Str(row, "FSignBackContent")) });
                extraList.Add(new JObject { ["attrName"] = "signbackNum", ["attrVal"] = Str(row, "FSignBackNum") });
                extraList.Add(new JObject { ["attrName"] = "signBackRemark", ["attrVal"] = Str(row, "FSignBackRemark") });
                msg["extraInfoList"] = extraList;
            }
            else
            {
                msg["isSignBack"] = 0;
            }

            // 拍照回传 IN91：serviceList
            if (pzhc)
            {
                string photoRemark = Str(row, "FPhotoRemark");
                string value5 = string.IsNullOrEmpty(photoRemark)
                    ? ""
                    : JsonConvert.SerializeObject(new { remark = photoRemark });

                var serviceList = new JArray();
                serviceList.Add(new JObject
                {
                    ["name"] = "IN91",
                    ["value"] = "13",
                    ["value1"] = Str(row, "FPhotoNum"),
                    ["value2"] = ToEnglishComma(Str(row, "FPhotoContent")),
                    ["value5"] = value5
                });
                msg["serviceList"] = serviceList;
            }

            return msg;
        }

        /// <summary>回写单据头状态</summary>
        private void UpdateHeadStatus(long fid, string orderStatus, string sxjg, string routeLabel,
            string qqdi, DateTime tbsj, string cwxx)
        {
            try
            {
                var sql = $@"/*dialect*/UPDATE {HEAD_TABLE} SET
                        FORDERSTATUS = '{Sql(orderStatus)}',
                        FSXJG = '{Sql(sxjg)}',
                        FRouteLabel = '{Sql(routeLabel)}',
                        FQQDI = '{Sql(qqdi)}',
                        FTBSJ = '{tbsj:yyyy-MM-dd HH:mm:ss}',
                        FCWXX = '{Sql(cwxx)}'
                    WHERE FID = {fid}";
                DBUtils.Execute(this.Context, sql);
            }
            catch (Exception ex)
            {
                _log.Error($"回写单据头状态失败，FID={fid}");
                _log.Error(ex);
            }
        }

        /// <summary>回写顺丰分配的运单号（自动分配运单号场景）</summary>
        private void UpdateWaybillNo(long fid, string waybillNo)
        {
            try
            {
                DBUtils.Execute(this.Context,
                    $@"/*dialect*/UPDATE {HEAD_TABLE} SET FSFYDH = '{Sql(waybillNo)}' WHERE FID = {fid}");
            }
            catch (Exception ex)
            {
                _log.Error($"回写运单号失败，FID={fid}");
                _log.Error(ex);
            }
        }

        /// <summary>写入报文单据体</summary>
        private void InsertMessage(long fid, string serviceCode, SFApiResult apiResult)
        {
            try
            {
                long entryId = NextEntryId(BW_TABLE);
                string success = apiResult.Success ? "1" : "0";

                var sql = $@"/*dialect*/INSERT INTO {BW_TABLE}
                        (FENTRYID, FID, FServiceCode, FRequestId, FCallTime, FRequestMsg, FResponseMsg, FSuccess, FErrorMsg)
                    VALUES
                        ({entryId}, {fid}, '{Sql(Truncate(serviceCode, 50))}', '{Sql(Truncate(apiResult.RequestId, 50))}', GETDATE(),
                         '{Sql(Truncate(apiResult.RequestMsg, 255))}', '{Sql(Truncate(apiResult.ResponseMsg, 255))}', '{success}', '{Sql(Truncate(apiResult.ErrorMsg, 50))}')";
                DBUtils.Execute(this.Context, sql);
            }
            catch (Exception ex)
            {
                _log.Error($"写入报文单据体失败，FID={fid}");
                _log.Error(ex);
            }
        }

        private long NextEntryId(string table)
        {
            var r = DBUtils.ExecuteDynamicObject(this.Context,
                $"/*dialect*/SELECT ISNULL(MAX(FENTRYID), 0) AS MX FROM {table}");
            return (r != null && r.Count > 0) ? Convert.ToInt64(r[0]["MX"]) + 1 : 1;
        }

        private static string Str(DynamicObject obj, string field)
        {
            try
            {
                var v = obj[field];
                if (v == null || v == DBNull.Value)
                    return "";
                return Convert.ToString(v).Trim();
            }
            catch
            {
                return "";
            }
        }

        private static bool BoolVal(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            return s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "是" || s == "Y";
        }

        /// <summary>多选下拉分隔符统一转为英文逗号（顺丰接口要求英文逗号分隔）</summary>
        private static string ToEnglishComma(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            return s.Replace("；", ",").Replace(";", ",").Replace("，", ",");
        }

        private static string Sql(string s)
        {
            return string.IsNullOrEmpty(s) ? "" : s.Replace("'", "''");
        }

        private static string Truncate(string s, int max = 2000)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}

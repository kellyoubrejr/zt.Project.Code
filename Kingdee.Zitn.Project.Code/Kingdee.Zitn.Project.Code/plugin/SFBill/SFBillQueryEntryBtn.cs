using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Globalization;

namespace Kingdee.Zitn.Project.Code.plugin.SFBill
{
    [Description("【物流面单单据插件】手动查询顺丰路由/费用/注册图片"), HotUpdate]
    public class SFBillQueryEntryBtn : AbstractBillPlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("物流面单查询");

        private const string HEAD_TABLE = "ZMER_t_Cust100030";       // 单据头
        private const string ROUTE_TABLE = "ZMER_t_Cust_Entry100084"; // 路由轨迹单据体
        private const string FEE_TABLE = "ZMER_t_Cust_Entry100087";   // 费用单据体
        private const string BW_TABLE = "ZMER_t_Cust_Entry100085";    // 报文单据体

        private const string ROUTE_ENTITY = "F_ZMER_Entity_LYGJ";
        private const string FEE_ENTITY = "F_ZMER_Entity_FYMX";

        private const string IMG_TYPE_PHOTO = "71"; // 拍照回传(IN91)
        private const string IMG_TYPE_PAGE = "2";   // 纸质回单(IN03)

        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);

            try
            {
                if (e.BarItemKey.Equals("btn_sel", StringComparison.OrdinalIgnoreCase))
                    QueryRoutes();
                else if (e.BarItemKey.Equals("btn_selp", StringComparison.OrdinalIgnoreCase))
                    QueryFreight();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                this.View.ShowErrMessage("查询失败：" + ex.Message);
            }
        }

        /// <summary>工具栏菜单按钮：注册图片（btn_img=拍照回传，btn_page=纸质回单）</summary>
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            try
            {
                if (e.BarItemKey.Equals("btn_img", StringComparison.OrdinalIgnoreCase))
                    RegisterPicture(IMG_TYPE_PHOTO, "拍照回传", "FPZHC");
                else if (e.BarItemKey.Equals("btn_page", StringComparison.OrdinalIgnoreCase))
                    RegisterPicture(IMG_TYPE_PAGE, "纸质回单", "FZZHD");
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                this.View.ShowErrMessage("图片注册失败：" + ex.Message);
            }
        }

        /// <summary>手动查询路由轨迹（EXP_RECE_SEARCH_ROUTES）</summary>
        private void QueryRoutes()
        {
            long fid = GetFid();
            string waybillNo = GetWaybillNo();
            if (string.IsNullOrWhiteSpace(waybillNo))
            {
                this.View.ShowErrMessage("顺丰运单号(FSFYDH)为空，无法查询路由");
                return;
            }

            var msg = new JObject
            {
                ["language"] = "zh-CN",
                ["trackingType"] = "1",
                ["trackingNumber"] = new JArray(waybillNo),
                ["methodType"] = "1"
            };

            var apiResult = SFExpressClient.Call(SFExpressClient.SERVICE_SEARCH_ROUTES, JsonConvert.SerializeObject(msg));
            InsertMessage(fid, SFExpressClient.SERVICE_SEARCH_ROUTES, apiResult);

            if (!apiResult.Success || apiResult.MsgData == null)
            {
                this.View.ShowErrMessage("路由查询失败：" + apiResult.ErrorCode + " " + apiResult.ErrorMsg);
                return;
            }

            var routeResps = apiResult.MsgData["routeResps"] as JArray;
            if (routeResps == null || routeResps.Count == 0)
            {
                this.View.ShowMessage("路由查询无返回数据");
                return;
            }

            var routes = routeResps[0]["routes"] as JArray;
            if (routes == null || routes.Count == 0)
            {
                this.View.ShowMessage("该运单暂无路由轨迹");
                return;
            }

            // 1. 先走 SetValue 写界面
            try
            {
                WriteRoutesToModel(routes);
                this.View.UpdateView(ROUTE_ENTITY);
            }
            catch (Exception ex)
            {
                _log.Error("模型写入路由失败");
                _log.Error(ex);
            }

            // 2. 再用 SQL 写库兜底
            WriteRoutesToSql(fid, routes);

            this.View.ShowMessage($"路由查询成功，共 {routes.Count} 条轨迹");
        }

        /// <summary>手动查询费用（EXP_RECE_QUERY_SFWAYBILL）</summary>
        private void QueryFreight()
        {
            long fid = GetFid();
            string waybillNo = GetWaybillNo();
            if (string.IsNullOrWhiteSpace(waybillNo))
            {
                this.View.ShowErrMessage("顺丰运单号(FSFYDH)为空，无法查询费用");
                return;
            }

            var msg = new JObject
            {
                ["trackingType"] = "2",
                ["trackingNum"] = waybillNo
            };

            var apiResult = SFExpressClient.Call(SFExpressClient.SERVICE_QUERY_SFWAYBILL, JsonConvert.SerializeObject(msg));
            InsertMessage(fid, SFExpressClient.SERVICE_QUERY_SFWAYBILL, apiResult);

            if (!apiResult.Success || apiResult.MsgData == null)
            {
                this.View.ShowErrMessage("费用查询失败：" + apiResult.ErrorCode + " " + apiResult.ErrorMsg);
                return;
            }

            var feeList = apiResult.MsgData["waybillFeeList"] as JArray;
            if (feeList == null || feeList.Count == 0)
            {
                this.View.ShowMessage("该运单暂无费用信息");
                return;
            }

            // 1. 先走 SetValue 写界面
            try
            {
                WriteFreightToModel(feeList);
                this.View.UpdateView(FEE_ENTITY);
            }
            catch (Exception ex)
            {
                _log.Error("模型写入费用失败");
                _log.Error(ex);
            }

            // 2. 再用 SQL 写库兜底
            WriteFreightToSql(fid, feeList);

            // 3. 回写单据头运费总额
            UpdateFreightTotal(fid, feeList);

            this.View.ShowMessage($"费用查询成功，共 {feeList.Count} 条费用");
        }

        /// <summary>手动注册图片（EXP_RECE_REGISTER_WAYBILL_PICTURE）</summary>
        private void RegisterPicture(string imgType, string typeName, string checkField)
        {
            long fid = GetFid();
            string waybillNo = GetWaybillNo();
            if (string.IsNullOrWhiteSpace(waybillNo))
            {
                this.View.ShowErrMessage("顺丰运单号(FSFYDH)为空，无法注册图片");
                return;
            }

            // 1. 未勾选对应增值服务则跳过
            if (!IsChecked(checkField))
            {
                this.View.ShowMessage($"未勾选{typeName}，无需注册");
                return;
            }

            // 2. 未签收则跳过（回单/拍照图片需签收后才能注册）
            if (!IsSigned(waybillNo))
            {
                this.View.ShowMessage("运单尚未签收，签收后才能注册回单/拍照图片");
                return;
            }

            var apiResult = SFExpressClient.RegisterPicture(waybillNo, imgType);
            InsertMessage(fid, SFExpressClient.SERVICE_REGISTER_PICTURE, apiResult);

            if (apiResult.Success)
                this.View.ShowMessage($"{typeName}图片注册成功 (imgType={imgType})");
            else
                this.View.ShowErrMessage($"{typeName}图片注册失败：" + apiResult.ErrorCode + " " + apiResult.ErrorMsg);
        }

        /// <summary>单据头复选框是否勾选（FPZHC/FZZHD）</summary>
        private bool IsChecked(string field)
        {
            try
            {
                object v = this.Model.GetValue(field);
                if (v == null || v == DBNull.Value)
                    return false;
                string s = Convert.ToString(v).Trim();
                return s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "是" || s == "Y";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>运单是否已签收（实时调路由查询接口判断）</summary>
        private bool IsSigned(string waybillNo)
        {
            try
            {
                var msg = new JObject
                {
                    ["language"] = "zh-CN",
                    ["trackingType"] = "1",
                    ["trackingNumber"] = new JArray(waybillNo),
                    ["methodType"] = "1"
                };

                var apiResult = SFExpressClient.Call(SFExpressClient.SERVICE_SEARCH_ROUTES, JsonConvert.SerializeObject(msg));

                if (!apiResult.Success || apiResult.MsgData == null)
                    return false;

                var routeResps = apiResult.MsgData["routeResps"] as JArray;
                if (routeResps == null || routeResps.Count == 0)
                    return false;

                var routes = routeResps[0]["routes"] as JArray;
                if (routes == null || routes.Count == 0)
                    return false;

                foreach (var r in routes)
                {
                    if (StatusName(r).Contains("签收"))
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _log.Error($"查询签收状态失败，运单号={waybillNo}");
                _log.Error(ex);
                return false;
            }
        }

        /// <summary>模型写入路由（主路径）</summary>
        private void WriteRoutesToModel(JArray routes)
        {
            this.Model.DeleteEntryData(ROUTE_ENTITY);

            for (int i = 0; i < routes.Count; i++)
            {
                var r = routes[i];
                this.Model.CreateNewEntryRow(ROUTE_ENTITY);
                //this.Model.SetValue("FAcceptTime", Str(r["acceptTime"]), i); // 原字段类型用错(日期)，已改用 FAcceptTime1
                this.Model.SetValue("FAcceptTime1", Str(r["acceptTime"]), i);
                this.Model.SetValue("FAcceptAddress", Str(r["acceptAddress"]), i);
                this.Model.SetValue("FRemark", Str(r["remark"]), i);
                this.Model.SetValue("FOpCode", Str(r["opCode"]), i);
                this.Model.SetValue("FStatusName", StatusName(r), i);
            }
        }

        /// <summary>模型写入费用（主路径），费用名称前加「自动」前缀区分手动录入</summary>
        private void WriteFreightToModel(JArray feeList)
        {
            this.Model.DeleteEntryData(FEE_ENTITY);

            for (int i = 0; i < feeList.Count; i++)
            {
                var fee = feeList[i];
                decimal feeValue = 0;
                decimal.TryParse(fee["value"]?.ToString(), out feeValue);

                this.Model.CreateNewEntryRow(FEE_ENTITY);
                this.Model.SetValue("FFeeType", FeeTypeName(Str(fee["type"])), i);
                this.Model.SetValue("FFeeName", "自动" + Str(fee["name"]), i);
                this.Model.SetValue("FFeeValue", feeValue, i);
                this.Model.SetValue("FSettlementType", SettlementTypeName(Str(fee["settlementTypeCode"])), i);
            }
        }

        /// <summary>SQL 兜底写入路由</summary>
        private void WriteRoutesToSql(long fid, JArray routes)
        {
            DBUtils.Execute(this.Context, $"/*dialect*/DELETE FROM {ROUTE_TABLE} WHERE FID = {fid}");

            foreach (var r in routes)
            {
                long entryId = NextEntryId(ROUTE_TABLE);
                var sql = $@"/*dialect*/INSERT INTO {ROUTE_TABLE}
                        (FENTRYID, FID, FAcceptTime1, FAcceptAddress, FRemark, FOpCode, FStatusName)
                    VALUES
                        ({entryId}, {fid}, '{Sql(Str(r["acceptTime"]))}', '{Sql(Str(r["acceptAddress"]))}', '{Sql(Str(r["remark"]))}', '{Sql(Str(r["opCode"]))}', '{Sql(StatusName(r))}')";
                DBUtils.Execute(this.Context, sql);
            }
        }

        /// <summary>SQL 兜底写入费用</summary>
        private void WriteFreightToSql(long fid, JArray feeList)
        {
            DBUtils.Execute(this.Context, $"/*dialect*/DELETE FROM {FEE_TABLE} WHERE FID = {fid}");

            foreach (var fee in feeList)
            {
                decimal feeValue = 0;
                decimal.TryParse(fee["value"]?.ToString(), out feeValue);

                long entryId = NextEntryId(FEE_TABLE);
                var sql = $@"/*dialect*/INSERT INTO {FEE_TABLE}
                        (FENTRYID, FID, FFeeType, FFeeName, FFeeValue, FSettlementType)
                    VALUES
                        ({entryId}, {fid}, '{Sql(FeeTypeName(Str(fee["type"])))}', '{Sql("自动" + Str(fee["name"]))}', {feeValue.ToString(CultureInfo.InvariantCulture)}, '{Sql(SettlementTypeName(Str(fee["settlementTypeCode"])))}')";
                DBUtils.Execute(this.Context, sql);
            }
        }

        /// <summary>回写单据头运费总额（FFreightTotal）</summary>
        private void UpdateFreightTotal(long fid, JArray feeList)
        {
            decimal total = 0;
            foreach (var fee in feeList)
            {
                decimal v = 0;
                decimal.TryParse(fee["value"]?.ToString(), out v);
                total += v;
            }

            try
            {
                DBUtils.Execute(this.Context,
                    $@"/*dialect*/UPDATE {HEAD_TABLE} SET FFreightTotal = {total.ToString(CultureInfo.InvariantCulture)} WHERE FID = {fid}");
            }
            catch (Exception ex)
            {
                _log.Error($"回写运费总额失败，FID={fid}");
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

        private long GetFid()
        {
            try
            {
                object v = this.Model.GetValue("Id");
                if (v == null || v == DBNull.Value)
                    return 0;
                return Convert.ToInt64(v);
            }
            catch
            {
                return 0;
            }
        }

        private string GetWaybillNo()
        {
            object v = this.Model.GetValue("FSFYDH");
            return (v == null || v == DBNull.Value) ? "" : Convert.ToString(v).Trim();
        }

        private long NextEntryId(string table)
        {
            var r = DBUtils.ExecuteDynamicObject(this.Context,
                $"/*dialect*/SELECT ISNULL(MAX(FENTRYID), 0) AS MX FROM {table}");
            return (r != null && r.Count > 0) ? Convert.ToInt64(r[0]["MX"]) + 1 : 1;
        }

        /// <summary>费用类型(type) 数字枚举 → 中文</summary>
        private static string FeeTypeName(string code)
        {
            switch (code)
            {
                case "1": return "运费";
                case "2": return "其它费用";
                case "3": return "基础保";
                case "8": return "签单返还-纸质回单";
                case "91": return "拍照回传";
                default: return code;
            }
        }

        /// <summary>结算类型(settlementTypeCode) 数字枚举 → 中文</summary>
        private static string SettlementTypeName(string code)
        {
            switch (code)
            {
                case "1": return "现结";
                case "2": return "月结";
                default: return code;
            }
        }

        private static string StatusName(JToken r)
        {
            string secondary = r["secondaryStatusName"]?.ToString();
            if (!string.IsNullOrEmpty(secondary))
                return secondary;
            return r["firstStatusName"]?.ToString() ?? "";
        }

        private static string Str(JToken t)
        {
            return t == null ? "" : Convert.ToString(t).Trim();
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

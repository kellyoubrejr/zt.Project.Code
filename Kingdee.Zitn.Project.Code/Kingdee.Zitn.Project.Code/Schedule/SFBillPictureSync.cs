using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.Zitn.Project.Code.conf;
using Kingdee.Zitn.Project.Code.plugin.SFBill;
using System;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.Schedule
{
    [Description("【跑批】：物流面单顺丰图片注册（拍照回传/纸质回单）")]
    [Kingdee.BOS.Util.HotUpdate]
    public class SFBillPictureSync : IScheduleService
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("物流面单图片注册");

        private const string HEAD_TABLE = "ZMER_t_Cust100030"; // 单据头

        // 图片类型 imgType：71=拍照回传(IN91)，2=纸质回单(IN03)
        private const string IMG_TYPE_PHOTO = "71";
        private const string IMG_TYPE_PAGE = "2";

        public void Run(Context ctx, Kingdee.BOS.Core.Schedule schedule)
        {
            _log.Section("开始注册物流面单顺丰图片");

            try
            {
                // 查询「已下单(B)」且有运单号的面单
                var bills = DBUtils.ExecuteDynamicObject(ctx,
                    $@"/*dialect*/SELECT FID, FBillNo, FSFYDH, FPZHC, FZZHD FROM {HEAD_TABLE}
                        WHERE FORDERSTATUS = 'B' AND FSFYDH IS NOT NULL AND FSFYDH <> ''");

                if (bills == null || bills.Count == 0)
                {
                    _log.WriteLog("没有需要注册图片的面单");
                    return;
                }

                _log.WriteLog($"共 {bills.Count} 条面单需要处理");

                for (int i = 0; i < bills.Count; i++)
                {
                    long fid = Convert.ToInt64(bills[i]["FID"]);
                    string billNo = Convert.ToString(bills[i]["FBillNo"]);
                    string waybillNo = Convert.ToString(bills[i]["FSFYDH"]);
                    bool pzhc = BoolVal(Convert.ToString(bills[i]["FPZHC"])); // 拍照回传
                    bool zzhd = BoolVal(Convert.ToString(bills[i]["FZZHD"])); // 纸质回单

                    try
                    {
                        _log.Section($"注册图片 [{i + 1}/{bills.Count}] {billNo}，运单号={waybillNo}");

                        if (pzhc)
                            RegisterOne(waybillNo, billNo, IMG_TYPE_PHOTO, "拍照回传");
                        if (zzhd)
                            RegisterOne(waybillNo, billNo, IMG_TYPE_PAGE, "纸质回单");
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"注册失败：{billNo}");
                        _log.Error(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }

            _log.Section("图片注册结束");
        }

        /// <summary>对单个运单注册图片。未签收时顺丰返回 8250（清单未生成），记日志跳过，下轮重试。</summary>
        private void RegisterOne(string waybillNo, string billNo, string imgType, string typeName)
        {
            var apiResult = SFExpressClient.RegisterPicture(waybillNo, imgType);

            if (apiResult.Success)
                _log.WriteLog($"{typeName}注册成功：{billNo} (imgType={imgType})");
            else
                _log.WriteLog($"{typeName}注册失败：{billNo} (imgType={imgType})，{apiResult.ErrorCode} {apiResult.ErrorMsg}");
        }

        private static bool BoolVal(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            return s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "是" || s == "Y";
        }
    }
}

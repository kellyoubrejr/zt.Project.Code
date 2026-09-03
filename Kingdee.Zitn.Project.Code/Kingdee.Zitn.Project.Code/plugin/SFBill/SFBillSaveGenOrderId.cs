using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.Zitn.Project.Code.conf;
using Kingdee.Zitn.Project.Code.Util;
using System;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.plugin.SFBill
{
    [Description("【保存服务】--物流面单保存时自动生成客户订单号(FKHDDH)"), HotUpdate]
    public class SFBillSaveGenOrderId : AbstractOperationServicePlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("物流面单保存");

        // 单据头物理表
        private const string HEAD_TABLE = "ZMER_t_Cust100030";

        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);

            if (!this.OperationResult.IsSuccess)
                return;

            if (e.DataEntitys == null || e.DataEntitys.Length == 0)
                return;

            foreach (DynamicObject dataEntity in e.DataEntitys)
            {
                try
                {
                    long fid = Convert.ToInt64(dataEntity["Id"]);
                    GenerateAndFillOrderId(fid);
                }
                catch (Exception ex)
                {
                    _log.Error($"生成客户订单号异常，FID={dataEntity?["Id"]}");
                    _log.Error(ex);
                    SendMsg.Send($"【物流面单】生成客户订单号异常，FID={dataEntity?["Id"]}", ex);
                }
            }
        }

        /// <summary>若 FKHDDH 为空则生成并回写，否则保持不变（保证唯一稳定）</summary>
        private void GenerateAndFillOrderId(long fid)
        {
            var heads = DBUtils.ExecuteDynamicObject(this.Context,
                $@"/*dialect*/SELECT FBillNo, FKHDDH FROM {HEAD_TABLE} WHERE FID = {fid}");

            if (heads == null || heads.Count == 0)
                return;

            var row = heads[0];
            string orderId = Convert.ToString(row["FKHDDH"]);
            if (!string.IsNullOrEmpty(orderId))
                return; // 已存在，不重复生成

            string billNo = Convert.ToString(row["FBillNo"]) ?? "";
            string newOrderId = GenerateOrderId();

            DBUtils.Execute(this.Context,
                $@"/*dialect*/UPDATE {HEAD_TABLE} SET FKHDDH = '{Sql(newOrderId)}' WHERE FID = {fid}");

            _log.WriteLog($"单据 {billNo} (FID={fid}) 自动生成客户订单号：{newOrderId}");
        }

        /// <summary>按顺丰接口规则生成客户订单号：字母数字，唯一，长度不超过64</summary>
        private static string GenerateOrderId()
        {
            return "ZMER" + DateTime.Now.ToString("yyyyMMddHHmmssfff")
                 + Guid.NewGuid().ToString("N").Substring(0, 6);
        }

        private static string Sql(string s)
        {
            return string.IsNullOrEmpty(s) ? "" : s.Replace("'", "''");
        }
    }
}

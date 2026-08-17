using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.WebApi.Client;
using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.plugin.PO
{
    [Description("采购订单审核-反写供应商主数据付款方式字段"), HotUpdate]
    public class PoRejWriteSupplierInfos : AbstractOperationServicePlugIn
    {
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);

            if (e.DataEntitys == null || e.DataEntitys.Length == 0)
                return;

            /* ================== 登录 K3Cloud ================== */
            var client = new K3CloudApiClient(ErpLogin.K3CloudUrl);
            var loginResult = client.ValidateLogin(
                ErpLogin.AppId,
                ErpLogin.UserName,
                ErpLogin.Password,
                ErpLogin.Lcid
            );

            if (JObject.Parse(loginResult)["LoginResultType"].Value<int>() != 1)
            {
                throw new KDBusinessException("LOGIN_FAIL", "K3Cloud 登录失败");
            }

            foreach (DynamicObject purBill in e.DataEntitys)
            {
                /* ================== 获取采购订单信息 ================== */
                string billNo = Convert.ToString(purBill["BillNo"]);

                DynamicObject supplier = purBill["SUPPLIERID"] as DynamicObject;
                string supplierName = supplier?["Name"]?.ToString();

                var fkjhSql = string.Format("/*dialect*/SELECT F_ZMER_COMBO_QTR FROM T_PUR_POORDER A JOIN T_PUR_POORDERINSTALLMENT B1 ON A.FID = B1.FID WHERE FBILLNO = '{0}'", billNo);
                DynamicObjectCollection rows = DBUtils.ExecuteDynamicObject(this.Context, fkjhSql);

                if (rows != null && rows.Count > 0)
                {
                    var rawFkfs = rows[0]["F_ZMER_COMBO_QTR"];
                    if (rawFkfs == null || rawFkfs == DBNull.Value || string.IsNullOrEmpty(rawFkfs.ToString()))
                    {
                        return;
                    }
                    if (!int.TryParse(rawFkfs.ToString(), out int fkfs))
                    {
                        return;
                    }

                    var upSql = string.Format("/*dialect*/UPDATE A SET F_UNW_COMBO_ZC5 = {0}  FROM T_BD_SUPPLIER A JOIN T_BD_SUPPLIER_L B ON A.FSUPPLIERID = B.FSUPPLIERID WHERE FNAME = '{1}'", fkfs, supplierName);
                    DBUtils.Execute(this.Context, upSql);
                }
            }
        }
    }
}

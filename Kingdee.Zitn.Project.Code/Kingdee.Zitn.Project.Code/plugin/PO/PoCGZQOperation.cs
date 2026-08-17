using Kingdee.BOS;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.ComponentModel;
using static Kingdee.K3.MFG.App.AppServiceContext;

namespace Kingdee.Zitn.Project.Code.plugin.PO
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("【采购订单订单保存/提交按钮点击后】获取标识为手工，标准周期反写物料并留痕")]
    public class PoCGZQOperation : AbstractBillPlugIn
    {
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            if (e.BarItemKey.Equals("tbSplitSave", StringComparison.OrdinalIgnoreCase) || e.BarItemKey.Equals("tbSplitSubmit", StringComparison.OrdinalIgnoreCase))
            {
                DynamicObject cgOrgObj = this.View.Model.GetValue("FPURCHASEORGID") as DynamicObject;
                if (cgOrgObj == null) return;
                var cgOrg = Convert.ToString(cgOrgObj["ID"]);

                string userName = this.Context.UserName;

                var entity = this.View.BillBusinessInfo.GetEntity("FPOOrderEntry");
                var entryData = this.View.Model.GetEntityDataObject(entity);
                if (entryData == null || entryData.Count == 0) return;

                for (int i = 0; i < entryData.Count; i++)
                {
                    DynamicObject wlidObj = this.View.Model.GetValue("FMaterialId", i) as DynamicObject;
                    if (wlidObj == null) continue;

                    string wlid = Convert.ToString(wlidObj["ID"]);
                    var materialid = string.Format("/*dialect*/select FNUMBER from t_bd_material where fmaterialid = '{0}' and FUSEORGID = 1", wlid);
                    DynamicObjectCollection materialidsql = DbUtils.ExecuteDynamicObject(this.Context, materialid);
                    if (materialidsql == null || materialidsql.Count == 0) continue;

                    string wlnum = Convert.ToString(materialidsql[0]["FNUMBER"]);

                    var status = Convert.ToString(this.View.Model.GetValue("F_ZMER_TEXT_3IY", i));
                    if (status == "手动写入")
                    {
                        var BZCGZQ = Convert.ToInt32(this.View.Model.GetValue("F_ZMER_QTY_W5C", i));

                        var query = string.Format("/*dialect*/SELECT F_ZMER_QTY_ZC5,F_ZMER_QTY_IMU,F_ZMER_QTY_1XJ FROM T_BD_MATERIAL WHERE FNUMBER = '{0}' AND FUSEORGID = {1}", wlnum, cgOrg);
                        DynamicObjectCollection querysql = DbUtils.ExecuteDynamicObject(this.Context, query);
                        if (querysql == null || querysql.Count == 0) continue;
                        var ZCGZQ = Convert.ToInt32(querysql[0]["F_ZMER_QTY_ZC5"]);

                        if (BZCGZQ != ZCGZQ)
                        {
                            throw new KDBusinessException($"行号 {i + 1}：", $"单据上的标准采购周期({BZCGZQ})与物料基础资料上的采购周期({ZCGZQ})不一致，请检查并保持一致。");
                        }
                        else
                        {
                            var query1 = string.Format("/*dialect*/UPDATE A SET F_ZMER_QTY_ZC5={0},F_ZMER_TEXT_LSN='{1}' FROM T_BD_MATERIAL A WHERE FNUMBER = '{2}' AND FUSEORGID = {3}", BZCGZQ, userName, wlnum, cgOrg);
                            DbUtils.Execute(this.Context, query1);
                        }
                    }

                }
            }
        }
    }
}

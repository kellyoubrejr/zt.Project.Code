using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.Zitn.Project.Code.conf;
using System;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.plugin.PO
{
    [Description("【表单插件】采购订单】带入供应商付款条件字段")]
    [HotUpdate]
    public class PoSupplier_FK : AbstractBillPlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("采购订单付款条件");

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            if (e.BarItemKey.Equals("tbSplitSubmit", StringComparison.OrdinalIgnoreCase) ||
                e.BarItemKey.Equals("tbSubmit", StringComparison.OrdinalIgnoreCase) ||
                e.BarItemKey.Equals("tbSplitSave", StringComparison.OrdinalIgnoreCase) ||
                e.BarItemKey.Equals("tbSave", StringComparison.OrdinalIgnoreCase))
            {
                var supplierObj = this.View.Model.GetValue("FSupplierId") as DynamicObject;
                if (supplierObj != null)
                {
                    var supplierId = supplierObj["Id"];
                    _log.WriteLog($"GYSID: {supplierId}");

                    if (supplierId != null)
                    {
                        var current = this.View.Model.GetValue("F_ZMER_Combo_qtr");
                        bool hasValue = current != null && !string.IsNullOrEmpty(current.ToString());

                        if (hasValue)
                        {
                            _log.WriteLog($"付款条件已有值: {current}，不覆盖");
                        }
                        else
                        {
                            var gysOther = string.Format($"/*dialect*/SELECT F_UNW_COMBO_ZC5 FROM T_BD_SUPPLIER WHERE FSUPPLIERID = {supplierId}");
                            var DT = DBUtils.ExecuteDynamicObject(this.Context, gysOther);
                            if (DT != null && DT.Count > 0)
                            {
                                for (int ii = 0; ii < DT.Count; ii++)
                                {
                                    var fkfs = DT[ii]["F_UNW_COMBO_ZC5"]?.ToString();
                                    _log.WriteLog($"FKFS: {fkfs}");

                                    if (!string.IsNullOrWhiteSpace(fkfs))
                                    {
                                        this.View.Model.SetValue("F_ZMER_Combo_qtr", fkfs);
                                    }
                                }
                            }
                        }
                    }
                }
                this.View.UpdateView("F_ZMER_Combo_qtr");
                this.View.Model.Save();
                _log.WriteLog($"保存成功, fkfs: {this.View.Model.GetValue("F_ZMER_Combo_qtr")}");
                this.View.ShowMessage("供应商付款条件已带入成功！");
            }
        }
    }
}

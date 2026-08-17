using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdee.Zitn.Project.Code.plugin.BillOrListPlugin
{
    [Description("【表单插件】DataChanged"), HotUpdate]
    public class BillControl : AbstractDynamicFormPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            this.View.UpdateView("F_ZMER_Entity_ne1");

            if (e.Field.Key.Equals("F_ZMER_DiscountRate_83g", StringComparison.OrdinalIgnoreCase))
            {
                var entity = this.View.BillBusinessInfo.GetEntity("F_ZMER_Entity_ne1");
                var entityObjs = this.View.Model.GetEntityDataObject(entity);
                for (int i = 0; i < entityObjs.Count; i++)
                {
                    string qty = entityObjs[i]["FQTY"].ToString();

                    if (qty.IsNullOrEmptyOrWhiteSpace()) continue;

                    int fqty = Convert.ToInt32(qty);

                    if (fqty == 0) continue;
                    string updownt = entityObjs[i]["F_ZMER_DiscountRate_83g"].ToString();
                    double updownt1 = Convert.ToDouble(updownt);

                    //想上取整fqty - (fqty * updownt1 / 100)
                    int fqty1 = (int)Math.Ceiling(fqty - (fqty * updownt1 / 100));
                    int fqty2 = (int)Math.Ceiling(fqty + (fqty * updownt1 / 100));
                    this.View.Model.SetValue("F_ZMER_TEXT_QTR", fqty1, i);
                    this.View.Model.SetValue("F_ZMER_TEXT_83G", fqty2, i);
                }
                this.View.UpdateView("F_ZMER_Entity_ne1");
            }
        }
    }

    [Description("【表单插件】免审采购订单打开AfterBindData【隐藏】")]
    public class AfterBindDataClass : AbstractBillPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);

            this.View.GetControl("fstart").Visible = false;
            this.View.GetControl("fend").Visible = false;
            this.View.GetControl("flong").Visible = false;
            this.View.GetControl("fstartprice1").Visible = false;
            this.View.GetControl("fendprice").Visible = false;
            this.View.GetControl<Button>("fsure").Visible = false;
        }
    }

    [Description("【表单插件】免审采购订单点击单据体按钮")]
    public class EntityButton : AbstractBillPlugIn
    {
        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {

            base.AfterEntryBarItemClick(e);

            if (e.BarItemKey.Equals("ZMER_tbButton_3"))
            {
                this.View.GetControl("fstart").Visible = true;
                this.View.GetControl("fend").Visible = true;
                this.View.GetControl("flong").Visible = true;
                this.View.GetControl("fstartprice1").Visible = true;
                this.View.GetControl("fendprice").Visible = true;
                this.View.GetControl<Button>("fsure").Visible = true;
            }
        }
    }

    [Description("【表单插件】点击确认按钮 隐藏字段 创建单据体明细")]
    public class createClass1 : AbstractBillPlugIn
    {
        public override void ButtonClick(ButtonClickEventArgs e)
        {

            base.ButtonClick(e);

            if (string.Equals(e.Key, "fsure", System.StringComparison.OrdinalIgnoreCase))
            {
                this.View.GetControl("fstart").Visible = false;
                this.View.GetControl("fend").Visible = false;
                this.View.GetControl("flong").Visible = false;
                this.View.GetControl("fstartprice1").Visible = false;
                this.View.GetControl("fendprice").Visible = false;
                this.View.GetControl<Button>("fsure").Visible = false;

                int flong = Convert.ToInt32(this.View.Model.GetValue("flong"));
                int fstart = Convert.ToInt32(this.View.Model.GetValue("fstart"));
                int fend = Convert.ToInt32(this.View.Model.GetValue("fend"));
                double fstartPrice = Convert.ToDouble(this.View.Model.GetValue("fstartprice1"));
                double fendPrice = Convert.ToDouble(this.View.Model.GetValue("fendprice"));

                int step = (fend - fstart) / flong;
                double priceStep = (fendPrice - fstartPrice) / flong;


                string billno = string.Empty;
                string supplier = string.Empty;
                int wlid = 0;

                var entity = this.View.BillBusinessInfo.GetEntity("F_ZMER_Entity_ne1");
                var entityObjs = this.View.Model.GetEntityDataObject(entity);

                if (entityObjs.Count > 0)
                {
                    billno = entityObjs[0]["FPURNO"]?.ToString();
                    supplier = entityObjs[0]["FSUPPLIER"]?.ToString();

                    var wlidObj = this.View.Model.GetValue("FMATERIALID", 0) as Kingdee.BOS.Orm.DataEntity.DynamicObject;
                    if (wlidObj != null)
                    {
                        wlid = Convert.ToInt32(wlidObj["Id"]);
                    }
                }

                var materialMeta = MetaDataServiceHelper.Load(this.Context, "BD_MATERIAL") as FormMetadata;
                DynamicObjectType materialType = materialMeta.BusinessInfo.GetDynamicObjectType();

                Kingdee.BOS.Orm.DataEntity.DynamicObject materialObj = BusinessDataServiceHelper.Load(
                    this.Context,
                    new object[] { wlid },
                    materialType
                ).FirstOrDefault();


                int currentStart = fstart;
                double currentPrice = fstartPrice;

                for (int i = 0; i < flong; i++)
                {
                    if (i > 0)
                    {
                        this.View.Model.CreateNewEntryRow("F_ZMER_Entity_ne1");
                    }

                    int currentEnd = (i == flong - 1) ? fend : currentStart + step;

                    double rowPrice = (i == flong - 1) ? fendPrice : currentPrice;

                    this.View.Model.SetValue("FPURNO", billno, i);
                    this.View.Model.SetValue("FSUPPLIER", supplier, i);
                    this.View.Model.SetValue("FMATERIALID", materialObj, i);
                    this.View.Model.SetValue("FQTY", "", i);

                    this.View.Model.SetValue("F_ZMER_Text_qtr", currentStart, i);
                    this.View.Model.SetValue("F_ZMER_TEXT_83G", currentEnd, i);
                    this.View.Model.SetValue("FTAXPRICE", rowPrice, i);

                    currentStart = currentEnd;
                    currentPrice += priceStep;
                }
            }
        }
    }
}

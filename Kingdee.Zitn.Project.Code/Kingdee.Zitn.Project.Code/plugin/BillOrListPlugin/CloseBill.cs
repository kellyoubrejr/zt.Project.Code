using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.plugin.BillOrListPlugin
{
    /// <summary>
    /// 【单据插件】单据界面关闭时不提示是否保存
    /// </summary>
    [Description("【单据插件】单据界面关闭时不提示是否保存"), HotUpdate]
    public class CloseBill : AbstractBillPlugIn
    {
        public override void BeforeClosed(BeforeClosedEventArgs e)
        {

            base.BeforeClosed(e);

            this.Model.DataChanged = false;

            /*if (this.View.ParentFormView != null)
            {
                this.View.ParentFormView.Refresh();
                this.View.SendAynDynamicFormAction(this.View.ParentFormView);
            }*/

        }
    }
}

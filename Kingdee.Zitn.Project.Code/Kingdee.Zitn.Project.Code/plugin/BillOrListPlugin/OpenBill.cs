using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.plugin.BillOrListPlugin
{

    /// <summary>
    /// 【表单插件】修改窗体显示名称
    /// </summary>
    [Description("【表单插件】修改窗体显示名称"), HotUpdate]

    public class OpenBill : AbstractDynamicFormPlugIn
    {
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);

            var formTitle = new LocaleValue(string.Format("{0}", "【定价依据（采购）信息预览】"));

            this.View.SetFormTitle(formTitle);

        }
    }
}

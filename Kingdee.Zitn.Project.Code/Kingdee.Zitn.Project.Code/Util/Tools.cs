using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;

namespace Kingdee.Zitn.Project.Code.Util
{
    /// <summary>
    /// 工具类
    /// </summary>
    public class Tools
    {
        /// <summary>
        /// 按单据标识创建并加载单据视图，返回 IBillView
        /// </summary>
        public static IBillView CreateBillView(Context ctx, string formId, object billId)
        {
            FormMetadata meta = MetaDataServiceHelper.Load(ctx, formId) as FormMetadata;
            Form form = meta.BusinessInfo.GetForm();

            Type type = Type.GetType("Kingdee.BOS.Web.Import.ImportBillView,Kingdee.BOS.Web");
            var billView = (IDynamicFormViewService)Activator.CreateInstance(type);

            BillOpenParameter openParam = CreateOpenParameter(meta, ctx, billId);
            var provider = form.GetFormServiceProvider();

            billView.Initialize(openParam, provider);
            billView.LoadData();

            return billView as IBillView;
        }

        /// <summary>
        /// 构建视图加载参数
        /// </summary>
        public static BillOpenParameter CreateOpenParameter(FormMetadata meta, Context ctx, object billId)
        {
            Form form = meta.BusinessInfo.GetForm();

            BillOpenParameter openParam = new BillOpenParameter(form.Id, meta.GetLayoutInfo().Id);
            openParam.Context = ctx;
            openParam.ServiceName = form.FormServiceName;
            openParam.PageId = Guid.NewGuid().ToString();
            openParam.FormMetaData = meta;

            string billIdStr = billId == null ? "" : billId.ToString();
            if (string.IsNullOrWhiteSpace(billIdStr) || billIdStr == "0")
            {
                openParam.Status = OperationStatus.ADDNEW;  
            }
            else
            {
                openParam.Status = OperationStatus.EDIT;     
            }

            openParam.PkValue = billId;
            openParam.CreateFrom = CreateFrom.Default;
            openParam.GroupId = "";
            openParam.ParentId = 0;
            openParam.DefaultBillTypeId = "";
            openParam.DefaultBusinessFlowId = "";
            openParam.SetCustomParameter("ShowConfirmDialogWhenChangeOrg", false);

            var plugs = form.CreateFormPlugIns();
            openParam.SetCustomParameter(FormConst.PlugIns, plugs);

            PreOpenFormEventArgs args = new PreOpenFormEventArgs(ctx, openParam);
            foreach (var plug in plugs)
            {
                plug.PreOpenForm(args);
            }

            return openParam;
        }

        /// <summary>
        /// 按过滤条件查询单据，返回 DynamicObject 数组
        /// </summary>
        public static DynamicObject[] GetItemArray(Context ctx, string formId, string filter, string orderBy = "")
        {
            var param = new QueryBuilderParemeter
            {
                FormId = formId,
                FilterClauseWihtKey = filter
            };
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                param.OrderByClauseWihtKey = orderBy;
            }

            FormMetadata formMeta = MetaDataServiceHelper.Load(ctx, formId) as FormMetadata;
            return BusinessDataServiceHelper.Load(ctx, formMeta.BusinessInfo.GetDynamicObjectType(), param);
        }
    }
}

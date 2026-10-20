using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Kingdee.Zitn.Project.Code.plugin.MSPO
{
    [HotUpdate, Description("【免审采购订单表单插件】点击选单按钮弹出采购订单选单，勾选后按行回填单据体")]
    public class MSPOSelectLookUp : AbstractDynamicFormPlugIn
    {
        /// <summary>免审采购订单单据体标识</summary>
        private const string ENTRY_KEY = "F_ZMER_Entity_ne1";

        /// <summary>选单按钮Key（工具栏）</summary>
        private const string BTN_KEY = "btn_draw";

        // ===== 回填字段Key（与单据体字段不一致时改这里）=====
        private const string FIELD_BILLNO = "FPURNO";      // 采购订单号
        private const string FIELD_SUPPLIER = "FSUPPLIER"; // 供应商
        private const string FIELD_MATERIAL = "FMATERIALID"; // 物料编码
        private const string FIELD_QTY = "FQTY";           // 采购数量
        private const string FIELD_TAXPRICE = "FTAXPRICE"; // 含税单价
        private const string FIELD_DANWEI = "FDANWEI"; // 单位

        /// <summary>工具栏按钮</summary>
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            if (e.BarItemKey.EqualsIgnoreCase(BTN_KEY))
                ShowPurchaseOrderLookUp();
        }

        /// <summary>单据体菜单按钮（防止按钮挂在单据体上）</summary>
        //public override void AfterEntryBarItemClick(AfterEntryBarItemClickEventArgs e)
        //{
        //    base.AfterEntryBarItemClick(e);

        //    if (e.BarItemKey.EqualsIgnoreCase(BTN_KEY))
        //        ShowPurchaseOrderLookUp();
        //}

        /// <summary>弹出采购订单选单列表（多选，只显示已审核）</summary>
        private void ShowPurchaseOrderLookUp()
        {
            ListShowParameter showParam = new ListShowParameter()
            {
                Context = this.Context,
                FormId = "PUR_PurchaseOrder",
                IsShowApproved = true,
                ParentPageId = this.View.PageId,
                IsLookUp = true,
                MultiSelect = true,
            };
            showParam.OpenStyle.ShowType = ShowType.Modal;

            showParam.ListFilterParameter.Filter = "FDocumentStatus IN ('B','C')";

            this.View.ShowForm(showParam, new Action<FormResult>((results) =>
            {
                ListSelectedRowCollection data = results.ReturnData as ListSelectedRowCollection;
                if (data == null || data.Count == 0)
                    return;

                List<string> entryIds = data.Select(m => m.EntryPrimaryKeyValue).Distinct().ToList();
                FillEntry(entryIds);
            }));
        }

        /// <summary>按选中分录回查采购订单明细，逐行回填单据体</summary>
        private void FillEntry(List<string> entryIds)
        {
            string fEntryIds = string.Join(",", entryIds);

            string sql = $@"/*dialect*/SELECT A.FBILLNO,
                                   S.FNAME  AS FSUPPLIER,
                                   B.FMATERIALID,
                                   B.FQTY,
                                   F.FTAXPRICE,
                                   U.FNAME AS FDANWEI
                            FROM T_PUR_POORDER A
                            JOIN T_PUR_POORDERENTRY B ON A.FID = B.FID
                            JOIN T_PUR_POORDERENTRY_F F ON B.FENTRYID = F.FENTRYID
                            JOIN T_BD_SUPPLIER_L S ON A.FSUPPLIERID = S.FSUPPLIERID AND S.FLOCALEID = 2052
                            JOIN T_BD_MATERIAL M ON B.FMATERIALID = M.FMATERIALID
                            JOIN T_BD_UNIT_L U ON B.FUNITID = U.FUNITID
                            WHERE A.FDOCUMENTSTATUS IN ('B','C')
                              AND B.FENTRYID IN ({fEntryIds})
                            ORDER BY A.FBILLNO, B.FSEQ";

            DynamicObjectCollection rows = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
            if (rows == null || rows.Count == 0)
            {
                this.View.ShowMessage("选中行未查询到已审核的采购订单明细！");
                return;
            }

            var materialMeta = MetaDataServiceHelper.Load(this.Context, "BD_MATERIAL") as FormMetadata;
            DynamicObjectType materialType = materialMeta.BusinessInfo.GetDynamicObjectType();

            if (this.View.Model.GetEntryRowCount(ENTRY_KEY) > 0)
                this.View.Model.DeleteEntryRow(ENTRY_KEY, 0);

            for (int i = 0; i < rows.Count; i++)
            {
                string materialId = Convert.ToString(rows[i]["FMATERIALID"]);

                DynamicObject materialObj = BusinessDataServiceHelper.Load(
                    this.Context,
                    new object[] { materialId },
                    materialType
                ).FirstOrDefault();

                int newRow = this.View.Model.GetEntryRowCount(ENTRY_KEY);
                this.View.Model.CreateNewEntryRow(ENTRY_KEY);

                this.View.Model.SetValue(FIELD_BILLNO, Convert.ToString(rows[i]["FBILLNO"]), newRow);
                this.View.Model.SetValue(FIELD_SUPPLIER, Convert.ToString(rows[i]["FSUPPLIER"]), newRow);
                this.View.Model.SetValue(FIELD_MATERIAL, materialObj, newRow);
                this.View.Model.SetValue(FIELD_QTY, Convert.ToString(rows[i]["FQTY"]), newRow);
                //this.View.Model.SetValue(FIELD_TAXPRICE, Convert.ToString(rows[i]["FTAXPRICE"]), newRow);
                this.View.Model.SetValue(FIELD_TAXPRICE, Convert.ToDecimal(rows[i]["FTAXPRICE"]).ToString("F2"), newRow);
                this.View.Model.SetValue(FIELD_DANWEI, Convert.ToString(rows[i]["FDANWEI"]), newRow);
            }

            this.View.UpdateView(ENTRY_KEY);
        }
    }
}

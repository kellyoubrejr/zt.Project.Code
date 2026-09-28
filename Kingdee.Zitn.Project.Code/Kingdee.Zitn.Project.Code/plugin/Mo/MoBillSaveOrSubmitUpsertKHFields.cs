using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.plugin.Mo
{
    [Description("【生产订单表单】：生产订单保存/提交，根据销售订单号+物料编码回填客户方产品编码/名称/型号"), HotUpdate]
    public class MoBillSaveOrSubmitUpsertKHFields : AbstractBillPlugIn
    {
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);

            string salBillNo = Convert.ToString(this.View.Model.GetValue("F_PAEZ_XSDDH"));
            if (string.IsNullOrWhiteSpace(salBillNo))
                return;

            var entryEntity = this.View.BusinessInfo.GetEntryEntity("FTreeEntity");
            var entries = this.View.Model.GetEntityDataObject(entryEntity);
            if (entries == null || entries.Count == 0)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                var materialObj = this.View.Model.GetValue("FMaterialId", i) as DynamicObject;
                if (materialObj == null) continue;
                var materialId = Convert.ToInt64(materialObj["Id"]);
                if (materialId <= 0) continue;

                var dt = DBUtils.ExecuteDynamicObject(this.Context, $@"/*dialect*/
                    SELECT FKHFCPBM, FKHFCPMC, FKHFCPXH
                    FROM T_SAL_ORDER A
                    JOIN T_SAL_ORDERENTRY B ON A.FID = B.FID
                    WHERE A.FBILLNO = '{salBillNo.Replace("'", "''")}'
                      AND B.FMATERIALID = {materialId}");

                if (dt == null || dt.Count == 0)
                    continue;

                this.View.Model.SetValue("FKHFCPBM", Convert.ToString(dt[0]["FKHFCPBM"] ?? ""), i);
                this.View.Model.SetValue("FKHFCPMC", Convert.ToString(dt[0]["FKHFCPMC"] ?? ""), i);
                this.View.Model.SetValue("FKHFCPXH", Convert.ToString(dt[0]["FKHFCPXH"] ?? ""), i);
            }
        }
    }
}

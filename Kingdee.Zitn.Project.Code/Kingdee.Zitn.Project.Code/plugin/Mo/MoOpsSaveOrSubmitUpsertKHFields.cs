using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;
using System.Linq;

namespace Kingdee.Zitn.Project.Code.plugin.Mo
{
    [Description("【生产订单服务】：生产订单保存/提交，根据销售订单号+物料编码回填客户方产品编码/名称/型号"), HotUpdate]
    public class MoOpsSaveOrSubmitUpsertKHFields : AbstractOperationServicePlugIn
    {
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);

            var ids = string.Join(",", e.SelectedRows
                .Select(r => r.DataEntity["Id"]?.ToString())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct());

            if (string.IsNullOrWhiteSpace(ids))
                return;

            var moDt = DBUtils.ExecuteDynamicObject(this.Context, $@"/*dialect*/
                SELECT B.FENTRYID, B.FMATERIALID, A.F_PAEZ_XSDDH AS SALBILLNO
                FROM T_PRD_MO A
                JOIN T_PRD_MOENTRY B ON A.FID = B.FID
                WHERE A.FID IN ({ids})");

            if (moDt == null || moDt.Count == 0)
                return;

            foreach (DynamicObject row in moDt)
            {
                long entryId = Convert.ToInt64(row["FENTRYID"]);
                long materialId = Convert.ToInt64(row["FMATERIALID"]);
                string salBillNo = Convert.ToString(row["SALBILLNO"] ?? "");
                if (string.IsNullOrWhiteSpace(salBillNo))
                    continue;

                var salDt = DBUtils.ExecuteDynamicObject(this.Context, $@"/*dialect*/
                    SELECT FKHFCPBM, FKHFCPMC, FKHFCPXH
                    FROM T_SAL_ORDER A
                    JOIN T_SAL_ORDERENTRY B ON A.FID = B.FID
                    WHERE A.FBILLNO = '{salBillNo.Replace("'", "''")}'
                      AND B.FMATERIALID = {materialId}");

                if (salDt == null || salDt.Count == 0)
                    continue;

                var bm = Convert.ToString(salDt[0]["FKHFCPBM"] ?? "");
                var mc = Convert.ToString(salDt[0]["FKHFCPMC"] ?? "");
                var xh = Convert.ToString(salDt[0]["FKHFCPXH"] ?? "");

                DBUtils.Execute(this.Context, $@"/*dialect*/
                    UPDATE T_PRD_MOENTRY
                    SET FKHFCPBM = '{bm.Replace("'", "''")}',
                        FKHFCPMC = '{mc.Replace("'", "''")}',
                        FKHFCPXH = '{xh.Replace("'", "''")}'
                    WHERE FENTRYID = {entryId}");
            }
        }
    }
}

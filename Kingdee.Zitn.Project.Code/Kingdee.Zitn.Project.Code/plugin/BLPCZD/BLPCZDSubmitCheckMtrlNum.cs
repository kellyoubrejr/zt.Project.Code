using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.plugin.BLPCZD
{
    [Description("【不良品处置单提交服务】:，提交检测物料编码是否为804/6开头"), HotUpdate]
    public class BLPCZDSubmitCheckMtrlNum : AbstractOperationServicePlugIn
    {
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);

            foreach (var billObj in e.DataEntitys)
            {
                string billNo = Convert.ToString(billObj["BillNo"]);

                string sql1 = $@"
                /*dialect*/
                SELECT COUNT(*) AS CNT
                FROM T_QM_DEFECTPROCESS A
                JOIN T_QM_DEFECTPROCESSENTRY B ON A.FID = B.FID
                JOIN T_BD_MATERIAL M1 ON B.FMATERIALID = M1.FMATERIALID
                WHERE A.FBILLNO = '{billNo}'";

                string sql2 = $@"
                /*dialect*/
                SELECT COUNT(*) AS CNT
                FROM T_QM_DEFECTPROCESS A
                JOIN T_QM_DEFECTPROCESSENTRY B ON A.FID = B.FID
                JOIN T_BD_MATERIAL M1 ON B.FMATERIALID = M1.FMATERIALID
                WHERE A.FBILLNO = '{billNo}'
                  AND (M1.FNUMBER LIKE '804%' OR M1.FNUMBER LIKE '806%')";

                var result1 = DBUtils.ExecuteDynamicObject(this.Context, sql1);
                var result2 = DBUtils.ExecuteDynamicObject(this.Context, sql2);

                int totalCount = 0;
                int matchCount = 0;

                if (result1 != null && result1.Count > 0)
                {
                    totalCount = Convert.ToInt32(result1[0]["CNT"]);
                }

                if (result2 != null && result2.Count > 0)
                {
                    matchCount = Convert.ToInt32(result2[0]["CNT"]);
                }

                string updateSql = "";

                if (totalCount == matchCount && totalCount > 0)
                {
                    updateSql = $@"
                    /*dialect*/
                    UPDATE T_QM_DEFECTPROCESS
                    SET FFLAG = N'是'
                    WHERE FBILLNO = '{billNo}'";
                }
                else
                {
                    // 不满足
                    updateSql = $@"
                    /*dialect*/
                    UPDATE T_QM_DEFECTPROCESS
                    SET FFLAG = N'否'
                    WHERE FBILLNO = '{billNo}'";
                }

                DBUtils.Execute(this.Context, updateSql);
            }
        }
    }
}

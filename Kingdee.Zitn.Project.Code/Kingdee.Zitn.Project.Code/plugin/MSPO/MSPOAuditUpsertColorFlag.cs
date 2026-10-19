using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.plugin.MSPO
{
    [HotUpdate, Description("【免审采购订单审核服务】遍历单据体根据PO、GYS、WL更新颜色标志")]
    public class MSPOAuditUpsertColorFlag : AbstractOperationServicePlugIn
    {
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            foreach (var billObj in e.DataEntitys)
            {
                string billNo = Convert.ToString(billObj["FPURNO"]);
                string GYS = Convert.ToString(billObj["FSUPPLIER"]);
                string FMATERIAL = Convert.ToString(billObj["FMATERIAL"]);
                string XIAXIAN = Convert.ToString(billObj["F_ZMER_TEXT_83G"]);
                string SHANGXIAN = Convert.ToString(billObj["F_ZMER_TEXT_QTR"]);
                string HANSHUIDANJIA = Convert.ToString(billObj["FTAXPRICE"]);

                string entitySql = $@"
                                    /*dialect*/
                                    SELECT  C.FNAME AS FSUPPLIERNAME,FMATERIALID,FQTY,FTAXPRICE,B.FENTRYID 
                                    FROM T_PUR_POORDER A 
                                    JOIN T_PUR_POORDERENTRY B ON A.FID = B.FID 
                                    JOIN T_PUR_POORDERENTRY_F B1 ON B.FENTRYID = B1.FENTRYID 
                                    JOIN T_BD_SUPPLIER_L C ON A.FSUPPLIERID = C.FSUPPLIERID
                                    WHERE FBILLNO = '{billNo}' AND C.FNAME = '{GYS}' AND FMATERIALID = '{FMATERIAL}'AND A.FDOCUMENTSTATUS = 'B'";
                var result = DBUtils.ExecuteDynamicObject(this.Context, entitySql);
                if (result == null || result.Count == 0) continue;
                foreach (var entity in result)
                {
                    decimal qty = Convert.ToDecimal(entity["FQTY"]);
                    decimal taxPrice = Convert.ToDecimal(entity["FTAXPRICE"]);
                    long entryId = Convert.ToInt64(entity["FENTRYID"]);
                    //判断免审的上下限和含税单价与采购订单的数量和含税单价是否符合条件
                    //数量大于下限，小于等于上限，含税单价小于等于采购订单含税单价
                    string updateSql = string.Empty;
                    if (qty > Convert.ToDecimal(XIAXIAN) && qty <= Convert.ToDecimal(SHANGXIAN) && taxPrice <= Convert.ToDecimal(HANSHUIDANJIA))
                    {
                        // 🟢 满足免审
                        updateSql = $"/*dialect*/UPDATE T_PUR_POORDERENTRY SET FCOLORFLAG = N'存在免审记录' WHERE FENTRYID = {entryId}";
                    }
                    else
                    {
                        // 🔵 有配置但不满足
                        updateSql = $"/*dialect*/UPDATE T_PUR_POORDERENTRY SET FCOLORFLAG = N'存在免审但不满足条件' WHERE FENTRYID = {entryId}";
                    }
                    DBUtils.Execute(this.Context, updateSql);
                }
            }
        }
    }
}

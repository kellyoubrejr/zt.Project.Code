using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdee.Zitn.Project.Code.plugin.PO
{
    [Description("【采购订单单据插件】点击【确认】按钮,获取物料等信息，然后设置采购订单单据体颜色")]
    [HotUpdate]
    public class PoAuditSetEntryBackColor : AbstractBillPlugIn
    {
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);

            if (!string.Equals(e.Key, "F_ZMER_Button_qtr", System.StringComparison.OrdinalIgnoreCase))
                return;
            //获取当前时间
            string FTIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            //var delsql = string.Format(@"/*dialect*/DELETE FROM ZMER_t_Cust_Entry100101 WHERE FPURNO = '历史数据'");
            //DBUtils.Execute(this.Context, delsql);

            var entity = this.View.BusinessInfo.GetEntity("F_ZMER_Entity_ne1");
            if (entity == null)
                return;

            int rowCount = this.Model.GetEntryRowCount("F_ZMER_Entity_ne1");

            for (int i = 0; i < rowCount; i++)
            {
                string billNo = this.View.Model.GetValue("FPURNO", i).ToString();

                DynamicObject materialObj = this.View.Model.GetValue("FMaterialId", i) as DynamicObject;
                if (materialObj == null)
                    return;

                string wlid = Convert.ToString(materialObj["Id"]);

                string sql = string.Format(
                    "/*dialect*/SELECT FNUMBER FROM T_BD_MATERIAL WHERE FMATERIALID = '{0}'",
                    wlid);

                DynamicObjectCollection result = DBUtils.ExecuteDynamicObject(this.Context, sql);

                if (result == null || result.Count == 0)
                    return;

                string wlnum = Convert.ToString(result[0]["FNUMBER"]);

                var purQuery = string.Format("/*dialect*/SELECT B.FENTRYID FROM T_PUR_POORDER A JOIN T_PUR_POORDERENTRY B ON A.FID = B.FID JOIN T_BD_MATERIAL M1 ON B.FMATERIALID = M1.FMATERIALID WHERE  FBILLNO = '{0}' AND M1.FNUMBER = '{1}'", billNo, wlnum);
                DynamicObjectCollection purQueryResult = DBUtils.ExecuteDynamicObject(this.Context, purQuery);
                if (purQueryResult != null && purQueryResult.Count > 0)
                {
                    for (int ii = 0; ii < purQueryResult.Count; ii++)
                    {
                        long entryId = Convert.ToInt64(purQueryResult[ii]["FENTRYID"]);

                        var updSql = string.Format("/*dialect*/update b set b.f_flag = '1' from T_PUR_POORDER a join T_PUR_POORDERENTRY b on a.fid = b.fid  where a.fbillno ='{0}' and b.fmaterialid = '{1}' and b.fentryid = {2}", billNo, wlid, entryId);
                        DBUtils.Execute(this.Context, updSql);

                        var updateSql = $"/*dialect*/UPDATE T_PUR_POORDERENTRY SET FCOLORFLAG = N'存在免审记录',FTIME = '{FTIME}' WHERE FENTRYID = {entryId}";
                        DBUtils.Execute(this.Context, updateSql);

                    }
                }
            }
            //this.View.ReturnToParentWindow(true);

            //this.View.Refresh();
            //this.View.ReturnToParentWindow(true);

        }
    }
}

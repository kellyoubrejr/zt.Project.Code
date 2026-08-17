using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.WebApi.Client;
using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.plugin.PO
{
    [Description("【采购订单服务operation】:提交，计算最近一次采购金额"), HotUpdate]
    public class PoPriceOperation : AbstractOperationServicePlugIn
    {
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);

            #region WebApi 登录

            var client = new K3CloudApiClient(ErpLogin.K3CloudUrl);
            var loginResult = client.ValidateLogin(
                ErpLogin.AppId,
                ErpLogin.UserName,
                ErpLogin.Password,
                ErpLogin.Lcid
            );

            if (JObject.Parse(loginResult)["LoginResultType"].Value<int>() != 1)
                return;

            #endregion

            /*var ids = string.Join(",", e.DataEntitys.Select(o => o[0]));*/

            string ids = string.Empty;

            foreach (DynamicObject obj in e.DataEntitys)
            {
                long fid = Convert.ToInt64(obj["Id"]);

                if (!string.IsNullOrWhiteSpace(ids))
                    ids += ",";

                ids += fid.ToString();
            }

            string query = $@"SELECT
                                  A.FBILLNO,
                                  B.FMATERIALID
                                FROM
                                  T_PUR_POORDER A
                                  JOIN T_PUR_POORDEREntry B ON B.FID = A.FID
                                WHERE
                                  A.FID IN ({ids})";
            DynamicObjectCollection coll = DBUtils.ExecuteDynamicObject(this.Context, query);
            if (coll != null && coll.Count > 0)
            {
                for (int i = 0; i < coll.Count; i++)
                {
                    string fbillno = Convert.ToString(coll[i]["FBILLNO"]);
                    string fmaterialid = Convert.ToString(coll[i]["FMATERIALID"]);

                    string query1 = string.Format($@"SELECT TOP
                                                              1 A.FBillno,
                                                              A.FSUPPLIERID,
                                                              C.FTAXPRICE,
                                                              B.Fqty from T_PUR_POORDER A
                                                              JOIN T_PUR_POORDEREntry B ON B.Fid= A.FID
                                                              JOIN T_PUR_POORDERENTRY_F C ON C.FENTRYID= B.FENTRYID 
                                                            WHERE
                                                              A.FDOCUMENTSTATUS= 'C' 
                                                              AND A.Fbillno<> '{fbillno}' 
                                                              AND B.FMATERIALID= '{fmaterialid}' 
                                                            ORDER BY
                                                              A.Fdate DESC");
                    DynamicObjectCollection coll1 = DBUtils.ExecuteDynamicObject(this.Context, query1);
                    if (coll1 != null && coll1.Count > 0)
                    {
                        for (int j = 0; j < coll1.Count; j++)
                        {
                            double ftaxprice = Convert.ToDouble(coll1[j]["FTAXPRICE"]);
                            int fqty = Convert.ToInt32(coll1[j]["Fqty"]);

                            string upd = string.Format(@"UPDATE b
                                                            SET 
                                                                  b.F_ZITN_LASTPOQTY = '{0}',
                                                                b.F_ZITN_LASTTAXPRICE = '{1}',
                                                                b.F_UNW_TEXT_RE5 = CAST('{0}' AS VARCHAR(MAX)) + '×' + CAST('{1}' AS VARCHAR(MAX))
                                                            FROM T_PUR_POORDER A
                                                              JOIN T_PUR_POORDEREntry B ON B.Fid= A.FID
                                                              JOIN T_PUR_POORDERENTRY_F C ON C.FENTRYID= B.FENTRYID 
                                                            WHERE a.FBILLNO = '{2}' AND b.FMATERIALID='{3}'", fqty, ftaxprice, fbillno, fmaterialid);
                            DBUtils.Execute(Context, upd);
                        }
                    }
                }
            }

        }
    }
}

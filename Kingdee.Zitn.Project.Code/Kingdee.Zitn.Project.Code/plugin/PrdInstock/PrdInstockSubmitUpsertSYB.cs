using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.WebApi.Client;
using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Kingdee.K3.MFG.App.AppServiceContext;

namespace Kingdee.Zitn.Project.Code.plugin.PRDinstock
{
    [Description("【生产入库提交服务】--检测生产入库单是否有产品事业部，没有更新为物料事业部"), HotUpdate]
    public class PrdInstockSubmitUpsertSYB : AbstractOperationServicePlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("生产入库提交更新事业部");

        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            try
            {
                base.AfterExecuteOperationTransaction(e);

                var ids = string.Join(",",
                    e.DataEntitys.Select(o => o[0]));

                _log.Section($"提交开始，FIDs: {ids}");

                var client = new K3CloudApiClient(ErpLogin.K3CloudUrl);
                var loginResult = client.ValidateLogin(
                    ErpLogin.AppId,
                    ErpLogin.UserName,
                    ErpLogin.Password,
                    ErpLogin.Lcid
                );

                _log.WriteLog($"登录结果: {loginResult}");

                var resultType =
                    JObject.Parse(loginResult)["LoginResultType"]
                    .Value<int>();

                if (resultType != 1)
                {
                    _log.WriteLog($"登录失败，LoginResultType={resultType}，跳过推送");
                    return;
                }

                _log.WriteLog("登录成功");

                var query = string.Format(@"
                        SELECT B.FCPSYB
                        FROM T_PRD_INSTOCK A JOIN T_PRD_INSTOCKENTRY B ON A.FID = B.FID
                        WHERE A.FID IN ({0})", ids);

                DynamicObjectCollection collection =
                    DbUtils.ExecuteDynamicObject(
                        this.Context,
                        query);

                if (collection == null || collection.Count == 0)
                {
                    _log.WriteLog($"未查询到运单号，FIDs: {ids}");
                    return;
                }
                for (int i = 0; i < collection.Count; i++)
                {
                    var syb = Convert.ToString(collection[i]["FCPSYB"]);

                    if (string.IsNullOrWhiteSpace(syb))
                    {
                        //var updateQuery = string.Format(@"
                        //                                                    UPDATE A
                        //                        SET A.FCPSYB = (
                        //                            SELECT TOP 1 B.F_PAEZ_SYB 
                        //                            FROM T_BD_MATERIAL B 
                        //                            WHERE B.FMATERIALID = A.FMATERIALID
                        //                        )
                        //                        FROM T_PRD_INSTOCKENTRY A
                        //                        WHERE A.FID IN ({0})", ids);
                        var updateQuery = string.Format(@"
    UPDATE T_PRD_INSTOCKENTRY 
    SET FCPSYB = (
        SELECT TOP 1 B.F_PAEZ_SYB 
        FROM T_BD_MATERIAL B 
        WHERE B.FMATERIALID = T_PRD_INSTOCKENTRY.FMATERIALID
    )
    WHERE FID IN ({0})", ids);
                        DbUtils.Execute(this.Context, updateQuery);
                        _log.WriteLog($"FID={ids}，事业部为空，已更新");
                    }
                    else
                    {
                        _log.WriteLog($"FID={ids}，事业部已存在-{syb}-，跳过更新");
                    }
                    
                }


            }
            catch (Exception ex)
            {
                _log.Error("审核插件异常");
                _log.Error(ex);
                _log.Error($"完整异常: {ex}");
            }
        }
    }
}

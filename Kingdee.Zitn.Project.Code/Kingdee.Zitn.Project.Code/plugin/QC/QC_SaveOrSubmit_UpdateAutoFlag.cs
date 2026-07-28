using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using Kingdee.BOS.WebApi.Client;
using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Kingdee.Zitn.Project.Code.plugin.QC
{
    [Description("【检验单保存/提交服务】--检验单更新是否自动审核标志"), HotUpdate]
    public class QC_SaveOrSubmit_UpdateAutoFlag : AbstractOperationServicePlugIn
    {
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);

            var log = CustomLog.For("检验单");

            var client = new K3CloudApiClient(ErpLogin.K3CloudUrl);
            log.WriteLog("登录....");
            var loginResult = client.ValidateLogin(
                ErpLogin.AppId,
                ErpLogin.UserName,
                ErpLogin.Password,
                ErpLogin.Lcid
                );
            if (JObject.Parse(loginResult)["LoginResultType"].Value<int>() != 1)
            {
                log.Error("登录失败");
                return;
            }
            log.WriteLog("登录成功");
            log.WriteLog("=========================检验单更新是否自动审核标志===========================");

            var allIds = e.SelectedRows
                            .Select(row => row.DataEntity["Id"]?.ToString())
                            .ToList();
            log.Section($"选中行数: {allIds.Count}, FIDs: {string.Join(",", allIds)}");

            if (allIds.Count == 0) return;

            var ids = string.Join(",", allIds);

            var sql = $@"/*dialect*/SELECT A.FBILLNO, B1.FINSPECTRESULT
                                    FROM T_QM_INSPECTBILL A
                                    JOIN T_QM_INSPECTBILLENTRY B ON A.FID = B.FID
                                    JOIN T_QM_INSPECTBILLENTRY_A B1 ON B.FENTRYID = B1.FENTRYID
                                    WHERE A.FID IN ({ids})";
            var dt = DBUtils.ExecuteDynamicObject(this.Context, sql);
            log.Section($"查询到检验结果条目数: {dt?.Count ?? 0}");

            if (dt == null || dt.Count == 0) return;

            var auditFlags = new Dictionary<string, bool>();
            for (int i = 0; i < dt.Count; i++)
            {
                var billNo = dt[i]["FBILLNO"]?.ToString();
                if (string.IsNullOrEmpty(billNo)) continue;

                var result = dt[i]["FINSPECTRESULT"] == null
                    ? 0
                    : long.Parse(dt[i]["FINSPECTRESULT"].ToString());

                if (!auditFlags.ContainsKey(billNo))
                {
                    auditFlags[billNo] = true;
                }

                if (result != 1)
                {
                    auditFlags[billNo] = false;
                }
            }

            foreach (var kvp in auditFlags)
            {
                var flag = kvp.Value ? "是" : "否";
                var updateSql = $@"/*dialect*/UPDATE T_QM_INSPECTBILL SET FISAUDIT = '{flag}' WHERE FBILLNO = '{kvp.Key}'";
                DBUtils.Execute(this.Context, updateSql);
                log.WriteLog($"FBILLNO={kvp.Key}, 全部合格={kvp.Value}, FISAUDIT更新为{flag}");
            }
        }
    }
}

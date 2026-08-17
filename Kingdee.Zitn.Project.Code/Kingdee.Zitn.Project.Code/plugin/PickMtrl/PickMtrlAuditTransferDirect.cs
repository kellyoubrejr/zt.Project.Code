using Kingdee.BOS.App.Data;
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

namespace Kingdee.Zitn.Project.Code.plugin.PickMtrl
{
    [Description("【生产领料单服务operation】:审核，根据仓库超发数量自动下推直接调拨单"), HotUpdate]
    public class PickMtrlAuditTransferDirect : AbstractOperationServicePlugIn
    {
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);

            List<long> entryIdList = new List<long>();
            string moBillno = string.Empty;
            string billNo = string.Empty;
            string fatherNum = string.Empty;

            foreach (DynamicObject billObj in e.DataEntitys)
            {
                billNo = Convert.ToString(billObj["BillNo"]);

                var moQurey = string.Format("/*dialect*/SELECT DISTINCT FMOBILLNO,M1.FNUMBER FROM T_PRD_PICKMTRL A JOIN T_PRD_PICKMTRLDATA B ON A.FID = B.FID JOIN T_PRD_PICKMTRLDATA_A B1 ON B.FENTRYID = B1.FENTRYID JOIN T_BD_MATERIAL M1 ON B1.FPARENTMATERIALID = M1.FMATERIALID WHERE FBILLNO = '{0}'", billNo);
                DynamicObjectCollection moRows = DBUtils.ExecuteDynamicObject(this.Context, moQurey);
                if (moRows != null && moRows.Count > 0)
                {
                    for (int i = 0; i < moRows.Count; i++)
                    {
                        moBillno = Convert.ToString(moRows[i]["FMOBILLNO"]);
                        fatherNum = Convert.ToString(moRows[i]["FNUMBER"]);
                    }
                }

                var query = string.Format("/*dialect*/SELECT FENTRYID,FSTKRGQTY FROM T_PRD_PICKMTRL A JOIN T_PRD_PICKMTRLDATA B ON A.FID = B.FID WHERE FBILLNO = '{0}'", billNo);
                DynamicObjectCollection rows = DBUtils.ExecuteDynamicObject(this.Context, query);
                if (rows != null && rows.Count > 0)
                {
                    for (int i = 0; i < rows.Count; i++)
                    {
                        decimal FSTKRGQTY = Convert.ToDecimal(rows[i]["FSTKRGQTY"]);
                        int FSTKRGQTYInt = Convert.ToInt32(FSTKRGQTY);

                        if (FSTKRGQTYInt > 0)
                        {
                            long entryId = Convert.ToInt64(rows[i]["FENTRYID"]);
                            entryIdList.Add(entryId);
                        }
                    }
                }
            }


            if (!entryIdList.Any())
                return;

            var client = new K3CloudApiClient(ErpLogin.K3CloudUrl);
            var loginResult = client.ValidateLogin(
                ErpLogin.AppId,
                ErpLogin.UserName,
                ErpLogin.Password,
                ErpLogin.Lcid
            );

            if (JObject.Parse(loginResult)["LoginResultType"].Value<int>() != 1)
                return;

            // ===== 下推 =====           

            string entryIds = string.Join(",", entryIdList.Distinct());

            JObject jObj = new JObject();

            jObj.Add("Ids", "");
            jObj.Add("Numbers", new JArray());
            jObj.Add("EntryIds", entryIds);
            jObj.Add("RuleId", "ZMER_PickMtrlToZJDB");
            jObj.Add("TargetBillTypeId", "");
            jObj.Add("TargetOrgId", 0);
            jObj.Add("TargetFormId", "STK_TransferDirect");
            jObj.Add("IsEnableDefaultRule", false);
            jObj.Add("IsDraftWhenSaveFail", true);
            jObj.Add("CustomParams", new JObject());

            var resultJson = client.Push("PRD_PickMtrl", jObj.ToString());

            var pushResult = JObject.Parse(resultJson);

            bool isSuccess = pushResult["Result"]["ResponseStatus"]["IsSuccess"].Value<bool>();
            if (!isSuccess)
                return;

            var successList = pushResult["Result"]["ResponseStatus"]["SuccessEntitys"] as JArray;

            foreach (var item in successList)
            {
                long fid = item["Id"].Value<long>();
                var FentryIds = item["EntryIds"]["FBillEntry"] as JArray;

                JObject model = new JObject();
                model["FID"] = fid;

                JArray entryArray = new JArray();

                foreach (var entryIdToken in FentryIds)
                {
                    long entryId = entryIdToken.Value<long>();

                    JObject entry = new JObject();
                    entry["FEntryID"] = entryId;

                    entry["Fisflag"] = true;
                    entry["Fmobillno"] = moBillno;
                    entry["Fpickmtrlbillno"] = billNo;
                    entry["FCPNUM"] = fatherNum;

                    entryArray.Add(entry);
                }

                model["FBillEntry"] = entryArray;

                JObject saveObj = new JObject();
                saveObj["Model"] = model;

                saveObj["NeedUpDateFields"] = new JArray(
                    "Fisflag",
                    "Fmobillno",
                    "Fpickmtrlbillno", "FCPNUM"
                );

                saveObj["IsDeleteEntry"] = false;

                var saveResult = client.Save("STK_TransferDirect", saveObj.ToString());

                var saveResultObj = JObject.Parse(saveResult);
                if (saveResultObj["Result"]["ResponseStatus"]["IsSuccess"].Value<bool>())
                {
                    JObject submitObj = new JObject();
                    submitObj["Ids"] = fid.ToString();
                    client.Submit("STK_TransferDirect", submitObj.ToString());
                }
            }
        }
    }
}

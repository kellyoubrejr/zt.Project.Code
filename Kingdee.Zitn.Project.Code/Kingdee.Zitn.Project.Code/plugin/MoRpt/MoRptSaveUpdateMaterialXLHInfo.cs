using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.plugin.MoRpt
{
    [HotUpdate, Description("【生产汇报保存服务】更新物料序列号主档信息")]
    public class MoRptSaveUpdateMaterialXLHInfo : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FSerialId");
            e.FieldKeys.Add("F_ISDSJ");
            e.FieldKeys.Add("FSCLOTNO");
            e.FieldKeys.Add("F_XM");
        }

        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            Boolean IsSuccess = this.OperationResult.IsSuccess;
            if (!IsSuccess) return;
            foreach (DynamicObject entity in e.DataEntitys)
            {
                DynamicObjectCollection entrys = entity["PRD_MORPTENTRY"] as DynamicObjectCollection;
                foreach (DynamicObject entry in entrys)
                {
                    string FSCLOTNO = Convert.ToString(entry["FSCLOTNO"]);
                    string F_XM = entry["F_XM"].IsNullOrEmptyOrWhiteSpace() ? "0" : Convert.ToString((entry["F_XM"] as DynamicObject)["Id"]);
                    DynamicObjectCollection serialEntrys = entry["SerialSubEntity"] as DynamicObjectCollection;
                    foreach (DynamicObject serialEntry in serialEntrys)
                    {
                        string F_ISDSJ = Convert.ToString(serialEntry["F_ISDSJ"]);
                        string SerialId = Convert.ToString(serialEntry["SerialId_Id"]);
                        string updateSerialSql = string.Format("update T_BD_SERIALMASTER set F_SCLOTNO='{1}',F_XM='{2}',F_ISDSJ='{3}' where FSERIALID='{0}'", SerialId, FSCLOTNO, F_XM, F_ISDSJ);
                        DBServiceHelper.ExecuteDynamicObject(this.Context, updateSerialSql);
                    }
                }
            }
        }
    }
}

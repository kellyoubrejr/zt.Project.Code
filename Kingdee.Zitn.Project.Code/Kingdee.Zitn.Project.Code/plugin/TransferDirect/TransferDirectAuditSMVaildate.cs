using Kingdee.BOS;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.plugin.TransferDirect
{
    [Description("【直接调拨单据插件-提交按钮】直接调拨单提交扫码校验物料+序列号一致性"), HotUpdate]
    public class TransferDirectAuditSMVaildate : AbstractBillPlugIn
    {
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);

            if (!e.BarItemKey.Equals("tbSplitSubmit", StringComparison.OrdinalIgnoreCase))
                return;

            var entryEntity = this.View.BusinessInfo.GetEntryEntity("FBillEntry");
            var serialEntity = this.View.BusinessInfo.GetEntryEntity("FSerialSubEntity");
            var serialPropName = serialEntity.DynamicObjectType.Name;

            var entries = this.View.Model.GetEntityDataObject(entryEntity);
            if (entries == null || entries.Count == 0)
                return;

            // 读取扫码单据体，构建物料编码#序列号 集合
            var scanEntity = this.View.BusinessInfo.GetEntryEntity("F_ZMER_Entity_83g");
            var scanData = this.View.Model.GetEntityDataObject(scanEntity);
            var scanSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (scanData != null)
            {
                foreach (DynamicObject scanRow in scanData)
                {
                    var fsmnr = Convert.ToString(scanRow["FSMNR"] ?? "").Trim();
                    if (!string.IsNullOrEmpty(fsmnr))
                        scanSet.Add(fsmnr);
                }
            }

            var mismatchList = new List<string>();
            bool hasMismatch = false;

            for (int i = 0; i < entries.Count; i++)
            {
                var materialObj = this.View.Model.GetValue("FMaterialId", i) as DynamicObject;
                if (materialObj == null) continue;
                var materialNo = Convert.ToString(materialObj["Number"]);

                var serials = entries[i][serialPropName] as DynamicObjectCollection;
                if (serials == null || serials.Count == 0) continue;

                for (int j = 0; j < serials.Count; j++)
                {
                    var serial = serials[j];
                    var serialNo = Convert.ToString(serial["SerialNo"] ?? "");
                    var scanKey = materialNo + "#" + serialNo;

                    if (scanSet.Contains(scanKey))
                    {
                        serial["FSMQY"] = scanKey;
                        serial["FISYZ"] = "是";
                    }
                    else
                    {
                        serial["FISYZ"] = "否";
                        hasMismatch = true;
                        mismatchList.Add($"物料{materialNo} 序列号{serialNo}");
                    }
                }
            }

            this.View.UpdateView();

            if (hasMismatch)
            {
                throw new KDBusinessException("本次发货产品与调拨单不一致，请检查", "本次发货产品与调拨单不一致，请检查!");
            }
        }
    }
}

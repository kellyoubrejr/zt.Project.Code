using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.plugin.OutStock
{
    [Description("【销售出库单据插件-提交按钮】销售出库单提交扫码校验物料+序列号一致性"), HotUpdate]
    public class OutStockAuditSMVaildate : AbstractBillPlugIn
    {
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);

            if (!e.BarItemKey.Equals("tbSplitSubmit", StringComparison.OrdinalIgnoreCase))
                return;

            var entryEntity = this.View.BusinessInfo.GetEntryEntity("FEntity");
            var serialEntity = this.View.BusinessInfo.GetEntryEntity("FSerialSubEntity");
            var serialPropName = serialEntity.DynamicObjectType.Name;

            var entries = this.View.Model.GetEntityDataObject(entryEntity);
            if (entries == null || entries.Count == 0)
                return;

            // 读取扫码单据体，解析包装序列号并反查包装箱标签，构建物料编码#序列号 集合
            var scanEntity = this.View.BusinessInfo.GetEntryEntity("F_ZMER_Entity_re5");
            var scanData = this.View.Model.GetEntityDataObject(scanEntity);
            var scanSet = BuildBoxScanSet(scanData);

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
                throw new KDBusinessException("本次发货产品与出库单不一致，请检查", "本次发货产品与出库单不一致，请检查!");
            }
        }

        /// <summary>
        /// 解析扫码内容（P包装序列号#...），按包装序列号反查包装箱标签，返回 物料编码#序列号 集合
        /// </summary>
        private HashSet<string> BuildBoxScanSet(DynamicObjectCollection scanData)
        {
            var scanSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (scanData == null)
                return scanSet;

            foreach (DynamicObject scanRow in scanData)
            {
                var fsmnr = Convert.ToString(scanRow["FSMNR"] ?? "").Trim();
                if (string.IsNullOrEmpty(fsmnr))
                    continue;

                // 取第一个#之前的包装序列号（P为固定字符）
                var fbzxLH = fsmnr.Split('#')[0].Trim();
                if (string.IsNullOrEmpty(fbzxLH))
                    continue;

                // 反查包装箱标签，取包含的物料+序列号
                var sql = string.Format(
                    "/*dialect*/SELECT B.FWLNUM, B.FXLH FROM ZMER_t_Cust100031 A " +
                    "INNER JOIN ZMER_t_Cust_Entry100089 B ON A.FID = B.FID " +
                    "WHERE A.FBZXLH = '{0}'",
                    fbzxLH.Replace("'", "''"));

                var dt = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (dt == null || dt.Count == 0)
                    continue;

                for (int j = 0; j < dt.Count; j++)
                {
                    var wlnum = Convert.ToString(dt[j]["FWLNUM"] ?? "").Trim();
                    var xlh = Convert.ToString(dt[j]["FXLH"] ?? "").Trim();
                    if (!string.IsNullOrEmpty(wlnum))
                        scanSet.Add(wlnum + "#" + xlh);
                }
            }

            return scanSet;
        }
    }
}

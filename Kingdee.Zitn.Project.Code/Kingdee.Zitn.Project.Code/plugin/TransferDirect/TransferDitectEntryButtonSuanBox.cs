using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.Zitn.Project.Code.conf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Kingdee.Zitn.Project.Code.plugin.TransferDirect
{
    [Description("【直接调拨单据插件-分录按钮】直接调拨单分录按钮实现重新算箱"), HotUpdate]
    public class TransferDitectEntryButtonSuanBox : AbstractBillPlugIn
    {
        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);

            if (!(e.Operation.ToString() ?? "").Contains("Save"))
                return;

            var log = CustomLog.For("直接调拨单");
            log.Section("保存前自动算箱");

            try
            {
                var entryEntity = this.View.BusinessInfo.GetEntryEntity("FBillEntry");
                var serialEntity = this.View.BusinessInfo.GetEntryEntity("FSerialSubEntity");
                var serialPropName = serialEntity?.DynamicObjectType.Name;

                var entries = this.View.Model.GetEntityDataObject(entryEntity);
                if (entries == null || entries.Count == 0)
                    return;

                for (int i = 0; i < entries.Count; i++)
                {
                    var materialObj = this.View.Model.GetValue("FMaterialId", i) as DynamicObject;
                    if (materialObj == null) continue;
                    var materialId = Convert.ToInt64(materialObj["Id"]);
                    var materialNo = Convert.ToString(materialObj["Number"]);
                    var qty = Convert.ToDecimal(this.View.Model.GetValue("FQTY", i));

                    if (qty <= 0) continue;

                    var specSql = $@"/*dialect*/SELECT FFHBZLB, FFHBZGG, FSCBZGG1, FFHBZSL1
FROM ZMER_BZGGQD
WHERE FCP = '{materialId}'
AND FDOCUMENTSTATUS = 'C' AND FFORBIDSTATUS = 'A'
ORDER BY FFHBZLB, FSCBZGG1, FCP";

                    var specs = DBUtils.ExecuteDynamicObject(this.Context, specSql);
                    if (specs == null || specs.Count == 0) continue;

                    var groupBoxOptions = new Dictionary<string, (decimal PerBox, int BoxCount, string FFHBZLB, string FSCBZGG, string FFHBZGG)>();
                    for (int j = 0; j < specs.Count; j++)
                    {
                        var ffhbzlb = Convert.ToString(specs[j]["FFHBZLB"] ?? "");
                        var fscbzgg = Convert.ToString(specs[j]["FSCBZGG1"] ?? "");
                        var ffhbzgg = Convert.ToString(specs[j]["FFHBZGG"] ?? "");
                        var ffhbzsl = Convert.ToDecimal(specs[j]["FFHBZSL1"]);

                        if (ffhbzsl <= 0) continue;

                        var groupKey = $"{ffhbzlb}|{fscbzgg}";
                        if (!groupBoxOptions.ContainsKey(groupKey))
                        {
                            var boxes = (int)Math.Ceiling(qty / ffhbzsl);
                            groupBoxOptions[groupKey] = (ffhbzsl, boxes, ffhbzlb, fscbzgg, ffhbzgg);
                        }
                    }

                    if (groupBoxOptions.Count == 0) continue;

                    var best = groupBoxOptions.Values
                        .OrderBy(o => o.BoxCount)
                        .ThenBy(o => qty % o.PerBox == 0 ? 0 : 1)
                        .ThenByDescending(o => o.PerBox)
                        .ThenBy(o => o.FFHBZLB == "2" ? 0 : 1)
                        .First();

                    int minBoxes = best.BoxCount;
                    decimal bestPerBox = best.PerBox;

                    log.WriteLog($"  第{i + 1}行 物料{materialNo} 数量={qty}, 最优: {best.FFHBZLB}/{best.FFHBZGG}, 每箱{bestPerBox}, 共{minBoxes}箱");

                    this.View.Model.SetValue("FFHBZLB", best.FFHBZLB, i);
                    this.View.Model.SetValue("FFHBZSL", bestPerBox, i);
                    this.View.Model.SetValue("FFHBZGG", best.FFHBZGG, i);
                    this.View.Model.SetValue("FSCBZGG", best.FSCBZGG, i);

                    if (!string.IsNullOrEmpty(serialPropName))
                    {
                        var serials = entries[i][serialPropName] as DynamicObjectCollection;
                        if (serials != null && serials.Count > 0)
                        {
                            for (int k = 0; k < serials.Count; k++)
                            {
                                int boxNo = (int)(k / bestPerBox) + 1;
                                serials[k]["FXC"] = boxNo;
                            }
                        }
                    }
                }

                this.View.UpdateView();
            }
            catch (Exception ex)
            {
                log.Error($"保存前算箱异常: {ex.Message}");
                log.Error(ex);
            }

            log.Section("保存前算箱结束");
        }

        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);

            if (!e.BarItemKey.Equals("tbSuanBox", StringComparison.OrdinalIgnoreCase))
                return;

            var log = CustomLog.For("直接调拨单");
            log.Section("手工重新算箱");

            var header = this.View.Model.DataObject;
            var billNo = Convert.ToString(header["BillNo"]);

            try
            {
                log.WriteLog($"开始处理: {billNo}");

                var entryEntity = this.View.BusinessInfo.GetEntryEntity("FBillEntry");
                var serialEntity = this.View.BusinessInfo.GetEntryEntity("FSerialSubEntity");
                var serialPropName = serialEntity.DynamicObjectType.Name;

                var entries = this.View.Model.GetEntityDataObject(entryEntity);
                if (entries == null || entries.Count == 0)
                {
                    log.WriteLog("无分录数据，跳过");
                    return;
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    var materialObj = this.View.Model.GetValue("FMaterialId", i) as DynamicObject;
                    var materialId = Convert.ToInt64(materialObj["Id"]);
                    var materialNo = Convert.ToString(materialObj["Number"]);
                    var qty = Convert.ToDecimal(this.View.Model.GetValue("FQTY", i));
                    var ffhbzlb = Convert.ToString(this.View.Model.GetValue("FFHBZLB", i) ?? "");
                    var ffhbzggObj = this.View.Model.GetValue("FFHBZGG", i) as DynamicObject;
                    var ffhbzgg = Convert.ToString(ffhbzggObj["Id"]);
                    var fscbzggObj = this.View.Model.GetValue("FSCBZGG", i) as DynamicObject;
                    var fscbzgg = Convert.ToString(fscbzggObj["Id"]);
                    var ffhbzsl = Convert.ToDecimal(this.View.Model.GetValue("FFHBZSL", i));

                    if (qty <= 0)
                    {
                        log.WriteLog($"  第{i + 1}行 物料{materialNo} 调拨数量为0，跳过");
                        continue;
                    }

                    if (string.IsNullOrEmpty(ffhbzlb) || string.IsNullOrEmpty(ffhbzgg) || string.IsNullOrEmpty(fscbzgg) || ffhbzsl <= 0)
                    {
                        log.WriteLog($"  第{i + 1}行 物料{materialNo} 包装规格信息不完整，跳过");
                        continue;
                    }

                    var specSql = $@"/*dialect*/SELECT FFHBZLB, FFHBZGG, FSCBZGG1, FFHBZSL1
FROM ZMER_BZGGQD
WHERE FCP = '{materialId}'
  AND FDOCUMENTSTATUS = 'C' AND FFORBIDSTATUS = 'A'
  AND FFHBZLB = '{ffhbzlb}'
  AND FFHBZGG = '{ffhbzgg}'
  AND FSCBZGG1 = '{fscbzgg}'
  AND FFHBZSL1 = {ffhbzsl}";

                    var specs = DBUtils.ExecuteDynamicObject(this.Context, specSql);
                    if (specs == null || specs.Count == 0)
                    {
                        log.WriteLog($"  第{i + 1}行 物料{materialNo} 未匹配到包装规格清单，跳过");
                        continue;
                    }

                    log.WriteLog($"  第{i + 1}行 物料{materialNo} 数量={qty}, 匹配规格: {ffhbzlb}/{ffhbzgg}, 每箱{ffhbzsl}");

                    var serials = entries[i][serialPropName] as DynamicObjectCollection;
                    if (serials == null || serials.Count == 0)
                    {
                        log.WriteLog($"  第{i + 1}行 物料{materialNo} 无序列号数据，跳过");
                        continue;
                    }

                    for (int k = 0; k < serials.Count; k++)
                    {
                        int boxNo = (int)(k / ffhbzsl) + 1;
                        serials[k]["FXC"] = boxNo;
                    }

                    int totalBoxes = (int)Math.Ceiling(serials.Count / ffhbzsl);
                    log.WriteLog($"  第{i + 1}行 物料{materialNo} 箱次重新分配完成: {serials.Count}个序列号分{totalBoxes}箱, 每箱{ffhbzsl}个");
                }

                log.WriteLog($"{billNo} 手工算箱完成");
                this.View.UpdateView();
            }
            catch (Exception ex)
            {
                log.Error($"{billNo} 手工算箱异常");
                log.Error(ex);
            }

            log.Section("手工算箱结束");
        }
    }
}

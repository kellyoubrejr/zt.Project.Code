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
using System.Text.RegularExpressions;

namespace Kingdee.Zitn.Project.Code.plugin.OutStock
{
    [Description("【销售出库单据插件-分录按钮】销售出库单分录按钮实现重新算箱"), HotUpdate]
    public class OutStockEntryButtonSuanBox : AbstractBillPlugIn
    {
        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);

            if (!(e.Operation.ToString() ?? "").Contains("Save"))
                return;

            var log = CustomLog.For("销售出库单");
            log.Section("保存前自动算箱");

            try
            {
                var entryEntity = this.View.BusinessInfo.GetEntryEntity("FEntity");
                var serialEntity = this.View.BusinessInfo.GetEntryEntity("FSerialSubEntity");
                var serialPropName = serialEntity?.DynamicObjectType.Name;

                var entries = this.View.Model.GetEntityDataObject(entryEntity);
                if (entries == null || entries.Count == 0)
                    return;

                var boxOffsets = new Dictionary<string, int>();

                for (int i = 0; i < entries.Count; i++)
                {
                    var materialObj = this.View.Model.GetValue("FMaterialId", i) as DynamicObject;
                    if (materialObj == null) continue;
                    var materialId = Convert.ToInt64(materialObj["Id"]);
                    var materialNo = Convert.ToString(materialObj["Number"]);
                    var realQty = Convert.ToDecimal(this.View.Model.GetValue("FREALQTY", i));

                    if (realQty <= 0) continue;

                    var specSql = $@"/*dialect*/SELECT FFHBZLB, FFHBZGG, FSCBZGG1, FFHBZSL1
FROM ZMER_BZGGQD
WHERE FCP = '{materialId}'
AND FDOCUMENTSTATUS = 'C' AND FFORBIDSTATUS = 'A'
ORDER BY FFHBZLB, FSCBZGG1, FCP";

                    var specs = DBUtils.ExecuteDynamicObject(this.Context, specSql);
                    if (specs == null || specs.Count == 0) continue;

                    var candidates = new List<(decimal PerBox, int BoxCount, string FFHBZLB, string FSCBZGG, string FFHBZGG)>();
                    for (int j = 0; j < specs.Count; j++)
                    {
                        var ffhbzlb = Convert.ToString(specs[j]["FFHBZLB"] ?? "");
                        var fscbzgg = Convert.ToString(specs[j]["FSCBZGG1"] ?? "");
                        var ffhbzgg = Convert.ToString(specs[j]["FFHBZGG"] ?? "");
                        var ffhbzsl = Convert.ToDecimal(specs[j]["FFHBZSL1"]);

                        if (ffhbzsl <= 0) continue;

                        var boxes = (int)Math.Ceiling(realQty / ffhbzsl);
                        candidates.Add((ffhbzsl, boxes, ffhbzlb, fscbzgg, ffhbzgg));
                    }

                    if (candidates.Count == 0) continue;

                    var best = candidates
                        .OrderBy(o => o.FFHBZLB == "2" ? 0 : 1)
                        .ThenBy(o => o.BoxCount)
                        .ThenBy(o => realQty % o.PerBox == 0 ? 0 : 1)
                        .ThenByDescending(o => o.PerBox)
                        .First();

                    int minBoxes = best.BoxCount;
                    decimal bestPerBox = best.PerBox;

                    var boxGroupKey = $"{best.FFHBZLB}|{best.FFHBZGG}|{best.FSCBZGG}";
                    int boxOffset = boxOffsets.TryGetValue(boxGroupKey, out var existingOffset) ? existingOffset : 0;

                    log.WriteLog($"  第{i + 1}行 物料{materialNo} 数量={realQty}, 最优: {best.FFHBZLB}/{best.FFHBZGG}, 每箱{bestPerBox}, 共{minBoxes}箱");

                    this.View.Model.SetValue("FFHBZLB", best.FFHBZLB, i);
                    this.View.Model.SetValue("FFHBZSL", bestPerBox, i);
                    this.View.Model.SetValue("FFHBZGG", best.FFHBZGG, i);
                    this.View.Model.SetValue("FSCBZGG", best.FSCBZGG, i);

                    if (!string.IsNullOrEmpty(serialPropName))
                    {
                        var serials = entries[i][serialPropName] as DynamicObjectCollection;
                        if (serials != null && serials.Count > 0)
                        {
                            var delivered = serials
                                .Where(s => !IsNo(Convert.ToString(s["FDSJSFJFSW"])))
                                .OrderByDescending(s => SerialSortKey(Convert.ToString(s["SerialNo"] ?? "")), StringComparer.Ordinal)
                                .ToList();

                            int boxedCount = 0;
                            for (int k = 0; k < delivered.Count; k++)
                            {
                                int boxNo = boxOffset + (int)(boxedCount / bestPerBox) + 1;
                                delivered[k]["FXC"] = boxNo;
                                boxedCount++;
                            }

                            int actualBoxes = (int)Math.Ceiling(boxedCount / bestPerBox);
                            boxOffsets[boxGroupKey] = boxOffset + actualBoxes;
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

            var log = CustomLog.For("销售出库单");
            log.Section("手工重新算箱");

            var header = this.View.Model.DataObject;
            var billNo = Convert.ToString(header["BillNo"]);

            try
            {
                log.WriteLog($"开始处理: {billNo}");

                var entryEntity = this.View.BusinessInfo.GetEntryEntity("FEntity");
                var serialEntity = this.View.BusinessInfo.GetEntryEntity("FSerialSubEntity");
                var serialPropName = serialEntity.DynamicObjectType.Name;

                var entries = this.View.Model.GetEntityDataObject(entryEntity);
                if (entries == null || entries.Count == 0)
                {
                    log.WriteLog("无分录数据，跳过");
                    return;
                }

                var boxOffsets = new Dictionary<string, int>();

                for (int i = 0; i < entries.Count; i++)
                {
                    var materialObj = this.View.Model.GetValue("FMaterialId", i) as DynamicObject;
                    var materialId = Convert.ToInt64(materialObj["Id"]);
                    var materialNo = Convert.ToString(materialObj["Number"]);
                    var realQty = Convert.ToDecimal(this.View.Model.GetValue("FREALQTY", i));
                    var ffhbzlb = Convert.ToString(this.View.Model.GetValue("FFHBZLB", i) ?? "");
                    var ffhbzggObj = this.View.Model.GetValue("FFHBZGG", i) as DynamicObject;
                    var ffhbzgg = Convert.ToString(ffhbzggObj["Id"]);
                    var fscbzggObj = this.View.Model.GetValue("FSCBZGG", i) as DynamicObject;
                    var fscbzgg = Convert.ToString(fscbzggObj["Id"]);
                    var ffhbzsl = Convert.ToDecimal(this.View.Model.GetValue("FFHBZSL", i));

                    if (realQty <= 0)
                    {
                        log.WriteLog($"  第{i + 1}行 物料{materialNo} 实发数量为0，跳过");
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

                    log.WriteLog($"  第{i + 1}行 物料{materialNo} 数量={realQty}, 匹配规格: {ffhbzlb}/{ffhbzgg}, 每箱{ffhbzsl}");

                    var boxGroupKey = $"{ffhbzlb}|{ffhbzgg}|{fscbzgg}";
                    int boxOffset = boxOffsets.TryGetValue(boxGroupKey, out var existingOffset) ? existingOffset : 0;

                    var serials = entries[i][serialPropName] as DynamicObjectCollection;
                    if (serials == null || serials.Count == 0)
                    {
                        log.WriteLog($"  第{i + 1}行 物料{materialNo} 无序列号数据，跳过");
                        continue;
                    }

                    var delivered = serials
                        .Where(s => !IsNo(Convert.ToString(s["FDSJSFJFSW"])))
                        .OrderByDescending(s => SerialSortKey(Convert.ToString(s["SerialNo"] ?? "")), StringComparer.Ordinal)
                        .ToList();

                    int boxedCount = 0;
                    for (int k = 0; k < delivered.Count; k++)
                    {
                        int boxNo = boxOffset + (int)(boxedCount / ffhbzsl) + 1;
                        delivered[k]["FXC"] = boxNo;
                        boxedCount++;
                    }

                    int totalBoxes = (int)Math.Ceiling(boxedCount / ffhbzsl);
                    boxOffsets[boxGroupKey] = boxOffset + totalBoxes;
                    log.WriteLog($"  第{i + 1}行 物料{materialNo} 箱次重新分配完成: {boxedCount}/{serials.Count}个序列号分{totalBoxes}箱, 每箱{ffhbzsl}个");
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

        private static string SerialSortKey(string serialNo)
        {
            if (string.IsNullOrEmpty(serialNo))
                return serialNo;
            return Regex.Replace(serialNo, @"\d+", m => m.Value.PadLeft(20, '0'));
        }

        private static bool IsNo(string s)
        {
            return s == "2";
        }
    }
}

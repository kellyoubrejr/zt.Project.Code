using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.Zitn.Project.Code.conf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Kingdee.Zitn.Project.Code.plugin.OutStock
{
    [Description("【销售出库审核服务】--销售出库单审核根据发货包装清单自动计算箱次"), HotUpdate]
    public class OutStockAuditSuanBox : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FFHBZLB");
            e.FieldKeys.Add("FFHBZGG");
            e.FieldKeys.Add("FSCBZGG");
            e.FieldKeys.Add("FFHBZSL");
            e.FieldKeys.Add("FREALQTY");
            e.FieldKeys.Add("FMaterialId");
            e.FieldKeys.Add("FDSJJFSW");
            e.FieldKeys.Add("FXC");
        }

        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);

            var log = CustomLog.For("销售出库单");
            log.Section("自动计算箱次");

            var entryEntity = this.BusinessInfo.GetEntryEntity("FEntity");
            if (entryEntity == null)
            {
                log.WriteLog("无法获取分录实体元数据，退出");
                return;
            }
            var entryPropName = entryEntity.DynamicObjectType.Name;

            var serialEntity = this.BusinessInfo.GetEntryEntity("FSerialSubEntity");
            string serialPropName = null;
            if (serialEntity != null)
                serialPropName = serialEntity.DynamicObjectType.Name;

            foreach (DynamicObject dataEntity in e.DataEntitys)
            {
                var fid = Convert.ToInt64(dataEntity["Id"]);
                var billNo = Convert.ToString(dataEntity["BillNo"]);

                if (!(dataEntity[entryPropName] is DynamicObjectCollection entries) || entries.Count == 0)
                {
                    log.WriteLog($"FID={fid} 无分录数据，跳过");
                    continue;
                }

                try
                {
                    log.WriteLog($"开始处理: {billNo}, FID={fid}");

                    for (int i = 0; i < entries.Count; i++)
                    {
                        var entry = entries[i];
                        var entryId = Convert.ToInt64(entry["Id"]);

                        var materialId = Convert.ToInt64(entry["MaterialId_id"]);
                        var materialNo = "";//!  Convert.ToString(materialObj?["Number"]);
                        var realQty = Convert.ToDecimal(entry["REALQTY"]);

                        if (realQty <= 0)
                        {
                            log.WriteLog($"  EntryID={entryId} 物料{materialNo} 实发数量为0，跳过");
                            continue;
                        }

                        var specSql = $@"/*dialect*/SELECT FFHBZLB, FFHBZGG, FSCBZGG1, FFHBZSL1
                                                    FROM ZMER_BZGGQD
                                                    WHERE FCP = '{materialId}'
                                                    AND FDOCUMENTSTATUS = 'C' AND FFORBIDSTATUS = 'A'
                                                    ORDER BY FFHBZLB, FSCBZGG1, FCP";

                        var specs = DBUtils.ExecuteDynamicObject(this.Context, specSql);
                        if (specs == null || specs.Count == 0)
                        {
                            log.WriteLog($"  EntryID={entryId} 物料{materialNo} 无包装规格清单，跳过");
                            continue;
                        }

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

                        if (candidates.Count == 0)
                        {
                            log.WriteLog($"  EntryID={entryId} 物料{materialNo} 无有效的包装规格，跳过");
                            continue;
                        }

                        var best = candidates
                            .OrderBy(o => o.FFHBZLB == "2" ? 0 : 1)
                            .ThenBy(o => o.BoxCount)
                            .ThenBy(o => realQty % o.PerBox == 0 ? 0 : 1)
                            .ThenByDescending(o => o.PerBox)
                            .First();

                        int minBoxes = best.BoxCount;
                        decimal bestPerBox = best.PerBox;

                        log.WriteLog($"  EntryID={entryId} 物料{materialNo} 数量={realQty}, 最优方案: {best.FFHBZLB}/{best.FFHBZGG}, 每箱{bestPerBox}, 共{minBoxes}箱");

                        entry["FFHBZLB"] = best.FFHBZLB;
                        entry["FFHBZSL"] = bestPerBox;
                        entry["FFHBZGG_Id"] = best.FFHBZGG;
                        SetRefField(entry, "FFHBZGG", best.FFHBZGG);
                        entry["FSCBZGG_Id"] = best.FSCBZGG;
                        SetRefField(entry, "FSCBZGG", best.FSCBZGG);

                        log.WriteLog($"  EntryID={entryId} 物料{materialNo} 箱次信息已设置到单据体");

                        if (!string.IsNullOrEmpty(serialPropName))
                        {
                            var serials = entry[serialPropName] as DynamicObjectCollection;
                            if (serials == null || serials.Count == 0)
                            {
                                log.WriteLog($"  EntryID={entryId} 无序列号数据，跳过");
                                continue;
                            }

                            int boxedCount = 0;
                            for (int k = 0; k < serials.Count; k++)
                            {
                                if (!IsYes(Convert.ToString(serials[k]["FDSJJFSW"])))
                                    continue;
                                int boxNo = (int)(boxedCount / bestPerBox) + 1;
                                serials[k]["FXC"] = boxNo;
                                boxedCount++;
                            }

                            int actualBoxes = (int)Math.Ceiling(boxedCount / bestPerBox);
                            log.WriteLog($"  EntryID={entryId} 物料{materialNo} 箱次分配完成: {boxedCount}/{serials.Count}个序列号分{actualBoxes}箱, 每箱{bestPerBox}个(剩余为典试件不交付实物，未参与装箱)");
                        }
                    }

                    log.WriteLog($"{billNo} (FID={fid}) 箱次计算完成");
                }
                catch (Exception ex)
                {
                    log.Error($"{billNo} (FID={fid}) 箱次计算异常");
                    log.Error(ex);
                }
            }

            log.Section("箱次计算结束");
        }

        private static bool IsYes(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            return s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "是" || s == "Y";
        }

        private static void SetRefField(DynamicObject entry, string fieldName, string idValue)
        {
            var obj = entry[fieldName] as DynamicObject;
            if (obj == null)
            {
                var dp = entry.DynamicObjectType.Properties[fieldName];
                var pi = dp.GetType().GetProperty("DynamicComplexPropertyType");
                var refType = pi?.GetValue(dp) as DynamicObjectType;
                if (refType == null) return;
                obj = new DynamicObject(refType);
                entry[fieldName] = obj;
            }
            obj["Id"] = idValue;
        }

    }
}

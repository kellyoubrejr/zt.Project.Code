using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
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
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);

            var log = CustomLog.For("销售出库单");
            log.Section("自动计算箱次");

            if (!this.OperationResult.IsSuccess)
            {
                log.WriteLog("操作未成功，跳过");
                return;
            }

            foreach (DynamicObject entity in e.DataEntitys)
            {
                var fid = Convert.ToInt64(entity["Id"]);
                var billNo = Convert.ToString(entity["BillNo"]);

                try
                {
                    log.WriteLog($"开始处理: {billNo}, FID={fid}");

                    var entrySql = $@"/*dialect*/SELECT E.FENTRYID,  E.FMATERIALID,M.FNUMBER AS FMATERIALNO, E.FREALQTY
FROM T_SAL_OUTSTOCK H
JOIN T_SAL_OUTSTOCKENTRY E ON H.FID = E.FID
JOIN T_BD_MATERIAL M ON E.FMATERIALID = M.FMATERIALID
WHERE H.FID = {fid}";

                    var entries = DBUtils.ExecuteDynamicObject(this.Context, entrySql);
                    if (entries == null || entries.Count == 0)
                    {
                        log.WriteLog($"FID={fid} 无分录数据，跳过");
                        continue;
                    }

                    for (int i = 0; i < entries.Count; i++)
                    {
                        var entryId = Convert.ToInt64(entries[i]["FENTRYID"]);
                        var materialNo = Convert.ToString(entries[i]["FMATERIALNO"]);
                        var materialId = Convert.ToInt64(entries[i]["FMATERIALID"]);
                        var realQty = Convert.ToDecimal(entries[i]["FREALQTY"]);

                        if (realQty <= 0)
                        {
                            log.WriteLog($"  EntryID={entryId} 物料{materialNo} 实发数量为0，跳过");
                            continue;
                        }

                        
                        var specSql = $@"/*dialect*/SELECT FFHBZLB, FFHBZGG, FSCBZGG1, FFHBZSL1
FROM ZMER_BZGGQD
WHERE FCP = '{materialId}'
ORDER BY FFHBZLB, FSCBZGG1, FCP";

                        var specs = DBUtils.ExecuteDynamicObject(this.Context, specSql);
                        if (specs == null || specs.Count == 0)
                        {
                            log.WriteLog($"  EntryID={entryId} 物料{materialNo} 无包装规格清单，跳过");
                            continue;
                        }

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
                                var boxes = (int)Math.Ceiling(realQty / ffhbzsl);
                                groupBoxOptions[groupKey] = (ffhbzsl, boxes, ffhbzlb, fscbzgg, ffhbzgg);
                            }
                        }

                        if (groupBoxOptions.Count == 0)
                        {
                            log.WriteLog($"  EntryID={entryId} 物料{materialNo} 无有效的包装规格，跳过");
                            continue;
                        }

                        var best = groupBoxOptions.Values
                            .OrderBy(o => o.BoxCount)
                            .ThenBy(o => realQty % o.PerBox == 0 ? 0 : 1)
                            .ThenByDescending(o => o.PerBox)
                            .First();

                        int minBoxes = best.BoxCount;
                        decimal bestPerBox = best.PerBox;

                        log.WriteLog($"  EntryID={entryId} 物料{materialNo} 数量={realQty}, 最优方案: {best.FFHBZLB}/{best.FFHBZGG}, 每箱{bestPerBox}, 共{minBoxes}箱");

                        var updateEntrySql = $@"/*dialect*/UPDATE T_SAL_OUTSTOCKENTRY
SET FFHBZLB = '{best.FFHBZLB}',
    FFHBZGG = '{best.FFHBZGG}',
    FSCBZGG = '{best.FSCBZGG}',
    FFHBZSL = {bestPerBox}
WHERE FENTRYID = {entryId}";
                        DBUtils.Execute(this.Context, updateEntrySql);
                        log.WriteLog($"  EntryID={entryId} 物料{materialNo} 箱次信息已回写单据体");

                        var serialSql = $@"/*dialect*/SELECT FSERIALID
FROM T_SAL_OUTSTOCKSERIAL
WHERE FENTRYID = {entryId}
ORDER BY FSERIALID";

                        var serials = DBUtils.ExecuteDynamicObject(this.Context, serialSql);
                        if (serials == null || serials.Count == 0)
                        {
                            log.WriteLog($"  EntryID={entryId} 无序列号数据，跳过");
                            continue;
                        }

                        for (int k = 0; k < serials.Count; k++)
                        {
                            long serialId = Convert.ToInt64(serials[k]["FSERIALID"]);
                            int boxNo = (int)(k / bestPerBox) + 1;

                            var updateSql = $@"/*dialect*/UPDATE T_SAL_OUTSTOCKSERIAL SET FXC = {boxNo}
WHERE FENTRYID = {entryId} AND FSERIALID = {serialId}";
                            DBUtils.Execute(this.Context, updateSql);
                        }

                        log.WriteLog($"  EntryID={entryId} 物料{materialNo} 箱次分配完成: {serials.Count}个序列号分{minBoxes}箱, 每箱{bestPerBox}个");
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
    }
}

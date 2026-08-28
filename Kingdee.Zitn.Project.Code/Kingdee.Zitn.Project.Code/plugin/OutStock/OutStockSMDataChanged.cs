using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.Zitn.Project.Code.conf;
using Kingdee.Zitn.Project.Code.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kingdee.Zitn.Project.Code.plugin.OutStock
{
    [Description("【销售出库单据插件-扫码回填】录入扫码内容FSMNR时，按物料回填发货包装信息"), HotUpdate]
    public class OutStockSMDataChanged : AbstractBillPlugIn
    {
        private const string ScanEntityKey = "F_ZMER_Entity_re5";
        private const string EntryEntityKey = "FEntity";
        private const string SerialEntityKey = "FSerialSubEntity";
        private const string DeliveredFlagField = "FDSJSFJFSW";

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);

            if (!e.Field.Key.EqualsIgnoreCase("FSMNR"))
                return;

            var fsmnr = Convert.ToString(e.NewValue ?? "").Trim();
            if (string.IsNullOrEmpty(fsmnr))
                return;

            var log = CustomLog.For("销售出库单");
            log.Section("扫码回填");

            try
            {
                var scanEntity = this.View.BusinessInfo.GetEntryEntity(ScanEntityKey);
                if (scanEntity == null)
                    return;

                var scanData = this.View.Model.GetEntityDataObject(scanEntity);
                if (scanData == null || e.Row < 0 || e.Row >= scanData.Count)
                    return;

                var scanRow = scanData[e.Row];

                // 1. 取第一个#之前的包装序列号（P为固定字符）
                var fbzxLH = fsmnr.Split('#')[0].Trim();
                if (string.IsNullOrEmpty(fbzxLH))
                    return;

                // 2. 反查包装箱标签，取物料编码 + 本箱包含的序列号
                var box = ResolveBoxInfo(fbzxLH);
                if (string.IsNullOrEmpty(box.MaterialNo))
                {
                    var msg = $"扫码回填失败：包装序列号 {fbzxLH} 未找到对应包装箱标签";
                    log.Error(msg);
                    SendMsg.Send($"【销售出库单】{msg}");
                    return;
                }

                // 3. 从单据体找到该物料的物料内码
                long materialId = FindMaterialId(box.MaterialNo);
                if (materialId == 0)
                {
                    var msg = $"扫码回填失败：物料 {box.MaterialNo} 不在当前单据中";
                    log.Error(msg);
                    SendMsg.Send($"【销售出库单】{msg}");
                    return;
                }

                // 4. 查询该物料第一条有效包装规格
                var specSql = $@"/*dialect*/SELECT FFHBZLB, FFHBZGG, FSCBZGG1, FFHBZSL1
FROM ZMER_BZGGQD
WHERE FCP = '{materialId}'
  AND FDOCUMENTSTATUS = 'C' AND FFORBIDSTATUS = 'A'
  AND FFHBZSL1 > 0
ORDER BY FFHBZLB, FSCBZGG1, FCP";
                var specs = DBUtils.ExecuteDynamicObject(this.Context, specSql);
                if (specs == null || specs.Count == 0)
                {
                    var msg = $"扫码回填失败：物料 {box.MaterialNo} 无有效包装规格";
                    log.Error(msg);
                    SendMsg.Send($"【销售出库单】{msg}");
                    return;
                }

                var ffhbzlb = Convert.ToString(specs[0]["FFHBZLB"] ?? "").Trim();
                var ffhbzgg = Convert.ToString(specs[0]["FFHBZGG"] ?? "").Trim();
                var fscbzgg = Convert.ToString(specs[0]["FSCBZGG1"] ?? "").Trim();
                var ffhbzsl = Convert.ToDecimal(specs[0]["FFHBZSL1"] ?? 0);

                // 5. 回填到扫码行
                //scanRow["FSMFHBZLB"] = ffhbzlb;
                //SetRefField(scanRow, "FSMSCBZGG", fscbzgg);
                //scanRow["FSMFHBZSL"] = ffhbzsl;
                this.View.Model.SetValue("FSMFHBZLB", ffhbzlb, e.Row);
                this.View.Model.SetValue("FSMFHBZGG", ffhbzgg, e.Row);
                this.View.Model.SetValue("FSMSCBZGG", fscbzgg, e.Row);
                this.View.Model.SetValue("FSMFHBZSL", ffhbzsl, e.Row);

                // 6. 箱次：按序列号分箱（与审核算箱逻辑一致）
                int boxNo = CalcBoxNo(box.MaterialNo, box.Serials);
                //scanRow["FSMXC"] = boxNo;
                this.View.Model.SetValue("FSMXC", boxNo, e.Row);

                log.WriteLog($"扫码内容={fsmnr}，物料={box.MaterialNo}，规格={ffhbzlb}/{ffhbzgg}/{fscbzgg}，每箱={ffhbzsl}，箱次={boxNo}");

                //this.View.UpdateView("FFHBZLB", e.Row);
                //this.View.UpdateView("FFHBZGG", e.Row);
                //this.View.UpdateView("FSMFHBZSL", e.Row);
                //this.View.UpdateView("FSMXC", e.Row);
            }
            catch (Exception ex)
            {
                log.Error($"扫码回填异常：{fsmnr}");
                log.Error(ex);
                SendMsg.Send($"【销售出库单】扫码回填异常：{fsmnr}", ex);
            }

            log.Section("扫码回填结束");
        }

        /// <summary>反查包装箱标签，返回该包装序列号对应的 物料编码 + 本箱序列号列表</summary>
        private (string MaterialNo, List<string> Serials) ResolveBoxInfo(string fbzxLH)
        {
            var sql = $@"/*dialect*/SELECT B.FWLNUM, B.FXLH
FROM ZMER_t_Cust100031 A
INNER JOIN ZMER_t_Cust_Entry100089 B ON A.FID = B.FID
WHERE A.FBZXLH = '{fbzxLH.Replace("'", "''")}'";
            var dt = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if (dt == null || dt.Count == 0)
                return ("", new List<string>());

            string materialNo = "";
            var serials = new List<string>();
            for (int i = 0; i < dt.Count; i++)
            {
                if (string.IsNullOrEmpty(materialNo))
                    materialNo = Convert.ToString(dt[i]["FWLNUM"] ?? "").Trim();
                var xlh = Convert.ToString(dt[i]["FXLH"] ?? "").Trim();
                if (!string.IsNullOrEmpty(xlh))
                    serials.Add(xlh);
            }
            return (materialNo, serials);
        }

        /// <summary>按物料编码从单据体找物料内码</summary>
        private long FindMaterialId(string materialNo)
        {
            var entryEntity = this.View.BusinessInfo.GetEntryEntity(EntryEntityKey);
            if (entryEntity == null)
                return 0;

            var entries = this.View.Model.GetEntityDataObject(entryEntity);
            if (entries == null || entries.Count == 0)
                return 0;

            for (int i = 0; i < entries.Count; i++)
            {
                var m = entries[i]["MaterialId"] as DynamicObject;
                if (m == null)
                    continue;
                if (string.Equals(Convert.ToString(m["Number"]), materialNo, StringComparison.OrdinalIgnoreCase))
                    return Convert.ToInt64(m["Id"]);
            }
            return 0;
        }

        /// <summary>
        /// 箱次：按审核算箱同逻辑——遍历分录，按 发货包装类别|发货包装规格|生产包装规格 分组维护跨物料偏移，
        /// 交付序列号降序排序后每 perBox 个序列号为一箱，箱次 = 组内偏移 + 物料内序号 + 1（同组合跨物料连续累加）。
        /// </summary>
        private int CalcBoxNo(string targetMaterialNo, List<string> targetBoxSerials)
        {
            var entryEntity = this.View.BusinessInfo.GetEntryEntity(EntryEntityKey);
            var serialEntity = this.View.BusinessInfo.GetEntryEntity(SerialEntityKey);
            if (entryEntity == null || serialEntity == null)
                return 1;
            var serialPropName = serialEntity.DynamicObjectType.Name;

            var entries = this.View.Model.GetEntityDataObject(entryEntity);
            if (entries == null || entries.Count == 0)
                return 1;

            var targetBoxSet = new HashSet<string>(targetBoxSerials, StringComparer.OrdinalIgnoreCase);
            var boxOffsets = new Dictionary<string, int>();

            for (int i = 0; i < entries.Count; i++)
            {
                var m = entries[i]["MaterialId"] as DynamicObject;
                if (m == null)
                    continue;
                var materialNo = Convert.ToString(m["Number"]);
                long materialId = Convert.ToInt64(m["Id"]);

                // 该物料第一条有效包装规格
                var specSql = $@"/*dialect*/SELECT FFHBZLB, FFHBZGG, FSCBZGG1, FFHBZSL1
FROM ZMER_BZGGQD
WHERE FCP = '{materialId}'
  AND FDOCUMENTSTATUS = 'C' AND FFORBIDSTATUS = 'A'
  AND FFHBZSL1 > 0
ORDER BY FFHBZLB, FSCBZGG1, FCP";
                var specs = DBUtils.ExecuteDynamicObject(this.Context, specSql);
                if (specs == null || specs.Count == 0)
                    continue;

                var groupKey = $"{Convert.ToString(specs[0]["FFHBZLB"] ?? "").Trim()}|{Convert.ToString(specs[0]["FFHBZGG"] ?? "").Trim()}|{Convert.ToString(specs[0]["FSCBZGG1"] ?? "").Trim()}";
                decimal perBox = Convert.ToDecimal(specs[0]["FFHBZSL1"] ?? 0);
                if (perBox <= 0)
                    continue;

                int boxOffset = boxOffsets.TryGetValue(groupKey, out var off) ? off : 0;

                var serials = entries[i][serialPropName] as DynamicObjectCollection;
                if (serials == null || serials.Count == 0)
                    continue;

                var delivered = serials
                    .Where(s => !IsNo(Convert.ToString(s[DeliveredFlagField])))
                    .OrderByDescending(s => SerialSortKey(Convert.ToString(s["SerialNo"] ?? "")), StringComparer.Ordinal)
                    .ToList();
                if (delivered.Count == 0)
                    continue;

                int actualBoxes = (int)Math.Ceiling(delivered.Count / perBox);
                boxOffsets[groupKey] = boxOffset + actualBoxes;

                if (string.Equals(materialNo, targetMaterialNo, StringComparison.OrdinalIgnoreCase))
                {
                    for (int k = 0; k < delivered.Count; k++)
                    {
                        if (targetBoxSet.Contains(Convert.ToString(delivered[k]["SerialNo"] ?? "")))
                            return boxOffset + (int)(k / perBox) + 1;
                    }
                    return boxOffset + 1;
                }
            }
            return 1;
        }

        /// <summary>设置基础资料引用字段（按内码）</summary>
        private static void SetRefField(DynamicObject entry, string fieldName, string idValue)
        {
            if (string.IsNullOrEmpty(idValue))
                return;
            var obj = entry[fieldName] as DynamicObject;
            if (obj == null)
            {
                var dp = entry.DynamicObjectType.Properties[fieldName];
                var pi = dp.GetType().GetProperty("DynamicComplexPropertyType");
                var refType = pi?.GetValue(dp) as DynamicObjectType;
                if (refType == null)
                    return;
                obj = new DynamicObject(refType);
                entry[fieldName] = obj;
            }
            obj["Id"] = idValue;
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

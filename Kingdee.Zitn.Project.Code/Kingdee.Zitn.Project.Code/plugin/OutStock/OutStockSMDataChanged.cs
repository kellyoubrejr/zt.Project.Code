using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;
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

        private bool _isBackfilling;

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);

            if (_isBackfilling)
                return;

            if (!e.Field.Key.EqualsIgnoreCase("FSMNR"))
                return;

            var fsmnr = Convert.ToString(e.NewValue ?? "").Trim();
            if (string.IsNullOrEmpty(fsmnr))
                return;

            var log = CustomLog.For("销售出库单");
            log.Section("扫码回填");

            _isBackfilling = true;
            try
            {
                var scanEntity = this.View.BusinessInfo.GetEntryEntity(ScanEntityKey);
                if (scanEntity == null)
                    return;

                var scanData = this.View.Model.GetEntityDataObject(scanEntity);
                if (scanData == null || e.Row < 0 || e.Row >= scanData.Count)
                    return;

                // 1. 取第一个#之前的包装序列号（P为固定字符）
                var fbzxLH = fsmnr.Split('#')[0].Trim();
                if (string.IsNullOrEmpty(fbzxLH))
                    return;

                // 2. 反查包装箱标签，取该箱包含的所有 物料+序列号 对
                var pairs = ResolveBoxPairs(fbzxLH);
                if (pairs.Count == 0)
                {
                    var msg = $"扫码回填失败：包装序列号 {fbzxLH} 未找到对应包装箱标签";
                    log.Error(msg);
                    SendMsg.Send($"【销售出库单】{msg}");
                    return;
                }

                // 3. 箱次：按审核同逻辑（最优规格 + 跨物料累加）计算 物料#序列号 -> 箱次
                var boxNoMap = BuildBoxNoMap();

                // 4. 回填字段用第一条有效规格；逐对回填，当前扫码行放第一对，其余插入下方
                var specCache = new Dictionary<long, (string FFHBZLB, string FFHBZGG, string FSCBZGG, decimal PerBox)>();
                int filled = 0;

                for (int i = 0; i < pairs.Count; i++)
                {
                    var pair = pairs[i];

                    long materialId = FindMaterialId(pair.MaterialNo);
                    if (materialId == 0)
                    {
                        var msg = $"扫码回填失败：物料 {pair.MaterialNo} 不在当前单据中";
                        log.Error(msg);
                        SendMsg.Send($"【销售出库单】{msg}");
                        continue;
                    }

                    if (!specCache.TryGetValue(materialId, out var spec))
                    {
                        spec = GetFirstSpec(materialId);
                        specCache[materialId] = spec;
                    }
                    if (spec.PerBox <= 0)
                    {
                        var msg = $"扫码回填失败：物料 {pair.MaterialNo} 无有效包装规格";
                        log.Error(msg);
                        SendMsg.Send($"【销售出库单】{msg}");
                        continue;
                    }

                    int boxNo = boxNoMap.TryGetValue(pair.MaterialNo + "#" + pair.Serial, out var b) ? b : 1;

                    int rowIndex;
                    if (filled == 0)
                    {
                        rowIndex = e.Row;
                    }
                    else
                    {
                        rowIndex = this.View.Model.GetEntryRowCount(ScanEntityKey);
                        this.View.Model.CreateNewEntryRow(ScanEntityKey);
                    }

                    this.View.Model.SetValue("FSMWL", pair.MaterialNo, rowIndex);
                    this.View.Model.SetValue("FSMXLH", pair.Serial, rowIndex);
                    if (filled > 0)
                        this.View.Model.SetValue("FSMNR", fsmnr, rowIndex);
                    this.View.Model.SetValue("FSMFHBZLB", spec.FFHBZLB, rowIndex);
                    this.View.Model.SetValue("FSMFHBZGG", spec.FFHBZGG, rowIndex);
                    this.View.Model.SetValue("FSMSCBZGG", spec.FSCBZGG, rowIndex);
                    this.View.Model.SetValue("FSMFHBZSL", spec.PerBox, rowIndex);
                    this.View.Model.SetValue("FSMXC", boxNo, rowIndex);

                    log.WriteLog($"扫码内容={fsmnr}，物料={pair.MaterialNo}，序列号={pair.Serial}，规格={spec.FFHBZLB}/{spec.FFHBZGG}/{spec.FSCBZGG}，每箱={spec.PerBox}，箱次={boxNo}");

                    filled++;
                }

                RemoveEmptyScanRows();

                // 末尾追加空白行并定位聚焦到 FSMNR，方便直接扫下一箱
                int nextRow = this.View.Model.GetEntryRowCount(ScanEntityKey);
                this.View.Model.CreateNewEntryRow(ScanEntityKey);
                this.View.UpdateView();
                this.View.GetControl<EntryGrid>(ScanEntityKey).SetFocusRowIndex(nextRow);
                this.View.GetControl("FSMNR").SetFocus();
            }
            catch (Exception ex)
            {
                log.Error($"扫码回填异常：{fsmnr}");
                log.Error(ex);
                SendMsg.Send($"【销售出库单】扫码回填异常：{fsmnr}", ex);
            }
            finally
            {
                _isBackfilling = false;
            }

            log.Section("扫码回填结束");
        }

        /// <summary>反查包装箱标签，返回该包装序列号对应的 物料编码+序列号 对列表</summary>
        private List<(string MaterialNo, string Serial)> ResolveBoxPairs(string fbzxLH)
        {
            var sql = $@"/*dialect*/SELECT B.FWLNUM, B.FXLH
FROM ZMER_t_Cust100031 A
INNER JOIN ZMER_t_Cust_Entry100089 B ON A.FID = B.FID
WHERE A.FBZXLH = '{fbzxLH.Replace("'", "''")}'";
            var dt = DBUtils.ExecuteDynamicObject(this.Context, sql);
            var pairs = new List<(string MaterialNo, string Serial)>();
            if (dt == null || dt.Count == 0)
                return pairs;

            for (int i = 0; i < dt.Count; i++)
            {
                var materialNo = Convert.ToString(dt[i]["FWLNUM"] ?? "").Trim();
                var serial = Convert.ToString(dt[i]["FXLH"] ?? "").Trim();
                if (!string.IsNullOrEmpty(materialNo))
                    pairs.Add((materialNo, serial));
            }

            pairs.Sort((a, b) =>
            {
                int cmp = string.Compare(a.MaterialNo, b.MaterialNo, StringComparison.OrdinalIgnoreCase);
                if (cmp != 0)
                    return cmp;
                return string.Compare(SerialSortKey(a.Serial), SerialSortKey(b.Serial), StringComparison.Ordinal);
            });

            return pairs;
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

        /// <summary>查询该物料第一条有效包装规格（回填字段用）</summary>
        private (string FFHBZLB, string FFHBZGG, string FSCBZGG, decimal PerBox) GetFirstSpec(long materialId)
        {
            var sql = $@"/*dialect*/SELECT FFHBZLB, FFHBZGG, FSCBZGG1, FFHBZSL1
FROM ZMER_BZGGQD
WHERE FCP = '{materialId}'
  AND FDOCUMENTSTATUS = 'C' AND FFORBIDSTATUS = 'A'
  AND FFHBZSL1 > 0
ORDER BY FFHBZLB, FSCBZGG1, FCP";
            var specs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if (specs == null || specs.Count == 0)
                return (null, null, null, 0);

            var ffhbzlb = Convert.ToString(specs[0]["FFHBZLB"] ?? "").Trim();
            var ffhbzgg = Convert.ToString(specs[0]["FFHBZGG"] ?? "").Trim();
            var fscbzgg = Convert.ToString(specs[0]["FSCBZGG1"] ?? "").Trim();
            var ffhbzsl = Convert.ToDecimal(specs[0]["FFHBZSL1"] ?? 0);
            return (ffhbzlb, ffhbzgg, fscbzgg, ffhbzsl);
        }

        /// <summary>
        /// 读取当前单据序列号子实体上已落库的箱次 FXC，返回 物料#序列号 -> 箱次 映射。
        /// 扫码发生在保存之后，箱次已按审核逻辑计算并落库，直接按物料+序列号读取，不再重算。
        /// </summary>
        private Dictionary<string, int> BuildBoxNoMap()
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var entryEntity = this.View.BusinessInfo.GetEntryEntity(EntryEntityKey);
            var serialEntity = this.View.BusinessInfo.GetEntryEntity(SerialEntityKey);
            if (entryEntity == null || serialEntity == null)
                return map;
            var serialPropName = serialEntity.DynamicObjectType.Name;

            var entries = this.View.Model.GetEntityDataObject(entryEntity);
            if (entries == null || entries.Count == 0)
                return map;

            for (int i = 0; i < entries.Count; i++)
            {
                var m = entries[i]["MaterialId"] as DynamicObject;
                if (m == null)
                    continue;
                var materialNo = Convert.ToString(m["Number"]);

                var serials = entries[i][serialPropName] as DynamicObjectCollection;
                if (serials == null || serials.Count == 0)
                    continue;

                for (int k = 0; k < serials.Count; k++)
                {
                    var serialNo = Convert.ToString(serials[k]["SerialNo"] ?? "");
                    var boxNo = Convert.ToInt32(serials[k]["FXC"] ?? 0);
                    if (string.IsNullOrEmpty(serialNo) || boxNo <= 0)
                        continue;
                    map[materialNo + "#" + serialNo] = boxNo;
                }
            }

            return map;
        }

        /// <summary>删除扫码单据体中 FSMNR 为空的多余空行</summary>
        private void RemoveEmptyScanRows()
        {
            var scanEntity = this.View.BusinessInfo.GetEntryEntity(ScanEntityKey);
            if (scanEntity == null)
                return;

            var scanData = this.View.Model.GetEntityDataObject(scanEntity);
            if (scanData == null || scanData.Count == 0)
                return;

            for (int i = scanData.Count - 1; i >= 0; i--)
            {
                var fsmnr = Convert.ToString(scanData[i]["FSMNR"] ?? "").Trim();
                if (string.IsNullOrEmpty(fsmnr))
                    this.View.Model.DeleteEntryRow(ScanEntityKey, i);
            }
        }

        private static string SerialSortKey(string serialNo)
        {
            if (string.IsNullOrEmpty(serialNo))
                return serialNo;
            return Regex.Replace(serialNo, @"\d+", m => m.Value.PadLeft(20, '0'));
        }
    }
}

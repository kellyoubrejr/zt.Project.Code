using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kingdee.Zitn.Project.Code.plugin.Assem
{
    [Description("【组装拆卸单提交服务】：校验是否存在在途组装拆卸 + 校验批号数量是否超限")]
    [Kingdee.BOS.Util.HotUpdate]
    public class AssemSubmitJudgeOpe : AbstractOperationServicePlugIn
    {
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);

            var currentIds = e.SelectedRows
                .Select(row => row.DataEntity["Id"]?.ToString())
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

            if (currentIds.Count == 0) return;

            //ValidateOnWayAssemblies(currentIds);
            ValidateLotQty(currentIds);
        }

        /// <summary>
        /// 校验是否存在在途组装拆卸单
        /// </summary>
        private void ValidateOnWayAssemblies(List<string> currentIds)
        {
            var idList = string.Join(",", currentIds.Select(id => $"'{id}'"));

            var materialSql = $@"
                SELECT DISTINCT M.FNUMBER
                FROM T_STK_ASSEMBLY A
                INNER JOIN T_STK_ASSEMBLYPRODUCT B ON A.FID = B.FID
                INNER JOIN T_STK_ASSEMBLYSUBITEM C ON B.FENTRYID = C.FENTRYID
                INNER JOIN T_BD_MATERIAL M ON C.FMATERIALID = M.FMATERIALID
                WHERE A.FID IN ({idList}) AND A.FSTOCKORGID <> '4665868'";

            var materialResult = DBUtils.ExecuteDynamicObject(this.Context, materialSql);

            if (materialResult == null || materialResult.Count == 0) return;

            var materialNumbers = new List<string>();
            foreach (var item in materialResult)
            {
                var number = item["FNUMBER"]?.ToString();
                if (!string.IsNullOrEmpty(number))
                    materialNumbers.Add(number);
            }

            if (materialNumbers.Count == 0) return;

            var materialFilter = string.Join(",", materialNumbers.Select(n => $"'{n}'"));

            var onWaySql = $@"
                SELECT DISTINCT A.FBILLNO, M.FNUMBER, M1.FNAME
                FROM T_STK_ASSEMBLY A
                INNER JOIN T_STK_ASSEMBLYPRODUCT B ON A.FID = B.FID
                INNER JOIN T_STK_ASSEMBLYSUBITEM C ON B.FENTRYID = C.FENTRYID
                INNER JOIN T_BD_MATERIAL M ON C.FMATERIALID = M.FMATERIALID
                INNER JOIN T_BD_MATERIAL_L M1 ON M.FMATERIALID = M1.FMATERIALID
                WHERE A.FDOCUMENTSTATUS = 'B'
                    AND A.FID NOT IN ({idList})
                    AND M.FNUMBER IN ({materialFilter}) AND A.FSTOCKORGID <> '4665868'";

            var onWayResult = DBUtils.ExecuteDynamicObject(this.Context, onWaySql);

            if (onWayResult != null && onWayResult.Count > 0)
            {
                var conflictBillNos = new HashSet<string>();

                foreach (var item in onWayResult)
                {
                    var billNo = item["FBILLNO"]?.ToString();
                    if (!string.IsNullOrEmpty(billNo))
                        conflictBillNos.Add(billNo);
                }

                var message = new StringBuilder();
                message.AppendLine("提交失败：该产品已在组装拆卸过程中，请确认！");
                message.AppendLine($"在途单据号：{string.Join("、", conflictBillNos)}");

                throw new KDBusinessException("提交失败", message.ToString());
            }
        }

        /// <summary>
        /// 校验批号数量：当前单据子件数量 + 历史已用数量 <= 批号总量
        /// </summary>
        private void ValidateLotQty(List<string> currentIds)
        {
            var idList = string.Join(",", currentIds.Select(id => $"'{id}'"));

            // 1. 查询当前单据子件的物料编码、批号、数量
            var subItemSql = $@"
                SELECT M.FNUMBER, C.FLOT_TEXT, C.FQTY
                FROM T_STK_ASSEMBLY A
                INNER JOIN T_STK_ASSEMBLYPRODUCT B ON A.FID = B.FID
                INNER JOIN T_STK_ASSEMBLYSUBITEM C ON B.FENTRYID = C.FENTRYID
                INNER JOIN T_BD_MATERIAL M ON C.FMATERIALID = M.FMATERIALID
                WHERE A.FID IN ({idList})
                    AND C.FLOT_TEXT IS NOT NULL AND C.FLOT_TEXT <> ''";

            var subItemResult = DBUtils.ExecuteDynamicObject(this.Context, subItemSql);

            if (subItemResult == null || subItemResult.Count == 0) return;

            // 2. 收集有批号的子件信息，按物料+批号汇总当前数量
            var lotItems = new Dictionary<string, decimal>();  // key=物料编码|批号, value=当前数量
            foreach (var item in subItemResult)
            {
                var materialNumber = item["FNUMBER"]?.ToString();
                var lotText = item["FLOT_TEXT"]?.ToString();
                var qty = item["FQTY"] == DBNull.Value ? 0 : Convert.ToDecimal(item["FQTY"]);

                if (string.IsNullOrWhiteSpace(materialNumber) || string.IsNullOrWhiteSpace(lotText))
                    continue;

                var key = $"{materialNumber}|{lotText}";
                if (lotItems.ContainsKey(key))
                    lotItems[key] += qty;
                else
                    lotItems[key] = qty;
            }

            if (lotItems.Count == 0) return;

            // 3. 构建物料+批号的查询条件
            var lotConditions = lotItems.Select(kvp =>
            {
                var parts = kvp.Key.Split('|');
                return $"(M.FNUMBER = '{parts[0].Replace("'", "''")}' AND C.FLOT_TEXT = '{parts[1].Replace("'", "''")}')";
            });
            var lotFilter = string.Join(" OR ", lotConditions);

            // 4. 查询批号总量（从批号主档追踪表）
            var lotQtyConditions = lotItems.Select(kvp =>
            {
                var parts = kvp.Key.Split('|');
                return $"(A.FNUMBER = '{parts[0].Replace("'", "''")}' AND B.FBILLFORMID = 'STK_InStock')";
            });
            var lotQtyFilter = string.Join(" OR ", lotQtyConditions);

            var lotQtySql = $@"
                SELECT A.FNUMBER, SUM(B.FQTY) AS TOTALQTY
                FROM T_BD_LOTMASTER A
                JOIN T_BD_LOTMASTERBILLTRACE B ON A.FLOTID = B.FLOTID
                WHERE {lotQtyFilter}
                GROUP BY A.FNUMBER";

            var lotQtyResult = DBUtils.ExecuteDynamicObject(this.Context, lotQtySql);

            // 批号总量：key=批号编码, value=总量
            var lotTotalQty = new Dictionary<string, decimal>();
            if (lotQtyResult != null)
            {
                foreach (var item in lotQtyResult)
                {
                    var lotNumber = item["FNUMBER"]?.ToString();
                    var totalQty = item["TOTALQTY"] == DBNull.Value ? 0 : Convert.ToDecimal(item["TOTALQTY"]);
                    if (!string.IsNullOrWhiteSpace(lotNumber))
                        lotTotalQty[lotNumber] = totalQty;
                }
            }

            // 5. 查询历史已用数量（所有状态，排除当前单据）
            var historySql = $@"
                SELECT M.FNUMBER, C.FLOT_TEXT, SUM(C.FQTY) AS USEDQTY
                FROM T_STK_ASSEMBLY A
                INNER JOIN T_STK_ASSEMBLYPRODUCT B ON A.FID = B.FID
                INNER JOIN T_STK_ASSEMBLYSUBITEM C ON B.FENTRYID = C.FENTRYID
                INNER JOIN T_BD_MATERIAL M ON C.FMATERIALID = M.FMATERIALID
                WHERE A.FID NOT IN ({idList})
                    AND ({lotFilter})
                GROUP BY M.FNUMBER, C.FLOT_TEXT";

            var historyResult = DBUtils.ExecuteDynamicObject(this.Context, historySql);

            // 历史已用：key=物料编码|批号编码, value=已用数量
            var historyUsed = new Dictionary<string, decimal>();
            if (historyResult != null)
            {
                foreach (var item in historyResult)
                {
                    var materialNumber = item["FNUMBER"]?.ToString();
                    var lotText = item["FLOT_TEXT"]?.ToString();
                    var usedQty = item["USEDQTY"] == DBNull.Value ? 0 : Convert.ToDecimal(item["USEDQTY"]);

                    if (!string.IsNullOrWhiteSpace(materialNumber) && !string.IsNullOrWhiteSpace(lotText))
                    {
                        var key = $"{materialNumber}|{lotText}";
                        historyUsed[key] = usedQty;
                    }
                }
            }

            // 6. 校验：当前数量 + 历史已用数量 <= 批号总量
            var errors = new List<string>();
            foreach (var kvp in lotItems)
            {
                var parts = kvp.Key.Split('|');
                var materialNumber = parts[0];
                var lotText = parts[1];
                var currentQty = kvp.Value;

                historyUsed.TryGetValue(kvp.Key, out decimal usedQty);
                var totalQty = lotTotalQty.ContainsKey(lotText) ? lotTotalQty[lotText] : 0;

                if (totalQty <= 0)
                {
                    errors.Add($"物料[{materialNumber}]批号[{lotText}]在批号主档中未找到入库记录，请确认！");
                }
                else if (currentQty + usedQty > totalQty)
                {
                    errors.Add($"物料[{materialNumber}]批号[{lotText}]数量超限：当前[{currentQty}] + 已用[{usedQty}] = [{currentQty + usedQty}] > 批号总量[{totalQty}]");
                }
            }

            if (errors.Count > 0)
            {
                var message = new StringBuilder();
                message.AppendLine("提交失败：批号数量校验不通过！");
                foreach (var err in errors)
                    message.AppendLine(err);

                throw new KDBusinessException("提交失败", message.ToString());
            }
        }
    }
}

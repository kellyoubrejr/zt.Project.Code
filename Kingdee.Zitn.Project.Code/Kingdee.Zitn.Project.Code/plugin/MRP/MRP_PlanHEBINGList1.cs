using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.K3.Core.MFG.Utils;
using Kingdee.K3.MFG.PLN.App.Core;
using Kingdee.K3.MFG.ServiceHelper.PLN;
using Kingdee.Zitn.Project.Code.conf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Kingdee.Zitn.Project.Code.plugin.MRP
{
    [Description("计划订单合并-按物料+计划跟踪号+云枢单据号+标记分组合并调用标准服务-列表"), HotUpdate]
    public class MRP_PlanHEBINGList1 : AbstractListPlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("计划订单合并");

        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);

            if (!e.BarItemKey.EqualsIgnoreCase("tbMerge1"))
                return;

            _log.WriteLog("========== 计划订单合并(tbMerge1→标准合并服务) 开始 ==========");

            DoMerge1();

            _log.WriteLog("========== 计划订单合并(tbMerge1→标准合并服务) 结束 ==========");
        }

        private void DoMerge1()
        {
            var rows = QueryPlanOrders();

            if (rows == null || rows.Count == 0)
            {
                _log.WriteLog("查询结果为空，退出");
                this.View.ShowMessage("没有找到符合条件的计划订单。");
                return;
            }
            _log.WriteLog($"查询到 {rows.Count} 条符合条件的计划订单");


            var detailLimit = Math.Min(rows.Count, 20);
            _log.WriteLog($"查询明细(前{detailLimit}条):");
            for (int i = 0; i < detailLimit; i++)
            {
                var r = rows[i];
                _log.WriteLog($"  物料=[{GetString(r, "WLNUM")}], 计划跟踪号=[{GetString(r, "JHGZH")}], 云枢单据号=[{GetString(r, "YSDJH")}], 标记=[{GetString(r, "BJ")}], FID={GetString(r, "FID")}");
            }
            if (rows.Count > 20)
                _log.WriteLog($"  ... 共{rows.Count}条，其余省略");

            var groups = rows
                .GroupBy(x => BuildGroupKey(x))
                .Where(g => g.Count() > 1)
                .ToList();

            if (groups.Count == 0)
            {
                _log.WriteLog("没有≥2条的分组，无需合并，退出");
                this.View.ShowMessage("没有找到可合并的计划订单分组。");
                return;
            }

            _log.WriteLog($"需合并分组: {groups.Count} 组，共 {groups.Sum(g => g.Count())} 条");
            foreach (var g in groups)
            {
                var first = g.First();
                _log.WriteLog($"  物料=[{GetString(first, "WLNUM")}], 计划跟踪号=[{GetString(first, "JHGZH")}], 云枢单据号=[{GetString(first, "YSDJH")}], 标记=[{GetString(first, "BJ")}] → {g.Count()}条");
            }

            _log.WriteLog("开始逐组调用标准合并服务(PlanOrderServiceHelper.MergeAction + MergeService.ExecuteMerge)...");

            var totalResult = new OperationResult();
            var successCount = 0;
            var failCount = 0;

            var idx = 0;
            foreach (var group in groups)
            {
                idx++;
                var groupRows = group.ToList();
                var first = groupRows[0];
                var selectedInfos = BuildSelectedRows(groupRows);

                _log.WriteLog($"---------- 第{idx}/{groups.Count}组合并 ----------");
                _log.WriteLog($"  物料=[{GetString(first, "WLNUM")}], 计划跟踪号=[{GetString(first, "JHGZH")}], 云枢单据号=[{GetString(first, "YSDJH")}], 标记=[{GetString(first, "BJ")}]");
                _log.WriteLog($"  合并行数={groupRows.Count}, 单据号=[{string.Join(",", groupRows.Select(r => GetString(r, "FBILLNO")))}]");
                _log.WriteLog($"  FIDs=[{string.Join(",", groupRows.Select(r => GetString(r, "FID")))}]");

                var option = OperateOption.Create();

                _log.WriteLog($"  阶段1: 调用 PlanOrderServiceHelper.MergeAction, 传入{selectedInfos.Count}行");
                var preResult = PlanOrderServiceHelper.MergeAction(
                    this.Context,
                    selectedInfos,
                    option
                );
                _log.WriteLog($"  MergeAction 结果: IsSuccess={preResult.IsSuccess}");

                if (!preResult.IsSuccess)
                {
                    _log.WriteLog($"  合并失败(MergeAction阶段)");
                    totalResult.MergeResult(preResult);
                    failCount++;
                    continue;
                }

                var mergeService = new MergeService();
                var token = Guid.NewGuid().ToString();
                _log.WriteLog($"  阶段2: 调用 MergeService.ExecuteMerge, Token={token}");

                var mergeResult = mergeService.ExecuteMerge(
                    this.Context,
                    option,
                    token
                );
                _log.WriteLog($"  ExecuteMerge 结果: IsSuccess={mergeResult.IsSuccess}");

                totalResult.MergeResult(mergeResult);

                if (mergeResult.IsSuccess)
                {
                    _log.WriteLog($"  合并成功 ✓");
                    successCount++;
                }
                else
                {
                    _log.WriteLog($"  合并失败(ExecuteMerge阶段)");
                    failCount++;
                }
            }

            _log.WriteLog($"========== 合并汇总: 共{groups.Count}组, 成功{successCount}组, 失败{failCount}组 ==========");

            this.View.ShowOperationResult(totalResult);
            this.View.Refresh();
            _log.WriteLog("列表已刷新");
        }

        private List<DynamicObject> QueryPlanOrders()
        {
            string sql = @"
SELECT
    A.FID,
    A.FBILLNO,
    A.FBILLTYPEID,
    A.FMATERIALID,
    M.FNUMBER AS WLNUM,
    ISNULL(A.FMtoNo, '') AS JHGZH,
    ISNULL(A.F_PAEZ_YSDJH, '') AS YSDJH,
    ISNULL(A.F_BJ, '') AS BJ
FROM T_PLN_PLANORDER A
JOIN T_PLN_PLANORDER_B B ON A.FID = B.FID
JOIN T_BD_MATERIAL M ON A.FMATERIALID = M.FMATERIALID
WHERE A.FDOCUMENTSTATUS IN ('A', 'D')
  AND A.FRELEASESTATUS = '0'
  AND A.FRELEASETYPE = '1' AND A.F_BJ =1
ORDER BY A.FMATERIALID, A.FMtoNo, A.F_PAEZ_YSDJH, A.F_BJ, A.FID";

            _log.WriteLog($"查询SQL: {sql}");

            var result = DBUtils.ExecuteDynamicObject(this.Context, sql);
            _log.WriteLog($"SQL执行完成, 返回{result?.Count ?? 0}条");

            return result.ToList();
        }

        private string BuildGroupKey(DynamicObject row)
        {
            return string.Join("_", new[]
            {
                GetString(row, "FMATERIALID"),
                GetString(row, "JHGZH"),
                GetString(row, "YSDJH"),
                GetString(row, "BJ")
            });
        }

        private ListSelectedRowCollection BuildSelectedRows(List<DynamicObject> rows)
        {
            var selectedRows = new ListSelectedRowCollection();

            foreach (var row in rows)
            {
                string fid = GetString(row, "FID");
                string billNo = GetString(row, "FBILLNO");
                string billTypeId = GetString(row, "FBILLTYPEID");

                selectedRows.Add(new ListSelectedRow(
                    fid,
                    string.Empty,
                    0,
                    "PLN_PLANORDER")
                {
                    BillNo = billNo,
                    BillTypeID = billTypeId
                });
            }

            return selectedRows;
        }

        private string GetString(DynamicObject row, string key)
        {
            object value = row[key];
            if (value == null || value == DBNull.Value)
                return string.Empty;

            return Convert.ToString(value);
        }
    }
}

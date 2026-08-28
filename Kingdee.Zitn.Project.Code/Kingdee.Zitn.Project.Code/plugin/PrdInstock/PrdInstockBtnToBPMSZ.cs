using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.Zitn.Project.Code.conf;
using Kingdee.Zitn.Project.Code.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Kingdee.Zitn.Project.Code.plugin.PRDinstock
{
    [Description("【生产入库表单服务】【按钮】--试制流程(BPM)补偿推送，逻辑同 PrdInstockAuditToBPMSZ")]
    [HotUpdate]
    public class PrdInstockBtnToBPMSZ : AbstractDynamicFormPlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("生产入库单按钮补偿推送BpmApi(试制)");

        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);

            if (!e.BarItemKey.Equals("bpmapi_sz_btn", StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                string billNo = this.View.Model.GetValue("FBillNo")?.ToString();
                if (string.IsNullOrWhiteSpace(billNo))
                {
                    this.View.ShowErrMessage("未获取到有效的入库单号，操作已中止。");
                    return;
                }

                var fidData = DBUtils.ExecuteDynamicObject(this.Context,
                    string.Format("/*dialect*/SELECT FID FROM T_PRD_INSTOCK WHERE FBILLNO = '{0}'", billNo));
                if (fidData == null || fidData.Count == 0)
                {
                    this.View.ShowErrMessage($"未查询到单号 {billNo} 对应的入库单，操作已中止。");
                    return;
                }

                long fid = Convert.ToInt64(fidData[0]["FID"]);
                _log.Section($"按钮补偿开始，单号: {billNo}, 单据ID: {fid}");

                var fids = fid.ToString();

                /*
                 * 按(生产订单号 + 物料编码)分组去重，仅处理物料编码90开头
                 * 同 PrdInstockAuditToBPMSZ 的 summarySql，只是 FID 限定为当前单据
                 */
                var summarySql = string.Format($@"/*dialect*/SELECT A.FBILLNO,
                    B.FMOBILLNO,
                    M.FNUMBER,
                    MOE.FQTY,
                    MOA.FSTOCKINQUASELAUXQTY,
                    MAX(MOE.FSFFQSZ) AS FSFFQSZ,
                    MAX(A.FCREATEDATE) AS MAXCREATEDATE
                FROM T_PRD_INSTOCK A
                JOIN T_PRD_INSTOCKENTRY B ON A.FID = B.FID
                JOIN T_BD_MATERIAL M ON B.FMATERIALID = M.FMATERIALID
                JOIN T_PRD_MOENTRY MOE ON B.FMOENTRYID = MOE.FENTRYID
                JOIN T_PRD_MOENTRY_A MOA ON MOE.FENTRYID = MOA.FENTRYID
                WHERE A.FID IN ({fids}) AND M.FNUMBER LIKE '90%'
                GROUP BY B.FMOBILLNO, M.FNUMBER, MOE.FQTY ,A.FBILLNO,MOA.FSTOCKINQUASELAUXQTY");

                DynamicObjectCollection summaryData = DBUtils.ExecuteDynamicObject(this.Context, summarySql);
                if (summaryData == null || summaryData.Count == 0)
                {
                    this.View.ShowMessage("未查询到符合条件的入库明细（物料编码90开头）。");
                    return;
                }

                int successCount = 0;
                var skipMessages = new List<string>();
                var failMessages = new List<string>();

                foreach (DynamicObject item in summaryData)
                {
                    string scddBillNo = "";
                    string materialCode = "";
                    string step = "";

                    try
                    {
                        step = "读取汇总行字段";
                        scddBillNo = GetStr(item, "FMOBILLNO") ?? "";
                        materialCode = GetStr(item, "FNUMBER") ?? "";

                        // 80%阈值校验
                        decimal fqty = GetDecimal(item, "FQTY");
                        decimal totalRealQty = GetDecimal(item, "FSTOCKINQUASELAUXQTY");
                        if (fqty <= 0 || (totalRealQty / fqty) <= 0.8m)
                        {
                            string ratioStr = fqty > 0 ? (totalRealQty / fqty).ToString("P2") : "0%";
                            _log.WriteLog($"跳过(未达80%): 生产订单号={scddBillNo}, 物料编码={materialCode}, " +
                                $"累计入库={totalRealQty}, 订单数量={fqty}, 比例={ratioStr}");
                            skipMessages.Add($"{materialCode}: 未达80%({ratioStr})");
                            continue;
                        }

                        // 已发起过试制，跳过
                        int fsffqsz = GetInt(item, "FSFFQSZ");
                        if (fsffqsz == 1)
                        {
                            _log.WriteLog($"跳过(已发起试制): 生产订单号={scddBillNo}, 物料编码={materialCode}, FSFFQSZ={fsffqsz}");
                            skipMessages.Add($"{materialCode}: 已发起过试制");
                            continue;
                        }

                        step = "查询入库单信息";
                        string billno = GetStr(item, "FBILLNO") ?? "";

                        var scrkSql = string.Format($@"/*dialect*/SELECT
                        T.FNAME AS RKDCJR,
                        A.FCREATEDATE AS RKDCJSJ,
                        CASE
                          WHEN A.FSTOCKORGID = '1' THEN '青岛智腾科技有限公司'
                          WHEN A.FSTOCKORGID = '101006' THEN '青岛智腾微电子有限公司'
                          WHEN A.FSTOCKORGID = '101007' THEN '青岛智腾电源有限公司'
                          WHEN A.FSTOCKORGID = '101050' THEN 'test'
                          WHEN A.FSTOCKORGID = '1404303' THEN '青岛智腾烽行能源有限公司'
                          WHEN A.FSTOCKORGID = '1516310' THEN '青岛晶英电子科技有限公司'
                          WHEN A.FSTOCKORGID = '3149866' THEN '青岛智腾微电子有限公司北京分公司'
                          WHEN A.FSTOCKORGID = '3241152' THEN '青岛加速度智能科技有限公司'
                          WHEN A.FSTOCKORGID = '4032930' THEN '青岛智腾微电子有限公司西安分公司'
                          WHEN A.FSTOCKORGID = '4665868' THEN '青岛智导电子有限公司'
                          WHEN A.FSTOCKORGID = '4665869' THEN '青岛深科睿探技术有限公司'
                          WHEN A.FSTOCKORGID = '4852744' THEN '青岛智导电子有限公司北京分公司'
                         END AS RKDZZ,
                         FCPSYB AS CPSYB,
                         M.FNUMBER AS MATERIALCODE,
                         M1.FNAME AS MATERIALNAME,
                         M1.FSPECIFICATION AS MATERIALSPEC,
                         A.FSCGCPCH AS SCGCPCH,
                         MO.FQTY AS QTY,
                         MOA.FSTOCKINQUASELAUXQTY AS LJRKSL,
                         MOA.FSTOCKINQUAAUXQTY AS HGRKSL,
                         MOA.FSTARTDATE AS KGRQ,
                         A.FDATE AS RKEQ,
                         DEP.FNAME AS SCCJ,
                         CASE
                            WHEN B.FCPSYB =1 THEN '一部：民品事业部'
                            WHEN B.FCPSYB =2 THEN '二部：军品电子事业部'
                            WHEN B.FCPSYB =3 THEN '三部：传感技术事业部'
                            WHEN B.FCPSYB =4 THEN '事业四部'
                            WHEN B.FCPSYB =11 THEN '事业五部'
                          END AS SYB,
                         M.F_PAEZ_SYB AS MATSYB
                        FROM T_PRD_INSTOCK A
                        JOIN T_PRD_INSTOCKENTRY B ON A.FID = B.FID
                        JOIN T_PRD_INSTOCKENTRY_A BA ON B.FENTRYID = BA.FENTRYID
                        JOIN T_SEC_USER T ON A.FCREATORID = T.FUSERID
                        JOIN T_BD_MATERIAL M ON B.FMATERIALID = M.FMATERIALID
                        JOIN T_BD_MATERIAL_L M1 ON M.FMATERIALID = M1.FMATERIALID
                        JOIN T_PRD_MOENTRY MO ON B.FMOENTRYID = MO.FENTRYID
                        JOIN T_PRD_MOENTRY_A MOA ON MO.FENTRYID = MOA.FENTRYID
                        JOIN T_BD_DEPARTMENT_L DEP ON B.FWORKSHOPID = DEP.FDEPTID
                        WHERE A.FBILLNO = '{billno}'");

                        DynamicObjectCollection scrkData = DBUtils.ExecuteDynamicObject(this.Context, scrkSql);
                        if (scrkData == null || scrkData.Count == 0) continue;

                        DynamicObject matchedItem = null;
                        foreach (DynamicObject row in scrkData)
                        {
                            if (GetStr(row, "MATERIALCODE") == materialCode)
                            {
                                matchedItem = row;
                                break;
                            }
                        }
                        if (matchedItem == null) continue;

                        step = "提取BPM字段";
                        string rkdzz = GetStr(matchedItem, "RKDZZ") ?? "";
                        string cpsybName = GetStr(matchedItem, "SYB") ?? "";
                        if (string.IsNullOrEmpty(cpsybName))
                        {
                            // 入库单分录事业部(FCPSYB)为空时，回退取物料上的事业部字段 F_PAEZ_SYB
                            string matSyb = GetStr(matchedItem, "MATSYB") ?? "";
                            var sybFallbackMap = new Dictionary<string, string>
                            {
                                { "1", "一部：民品事业部" },
                                { "2", "二部：军品电子事业部" },
                                { "3", "三部：传感技术事业部" },
                                { "4", "事业四部" },
                                { "11", "事业五部" },
                            };
                            cpsybName = sybFallbackMap.TryGetValue(matSyb, out var fallbackName)
                                ? fallbackName : "";
                        }
                        string matCode = GetStr(matchedItem, "MATERIALCODE") ?? "";
                        string materialname = GetStr(matchedItem, "MATERIALNAME") ?? "";
                        string materialspec = GetStr(matchedItem, "MATERIALSPEC") ?? "";
                        string scgcpch = GetStr(matchedItem, "SCGCPCH") ?? "";
                        string yzjd = scgcpch.Length >= 2 ? scgcpch.Substring(1, 1) : "";
                        string qty = GetStr(matchedItem, "QTY") ?? "";
                        string ljrkls = GetStr(matchedItem, "LJRKSL") ?? "";
                        string hgrksl = GetStr(matchedItem, "HGRKSL") ?? "";
                        string kgrq = GetStr(matchedItem, "KGRQ") ?? "";
                        string rkeq = GetStr(matchedItem, "RKEQ") ?? "";
                        string sccj = GetStr(matchedItem, "SCCJ") ?? "";

                        // 生产车间 → 责任人ID映射（creater字段用）
                        var workshopUserMap = new Dictionary<string, string>
                        {
                            { "电装车间", "2c2c80849f8e4fa7019f91d1e1e65394" },
                            { "微电子车间", "ff8080818fd2b96c018fd2bb32eb023e" },
                            { "传感器车间", "ff8080818fd2b96c018fd2bbcd6a0627" },
                            { "加表车间", "ff8080818fd2b96c018fd2bbcd6a0627" },
                            { "加速度计车间", "ff8080818fd2b96c018fd2bbcd6a0627" },
                            { "机加工车间", "ff8080818fd2b96c018fd2bb419002bc" },
                            { "电测车间", "ff8080818fd2b96c018fd2bb32eb023e" },
                        };
                        string rkdcjr = workshopUserMap.TryGetValue(sccj, out var userId)
                            ? userId : "";

                        // 事业部名称 → 组织ID映射（shiyebu字段用）
                        var deptOrgMap = new Dictionary<string, string>
                        {
                            { "事业五部", "ff8080818fd22d5b018fd271c1c40261" },
                            { "一部：民品事业部", "ff8080818fd22d5b018fd271c56c028d" },
                            { "二部：军品电子事业部", "ff8080818fd22d5b018fd271c6dd029d" },
                            { "三部：传感技术事业部", "ff8080818fd22d5b018fd271c90202b9" },
                            { "事业四部", "ff808081948850b401948d5a28531958" },
                        };
                        string cpsyb = deptOrgMap.TryGetValue(cpsybName, out var orgId)
                            ? orgId : "";

                        var deptmentMap = new Dictionary<string, string>
                        {
                            { "电装车间", "ff80808194de9bc30194dfc15d712392" },
                            { "微电子车间", "ff80808194de9bc30194dfc15fb32398" },
                            { "传感器车间", "ff8080818fd22d5b018fd271c9ff02c6" },
                            { "加表车间", "ff8080818fd22d5b018fd271c9ff02c6" },
                            { "加速度计车间", "ff8080818fd22d5b018fd271c9ff02c6" },
                            { "机加工车间", "ff80808194de9bc30194dfc16097239b" },
                            { "电测车间", "ff80808194de9bc30194dfc15fb32398" },
                        };
                        string deptmentId = deptmentMap.TryGetValue(sccj, out var deptId)
                            ? deptId : "";

                        // 根据物料编码查询生产订单信息（拼接 LongText1780472941618）
                        var scddSql = string.Format($@"/*dialect*/SELECT
                        FBILLNO,
                        BA.FSTOCKINQUAAUXQTY AS HGSL,
                        (B.FQTY - BQ.FNOSTOCKINQTY) AS LJRKSL,
                        A.FCREATEDATE
                    FROM T_PRD_MO A
                    JOIN T_PRD_MOENTRY B ON A.FID = B.FID
                    JOIN T_PRD_MOENTRY_A BA ON B.FENTRYID = BA.FENTRYID
                    JOIN T_PRD_MOENTRY_Q BQ ON B.FENTRYID = BQ.FENTRYID
                    JOIN T_BD_MATERIAL M ON B.FMATERIALID = M.FMATERIALID
                    WHERE M.FNUMBER = '{matCode}' AND (BA.FSTATUS = 5 OR BA.FSTATUS = 6 OR BA.FSTATUS = 7)
                    ORDER BY A.FCREATEDATE DESC");

                        DynamicObjectCollection scddData = DBUtils.ExecuteDynamicObject(this.Context, scddSql);
                        var scddInfo = new StringBuilder();
                        if (scddData != null && scddData.Count > 0)
                        {
                            var prefixSet = new HashSet<string>();
                            for (int j = 0; j < scddData.Count; j++)
                            {
                                var scddItem = scddData[j];
                                string moBillNo = GetStr(scddItem, "FBILLNO") ?? "";
                                string prefix = moBillNo.Contains('-') ? moBillNo.Split('-')[0] : moBillNo;

                                if (prefixSet.Count >= 3 && !prefixSet.Contains(prefix))
                                    break;

                                prefixSet.Add(prefix);
                                decimal hgslVal = GetDecimal(scddItem, "HGSL");
                                decimal ljrkslVal = GetDecimal(scddItem, "LJRKSL");
                                decimal ratio = ljrkslVal > 0 ? hgslVal / ljrkslVal : 0;

                                if (scddInfo.Length > 0)
                                    scddInfo.Append('\n');
                                scddInfo.Append(moBillNo).Append(':')
                                                        .Append((ratio * 100).ToString("F2")).Append("%(")
                                                        .Append(hgslVal == 0 ? "0" : hgslVal.ToString().TrimEnd('0').TrimEnd('.'))
                                                        .Append('/')
                                                        .Append(ljrkslVal == 0 ? "0" : ljrkslVal.ToString().TrimEnd('0').TrimEnd('.'))
                                                        .Append(')');
                            }
                        }

                        _log.WriteLog($"【满足80%条件，开始处理】");
                        _log.WriteLog($"  生产订单号: {scddBillNo}");
                        _log.WriteLog($"  物料编码: {matCode}");
                        _log.WriteLog($"  物料名称: {materialname}");
                        _log.WriteLog($"  物料规格: {materialspec}");
                        _log.WriteLog($"  入库单号: {billno}");
                        _log.WriteLog($"  入库组织: {rkdzz}");
                        _log.WriteLog($"  产品事业部: {cpsybName}");
                        _log.WriteLog($"  研发阶段: {yzjd}");
                        _log.WriteLog($"  生产订单数量: {qty}");
                        _log.WriteLog($"  累计入库数量: {ljrkls}");
                        _log.WriteLog($"  合格入库数量: {hgrksl}");
                        _log.WriteLog($"  开工日期: {kgrq}");
                        _log.WriteLog($"  入库日期: {rkeq}");
                        _log.WriteLog($"  生产车间: {sccj}");
                        _log.WriteLog($"  生产车间匹配责任人ID: {(string.IsNullOrEmpty(rkdcjr) ? "(未匹配)" : rkdcjr)}");
                        _log.WriteLog($"  事业部匹配组织ID: {(string.IsNullOrEmpty(cpsyb) ? "(未匹配)" : cpsyb)}");
                        _log.WriteLog($"  生产车间匹配部门ID: {(string.IsNullOrEmpty(deptmentId) ? "(未匹配)" : deptmentId)}");
                        _log.WriteLog($"  生产订单拼接信息: {(scddInfo.Length > 0 ? scddInfo.ToString() : "(无)")}");

                        // 获取BPM认证token
                        step = "获取BPM Token";
                        var tokenParams = new Dictionary<string, string>
                        {
                            { "grant_type", "client_credentials" },
                            { "client_id", "xclient" },
                            { "client_secret", "0a417ecce58c31b32364ce19ca8fcd15" }
                        };
                        string accessToken = "";
                        using (var httpClient = new HttpClient())
                        {
                            var tokenResponse = httpClient.PostAsync(
                                "http://10.0.32.10:8769/api/oauth/token",
                                new FormUrlEncodedContent(tokenParams)
                            ).Result;
                            var tokenJson = tokenResponse.Content.ReadAsStringAsync().Result;
                            var tokenObj = JObject.Parse(tokenJson);
                            accessToken = tokenObj["access_token"].ToString();
                        }

                        // 调用BPM发起流程接口
                        step = "调用BPM发起流程";
                        var workflowBody = new JObject
                        {
                            ["data"] = new JObject
                            {
                                ["creater"] = rkdcjr,
                                ["org"] = rkdzz,
                                ["shiyebu"] = cpsyb,
                                ["productCode"] = matCode,
                                ["productName"] = materialname,
                                ["model"] = materialspec,
                                ["developmentPhase"] = yzjd,
                                ["makeQty"] = qty,
                                ["inStoreQty"] = ljrkls,
                                ["hgRkQty"] = hgrksl,
                                ["productDate"] = kgrq,
                                ["rkDate"] = rkeq,
                                ["workShop"] = sccj,
                                ["scrkOrderNo"] = billno,
                                ["LongText1780472941618"] = scddInfo.ToString()
                            },
                            ["departmentId"] = deptmentId,
                            ["finishStart"] = true,
                            ["nextParticipants"] = new JArray
                            {
                                new JObject
                                {
                                    ["activityCode"] = "",
                                    ["participants"] = new JArray()
                                }
                            },
                            ["trustor"] = "",
                            ["userId"] = rkdcjr,
                            ["workflowCode"] = "trial_production_report_flow"
                        };

                        using (var httpClient = new HttpClient())
                        {
                            httpClient.DefaultRequestHeaders.Authorization =
                                new AuthenticationHeaderValue("Bearer", accessToken);
                            var content = new StringContent(workflowBody.ToString(), Encoding.UTF8, "application/json");
                            var workflowResponse = httpClient.PostAsync(
                                "http://10.0.32.10:8769/api/openapi/v3/workflow/start",
                                content
                            ).Result;
                            var workflowResult = workflowResponse.Content.ReadAsStringAsync().Result;

                            var resultObj = JObject.Parse(workflowResult);
                            int errcode = resultObj["errcode"]?.Value<int>() ?? -1;
                            string errmsg = resultObj["errmsg"]?.ToString() ?? "";

                            if (errcode == 0 && errmsg == "流程启动成功")
                            {
                                step = "更新FSFFQSZ字段";
                                var updateMoSql = string.Format($@"/*dialect*/UPDATE B
                            SET B.FSFFQSZ = 1
                            FROM T_PRD_MO A
                            JOIN T_PRD_MOENTRY B ON A.FID = B.FID
                            JOIN T_BD_MATERIAL M ON B.FMATERIALID = M.FMATERIALID
                            WHERE M.FNUMBER = '{matCode}' AND A.FBILLNO = '{scddBillNo}'");

                                DBUtils.Execute(this.Context, updateMoSql);
                                _log.WriteLog($"【处理成功】生产订单号={scddBillNo}, 物料编码={matCode}, " +
                                    $"入库单号={billno}, 已更新FSFFQSZ=1");
                                successCount++;
                            }
                            else
                            {
                                _log.WriteLog($"【处理失败】生产订单号={scddBillNo}, 物料编码={matCode}, " +
                                    $"入库单号={billno}, errcode={errcode}, errmsg={errmsg}, " +
                                    $"完整BPM响应: {workflowResult}");

                                string pluginUrl = "Kingdee.Zitn.Project.Code.plugin.PRDinstock.PrdInstockBtnToBPMSZ";

                                SendMsg.Send($@"🚨【紧急】【生产入库】手工BPM发起流程失败！

                                操作单据：“ 试制总结”
                                入库单号：{billno}
                                时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}
                                异常信息：{errmsg}
                                完整BPM响应：{workflowResult}
                                插件：{pluginUrl}");

                                failMessages.Add($"{matCode}: {errmsg}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"生产订单号={scddBillNo}, 物料编码={materialCode}, 当前步骤={step}, " +
                            $"异常类型={ex.GetType().Name}, 异常消息={ex.Message}");
                        _log.Error(ex);
                        SendMsg.Send($@"🚨【紧急】【生产入库】手工BPM发起流程异常！
                        操作单据：“ 试制总结”
                        入库单号：{billNo}
                        时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}
                        异常类型：{ex.GetType().Name}
                        异常消息：{ex.Message}
                        堆栈信息：{ex.StackTrace}");

                        failMessages.Add($"{materialCode}: {ex.Message}");
                    }
                }

                // 汇总反馈
                if (failMessages.Count > 0)
                {
                    this.View.ShowErrMessage($"推送失败 {failMessages.Count} 条：\n" +
                        string.Join("\n", failMessages));
                }
                else if (successCount > 0)
                {
                    string skipStr = skipMessages.Count > 0
                        ? $"\n跳过 {skipMessages.Count} 条：\n" + string.Join("\n", skipMessages)
                        : "";
                    this.View.ShowMessage($"推送BpmApi成功 {successCount} 条！{skipStr}");
                }
                else
                {
                    this.View.ShowMessage("未推送任何流程：\n" + string.Join("\n", skipMessages));
                }
            }
            catch (Exception ex)
            {
                _log.Error("按钮补偿插件异常");
                _log.Error(ex);
                this.View.ShowErrMessage($"系统异常：{ex.Message}");
            }
        }

        private static string GetStr(DynamicObject obj, string field)
        {
            try
            {
                var val = obj[field];
                return val?.ToString();
            }
            catch { return null; }
        }

        private static decimal GetDecimal(DynamicObject obj, string field)
        {
            try
            {
                var val = obj[field];
                return val == null ? 0m : Convert.ToDecimal(val);
            }
            catch { return 0m; }
        }

        private static int GetInt(DynamicObject obj, string field)
        {
            try
            {
                var val = obj[field];
                return val == null ? 0 : Convert.ToInt32(val);
            }
            catch { return 0; }
        }
    }
}

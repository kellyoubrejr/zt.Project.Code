using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.Zitn.Project.Code.conf;
using Kingdee.Zitn.Project.Code.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Text;

namespace Kingdee.Zitn.Project.Code.Schedule
{
    [Description("【跑批】：同步BPM销售项目信息到本地表")]
    [Kingdee.BOS.Util.HotUpdate]
    public class BPMSalesProject : IScheduleService
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("BPM销售项目同步");

        private const string API_URL = "http://10.0.32.10:8769/api/public/project/kdSysSaleLead";
        private const string TABLE_NAME = "QFJE_t_Cust100021";

        public void Run(Context ctx, Kingdee.BOS.Core.Schedule schedule)
        {
            _log.Section("开始同步BPM销售项目信息");

            try
            {
                // 1. 调用BPM接口获取销售项目数据
                _log.WriteLog($"调用接口: {API_URL}");
                 
                string responseBody;
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(5);

                    var request = new HttpRequestMessage(HttpMethod.Post, API_URL);
                    request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

                    var response = httpClient.SendAsync(request).Result;
                    responseBody = response.Content.ReadAsStringAsync().Result;

                    _log.WriteLog($"接口返回HTTP状态: {response.StatusCode}");

                    if (!response.IsSuccessStatusCode)
                    {
                        _log.Error($"接口调用失败，HTTP {response.StatusCode}，响应：{responseBody}");
                        return;
                    }
                }

                // 2. 解析返回数据
                var responseObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);
                int errcode = responseObj != null && responseObj.ContainsKey("errcode") ? Convert.ToInt32(responseObj["errcode"]) : -1;
                string errmsg = responseObj != null && responseObj.ContainsKey("errmsg") ? responseObj["errmsg"]?.ToString() : "";

                _log.WriteLog($"接口业务码: errcode={errcode}, errmsg={errmsg}");

                if (errcode != 0)
                {
                    _log.Error($"接口返回错误: errcode={errcode}, errmsg={errmsg}");
                    return;
                }

                var dataArray = responseObj.ContainsKey("data") ? responseObj["data"] as JArray : null;
                if (dataArray == null || dataArray.Count == 0)
                {
                    _log.WriteLog("接口返回数据为空，无需同步");
                    return;
                }

                _log.WriteLog($"接口返回 {dataArray.Count} 条销售项目数据");

                // 3. 逐条处理
                int skipCount = 0;
                int insertCount = 0;
                int failCount = 0;

                foreach (var item in dataArray)
                {
                    string crmId = item["CRMID"]?.ToString();
                    string fName = item["FNAME"]?.ToString();

                    if (string.IsNullOrWhiteSpace(crmId))
                    {
                        _log.WriteLog($"跳过：CRMID为空，FNAME={fName}");
                        continue;
                    }

                    try
                    {
                        // 4. 查询是否已存在
                        string checkSql = $"SELECT 1 FROM {TABLE_NAME} WHERE FCRMID = '{crmId.Replace("'", "''")}'";
                        _log.WriteLog($"查询是否存在SQL: {checkSql}");
                        var ds = DBUtils.ExecuteDataSet(ctx, checkSql);

                        if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                        {
                            _log.WriteLog($"跳过已存在: CRMID={crmId}, FNAME={fName}");
                            skipCount++;
                            continue;
                        }

                        string insertSql = $@"/*dialect*/
                                            DECLARE @NewFID INT;
                                            SET @NewFID = (SELECT ISNULL(MAX(FID), 0) + 1 FROM {TABLE_NAME});

                                            INSERT INTO {TABLE_NAME}
                                                (FID, FNUMBER, FDOCUMENTSTATUS, FFORBIDSTATUS, FNAME, FCRMID)
                                            VALUES
                                                (@NewFID, CAST(@NewFID AS NVARCHAR(50)), 'C', 'A',
                                                 N'{fName.Replace("'", "''")}',
                                                 '{crmId.Replace("'", "''")}');";

                        _log.WriteLog($"插入SQL: {insertSql}");
                        var insertResult = DBUtils.Execute(ctx, insertSql);

                        _log.WriteLog($"插入成功: CRMID={crmId}, FNAME={fName}");
                        insertCount++;
                    }
                    catch (Exception ex)
                    {
                        _log.WriteLog($"插入失败: CRMID={crmId}, FNAME={fName}, 错误={ex.Message}");
                        _log.Error(ex);
                        failCount++;
                    }
                }

                // 6. 汇总结果
                string summary = $"同步完成: 共{dataArray.Count}条, 新增{insertCount}条, 跳过{skipCount}条, 失败{failCount}条";
                _log.Section(summary);
                _log.WriteLog($"跳过明细: {skipCount}条已存在");
                _log.WriteLog($"新增明细: {insertCount}条新插入");
                _log.WriteLog($"失败明细: {failCount}条插入失败");

                if (failCount > 0)
                {
                    SendMsg.Send($"[BPM销售项目同步] {summary}");
                }
            }
            catch (Exception ex)
            {
                _log.Error("同步BPM销售项目信息异常");
                _log.Error(ex);
                SendMsg.Send("[BPM销售项目同步] 同步异常", ex);
            }
        }
    }
}

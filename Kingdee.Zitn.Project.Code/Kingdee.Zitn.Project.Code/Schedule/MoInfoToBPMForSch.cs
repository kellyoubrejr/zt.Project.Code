using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.Zitn.Project.Code.conf;
using Kingdee.Zitn.Project.Code.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Net.Http;
using System.Text;

namespace Kingdee.Zitn.Project.Code.Schedule
{
    [Description("【跑批】：生产订单信息每天凌晨1点推送bpm系统")]
    [Kingdee.BOS.Util.HotUpdate]
    public class MoInfoToBPMForSch : IScheduleService
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("生产订单推送BPM");

        private const string API_URL = "http://10.0.32.10:8769/api/public/salenotice/batchReceiveProductionOrderProgress";

        public void Run(Context ctx, Kingdee.BOS.Core.Schedule schedule)
        {
            _log.Section("开始推送生产订单信息到BPM");

            try
            {
                string sql = "EXEC sp_GetProductionOrderInfoToBPMSch";
                var result = DBUtils.ExecuteDataSet(ctx, sql);

                if (result.Tables.Count == 0 || result.Tables[0].Rows.Count == 0)
                {
                    _log.WriteLog("存储过程返回数据为空，无需推送");
                    SendMsg.Send("[生产订单推送BPM] 存储过程返回数据为空，无需推送");
                    return;
                }

                DataTable table = result.Tables[0];
                int totalCount = table.Rows.Count;
                _log.WriteLog($"查询到 {totalCount} 条生产订单数据，开始组装推送");

                var dataList = new List<Dictionary<string, object>>();
                foreach (DataRow row in table.Rows)
                {
                    var item = new Dictionary<string, object>
                    {
                        ["organiz"] = row["organiz"] == DBNull.Value ? "" : row["organiz"].ToString(),
                        ["organizationalCode"] = row["organizationalCode"] == DBNull.Value ? "" : row["organizationalCode"].ToString(),
                        ["materialCode"] = row["WLNUM"] == DBNull.Value ? "" : row["WLNUM"].ToString(),
                        ["materialName"] = row["WLNAME"] == DBNull.Value ? "" : row["WLNAME"].ToString(),
                        ["modelNumber"] = row["WLGG"] == DBNull.Value ? "" : row["WLGG"].ToString(),
                        ["quantity"] = row["FQTY"] == DBNull.Value ? 0 : Convert.ToDecimal(row["FQTY"]),
                        ["inStoreQty"] = row["INSTOCKQTY"] == DBNull.Value ? 0 : Convert.ToDecimal(row["INSTOCKQTY"]),
                        ["productionOrderNumber"] = row["SCDDH"] == DBNull.Value ? "" : row["SCDDH"].ToString(),
                        ["salesOrderNumber"] = row["XSDDH"] == DBNull.Value ? "" : row["XSDDH"].ToString(),
                        ["productionOrderStatus"] = row["FStatus"] == DBNull.Value ? "" : row["FStatus"].ToString()
                    };
                    dataList.Add(item);
                }

                var postData = new Dictionary<string, object>
                {
                    ["idh2oProductionOrderProgresList"] = dataList
                };

                string jsonBody = JsonConvert.SerializeObject(postData);
                _log.WriteLog($"数据组装完成，共 {totalCount} 条，开始调用BPM接口");

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(10);

                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    var response = httpClient.PostAsync(API_URL, content).Result;
                    string responseBody = response.Content.ReadAsStringAsync().Result;

                    if (!response.IsSuccessStatusCode)
                    {
                        _log.Error($"推送失败，HTTP {response.StatusCode}，响应：{responseBody}");
                        SendMsg.Send($"[生产订单推送BPM] 推送失败！HTTP {response.StatusCode}，共 {totalCount} 条数据\n响应：{responseBody}");
                        return;
                    }

                    // HTTP 200 但需要校验业务状态码，errcode=0 才算成功
                    var resultObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);
                    int errcode = resultObj != null && resultObj.ContainsKey("errcode") ? Convert.ToInt32(resultObj["errcode"]) : -1;
                    string errmsg = resultObj != null && resultObj.ContainsKey("errmsg") ? resultObj["errmsg"]?.ToString() : "";

                    if (errcode == 0)
                    {
                        _log.Section($"推送成功：共 {totalCount} 条，HTTP {response.StatusCode}");
                        SendMsg.Send($"[生产订单推送BPM] 推送成功，共 {totalCount} 条生产订单数据已同步");
                    }
                    else
                    {
                        _log.Error($"推送失败，errcode={errcode}，errmsg={errmsg}，响应：{responseBody}");
                        SendMsg.Send($"[生产订单推送BPM] 推送失败！errcode={errcode}，共 {totalCount} 条数据\nerrmsg：{errmsg}");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("推送生产订单信息到BPM异常");
                _log.Error(ex);
                SendMsg.Send("[生产订单推送BPM] 推送异常", ex);
            }
        }
    }
}

using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Kingdee.Zitn.Project.Code.plugin.SFForm
{
    /// <summary>
    /// 顺丰开放平台 API 客户端
    /// </summary>
    public class SFExpressClient
    {
        private readonly string _partnerID;
        private readonly string _checkWord;
        private readonly string _apiUrl;
        private readonly int _timeoutMs;
        private readonly CustomLog.LogWriter _log;

        public SFExpressClient(string partnerID, string checkWord, string apiUrl,
            int timeoutMs = 15000, CustomLog.LogWriter logger = null)
        {
            _partnerID = partnerID;
            _checkWord = checkWord;
            _apiUrl = apiUrl;
            _timeoutMs = timeoutMs;
            _log = logger ?? CustomLog.For("顺丰API");
        }

        #region 下单 EXP_RECE_CREATE_ORDER

        public SFResult<SFOrderResponse> CreateOrder(SFOrderRequest request)
        {
            return CallService<SFOrderResponse>("EXP_RECE_CREATE_ORDER", request);
        }

        #endregion

        #region 订单结果查询 EXP_RECE_SEARCH_ORDER_RESP

        public SFResult<SFSearchOrderRespResponse> SearchOrderResp(SFSearchOrderRespRequest request)
        {
            return CallService<SFSearchOrderRespResponse>("EXP_RECE_SEARCH_ORDER_RESP", request);
        }

        /// <summary>
        /// 用 orderId 查单，用于超时补偿
        /// </summary>
        public SFResult<SFSearchOrderRespResponse> SearchOrderResp(string orderId)
        {
            return SearchOrderResp(new SFSearchOrderRespRequest { orderId = orderId });
        }

        #endregion

        #region 订单确认/取消 EXP_RECE_UPDATE_ORDER

        /// <summary>
        /// 取消订单（dealType=2）
        /// </summary>
        public SFResult<SFUpdateOrderResponse> CancelOrder(string orderId)
        {
            return UpdateOrder(new SFUpdateOrderRequest
            {
                dealType = 2,
                orderId = orderId
            });
        }

        /// <summary>
        /// 确认订单（dealType=1）
        /// </summary>
        public SFResult<SFUpdateOrderResponse> ConfirmOrder(string orderId)
        {
            return UpdateOrder(new SFUpdateOrderRequest
            {
                dealType = 1,
                orderId = orderId
            });
        }

        public SFResult<SFUpdateOrderResponse> UpdateOrder(SFUpdateOrderRequest request)
        {
            return CallService<SFUpdateOrderResponse>("EXP_RECE_UPDATE_ORDER", request);
        }

        #endregion

        #region 路由查询 EXP_RECE_SEARCH_ROUTES

        /// <summary>
        /// 按运单号查询路由
        /// </summary>
        public SFResult<SFRouteResponse> SearchRoutesByWaybill(string waybillNo)
        {
            return SearchRoutes(new SFRouteRequest
            {
                trackingType = 2,
                trackingNumber = new List<string> { waybillNo }
            });
        }

        /// <summary>
        /// 按订单号查询路由
        /// </summary>
        public SFResult<SFRouteResponse> SearchRoutesByOrder(string orderId)
        {
            return SearchRoutes(new SFRouteRequest
            {
                trackingType = 1,
                trackingNumber = new List<string> { orderId }
            });
        }

        public SFResult<SFRouteResponse> SearchRoutes(SFRouteRequest request)
        {
            return CallService<SFRouteResponse>("EXP_RECE_SEARCH_ROUTES", request);
        }

        #endregion

        #region 通用调用

        private SFResult<T> CallService<T>(string serviceCode, object msgDataObj) where T : class
        {
            var requestID = Guid.NewGuid().ToString("N");
            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            var msgData = JsonConvert.SerializeObject(msgDataObj, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            var msgDigest = GenerateSignature(msgData, timestamp);

            _log.WriteLog($"========== API调用 ==========");
            _log.WriteLog($"serviceCode: {serviceCode}");
            _log.WriteLog($"requestID: {requestID}");
            _log.WriteLog($"msgData: {msgData}");

            try
            {
                var postData = new Dictionary<string, string>
                {
                    { "partnerID", _partnerID },
                    { "requestID", requestID },
                    { "serviceCode", serviceCode },
                    { "timestamp", timestamp },
                    { "msgDigest", msgDigest },
                    { "msgData", msgData }
                };

                string responseText;
                using (var client = new WebClientEx(_timeoutMs))
                {
                    client.Headers[HttpRequestHeader.ContentType] =
                        "application/x-www-form-urlencoded;charset=UTF-8";

                    var sb = new StringBuilder();
                    foreach (var kv in postData)
                    {
                        if (sb.Length > 0) sb.Append("&");
                        sb.Append($"{kv.Key}={Uri.EscapeDataString(kv.Value)}");
                    }
                    var postBytes = Encoding.UTF8.GetBytes(sb.ToString());
                    var responseBytes = client.UploadData(_apiUrl, "POST", postBytes);
                    responseText = Encoding.UTF8.GetString(responseBytes);
                }

                _log.WriteLog($"响应: {responseText}");

                var result = ParseResponse<T>(responseText);
                result.RequestID = requestID;
                result.RawResponse = responseText;
                return result;
            }
            catch (WebException wex) when (wex.Status == WebExceptionStatus.Timeout)
            {
                _log.Error($"请求超时, serviceCode: {serviceCode}, requestID: {requestID}");
                return new SFResult<T>
                {
                    Success = false,
                    ErrorCode = "TIMEOUT",
                    ErrorMsg = $"顺丰API请求超时（{_timeoutMs / 1000}秒）",
                    RequestID = requestID,
                    IsTimeout = true
                };
            }
            catch (WebException wex)
            {
                _log.Error($"网络异常: {wex.Message}");
                return new SFResult<T>
                {
                    Success = false,
                    ErrorCode = "NETWORK_ERROR",
                    ErrorMsg = $"网络异常: {wex.Message}",
                    RequestID = requestID
                };
            }
            catch (Exception ex)
            {
                _log.Error($"调用异常: {ex.Message}");
                return new SFResult<T>
                {
                    Success = false,
                    ErrorCode = "EXCEPTION",
                    ErrorMsg = $"调用异常: {ex.Message}",
                    RequestID = requestID
                };
            }
        }

        private SFResult<T> ParseResponse<T>(string responseText) where T : class
        {
            try
            {
                var jObj = JObject.Parse(responseText);
                var apiResultCode = jObj["apiResultCode"]?.Value<string>();
                var apiErrorMsg = jObj["apiErrorMsg"]?.Value<string>();
                var apiResponse = jObj["apiResultData"]?.Value<string>();

                if (apiResultCode != "A1000")
                {
                    return new SFResult<T>
                    {
                        Success = false,
                        ErrorCode = apiResultCode,
                        ErrorMsg = apiErrorMsg ?? "未知错误"
                    };
                }

                T data = null;
                if (!string.IsNullOrEmpty(apiResponse))
                {
                    data = JsonConvert.DeserializeObject<T>(apiResponse);
                }

                return new SFResult<T>
                {
                    Success = true,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                return new SFResult<T>
                {
                    Success = false,
                    ErrorCode = "PARSE_ERROR",
                    ErrorMsg = $"解析响应失败: {ex.Message}"
                };
            }
        }

        #endregion

        #region 签名

        /// <summary>
        /// 数字签名: Base64(MD5(msgData + timestamp + checkWord))
        /// </summary>
        public static string GenerateSignature(string msgData, string timestamp, string checkWord)
        {
            var raw = msgData + timestamp + checkWord;
            using (var md5 = MD5.Create())
            {
                var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
                return Convert.ToBase64String(hashBytes);
            }
        }

        private string GenerateSignature(string msgData, string timestamp)
        {
            return GenerateSignature(msgData, timestamp, _checkWord);
        }

        #endregion

        #region 支持超时的 WebClient

        private class WebClientEx : WebClient
        {
            private readonly int _timeoutMs;
            public WebClientEx(int timeoutMs) { _timeoutMs = timeoutMs; }
            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = base.GetWebRequest(address);
                request.Timeout = _timeoutMs;
                return request;
            }
        }

        #endregion
    }

    #region 通用结果

    public class SFResult<T> where T : class
    {
        public bool Success { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMsg { get; set; }
        public T Data { get; set; }
        public string RequestID { get; set; }
        public string RawResponse { get; set; }
        public bool IsTimeout { get; set; }
    }

    #endregion

    #region 请求/响应模型 — 下订单

    public class SFOrderRequest
    {
        public string language { get; set; } = "zh-CN";
        public string orderId { get; set; }
        public string monthlyCard { get; set; }
        public int payMethod { get; set; } = 1;
        public int expressTypeId { get; set; } = 1;
        public int isDoCall { get; set; } = 1;
        public int isReturnRoutelabel { get; set; } = 0;
        public List<SFContactInfo> contactInfoList { get; set; } = new List<SFContactInfo>();
        public List<SFCargoDetail> cargoDetails { get; set; } = new List<SFCargoDetail>();
        public string remark { get; set; }
    }

    public class SFContactInfo
    {
        public int contactType { get; set; }
        public string contact { get; set; }
        public string tel { get; set; }
        public string mobile { get; set; }
        public string country { get; set; } = "CN";
        public string province { get; set; }
        public string city { get; set; }
        public string county { get; set; }
        public string address { get; set; }
    }

    public class SFCargoDetail
    {
        public string name { get; set; }
        public int count { get; set; }
        public string unit { get; set; } = "件";
        public double weight { get; set; }
    }

    public class SFOrderResponse
    {
        public string orderId { get; set; }
        public List<SFWaybillNoInfo> waybillNoInfoList { get; set; }
        public string pickUpTime { get; set; }
    }

    public class SFWaybillNoInfo
    {
        public string waybillNo { get; set; }
        public string waybillType { get; set; }
    }

    #endregion

    #region 请求/响应模型 — 订单结果查询

    public class SFSearchOrderRespRequest
    {
        public string orderId { get; set; }
    }

    public class SFSearchOrderRespResponse
    {
        public string orderId { get; set; }
        public string orderState { get; set; }
        public string orderStateName { get; set; }
        public List<SFWaybillNoInfo> waybillNoInfoList { get; set; }
    }

    #endregion

    #region 请求/响应模型 — 订单取消

    public class SFUpdateOrderRequest
    {
        public int dealType { get; set; } = 1;
        public string orderId { get; set; }
    }

    public class SFUpdateOrderResponse
    {
        public string orderId { get; set; }
        public string result { get; set; }
    }

    #endregion

    #region 请求/响应模型 — 路由查询

    public class SFRouteRequest
    {
        public int trackingType { get; set; } = 2;
        public List<string> trackingNumber { get; set; }
    }

    public class SFRouteResponse
    {
        public List<SFRouteInfo> routeResps { get; set; }
    }

    public class SFRouteInfo
    {
        public string mailNo { get; set; }
        public List<SFRouteNode> routes { get; set; }
    }

    public class SFRouteNode
    {
        public string acceptAddress { get; set; }
        public string acceptTime { get; set; }
        public string remark { get; set; }
        public string opCode { get; set; }
        public string opName { get; set; }
    }

    #endregion
}

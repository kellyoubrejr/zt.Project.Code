using Kingdee.BOS.Orm.DataEntity;
using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kingdee.Zitn.Project.Code.plugin.SFForm
{
    /// <summary>
    /// 顺丰路由查询服务
    /// 可从金蝶定时任务 / 自定义按钮中调用
    /// </summary>
    [Description("顺丰路由查询服务")]
    public static class SFRouteService
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("顺丰路由查询");

        /// <summary>
        /// 按运单号查询路由，返回 JSON 字符串（可直接存库）
        /// </summary>
        public static string QueryRoutesByWaybill(string waybillNo)
        {
            return QueryRoutes(waybillNo, 2);
        }

        /// <summary>
        /// 按订单号查询路由
        /// </summary>
        public static string QueryRoutesByOrder(string orderId)
        {
            return QueryRoutes(orderId, 1);
        }

        /// <summary>
        /// 查询路由并返回格式化文本（适用于日志/消息展示）
        /// </summary>
        public static string QueryRoutesAsText(string trackingNo, int trackingType = 2)
        {
            var json = QueryRoutes(trackingNo, trackingType);
            if (string.IsNullOrEmpty(json)) return "查询失败或无轨迹数据";

            try
            {
                var response = JsonConvert.DeserializeObject<SFRouteResponse>(json);
                if (response?.routeResps == null || response.routeResps.Count == 0)
                    return "暂无路由信息";

                var sb = new StringBuilder();
                foreach (var info in response.routeResps)
                {
                    sb.AppendLine($"运单号: {info.mailNo}");
                    if (info.routes != null)
                    {
                        foreach (var node in info.routes)
                        {
                            sb.AppendLine(
                                $"  [{node.acceptTime}] {node.remark} ({node.opName})");
                        }
                    }
                    sb.AppendLine();
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _log.Error($"解析路由数据异常: {ex.Message}");
                return $"解析异常: {ex.Message}\r\n原始数据: {json}";
            }
        }

        /// <summary>
        /// 查询路由并写入顺丰面单的物流轨迹子表
        /// 在金蝶定时任务中调用此方法，遍历所有"已下单"状态的面单
        /// </summary>
        public static int SyncRoutesForWaybills(DynamicObjectCollection waybillEntities, Func<DynamicObject, string> getWaybillNo, Action<DynamicObject, List<SFRouteNode>> writeRoutes)
        {
            var client = CreateClient();
            if (client == null)
            {
                _log.Error("顺丰配置不完整，无法同步路由");
                return -1;
            }

            int successCount = 0;

            foreach (DynamicObject entity in waybillEntities)
            {
                try
                {
                    var waybillNo = getWaybillNo(entity);
                    if (string.IsNullOrEmpty(waybillNo))
                        continue;

                    var result = client.SearchRoutesByWaybill(waybillNo);
                    if (!result.Success || result.Data?.routeResps == null)
                        continue;

                    var routes = result.Data.routeResps
                        .SelectMany(r => r.routes ?? new List<SFRouteNode>())
                        .OrderBy(r => r.acceptTime)
                        .ToList();

                    if (routes.Count > 0)
                    {
                        writeRoutes(entity, routes);
                        successCount++;
                        _log.WriteLog($"路由同步成功, 运单号: {waybillNo}, 轨迹数: {routes.Count}");
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }
            }

            _log.WriteLog($"路由同步完成, 成功: {successCount}/{waybillEntities.Count}");
            return successCount;
        }

        private static string QueryRoutes(string trackingNo, int trackingType)
        {
            if (string.IsNullOrEmpty(trackingNo))
            {
                _log.Error("运单号/订单号为空");
                return null;
            }

            _log.WriteLog($"查询路由, trackingNo: {trackingNo}, type: {trackingType}");

            var client = CreateClient();
            if (client == null) return null;

            var result = client.SearchRoutes(new SFRouteRequest
            {
                trackingType = trackingType,
                trackingNumber = new List<string> { trackingNo }
            });

            if (result.Success && result.Data != null)
            {
                var json = JsonConvert.SerializeObject(result.Data);
                _log.WriteLog($"路由查询成功: {json}");
                return json;
            }

            _log.Error($"路由查询失败: [{result.ErrorCode}] {result.ErrorMsg}");
            return null;
        }

        private static SFExpressClient CreateClient()
        {
            var partnerID = SFConfig.PartnerID;
            var checkWord = SFConfig.CheckWord;
            var apiUrl = SFConfig.ApiUrl;

            if (string.IsNullOrEmpty(partnerID) || string.IsNullOrEmpty(checkWord))
                return null;

            return new SFExpressClient(partnerID, checkWord, apiUrl, timeoutMs: 15000, logger: _log);
        }
    }
}

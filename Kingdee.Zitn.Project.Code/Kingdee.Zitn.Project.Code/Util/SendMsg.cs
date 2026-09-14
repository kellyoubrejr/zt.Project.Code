using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Text;

namespace Kingdee.Zitn.Project.Code.Util
{
    /// <summary>
    /// 企业微信消息发送工具类（通用）。
    /// 用法：SendMsg.Send("要发送的内容");
    /// 配置在 K3 Cloud AppSettings（web.config）：WeCom_CorpId / WeCom_Secret / WeCom_AgentId / WeCom_ToUser
    /// 发送失败仅记录日志，不抛异常，不影响调用方主流程。
    /// </summary>
    public static class SendMsg
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("企业微信消息");

        // access_token 缓存（有效期 7200 秒，提前 200 秒过期）
        private static readonly object _lock = new object();
        private static string _token;
        private static DateTime _tokenExpire = DateTime.MinValue;

        /// <summary>发送文本消息给默认收件人</summary>
        public static void Send(string content)
        {
            Send(content, WeComConfig.ToUser);
        }

        /// <summary>发送文本消息给指定收件人（多个用 | 分隔）</summary>
        public static void Send(string content, string toUser)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                    return;

                string token = GetAccessToken();
                if (string.IsNullOrEmpty(token))
                {
                    _log.Error("获取 access_token 失败，消息未发送");
                    return;
                }

                string url = $"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={token}";

                var postData = new
                {
                    touser = toUser,
                    msgtype = "text",
                    agentid = WeComConfig.AgentId,
                    text = new { content = content },
                    safe = 0
                };
                string jsonContent = JsonConvert.SerializeObject(postData);

                using (var client = new HttpClient())
                using (var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                {
                    var response = client.PostAsync(url, httpContent).Result;
                    string result = response.Content.ReadAsStringAsync().Result;

                    var obj = JObject.Parse(result);
                    int errcode = obj["errcode"]?.Value<int>() ?? -1;
                    if (errcode != 0)
                    {
                        _log.Error($"发送失败，errcode={errcode}，返回：{result}");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("发送企业微信消息异常");
                _log.Error(ex);
            }
        }

        /// <summary>发送图片消息给默认收件人</summary>
        public static void SendImage(string mediaId)
        {
            SendImage(mediaId, WeComConfig.ToUser);
        }

        /// <summary>发送图片消息给指定收件人（多个用 | 分隔）</summary>
        public static void SendImage(string mediaId, string toUser)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mediaId))
                    return;

                string token = GetAccessToken();
                if (string.IsNullOrEmpty(token))
                {
                    _log.Error("获取 access_token 失败，图片消息未发送");
                    return;
                }

                string url = $"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={token}";

                var postData = new
                {
                    touser = toUser,
                    msgtype = "image",
                    agentid = WeComConfig.AgentId,
                    image = new { media_id = mediaId },
                    safe = 0
                };
                string jsonContent = JsonConvert.SerializeObject(postData);

                using (var client = new HttpClient())
                using (var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                {
                    var response = client.PostAsync(url, httpContent).Result;
                    string result = response.Content.ReadAsStringAsync().Result;

                    var obj = JObject.Parse(result);
                    int errcode = obj["errcode"]?.Value<int>() ?? -1;
                    if (errcode != 0)
                    {
                        _log.Error($"图片消息发送失败，errcode={errcode}，返回：{result}");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("发送企业微信图片消息异常");
                _log.Error(ex);
            }
        }

        /// <summary>发送文件消息给默认收件人</summary>
        public static void SendFile(string mediaId)
        {
            SendFile(mediaId, WeComConfig.ToUser);
        }

        /// <summary>发送文件消息给指定收件人（多个用 | 分隔）</summary>
        public static void SendFile(string mediaId, string toUser)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mediaId))
                    return;

                string token = GetAccessToken();
                if (string.IsNullOrEmpty(token))
                {
                    _log.Error("获取 access_token 失败，文件消息未发送");
                    return;
                }

                string url = $"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={token}";

                var postData = new
                {
                    touser = toUser,
                    msgtype = "file",
                    agentid = WeComConfig.AgentId,
                    file = new { media_id = mediaId },
                    safe = 0
                };
                string jsonContent = JsonConvert.SerializeObject(postData);

                using (var client = new HttpClient())
                using (var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                {
                    var response = client.PostAsync(url, httpContent).Result;
                    string result = response.Content.ReadAsStringAsync().Result;

                    var obj = JObject.Parse(result);
                    int errcode = obj["errcode"]?.Value<int>() ?? -1;
                    if (errcode != 0)
                    {
                        _log.Error($"文件消息发送失败，errcode={errcode}，返回：{result}");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("发送企业微信文件消息异常");
                _log.Error(ex);
            }
        }

        /// <summary>上传临时素材，返回media_id。fileName可选，不传则自动生成。</summary>
        public static string UploadMedia(string base64Data, string type, out string errorMsg, string fileName = null)
        {
            errorMsg = null;
            try
            {
                if (string.IsNullOrWhiteSpace(base64Data))
                {
                    errorMsg = "base64数据为空";
                    return null;
                }

                // 去掉 Data URI 前缀，如 "data:image/jpeg;base64,/9j/4AAQ..."
                string pureBase64 = base64Data;
                string detectedMimeType = null;
                if (base64Data.Contains(","))
                {
                    string dataUriPrefix = base64Data.Substring(0, base64Data.IndexOf(',') + 1);
                    pureBase64 = base64Data.Substring(base64Data.IndexOf(',') + 1);
                    // 从 Data URI 中提取 MIME 类型，如 "data:image/jpeg;base64," → "image/jpeg"
                    if (dataUriPrefix.StartsWith("data:") && dataUriPrefix.Contains("/"))
                    {
                        detectedMimeType = dataUriPrefix.Substring(5, dataUriPrefix.IndexOf(';') - 5);
                    }
                }

                string token = GetAccessToken();
                if (string.IsNullOrEmpty(token))
                {
                    errorMsg = "获取 access_token 失败";
                    _log.Error("获取 access_token 失败，素材上传失败");
                    return null;
                }

                string url = $"https://qyapi.weixin.qq.com/cgi-bin/media/upload?access_token={token}&type={type}";

                // 清除base64中的换行、空格等干扰字符
                pureBase64 = pureBase64.Replace("\r", "").Replace("\n", "").Replace(" ", "").Replace("\t", "");

                // 解码base64
                byte[] fileBytes = Convert.FromBase64String(pureBase64);
                _log.WriteLog($"开始上传临时素材，类型={type}，文件大小={fileBytes.Length}字节，base64纯数据长度={pureBase64.Length}");

                if (fileBytes.Length == 0)
                {
                    errorMsg = "base64解码后数据为空";
                    _log.Error("base64解码后文件字节数为0");
                    return null;
                }

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(5);

                    // 手动构造 multipart/form-data body，避免 boundary 兼容性问题
                    string boundary = $"----WebKitFormBoundary{DateTime.Now.Ticks.ToString("x")}";
                    string httpContentType = "multipart/form-data; boundary=" + boundary;

                    // 根据类型设置文件Content-Type和扩展名
                    string fileContentType;
                    string fileExt;
                    switch (type)
                    {
                        case "image":
                            fileContentType = detectedMimeType ?? "image/jpeg";
                            fileExt = "jpg";
                            break;
                        case "voice":
                            fileContentType = "audio/amr";
                            fileExt = "amr";
                            break;
                        case "video":
                            fileContentType = "video/mp4";
                            fileExt = "mp4";
                            break;
                        default:
                            fileContentType = "application/octet-stream";
                            fileExt = "bin";
                            break;
                    }

                    // 确定最终文件名：优先使用调用方传入的fileName
                    string finalFileName;
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        finalFileName = fileName;
                        // 从传入文件名中提取扩展名来覆盖默认值
                        string extFromName = Path.GetExtension(fileName);
                        if (!string.IsNullOrWhiteSpace(extFromName))
                        {
                            fileExt = extFromName.TrimStart('.');
                            // 根据扩展名推断 Content-Type
                            fileContentType = GetContentTypeByExt(fileExt, detectedMimeType);
                        }
                    }
                    else
                    {
                        finalFileName = $"{type}_{DateTime.Now.Ticks}.{fileExt}";
                    }

                    _log.WriteLog($"上传文件名={finalFileName}，Content-Type={fileContentType}");

                    using (var ms = new MemoryStream())
                    {
                        byte[] boundaryBytes = Encoding.UTF8.GetBytes($"--{boundary}\r\n");
                        byte[] headerBytes = Encoding.UTF8.GetBytes(
                            $"Content-Disposition: form-data; name=\"media\"; filename=\"{finalFileName}\"\r\n" +
                            $"Content-Type: {fileContentType}\r\n\r\n");
                        byte[] endBoundaryBytes = Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n");

                        ms.Write(boundaryBytes, 0, boundaryBytes.Length);
                        ms.Write(headerBytes, 0, headerBytes.Length);
                        ms.Write(fileBytes, 0, fileBytes.Length);
                        ms.Write(endBoundaryBytes, 0, endBoundaryBytes.Length);

                        byte[] bodyBytes = ms.ToArray();
                        _log.WriteLog($"multipart body总大小={bodyBytes.Length}字节，准备上传");

                        using (var content = new ByteArrayContent(bodyBytes))
                        {
                            // 直接设置完整Content-Type字符串，包含boundary参数
                            content.Headers.TryAddWithoutValidation("Content-Type", httpContentType);

                            var response = client.PostAsync(url, content).Result;
                            string result = response.Content.ReadAsStringAsync().Result;
                            _log.WriteLog($"临时素材上传返回: {result}");

                            var obj = JObject.Parse(result);
                            int errcode = obj["errcode"]?.Value<int>() ?? -1;
                            if (errcode != 0)
                            {
                                errorMsg = $"微信API返回errcode={errcode}，{obj["errmsg"]?.ToString()}";
                                _log.Error($"素材上传失败，errcode={errcode}，返回：{result}");
                                return null;
                            }

                            return obj["media_id"]?.ToString();
                        }
                    }
                }
            }
            catch (FormatException fex)
            {
                errorMsg = "数据格式错误: " + fex.Message;
                _log.Error("格式错误: " + fex.Message);
                return null;
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                _log.Error("上传企业微信临时素材异常: " + ex.Message);
                return null;
            }
        }

        /// <summary>发送“内容 + 异常信息”给默认收件人，用于失败/异常场景</summary>
        public static void Send(string content, Exception ex)
        {
            string msg = content;
            if (ex != null)
            {
                msg += "\r\n异常：" + ex.Message;
            }
            Send(msg);
        }

        private static string GetAccessToken()
        {
            lock (_lock)
            {
                if (!string.IsNullOrEmpty(_token) && DateTime.Now < _tokenExpire)
                    return _token;

                string url = $"https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={WeComConfig.CorpId}&corpsecret={WeComConfig.Secret}";

                using (var client = new HttpClient())
                {
                    string result = client.GetStringAsync(url).Result;
                    var obj = JObject.Parse(result);

                    string token = obj["access_token"]?.ToString();
                    if (string.IsNullOrEmpty(token))
                    {
                        _log.Error($"获取 access_token 失败，返回：{result}");
                        return string.Empty;
                    }

                    _token = token;
                    _tokenExpire = DateTime.Now.AddSeconds(7000);
                    return token;
                }
            }
        }

        /// <summary>根据文件扩展名返回对应的Content-Type</summary>
        private static string GetContentTypeByExt(string ext, string fallback = null)
        {
            if (string.IsNullOrWhiteSpace(ext)) return fallback ?? "application/octet-stream";
            ext = ext.ToLower().TrimStart('.');
            switch (ext)
            {
                // 图片
                case "jpg": case "jpeg": return "image/jpeg";
                case "png": return "image/png";
                case "gif": return "image/gif";
                case "bmp": return "image/bmp";
                case "webp": return "image/webp";
                // 文档
                case "pdf": return "application/pdf";
                case "doc": return "application/msword";
                case "docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case "xls": return "application/vnd.ms-excel";
                case "xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case "ppt": return "application/vnd.ms-powerpoint";
                case "pptx": return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                case "txt": return "text/plain";
                // 压缩
                case "zip": return "application/zip";
                case "rar": return "application/x-rar-compressed";
                case "7z": return "application/x-7z-compressed";
                // 音视频
                case "mp3": return "audio/mpeg";
                case "amr": return "audio/amr";
                case "mp4": return "video/mp4";
                case "avi": return "video/x-msvideo";
                default: return fallback ?? "application/octet-stream";
            }
        }
    }
}

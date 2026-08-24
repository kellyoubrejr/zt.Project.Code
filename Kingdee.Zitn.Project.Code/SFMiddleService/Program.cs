using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace SFMiddleService
{
    /// <summary>
    /// 顺丰图片中转服务（部署在公网可达的服务器上，与 K3 分离）
    /// - POST /sf/callback : 顺丰推送图片（密文），原样落盘，回 ack
    /// - GET  /sf/pull     : K3 拉取未处理列表（需 token）
    /// - POST /sf/ack      : K3 确认处理完成，归档（需 token）
    /// 服务全程不持有解密密钥、不解密，只转发密文。
    /// </summary>
    internal class Program
    {
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        private static string _inbox;
        private static string _done;
        private static string _token;

        private static void Main()
        {
            int port = GetInt("port", 8080);
            _token = ConfigurationManager.AppSettings["token"] ?? "";
            _inbox = Path.GetFullPath(ConfigurationManager.AppSettings["inbox"] ?? "inbox");
            _done = Path.GetFullPath(ConfigurationManager.AppSettings["done"] ?? "done");

            Directory.CreateDirectory(_inbox);
            Directory.CreateDirectory(_done);

            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add($"http://+:{port}/");
                listener.Start();
                Console.WriteLine($"顺丰图片中转服务已启动，监听端口 {port}");
                Console.WriteLine($"inbox={_inbox}");
                Console.WriteLine($"done ={_done}");

                while (true)
                {
                    var ctx = listener.GetContext();
                    try
                    {
                        Route(ctx);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("处理异常：" + ex.Message);
                    }
                }
            }
        }

        private static void Route(HttpListenerContext ctx)
        {
            string path = ctx.Request.Url.AbsolutePath.ToLowerInvariant();

            if (path == "/sf/callback" && ctx.Request.HttpMethod == "POST") Callback(ctx);
            else if (path == "/sf/pull" && ctx.Request.HttpMethod == "GET") Pull(ctx);
            else if (path == "/sf/ack" && ctx.Request.HttpMethod == "POST") Ack(ctx);
            else WriteJson(ctx, 404, new { success = false, msg = "not found" });
        }

        /// <summary>顺丰推送：原样存密文，回 ack</summary>
        private static void Callback(HttpListenerContext ctx)
        {
            string body = ReadBody(ctx);
            if (string.IsNullOrWhiteSpace(body))
            {
                WriteJson(ctx, 200, new { return_code = "1000", return_msg = "空报文" });
                return;
            }

            string id = Guid.NewGuid().ToString("N") + ".json";
            File.WriteAllText(Path.Combine(_inbox, id), body, Encoding.UTF8);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 收到顺丰推送 -> {id}");

            WriteJson(ctx, 200, new { return_code = "0000", return_msg = "成功" });
        }

        /// <summary>K3 拉取未处理列表</summary>
        private static void Pull(HttpListenerContext ctx)
        {
            if (!CheckToken(ctx)) return;

            var items = new List<object>();
            foreach (var file in Directory.GetFiles(_inbox, "*.json"))
            {
                try
                {
                    string raw = File.ReadAllText(file, Encoding.UTF8);
                    var o = Json.Deserialize<Dictionary<string, object>>(raw);
                    items.Add(new
                    {
                        id = Path.GetFileName(file),
                        waybillNo = GetStr(o, "waybillNo"),
                        content = GetStr(o, "content"),
                        receivedTime = File.GetLastWriteTime(file).ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
                catch { }
            }

            WriteJson(ctx, 200, new { success = true, items = items });
        }

        /// <summary>K3 确认处理完成，归档到 done</summary>
        private static void Ack(HttpListenerContext ctx)
        {
            if (!CheckToken(ctx)) return;

            string body = ReadBody(ctx);
            var req = Json.Deserialize<Dictionary<string, object>>(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            int moved = 0;

            if (req != null && req.ContainsKey("ids") && req["ids"] is object[] ids)
            {
                foreach (var id in ids)
                {
                    string name = id == null ? "" : id.ToString();
                    if (string.IsNullOrEmpty(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                        continue;

                    string src = Path.Combine(_inbox, name);
                    string dst = Path.Combine(_done, name);
                    if (File.Exists(src))
                    {
                        File.Move(src, dst);
                        moved++;
                    }
                }
            }

            WriteJson(ctx, 200, new { success = true, moved = moved });
        }

        private static bool CheckToken(HttpListenerContext ctx)
        {
            string t = ctx.Request.QueryString["token"] ?? "";
            if (t != _token)
            {
                WriteJson(ctx, 401, new { success = false, msg = "token 无效" });
                return false;
            }
            return true;
        }

        private static string ReadBody(HttpListenerContext ctx)
        {
            using (var r = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
                return r.ReadToEnd();
        }

        private static string GetStr(Dictionary<string, object> o, string key)
        {
            return o != null && o.ContainsKey(key) && o[key] != null ? o[key].ToString() : "";
        }

        private static void WriteJson(HttpListenerContext ctx, int status, object data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(Json.Serialize(data));
            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = "application/json;charset=utf-8";
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.Close();
        }

        private static int GetInt(string key, int def)
        {
            int v;
            return int.TryParse(ConfigurationManager.AppSettings[key], out v) ? v : def;
        }
    }
}

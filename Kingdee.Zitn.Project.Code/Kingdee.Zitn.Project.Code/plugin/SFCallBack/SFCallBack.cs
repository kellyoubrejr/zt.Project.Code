using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Kingdee.Zitn.Project.Code.SFCallBack
{
    /// <summary>
    /// 顺丰图片推送接收端（raw HTTP handler，非 kdsvc）
    /// 接收顺丰推送的 { content, waybillNo, companyLogo }，解密后落盘，回 ack 让顺丰继续增量推送。
    /// content = Base64( AES/CBC/PKCS5 加密( Base64(图片字节) ) )，两次 Base64 解包。
    /// </summary>
    public class SFCallBack : IHttpHandler
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("顺丰图片推送");

        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json;charset=utf-8";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.StatusCode = 200;

            string returnCode = "0000";
            string returnMsg = "成功";
            string waybillNo = "";

            try
            {
                string body = ReadBody(context);
                if (string.IsNullOrWhiteSpace(body))
                {
                    returnCode = "1000";
                    returnMsg = "请求报文为空";
                }
                else
                {
                    var root = JObject.Parse(body);
                    waybillNo = (string)root["waybillNo"] ?? "";
                    string content = (string)root["content"] ?? "";

                    if (string.IsNullOrWhiteSpace(waybillNo) || string.IsNullOrWhiteSpace(content))
                    {
                        returnCode = "1000";
                        returnMsg = "缺少 waybillNo 或 content";
                    }
                    else
                    {
                        byte[] image = DecryptImage(content);
                        string path = SaveImage(waybillNo, image);
                        _log.WriteLog($"图片接收成功：运单={waybillNo}，大小={image.Length} 字节，路径={path}");
                    }
                }
            }
            catch (Exception ex)
            {
                returnCode = "1000";
                returnMsg = "处理异常：" + ex.Message;
                _log.Error($"图片处理失败，运单={waybillNo}");
                _log.Error(ex);
            }

            context.Response.Write("{\"return_code\":\"" + returnCode + "\",\"return_msg\":\"" + returnMsg + "\"}");
            context.Response.End();
        }

        private static string ReadBody(HttpContext context)
        {
            context.Request.InputStream.Position = 0;
            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                return reader.ReadToEnd();
        }

        /// <summary>解密：外层 Base64 → AES/CBC 解密 → 内层 Base64 → 图片字节</summary>
        private static byte[] DecryptImage(string content)
        {
            string secret = SFConfig.PictureSecret;
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("未配置 SF_PictureSecret（图片解密密钥）");

            byte[] key = Encoding.UTF8.GetBytes(secret);
            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
                throw new InvalidOperationException($"解密密钥长度非法({key.Length}字节)，应为16/24/32字节");

            string b64 = content.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");
            byte[] cipher = Convert.FromBase64String(b64);
            byte[] inner = AesDecrypt(cipher, key);
            return Convert.FromBase64String(Encoding.UTF8.GetString(inner).Trim());
        }

        private static byte[] AesDecrypt(byte[] cipher, byte[] key)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = new byte[16]; // 顺丰固定全零 IV
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7; // PKCS5Padding == PKCS7（AES 场景等价）
                using (var dec = aes.CreateDecryptor())
                    return dec.TransformFinalBlock(cipher, 0, cipher.Length);
            }
        }

        private static string SaveImage(string waybillNo, byte[] image)
        {
            string dir = SFConfig.PictureDir;
            if (string.IsNullOrWhiteSpace(dir))
                dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SFImage");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string file = $"{waybillNo}_{DateTime.Now:yyyyMMddHHmmssfff}.{DetectExt(image)}";
            string full = Path.Combine(dir, file);
            File.WriteAllBytes(full, image);
            return full;
        }

        private static string DetectExt(byte[] data)
        {
            if (data.Length >= 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                return "png";
            if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
                return "jpg";
            if (data.Length >= 4 && data[0] == 0x25 && data[1] == 0x50 && data[2] == 0x44 && data[3] == 0x46)
                return "pdf";
            return "bin";
        }
    }
}

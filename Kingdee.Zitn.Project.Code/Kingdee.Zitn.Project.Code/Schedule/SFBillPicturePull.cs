using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.Zitn.Project.Code.conf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Kingdee.Zitn.Project.Code.Schedule
{
    [Description("【跑批】：从顺丰图片中转服务拉取图片并回写面单")]
    [Kingdee.BOS.Util.HotUpdate]
    public class SFBillPicturePull : IScheduleService
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("顺丰图片拉取");

        private const string HEAD_TABLE = "ZMER_t_Cust100030";      // 单据头
        private const string BW_TABLE = "ZMER_t_Cust_Entry100085";  // 报文单据体

        public void Run(Context ctx, Kingdee.BOS.Core.Schedule schedule)
        {
            _log.Section("开始拉取顺丰图片");
            try
            {
                string url = SFConfig.MiddleServiceUrl;
                if (string.IsNullOrWhiteSpace(url))
                {
                    _log.WriteLog("未配置 SF_MiddleServiceUrl，跳过");
                    return;
                }

                var items = Pull(url, SFConfig.MiddleServiceToken);
                if (items == null || items.Count == 0)
                {
                    _log.WriteLog("没有待处理的图片");
                    return;
                }

                _log.WriteLog($"共拉取 {items.Count} 条图片");

                var doneIds = new List<string>();
                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    try
                    {
                        HandleOne(ctx, it);
                        doneIds.Add(it.Id);
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"处理失败：运单={it.WaybillNo}，id={it.Id}");
                        _log.Error(ex);
                        // 失败不 ack，下轮重试
                    }
                }

                if (doneIds.Count > 0)
                    Ack(url, SFConfig.MiddleServiceToken, doneIds);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            _log.Section("顺丰图片拉取结束");
        }

        /// <summary>单条处理：解密 → 落盘 → 写报文单据体</summary>
        private static void HandleOne(Context ctx, PullItem it)
        {
            byte[] image = DecryptImage(it.Content);
            string path = SaveImage(it.WaybillNo, image);
            RecordToBill(ctx, it.WaybillNo, path, image.Length);
            _log.WriteLog($"图片已保存：运单={it.WaybillNo}，大小={image.Length} 字节，路径={path}");
        }

        /// <summary>按运单号找到面单，把图片路径记入报文单据体</summary>
        private static void RecordToBill(Context ctx, string waybillNo, string path, int size)
        {
            var r = DBUtils.ExecuteDynamicObject(ctx,
                $"/*dialect*/SELECT FID FROM {HEAD_TABLE} WHERE FSFYDH = '{Sql(waybillNo)}'");
            if (r == null || r.Count == 0)
            {
                _log.WriteLog($"未找到对应面单：运单={waybillNo}");
                return;
            }

            long fid = Convert.ToInt64(r[0]["FID"]);
            long entryId = NextEntryId(ctx, BW_TABLE);
            DBUtils.Execute(ctx,
                $@"/*dialect*/INSERT INTO {BW_TABLE}
                        (FENTRYID, FID, FServiceCode, FCallTime, FRequestMsg, FResponseMsg, FSuccess, FErrorMsg)
                    VALUES
                        ({entryId}, {fid}, 'EXP_RECE_PICTURE_PUSH', GETDATE(), '{Sql(waybillNo)}', '{Sql(path + $" ({size}字节)")}', '1', '')");
        }

        private static long NextEntryId(Context ctx, string table)
        {
            var r = DBUtils.ExecuteDynamicObject(ctx,
                $"/*dialect*/SELECT ISNULL(MAX(FENTRYID), 0) AS MX FROM {table}");
            return (r != null && r.Count > 0) ? Convert.ToInt64(r[0]["MX"]) + 1 : 1;
        }

        /* ---------- 中转服务 HTTP 交互 ---------- */

        private static List<PullItem> Pull(string url, string token)
        {
            using (var wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                string json = wc.DownloadString(url.TrimEnd('/') + "/sf/pull?token=" + Uri.EscapeDataString(token ?? ""));
                var root = JObject.Parse(json);
                if (!root["success"].Value<bool>())
                    return new List<PullItem>();

                var arr = root["items"] as JArray;
                var list = new List<PullItem>();
                if (arr == null)
                    return list;

                foreach (var t in arr)
                {
                    list.Add(new PullItem
                    {
                        Id = (string)t["id"],
                        WaybillNo = (string)t["waybillNo"],
                        Content = (string)t["content"]
                    });
                }
                return list;
            }
        }

        private static void Ack(string url, string token, List<string> ids)
        {
            var payload = new JObject { ["ids"] = JArray.FromObject(ids) };
            using (var wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                wc.Headers[HttpRequestHeader.ContentType] = "application/json";
                wc.UploadString(url.TrimEnd('/') + "/sf/ack?token=" + Uri.EscapeDataString(token ?? ""), "POST", payload.ToString(Formatting.None));
            }
        }

        /* ---------- 解密 / 落盘 ---------- */

        /// <summary>content = Base64( AES/CBC 加密( Base64(图片字节) ) )，两次 Base64 解包</summary>
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
                aes.Padding = PaddingMode.PKCS7; // PKCS5Padding == PKCS7
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
            if (data.Length >= 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47) return "png";
            if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) return "jpg";
            if (data.Length >= 4 && data[0] == 0x25 && data[1] == 0x50 && data[2] == 0x44 && data[3] == 0x46) return "pdf";
            return "bin";
        }

        private static string Sql(string s)
        {
            return string.IsNullOrEmpty(s) ? "" : s.Replace("'", "''");
        }

        private class PullItem
        {
            public string Id { get; set; }
            public string WaybillNo { get; set; }
            public string Content { get; set; }
        }
    }
}

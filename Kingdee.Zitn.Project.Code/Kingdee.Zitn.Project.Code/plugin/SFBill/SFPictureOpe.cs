using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using Kingdee.BOS.WebApi.FormService;
using Kingdee.Zitn.Project.Code.conf;
using Kingdee.Zitn.Project.Code.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Kingdee.Zitn.Project.Code.plugin.SFBill
{
    /// <summary>
    /// 顺丰图片下载与附件上传插件
    /// 点击 btn_page → 从中间服务获取加密content → AES解密 → 上传到金蝶附件 
    /// </summary>
    [Description("【物流面单】下载顺丰回单图片到附件"), HotUpdate]
    public class SFPictureOpe : AbstractBillPlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("顺丰回单下载");
                
        private const string SECRET_KEY = "axjGikUwgYVKiJ3A";
        
        private static readonly byte[] AesIv = new byte[16];

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            try
            {
                if (e.BarItemKey.Equals("btn_page", StringComparison.OrdinalIgnoreCase))
                {
                    DownloadAndUploadPicture();
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                this.View.ShowErrMessage("下载图片失败：" + ex.Message);
                SendMsg.Send("【物流面单】下载回单图片失败", ex);
            }
        }

        /// <summary>
        /// 下载顺丰回单图片并上传到金蝶附件
        /// 流程：获取运单号 → 调中间服务API取加密content → AES解密 → 上传到金蝶附件
        /// </summary>
        private void DownloadAndUploadPicture()
        {
            string waybillNo = GetWaybillNo();
            if (string.IsNullOrWhiteSpace(waybillNo))
            {
                this.View.ShowErrMessage("顺丰运单号(FSFYDH)为空，无法下载图片");
                return;
            }

            string serviceUrl = SFConfig.MiddleServiceUrl;
            string token = SFConfig.MiddleServiceToken;
            string secretKey = SFConfig.PictureSecret;

            if (string.IsNullOrWhiteSpace(serviceUrl))
            {
                this.View.ShowErrMessage("未配置顺丰图片中转服务地址(SF_MiddleServiceUrl)");
                return;
            }
            if (string.IsNullOrWhiteSpace(token))
            {
                this.View.ShowErrMessage("未配置顺丰图片中转服务令牌(SF_MiddleServiceToken)");
                return;
            }
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                this.View.ShowErrMessage("未配置图片解密密钥(SF_PictureSecret)，请从顺丰丰桥控制台获取");
                return;
            }

            _log.WriteLog($"开始获取图片，运单号={waybillNo}");

            string apiUrl = serviceUrl.TrimEnd('/') + "/api/internal/picture/" + waybillNo;
            string jsonData = HttpGet(apiUrl, token);

            _log.WriteLog($"API响应：{jsonData}");

            var root = JObject.Parse(jsonData);
            bool success = root["success"]?.Value<bool>() ?? false;
            if (!success)
            {
                string msg = root["message"]?.Value<string>() ?? "未知错误";
                this.View.ShowErrMessage($"获取图片失败：{msg}");
                SendMsg.Send($"【物流面单】获取回单图片失败：{waybillNo}，{msg}");
                return;
            }

            var dataArray = root["data"] as JArray;
            if (dataArray == null || dataArray.Count == 0)
            {
                this.View.ShowMessage($"运单号 {waybillNo} 暂无图片，请确认顺丰已推送");
                SendMsg.Send($"【物流面单】回单图片为空：{waybillNo}，请确认顺丰已推送");
                return;
            }

            string encryptedContent = dataArray[0]["EncryptedContent"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(encryptedContent))
            {
                this.View.ShowErrMessage("图片内容为空，无法解密");
                return;
            }

            _log.WriteLog($"开始解密图片，密文长度={encryptedContent.Length}");

            byte[] imageBytes = DecryptImage(encryptedContent, secretKey);

            if (imageBytes == null || imageBytes.Length == 0)
            {
                this.View.ShowErrMessage("图片解密失败，请检查解密密钥是否正确");
                SendMsg.Send($"【物流面单】回单图片解密失败：{waybillNo}");
                return;
            }

            _log.WriteLog($"解密成功，图片大小={imageBytes.Length} bytes");

            long fid = GetFid();
            string billNo = GetBillNo();

            if (fid <= 0)
            {
                this.View.ShowErrMessage("单据未保存，请先保存单据后再下载图片");
                return;
            }

            string fileName = $"{waybillNo}.jpg";
            string fileBase64 = Convert.ToBase64String(imageBytes);

            var uploadData = new JObject
            {
                ["FileName"] = fileName,
                ["FormId"] = "ZMER_SFBill",  
                ["IsLast"] = "true",
                ["InterId"] = fid.ToString(),
                ["BillNO"] = billNo,
                ["AliasFileName"] = fileName,
                ["SendByte"] = fileBase64
            };

            var imageDataJson = JsonConvert.SerializeObject(uploadData);
            _log.WriteLog($"开始上传附件，文件名={fileName}，大小={imageBytes.Length} bytes");

            var uploadResult = WebApiServiceCall.AttachmentUpload(this.Context, imageDataJson);
            var resultJson = JsonUtil.Serialize(uploadResult);

            _log.WriteLog($"附件上传结果：{resultJson}");

            var resultRoot = JObject.Parse(resultJson);
            var responseStatus = resultRoot["Result"]?["ResponseStatus"];
            bool uploadSuccess = responseStatus?["IsSuccess"]?.Value<bool>() ?? false;

            if (uploadSuccess)
            {
                string fileId = resultRoot["Result"]?["FileId"]?.Value<string>() ?? "";
                this.View.ShowMessage($"回单图片下载并上传成功！\n运单号：{waybillNo}\n文件：{fileName}\nFileId：{fileId}");
            }
            else
            {
                string errorMsg = responseStatus?["Errors"]?.ToString() ?? "未知错误";
                this.View.ShowErrMessage($"附件上传失败：{errorMsg}");
                SendMsg.Send($"【物流面单】附件上传失败：{waybillNo}，{errorMsg}");
            }
        }

        /// <summary>
        /// 解密顺丰图片
        /// 加密流程：图片二进制 → Base64编码 → AES/CBC/PKCS5Padding加密 → Base64编码
        /// 解密流程：Base64解码 → AES解密 → Base64解码 → 图片二进制
        /// </summary>
        private byte[] DecryptImage(string encryptedContent, string secretKey)
        {
            try
            {
                byte[] firstDecode = Convert.FromBase64String(encryptedContent);

                // AES/CBC/PKCS5Padding 解密
                byte[] decrypted;
                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(secretKey);
                    aes.IV = AesIv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7; // PKCS5 = PKCS7

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(firstDecode))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var result = new MemoryStream())
                    {
                        cs.CopyTo(result);
                        decrypted = result.ToArray();
                    }
                }

                // 移除PKCS7填充
                int padLen = decrypted[decrypted.Length - 1];
                if (padLen > 0 && padLen <= 16)
                {
                    byte[] unpadded = new byte[decrypted.Length - padLen];
                    Array.Copy(decrypted, unpadded, unpadded.Length);
                    decrypted = unpadded;
                }

                string decryptedStr = Encoding.UTF8.GetString(decrypted);
                byte[] imageBytes = Convert.FromBase64String(decryptedStr);

                return imageBytes;
            }
            catch (Exception ex)
            {
                _log.Error($"AES解密失败：{ex.Message}");
                _log.Error(ex);
                return null;
            }
        }

        /// <summary>HTTP GET请求，带API Key鉴权</summary>
        private string HttpGet(string url, string apiKey)
        {
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                client.Headers.Add("X-Api-Key", apiKey);
                return client.DownloadString(url);
            }
        }

        /// <summary>获取单据内码</summary>
        private long GetFid()
        {
            try
            {
                object v = this.Model.DataObject["Id"];
                if (v == null || v == DBNull.Value)
                    return 0;
                return Convert.ToInt64(v);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>获取单据编号</summary>
        private string GetBillNo()
        {
            try
            {
                object v = this.Model.GetValue("FBillNo");
                if (v == null || v == DBNull.Value)
                    return "";
                return Convert.ToString(v).Trim();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>获取运单号</summary>
        private string GetWaybillNo()
        {
            object v = this.Model.GetValue("FSFYDH");
            return (v == null || v == DBNull.Value) ? "" : Convert.ToString(v).Trim();
        }
    }
}

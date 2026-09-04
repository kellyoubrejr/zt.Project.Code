using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using Kingdee.Zitn.Project.Code.conf;
using Kingdee.Zitn.Project.Code.Util;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Kingdee.Zitn.Project.Code.plugin.SFBill
{
    /// <summary>
    /// 顺丰图片查看插件
    /// 点击 btn_img 按钮 → 从公网中间服务获取加密content → AES解密 → 显示图片
    /// 
    /// 配置项（K3 Cloud AppSettings）：
    ///   SF_MiddleServiceUrl  = http://bpm.ztmicro.com:8769
    ///   SF_MiddleServiceToken = sf-image-service-2024-secure-key
    ///   SF_PictureSecret      = axjGikUwgYVKiJ3A  (顺丰控制台获取的AES密钥)
    /// </summary>
    [Description("【物流面单】查看顺丰图片"), HotUpdate]
    public class SFPicture : AbstractBillPlugIn
    {
        private static readonly CustomLog.LogWriter _log = CustomLog.For("顺丰图片查看");

        // AES密钥（从顺丰控制台获取的解密密钥）
        private static readonly byte[] AesIv = new byte[16]; // 16字节全0 IV

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            try
            {
                if (e.BarItemKey.Equals("btn_img", StringComparison.OrdinalIgnoreCase))
                {
                    ViewAndDecryptPicture();
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                this.View.ShowErrMessage("查看图片失败：" + ex.Message);
                SendMsg.Send("【物流面单】查看图片失败", ex);
            }
        }

        /// <summary>
        /// 查看并解密顺丰图片
        /// 流程：获取运单号 → 调中间服务API取加密content → AES解密 → 显示图片
        /// </summary>
        private void ViewAndDecryptPicture()
        {
            // 1. 获取运单号
            string waybillNo = GetWaybillNo();
            if (string.IsNullOrWhiteSpace(waybillNo))
            {
                this.View.ShowErrMessage("顺丰运单号(FSFYDH)为空，无法查看图片");
                return;
            }

            // 2. 检查配置
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

            // 3. 调用中间服务API获取加密content
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
                return;
            }

            var dataArray = root["data"] as JArray;
            if (dataArray == null || dataArray.Count == 0)
            {
                this.View.ShowMessage($"运单号 {waybillNo} 暂无图片，请确认顺丰已推送");
                return;
            }

            // 4. 取第一条图片的加密content，进行AES解密
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
                return;
            }

            _log.WriteLog($"解密成功，图片大小={imageBytes.Length} bytes");

            // 5. 显示图片
            ShowImage(imageBytes, waybillNo);
        }

        /// <summary>
        /// 解密顺丰图片
        /// 顺丰加密流程：图片二进制 → Base64编码 → AES/CBC/PKCS5Padding加密 → Base64编码
        /// 解密流程：Base64解码 → AES解密 → Base64解码 → 图片二进制
        /// </summary>
        private byte[] DecryptImage(string encryptedContent, string secretKey)
        {
            try
            {
                // 第一次Base64解码
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

                // 第二次Base64解码（得到图片二进制）
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

        /// <summary>
        /// 在新窗口中显示图片
        /// </summary>
        private void ShowImage(byte[] imageBytes, string waybillNo)
        {
            try
            {
                using (var ms = new MemoryStream(imageBytes))
                {
                    var img = Image.FromStream(ms);

                    // 创建窗口显示图片
                    var form = new Form
                    {
                        Text = $"顺丰图片 - {waybillNo}",
                        Size = new Size(Math.Min(img.Width + 40, 1200), Math.Min(img.Height + 80, 900)),
                        StartPosition = FormStartPosition.CenterScreen,
                        BackColor = Color.White
                    };

                    var pictureBox = new PictureBox
                    {
                        Dock = DockStyle.Fill,
                        Image = img,
                        SizeMode = PictureBoxSizeMode.Zoom
                    };

                    form.Controls.Add(pictureBox);
                    form.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                _log.Error($"显示图片失败：{ex.Message}");
                _log.Error(ex);
                this.View.ShowErrMessage("显示图片失败：" + ex.Message);
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

        /// <summary>获取运单号</summary>
        private string GetWaybillNo()
        {
            object v = this.Model.GetValue("FSFYDH");
            return (v == null || v == DBNull.Value) ? "" : Convert.ToString(v).Trim();
        }
    }
}

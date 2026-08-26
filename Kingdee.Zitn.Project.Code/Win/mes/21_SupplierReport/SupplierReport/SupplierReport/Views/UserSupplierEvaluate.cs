using Microsoft.Office.Interop.Excel;
using Microsoft.Web.WebView2.WinForms;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZT.Cloud.SupplierReport.Models;
using DictRow = System.Collections.Generic.Dictionary<string, object>;


namespace ZT.Cloud.SupplierReport.Views
{

    public partial class UserSupplierEvaluate : UserControl
    {
        #region 变量
        private SupplierReportHelper myHelper;
        private bool _isInitialized;
        private string _HtmlFileName= "供应商质量评分系统.html";

        private class DeductionItem
        {
            public string Code;
            public double Total;
            public double Improve;
            public double Cost;
            public double Complaint;
        }

        // 解析后的数据
        private List<DictRow> _inspectionData = new List<DictRow>();
        private Dictionary<string, string> _supMap = new Dictionary<string, string>();
        private List<DeductionItem> _deductionData = new List<DeductionItem>();

        // WebView2 状态
        private bool _pageReady = false;
        #endregion

        #region "初始化"
        public UserSupplierEvaluate()
        {
            InitializeComponent();
        }
        public async void Initial(SupplierReportHelper Helper)
        {
            if(!_isInitialized)
            {
                _isInitialized = true;
                myHelper = Helper;
                await InitWebView2();
            }
        }
        private async Task InitWebView2()
        {
            try
            {
                WebView21.CoreWebView2InitializationCompleted += OnCoreInitDone;
                await WebView21.EnsureCoreWebView2Async(null);
            }
            catch (Exception ex)
            {
                ToolStripStatusLblErrorMsg.Text = "WebView初始化失败: " + ex.Message;
            }
        }

        private void OnCoreInitDone(object sender,
            Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                ToolStripStatusLblErrorMsg.Text = "WebView2 Core 失败: " + e.InitializationException?.Message;
                return;
            }

            // 处理文件下载（PDF/Word/Excel 导出）
            WebView21.CoreWebView2.DownloadStarting += (s, args) =>
            {
                using (var dlg = new SaveFileDialog())
                {
                    dlg.FileName = args.ResultFilePath ?? "download";
                    dlg.Filter = "所有文件|*.*";
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        args.ResultFilePath = dlg.FileName;
                    }
                    else
                    {
                        args.Cancel = true;
                    }
                }
            };

            // 页面加载完成后，标记桥接可用
            WebView21.CoreWebView2.NavigationCompleted += (s, args) =>
            {
                if (args.IsSuccess)
                {
                    Task.Delay(300).ContinueWith(_ =>
                    {
                        BeginInvoke(new System.Action(() =>
                        {
                            _pageReady = true;
                            WebView21.CoreWebView2.ExecuteScriptAsync(
                                "typeof window.__winformBridge");
                        }));
                    });
                }
            };

            LoadHtml();
    }

        /// <summary>
        /// 读原始 HTML → 在 IIFE 内部注入桥接函数 → 临时文件 → Navigate(file://)
        ///
        /// 桥接函数 window.__winformBridge 运行在 IIFE 闭包内部，
        /// 可直接操作 allData / supMap / updateSupplierSearch 等变量。
        ///
        /// C# 通过 ExecuteScriptAsync 调用此函数，不依赖 chrome.webview
        /// </summary>
        private void LoadHtml()
        {
            string dir = Path.Combine(myHelper.AppStartPath, "Htmls");
            string src = Path.Combine(dir, _HtmlFileName);
            if (!File.Exists(src))
            {
                dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Htmls");
                src = Path.Combine(dir, _HtmlFileName);
            }
            if (!File.Exists(src))
            {
                ToolStripStatusLblErrorMsg.Text = "找不到 HTML";
                return;
            }
            WebView21.CoreWebView2.Navigate(new Uri(src).AbsoluteUri);
        }

        #endregion

        #region "数据上传"

        /// <summary>
        /// 上传来料检验合格率总表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_upload_inspection_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "选择来料检验合格率总表";
                dlg.Filter = "Excel 文件|*.xlsx;*.xls;*.xlsm;*.csv";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                LoadInspectionData(dlg.FileName);
                if (_inspectionData.Count == 0)
                {
                    MessageBox.Show("未识别到有效的来料检验数据（需包含：供应商编码、检验数量、不合格数、状态等列）", "提示");
                    return;
                }

                string json = BuildInspectionJson();
                SendToHtml(json);

                ToolStripLblDataStatus.Text = $"✅ 来料: {_inspectionData.Count}条";
            }
        }

        /// <summary>
        /// 上传额外扣分表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_upload_deduction_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "选择额外扣分表";
                dlg.Filter = "Excel 文件|*.xlsx;*.xls;*.csv";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                LoadDeductionData(dlg.FileName);
                if (_deductionData.Count == 0)
                {
                    MessageBox.Show("未识别到有效的额外扣分数据（需包含：供应商编码、改善配合分扣分项、质量成本损失扣分项、客诉或归零扣分项）", "提示");
                    return;
                }

                string json = BuildDeductionJson();
                SendToHtml(json);

                ToolStripLblDataStatus.Text += $" | 扣分: {_deductionData.Count}条";
            }
        }

        /// <summary>
        /// 上传供应商编码对应表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_upload_supplier_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "选择供应商编码对应表";
                dlg.Filter = "Excel 文件|*.xlsx;*.xls;*.csv";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                LoadSupplierMap(dlg.FileName);
                if (_supMap.Count == 0)
                {
                    MessageBox.Show("未识别到有效的供应商编码-名称对应数据", "提示");
                    return;
                }

                string json = BuildSupplierJson();
                SendToHtml(json);

                ToolStripLblDataStatus.Text += $" | 供应商: {_supMap.Count}个";
            }
        }


        private void LoadInspectionData(string path)
        {
            _inspectionData.Clear();
            var rows = ReadSheet(path);
            if (rows.Count == 0) return;

            var h = rows[0];
            if (!h.ContainsKey("供应商编码") || !h.ContainsKey("检验数量") ||
                !h.ContainsKey("不合格数") || !h.ContainsKey("状态"))
                return;

            _inspectionData = rows;
        }
        private void LoadSupplierMap(string path)
        {
            _supMap.Clear();
            var rows = ReadSheet(path);
            foreach (var r in rows)
            {
                string code = JsonHtmlMethods.StrVal(r, "供应商编码") ?? JsonHtmlMethods.StrVal(r, "supplier_code")
                           ?? JsonHtmlMethods.FirstVal(r);
                string name = JsonHtmlMethods.StrVal(r, "供应商名称") ?? JsonHtmlMethods.StrVal(r, "supplier_name")
                           ?? JsonHtmlMethods.SecondVal(r) ?? code;
                if (string.IsNullOrEmpty(code)) continue;
                code = code.Trim();
                if (!_supMap.ContainsKey(code))
                    _supMap[code] = (name ?? code).Trim();
            }
        }
        private void LoadDeductionData(string path)
        {
            _deductionData.Clear();
            var rows = ReadSheet(path);
            if (rows.Count == 0) return;

            var h = rows[0];
            if (!h.ContainsKey("供应商编码")) return;

            foreach (var r in rows)
            {
                string code = JsonHtmlMethods.StrVal(r, "供应商编码")?.Trim();
                if (string.IsNullOrEmpty(code)) continue;

                double improve = JsonHtmlMethods.ToDouble(r, "改善配合分扣分项");
                double cost = JsonHtmlMethods.ToDouble(r, "质量成本损失扣分项");
                double complaint = JsonHtmlMethods.ToDouble(r, "客诉或归零扣分项");
                double total = improve + cost + complaint;

                if (total > 0)
                {
                    _deductionData.Add(new DeductionItem
                    {
                        Code = code,
                        Improve = improve,
                        Cost = cost,
                        Complaint = complaint,
                        Total = total
                    });
                }
            }
        }

        private List<DictRow> ReadSheet(string path)
        {
            var list = new List<DictRow>();
            string ext = Path.GetExtension(path).ToLower();

            if (ext == ".csv")
            {
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                if (lines.Length < 2) return list;
                string[] heads = lines[0].Split(',');
                for (int i = 1; i < lines.Length; i++)
                {
                    var d = new DictRow();
                    string[] vals = lines[i].Split(',');
                    for (int j = 0; j < heads.Length && j < vals.Length; j++)
                        d[heads[j].Trim()] = vals[j].Trim();
                    list.Add(d);
                }
                return list;
            }

            //ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using (var pkg = new ExcelPackage(new FileInfo(path)))
            {
                var sheet = pkg.Workbook.Worksheets[1];
                if (sheet.Dimension == null) return list;

                int rows = sheet.Dimension.Rows, cols = sheet.Dimension.Columns;
                var heads = new List<string>();
                for (int c = 1; c <= cols; c++)
                    heads.Add((sheet.Cells[1, c].Text ?? "").Trim());

                for (int r = 2; r <= rows; r++)
                {
                    var d = new DictRow();
                    for (int c = 1; c <= cols; c++)
                        d[heads[c - 1]] = sheet.Cells[r, c].Value;
                    list.Add(d);
                }
            }
            return list;
        }

        #endregion

        #region "将报文发送给Html"
        private string BuildInspectionJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\"inspectionData\":[");
            for (int i = 0; i < _inspectionData.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('{');
                bool first = true;
                foreach (var kv in _inspectionData[i])
                {
                    if (!first) sb.Append(',');
                    sb.Append('"'); sb.Append(JsonHtmlMethods.Esc(kv.Key)); sb.Append("\":\"");
                    sb.Append(JsonHtmlMethods.Esc(kv.Value?.ToString() ?? ""));
                    sb.Append('"');
                    first = false;
                }
                sb.Append('}');
            }
            sb.Append("]}");
            return sb.ToString();
        }
        private string BuildSupplierJson()  
        {
            var sb = new StringBuilder();
            sb.Append("{\"supplierMap\":[");
            bool first = true;
            foreach (var kv in _supMap)
            {
                if (!first) sb.Append(',');
                sb.Append("{\"code\":\""); sb.Append(JsonHtmlMethods.Esc(kv.Key));
                sb.Append("\",\"name\":\""); sb.Append(JsonHtmlMethods.Esc(kv.Value));
                sb.Append("\"}");
                first = false;
            }
            sb.Append("]}");
            return sb.ToString();
        }
        private string BuildDeductionJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\"deductionData\":[");
            for (int i = 0; i < _deductionData.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var d = _deductionData[i];
                sb.Append("{\"code\":\""); sb.Append(JsonHtmlMethods.Esc(d.Code));
                sb.Append("\",\"total\":").Append(d.Total.ToString("F1"));
                sb.Append(",\"improve\":").Append(d.Improve.ToString("F1"));
                sb.Append(",\"cost\":").Append(d.Cost.ToString("F1"));
                sb.Append(",\"complaint\":").Append(d.Complaint.ToString("F1"));
                sb.Append('}');
            }
            sb.Append("]}");
            return sb.ToString();
        }
        private void SendToHtml(string json)
        {
            if (!_pageReady)
            {
                Task.Delay(500).ContinueWith(_ =>
                {
                    BeginInvoke(new System.Action(() => SendToHtml(json)));
                });
                return;
            }

            string script = "window.__winformPayload = " + json + ";" + "window.__winformBridge();";
            WebView21.CoreWebView2.ExecuteScriptAsync(script);
        }

        #endregion

    }

}

using Microsoft.Web.WebView2.WinForms;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZT.Cloud.SupplierReport.Models;

namespace ZT.Cloud.SupplierReport.Views
{
    public partial class UserCPKAnalysis : UserControl
    {
        #region 变量
        private string _HtmlFileName = "单尺寸CPK分析程序.html";

        private SupplierReportHelper myHelper;
        private bool _isInitialized;

        private List<List<double>> _subgroups = new List<List<double>>();

        private bool _pageReady = false;
        #endregion

        #region "初始化"
        public UserCPKAnalysis()
        {
            InitializeComponent();
        }
        public async void Initial(SupplierReportHelper Helper)
        {
            if (!_isInitialized)
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
                ToolStripStatusLblErrorMsg.Text = "WebView2 初始化失败: " + ex.Message;
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

            WebView21.CoreWebView2.DownloadStarting += (s, args) =>
            {
                using (var dlg = new SaveFileDialog())
                {
                    dlg.FileName = args.ResultFilePath ?? "download";
                    dlg.Filter = "所有文件|*.*";
                    if (dlg.ShowDialog() == DialogResult.OK)
                        args.ResultFilePath = dlg.FileName;
                    else
                        args.Cancel = true;
                }
            };

            WebView21.CoreWebView2.NavigationCompleted += (s, args) =>
            {
                if (args.IsSuccess)
                {
                    Task.Delay(300).ContinueWith(_ =>
                    {
                        BeginInvoke(new Action(() =>
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

        #region "上传按钮"

        private void btn_upload_data_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "选择数据文件（每行一组数据，逗号分隔）";
                dlg.Filter = "Excel/CSV 文件|*.xlsx;*.xls;*.xlsm;*.csv";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                LoadSubgroupData(dlg.FileName);
                if (_subgroups.Count == 0)
                {
                    MessageBox.Show("未识别到有效的子组数据（每行至少包含一个数值）", "提示");
                    return;
                }

                string json = BuildJson();
                SendToHtml(json);

                int total = _subgroups.Sum(g => g.Count);
                ToolStripLblDataStatus.Text = $"✅ {_subgroups.Count}组, {total}个数据点";
            }
        }

        #endregion

        #region "数据解析"

        private void LoadSubgroupData(string path)
        {
            _subgroups.Clear();
            string ext = Path.GetExtension(path).ToLower();

            if (ext == ".csv")
            {
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    var vals = lines[i].Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => { double.TryParse(s.Trim(), out double d); return d; })
                        .Where(d => !double.IsNaN(d))
                        .ToList();
                    if (vals.Count > 0) _subgroups.Add(vals);
                }
                return;
            }

            // Excel: each row is a sub-group
            using (var pkg = new ExcelPackage(new FileInfo(path)))
            {
                var sheet = pkg.Workbook.Worksheets[1];
                if (sheet.Dimension == null) return;

                int rows = sheet.Dimension.Rows, cols = sheet.Dimension.Columns;

                // Check if first row is header (all non-numeric)
                int startRow = 1;
                bool firstRowAllText = true;
                for (int c = 1; c <= cols; c++)
                {
                    var val = sheet.Cells[1, c].Value;
                    if (val != null && double.TryParse(val.ToString().Trim(), out _))
                    {
                        firstRowAllText = false;
                        break;
                    }
                }
                if (firstRowAllText) startRow = 2;

                for (int r = startRow; r <= rows; r++)
                {
                    var vals = new List<double>();
                    for (int c = 1; c <= cols; c++)
                    {
                        var val = sheet.Cells[r, c].Value;
                        if (val != null && double.TryParse(val.ToString().Trim(), out double d))
                            vals.Add(d);
                    }
                    if (vals.Count > 0) _subgroups.Add(vals);
                }
            }
        }

        #endregion

        #region "构建JSON & 发送"

        private string BuildJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\"subgroups\":[");
            for (int i = 0; i < _subgroups.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('[');
                for (int j = 0; j < _subgroups[i].Count; j++)
                {
                    if (j > 0) sb.Append(',');
                    sb.Append(_subgroups[i][j].ToString("F4"));
                }
                sb.Append(']');
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
                    BeginInvoke(new Action(() => SendToHtml(json)));
                });
                return;
            }

            string script = "window.__winformPayload = " + json + ";" + "window.__winformBridge();";
            WebView21.CoreWebView2.ExecuteScriptAsync(script);
        }

        #endregion
    }
}

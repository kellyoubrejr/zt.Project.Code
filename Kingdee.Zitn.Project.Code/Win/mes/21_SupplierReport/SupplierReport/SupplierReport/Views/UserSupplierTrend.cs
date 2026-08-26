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
using DictRow = System.Collections.Generic.Dictionary<string, object>;

namespace ZT.Cloud.SupplierReport.Views
{
    public partial class UserSupplierTrend : UserControl
    {
        #region 变量
        private string _HtmlFileName = "供应商月度趋势分析系统.html";

        private SupplierReportHelper myHelper;
        private bool _isInitialized;

        private List<DictRow> _raw2025 = new List<DictRow>();
        private Dictionary<string, List<DictRow>> _raw2026ByMonth = new Dictionary<string, List<DictRow>>();

        private bool _pageReady = false;
        #endregion

        #region "初始化"
        public UserSupplierTrend()
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

        private void btn_upload_2025_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "选择2025年全年数据";
                dlg.Filter = "Excel 文件|*.xlsx;*.xls;*.xlsm;*.csv";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                LoadYearData(dlg.FileName, _raw2025);
                if (_raw2025.Count == 0)
                {
                    MessageBox.Show("未识别到有效数据（需包含：供应商编码、检验数量、不合格数、状态等列）", "提示");
                    return;
                }

                string json = Build2025Json();
                SendToHtml(json);

                ToolStripLblDataStatus.Text = $"✅ 2025年: {_raw2025.Count}条";
            }
        }

        private void btn_upload_2026_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "选择2026年各月数据（可多选）";
                dlg.Filter = "Excel 文件|*.xlsx;*.xls;*.xlsm;*.csv";
                dlg.Multiselect = true;
                if (dlg.ShowDialog() != DialogResult.OK || dlg.FileNames.Length == 0) return;

                if (dlg.FileNames.Length > 14)
                {
                    MessageBox.Show("最多选择13个月的数据文件", "提示");
                    return;
                }

                LoadMonthlyData(dlg.FileNames.ToList());
                if (_raw2026ByMonth.Count == 0)
                {
                    MessageBox.Show("未识别到有效的月度数据", "提示");
                    return;
                }

                string json = Build2026Json();
                SendToHtml(json);

                int total = _raw2026ByMonth.Sum(kv => kv.Value.Count);
                ToolStripLblDataStatus.Text += $" | 2026年: {_raw2026ByMonth.Count}个月, {total}条";
            }
        }

        #endregion

        #region "数据解析"

        private void LoadYearData(string path, List<DictRow> target)
        {
            target.Clear();
            var rows = ReadSheet(path);
            if (rows.Count == 0) return;

            var h = rows[0];
            if (!h.ContainsKey("供应商编码") || !h.ContainsKey("检验数量") ||
                !h.ContainsKey("不合格数") || !h.ContainsKey("状态"))
                return;

            target.AddRange(rows);
        }

        private void LoadMonthlyData(List<string> files)
        {
            _raw2026ByMonth.Clear();

            foreach (string f in files)
            {
                string name = Path.GetFileName(f);
                try
                {
                    var rows = ReadSheet(f);
                    if (rows.Count == 0) continue;

                    var h = rows[0];
                    if (!h.ContainsKey("供应商编码") || !h.ContainsKey("检验数量") ||
                        !h.ContainsKey("不合格数") || !h.ContainsKey("状态"))
                        continue;

                    string month = JsonHtmlMethods.MonthFromName(name);
                    if (month == null)
                    {
                        foreach (var r in rows)
                        {
                            month = JsonHtmlMethods.DateFromRow(r, "检验结束日期")
                                 ?? JsonHtmlMethods.DateFromRow(r, "单据日期");
                            if (month != null) break;
                        }
                    }
                    if (month == null) month = "未知";

                    if (!_raw2026ByMonth.ContainsKey(month))
                        _raw2026ByMonth[month] = new List<DictRow>();
                    _raw2026ByMonth[month].AddRange(rows);
                }
                catch { }
            }

            var sorted = new Dictionary<string, List<DictRow>>();
            foreach (var kv in _raw2026ByMonth.OrderBy(kv => kv.Key))
                sorted[kv.Key] = kv.Value;
            _raw2026ByMonth = sorted;
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

        #region "构建JSON & 发送"

        private string Build2025Json()
        {
            return "{\"raw2025\":" + RowsToJson(_raw2025) + "}";
        }

        private string Build2026Json()
        {
            var sb = new StringBuilder();
            sb.Append("{\"raw2026ByMonth\":{");
            bool firstMonth = true;
            foreach (var kv in _raw2026ByMonth)
            {
                if (!firstMonth) sb.Append(',');
                sb.Append('"'); sb.Append(JsonHtmlMethods.Esc(kv.Key));
                sb.Append("\":");
                sb.Append(RowsToJson(kv.Value));
                firstMonth = false;
            }
            sb.Append("}}");
            return sb.ToString();
        }

        private string RowsToJson(List<DictRow> rows)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < rows.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('{');
                bool first = true;
                foreach (var kv in rows[i])
                {
                    if (!first) sb.Append(',');
                    sb.Append('"'); sb.Append(JsonHtmlMethods.Esc(kv.Key)); sb.Append("\":\"");
                    sb.Append(JsonHtmlMethods.Esc(kv.Value?.ToString() ?? ""));
                    sb.Append('"');
                    first = false;
                }
                sb.Append('}');
            }
            sb.Append(']');
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

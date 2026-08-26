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
using ZT.Cloud.SupplierReport.Views;
using DictRow = System.Collections.Generic.Dictionary<string, object>;


namespace ZT.Cloud.SupplierReport.Views
{
    public partial class UserSupplierScore : UserControl
    {
        #region 变量
        private string _HtmlFileName = "供应商质量表现系统.html";

        private SupplierReportHelper myHelper;
        private bool _isInitialized;

        private class MonthlyGroup
        {
            public string Month;
            public List<DictRow> Rows = new List<DictRow>();
        }

        // 解析后的数据，对应 HTML IIFE 内的 allData / supMap
        private List<MonthlyGroup> _allData = new List<MonthlyGroup>();
        private Dictionary<string, string> _supMap = new Dictionary<string, string>();

        // WebView2 状态
        private bool _pageReady = false; // HTML 页面加载完成、桥接可用
        #endregion

        #region "初始化"
        public UserSupplierScore()
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
               // ToolStripStatusLblErrorMsg.Text = "WebView2 初始化失败: " + ex.Message;
            }
        }
        private void OnCoreInitDone(object sender,
            Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
               // ToolStripStatusLblErrorMsg.Text = "WebView2 Core 失败: " + e.InitializationException?.Message;
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
                    // 延迟一小段时间确保 IIFE 执行完毕
                    Task.Delay(300).ContinueWith(_ =>
                    {
                        BeginInvoke(new System.Action(() =>
                        {
                            _pageReady = true;

                            // 验证桥接函数是否存在
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


        #region "上传按钮"


        #endregion 

        private void btn_shangchuan_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "选择 Excel 数据文件（可多选）";
                dlg.Filter = "Excel 文件|*.xlsx;*.xls;*.xlsm;*.csv";
                dlg.Multiselect = true;

                if (dlg.ShowDialog() != DialogResult.OK || dlg.FileNames.Length == 0)
                    return;

                if (dlg.FileNames.Length > 14)
                {
                    MessageBox.Show("最多13个数据文件 + 1个对应表", "提示");
                    return;
                }

                var dataFiles = new List<string>();
                string suppFile = null;

                foreach (string f in dlg.FileNames)
                {
                    if (IsSupplierMap(f))
                        suppFile = f;
                    else
                        dataFiles.Add(f);
                }

                if (dataFiles.Count == 0 && suppFile == null)
                {
                    MessageBox.Show("未识别到有效 Excel", "提示");
                    return;
                }

                // 1. EPPlus 解析 Excel
                if (suppFile != null)
                    LoadSupplierMap(suppFile);
                if (dataFiles.Count > 0)
                    LoadMonthly(dataFiles);

                // 2. 构建 JSON
                string json = BuildJson();
                if (json == null) return;

                // 4. 通过 ExecuteScriptAsync 注入 JSON 并调用桥接
                SendToHtml(json);
            }
        }
        private bool IsSupplierMap(string path)
        {
            try
            {
                var rows = ReadSheet(path);
                return rows.Count > 0 &&
                       (rows[0].ContainsKey("供应商名称") || rows[0].ContainsKey("supplier_name"));
            }
            catch { return false; }
        }

        private void LoadMonthly(List<string> files)
        {
            _allData.Clear();
            int total = 0;

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

                    if (month.Length == 4)
                    {
                        var groups = new Dictionary<string, List<DictRow>>();
                        foreach (var r in rows)
                        {
                            string ym = JsonHtmlMethods.DateFromRow(r, "检验结束日期")
                                     ?? JsonHtmlMethods.DateFromRow(r, "单据日期");
                            if (ym == null || !ym.StartsWith(month)) ym = month + "-未知";
                            if (!groups.ContainsKey(ym)) groups[ym] = new List<DictRow>();
                            groups[ym].Add(r);
                        }
                        foreach (var kv in groups)
                        {
                            var exist = _allData.FirstOrDefault(d => d.Month == kv.Key);
                            if (exist != null) exist.Rows.AddRange(kv.Value);
                            else _allData.Add(new MonthlyGroup { Month = kv.Key, Rows = kv.Value });
                            total += kv.Value.Count;
                        }
                    }
                    else
                    {
                        var exist = _allData.FirstOrDefault(d => d.Month == month);
                        if (exist != null) exist.Rows.AddRange(rows);
                        else _allData.Add(new MonthlyGroup { Month = month, Rows = rows });
                        total += rows.Count;
                    }
                }
                catch (Exception ex) { }
            }

            _allData = _allData.OrderBy(d => d.Month).ToList();
            ToolStripLblDataStatus.Text = $"✅ {_allData.Count}个月份, {total}条";
        }
        private void LoadSupplierMap(string path)
        {
            _supMap.Clear();
            try
            {
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
                ToolStripLblDataStatus.Text += $" | 供应商: {_supMap.Count}个";
            }
            catch (Exception ex) { }
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

        private string BuildJson()
        {
            if (_allData.Count == 0) return null;

            var sb = new StringBuilder();
            sb.Append("{\"monthlyData\":[");

            for (int i = 0; i < _allData.Count; i++)
            {
                var md = _allData[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"month\":\"");
                sb.Append(JsonHtmlMethods.Esc(md.Month));
                sb.Append("\",\"rows\":[");
                for (int j = 0; j < md.Rows.Count; j++)
                {
                    if (j > 0) sb.Append(',');
                    sb.Append('{');
                    bool first = true;
                    foreach (var kv in md.Rows[j])
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
            }

            sb.Append("],\"supplierMap\":[");
            bool fm = true;
            foreach (var kv in _supMap)
            {
                if (!fm) sb.Append(',');
                sb.Append("{\"code\":\""); sb.Append(JsonHtmlMethods.Esc(kv.Key));
                sb.Append("\",\"name\":\""); sb.Append(JsonHtmlMethods.Esc(kv.Value));
                sb.Append("\"}");
                fm = false;
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


        private void btn_shangchuan_supplier_Click(object sender, EventArgs e)
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
                SendSupplierMapToHtml(json);
            }
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
        private void SendSupplierMapToHtml(string json)
        {
            if (!_pageReady)
            {
                Task.Delay(500).ContinueWith(_ =>
                {
                    BeginInvoke(new System.Action(() => SendSupplierMapToHtml(json)));
                });
                return;
            }

            // 复用统一桥接：json 只有 supplierMap，桥接内不覆盖 monthlyData
            string script = "window.__winformPayload = " + json + ";" +
                           "window.__winformBridge();";
            WebView21.CoreWebView2.ExecuteScriptAsync(script);
        }

    }


}

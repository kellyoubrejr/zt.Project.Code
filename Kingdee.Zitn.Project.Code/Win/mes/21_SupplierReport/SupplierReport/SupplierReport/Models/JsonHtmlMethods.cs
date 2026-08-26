using Microsoft.Office.Interop.Excel;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DictRow = System.Collections.Generic.Dictionary<string, object>;


namespace ZT.Cloud.SupplierReport.Models
{
    internal static class JsonHtmlMethods
    {
        public static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
        public static string MonthFromName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string n = Path.GetFileNameWithoutExtension(name);
            var m = System.Text.RegularExpressions.Regex.Match(n, @"(\d{2})\.(\d{1,2})月");
            if (m.Success) return $"{2000 + int.Parse(m.Groups[1].Value)}-{int.Parse(m.Groups[2].Value):D2}";
            m = System.Text.RegularExpressions.Regex.Match(n, @"(\d{4})年");
            if (m.Success) return m.Groups[1].Value;
            m = System.Text.RegularExpressions.Regex.Match(n, @"(\d{4})[-_](\d{1,2})");
            if (m.Success) return $"{m.Groups[1].Value}-{int.Parse(m.Groups[2].Value):D2}";
            m = System.Text.RegularExpressions.Regex.Match(n, @"(\d{4})(\d{2})");
            if (m.Success && int.Parse(m.Groups[2].Value) >= 1 && int.Parse(m.Groups[2].Value) <= 12)
                return $"{m.Groups[1].Value}-{int.Parse(m.Groups[2].Value):D2}";
            return null;
        }
        public static string DateFromRow(DictRow row, string key)
        {
            if (!row.ContainsKey(key)) return null;
            string v = row[key]?.ToString()?.Trim();
            if (string.IsNullOrEmpty(v)) return null;
            var m = System.Text.RegularExpressions.Regex.Match(v, @"(\d{4})[-/](\d{1,2})");
            if (m.Success) return $"{m.Groups[1].Value}-{int.Parse(m.Groups[2].Value):D2}";
            if (DateTime.TryParse(v, out DateTime dt))
                return $"{dt.Year}-{dt.Month:D2}";
            return null;
        }
        public static string StrVal(DictRow row, string key)
            => row.TryGetValue(key, out object v) && v != null ? v.ToString().Trim() : null;
        public static string FirstVal(DictRow row)
            => row.Count > 0 ? row.First().Value?.ToString()?.Trim() : null;
        public static string SecondVal(DictRow row)
            => row.Count > 1 ? row.Skip(1).First().Value?.ToString()?.Trim() : null;
        public static double ToDouble(DictRow row, string key)
        {
            if (row.TryGetValue(key, out object v) && v != null)
            {
                if (double.TryParse(v.ToString().Trim(), out double result))
                    return result;
            }
            return 0;
        }


    }
}

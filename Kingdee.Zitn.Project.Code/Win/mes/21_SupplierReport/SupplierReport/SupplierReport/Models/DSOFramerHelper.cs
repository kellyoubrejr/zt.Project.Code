using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Word;
using Microsoft.Win32;
using OfficeOpenXml;
using ExcelWorksheet = OfficeOpenXml.ExcelWorksheet;
using ExcelRange = OfficeOpenXml.ExcelRange;
using Workbook = Microsoft.Office.Interop.Excel.Workbook;
using Worksheet = Microsoft.Office.Interop.Excel.Worksheet;
using Application = Microsoft.Office.Interop.Excel.Application;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ZT.Cloud.SupplierReport.Models
{
   
    internal class DSOFramerHelper
    {
        #region 变量声明

        public enum DocTypes
        {
            EXCEL,
            WORD,
            PPT,
            VSD
        }

        private string _ErrorMessage = "";
        public string ErrorMessage
        {
            get { return _ErrorMessage; }
            set { _ErrorMessage = value; }
        }

        private DocTypes _DocType = DocTypes.EXCEL;
        public DocTypes DocType
        {
            get { return _DocType; }
        }

        private string _OpenType = "";
        public string OpenType
        {
            get { return _OpenType; }
        }

        private AxDSOFramer.AxFramerControl m_axFramerControl;
        private object m_Document; // 获取加载的Excel,word,ppt等

        #endregion

        #region 初始化

        public DSOFramerHelper(AxDSOFramer.AxFramerControl _m_axFramerControl)
        {
            m_axFramerControl = _m_axFramerControl;

            m_axFramerControl.Titlebar = false;   // 是否显示标题栏
            m_axFramerControl.Menubar = true;     // 是否显示菜单栏
            m_axFramerControl.Toolbars = true;    // 是否显示工具栏
        }

        /// <summary>
        /// 初始化office控件,加载Excel/Word/PPT
        /// </summary>
        /// <param name="_sFilePath"></param>
        public bool InitOfficeControl(string _sFilePath)
        {
            try
            {
                string sExt = Path.GetExtension(_sFilePath).Replace(".", "");

                // 先关闭已经打开的文档
                m_axFramerControl.Close();

                // 再打开新的文档
                m_axFramerControl.Open(_sFilePath, false, LoadOpenFileType(sExt), "", ""); // 打开文件
                m_Document = m_axFramerControl.ActiveDocument;

                // 准备操作Excel
                switch (this._DocType)
                {
                    case DocTypes.EXCEL:
                        GetExcelActiveBookAndSheet();
                        break;
                    case DocTypes.WORD:
                        GetWordActiveDocument();
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                this._ErrorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 下面这个方法是dso打开文件时需要的一个参数，代表office文件类型
        /// 根据后缀名得到打开方式
        /// </summary>
        /// <param name="_sExten"></param>
        /// <returns></returns>
        private string LoadOpenFileType(string _sExten)
        {
            try
            {
                switch (_sExten.ToLower())
                {
                    case "xls":
                    case "xlsx":
                        _OpenType = "Excel.Sheet";
                        _DocType = DocTypes.EXCEL;
                        break;
                    case "doc":
                    case "docx":
                        _OpenType = "Word.Document";
                        _DocType = DocTypes.WORD;
                        break;
                    case "ppt":
                    case "pptx":
                        _OpenType = "PowerPoint.Show";
                        _DocType = DocTypes.PPT;
                        break;
                    case "vsd":
                        _OpenType = "Visio.Drawing";
                        _DocType = DocTypes.VSD;
                        break;
                    default:
                        _OpenType = "Word.Document";
                        _DocType = DocTypes.WORD;
                        break;
                }
            }
            catch (Exception ex)
            {
                _OpenType = ex.Message;
            }
            return _OpenType;
        }

        public void PrintPreview()
        {
            m_axFramerControl.PrintPreview();
        }

        public void Print()
        {
            m_axFramerControl.PrintOut();
        }

        #endregion

        #region DsoFramer.ocx注册相关

        public enum RegisterResults
        {
            Failed = -1,
            Success = 1,
            AlreadyRegistered = 2
        }

        /// <summary>
        /// 注册dsoframer.ocx，并返回注册操作的结果
        /// </summary>
        /// <returns></returns>
        public static RegisterResults RegisterDsoFramer()
        {
            //copy dsoframer.ocx c:\windows\system32\dsoframer.ocx
            //regsvr32.exe c:\windows\system32\dsoframer.ocx

            string dsoframerFileName = "dsoframer.ocx";
            //1.检查存放ocx文件的目录bin\Debug\DLL\DSOFramer.ocx是否存在ocx文件
            string fileFullPath = $"{System.Windows.Forms.Application.StartupPath}\\{dsoframerFileName}";
            string systemDrive = System.Environment.GetEnvironmentVariable("systemdrive"); // 获取系统所在的盘符
            if (!File.Exists(fileFullPath) || string.IsNullOrEmpty(systemDrive))
            {
                return RegisterResults.Failed; // dsoframer.ocx不存在，注册失败
            }

            //2.获取系统目录
            string windowsPath = "";
            if (Environment.Is64BitOperatingSystem)
            {
                windowsPath = string.Format("{0}\\Windows\\SysWOW64", systemDrive);
            }
            else
            {
                windowsPath = string.Format("{0}\\Windows\\System32", systemDrive);
            }
            if (!Directory.Exists(windowsPath)) return RegisterResults.Failed; // 目标目录不存在，注册失败

            //3.判断是否已经注册过
            //bool isRegisted = IsRegistered("00460182-9E5E-11D5-B7C8-B8269041DD57");
            //if (isRegisted)
            //{
            //    return RegisterResults.AlreadyRegistered; // 已注册过，注册成功
            //}

            string DesFile = string.Format("{0}\\{1}", windowsPath, dsoframerFileName);
            if (!File.Exists(DesFile))
            {
                //4.复制dsoframer.ocx文件到C:\\Windows\\SysWOW64 或 C:\\Windows\\System32
                File.Copy(fileFullPath, DesFile, true);

                //5.开始注册
                bool result = Register(string.Format("{0}\\{1}", windowsPath, dsoframerFileName));
                if (result)
                {
                    return RegisterResults.Success;
                }
                else
                {
                    return RegisterResults.Failed;
                }
            }
            else
            {
                return RegisterResults.AlreadyRegistered; // 注册成功
            }
        }

        /// <summary>
        /// 判断dsoframer.ocx控件是否已经注册(CLSID='"00460182-9E5E-11D5-B7C8-B8269041DD57"')
        /// </summary>
        /// <param name="CLSID"></param>
        /// <returns></returns>
        private static bool IsRegistered(string CLSID)
        {
            if (string.IsNullOrEmpty(CLSID)) return false;
            string key = string.Format(@"CLSID\{{{0}}}", CLSID);
            RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(key);
            if (regKey != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 开始注册ocx
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <returns></returns>
        private static bool Register(string fileFullName)
        {
            bool result = false;
            System.Diagnostics.Process p = System.Diagnostics.Process.Start("regsvr32", fileFullName); // 注册完毕不显示是否成功的提示
            if (p != null && p.HasExited)
            {
                int exitCode = p.ExitCode;
                if (exitCode == 0)
                {
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// 取消注册dsoframer.ocx
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <returns></returns>
        private static bool UnRegiste(string fileFullName)
        {
            bool result = false;
            System.Diagnostics.Process p = System.Diagnostics.Process.Start("regsvr32", fileFullName + " /u"); // 注册完毕不显示是否成功的提示
            if (p != null && p.HasExited)
            {
                int exitCode = p.ExitCode;
                if (exitCode == 0)
                {
                    result = true;
                }
            }
            return result;
        }

        #endregion

        #region Excel处理相关

        public Workbook workbook;
        public Worksheet worksheet;

        /// <summary>
        /// 获取当前活动的Sheet表
        /// </summary>
        public void GetExcelActiveBookAndSheet()
        {
            workbook = (Workbook)m_Document;
            worksheet = workbook.Worksheets[workbook.Worksheets.Count];
            worksheet.Activate();
            worksheet.Range["A1"].Select();
        }

        /// <summary>
        /// 将字母转成数字列号
        /// </summary>
        /// <param name="columnLetter"></param>
        /// <returns></returns>
        public static int ColumnLetterToNumber(string columnLetter)
        {
            int number = 0;
            int power = 0;
            for (int i = columnLetter.Length - 1; i >= 0; i--)
            {
                int charValue = (int)columnLetter[i] - (int)'A' + 1;
                number += charValue * (int)Math.Pow(26, power);
                power += 1;
            }
            return number;
        }

        public static void GetRowColNumber(string Range, out int RowNo, out int ColNo)
        {
            try
            {
                if (Range == "")
                {
                    RowNo = 0;
                    ColNo = 0;
                    return;
                }

                string ColName = "";
                string strRowNo = "";
                for (int t = 0; t < Range.Length; t++)
                {
                    if (!char.IsDigit(Range[t]))
                    {
                        ColName += Range[t];
                    }
                    else
                    {
                        strRowNo += Range[t];
                    }
                }

                RowNo = int.Parse(strRowNo);
                ColNo = ColumnLetterToNumber(ColName);
            }
            catch (Exception)
            {
                RowNo = 0;
                ColNo = 0;
            }
        }

        public string GetOfficeRangeValue(Microsoft.Office.Interop.Excel.Range Cell)
        {
            if (Cell.Value == null)
            {
                return "";
            }
            else
            {
                return Cell.Value.ToString().Trim();
            }
        }

        public string GetEPPlusCellValue(ExcelRange Cell)
        {
            if (Cell.Value == null)
            {
                return "";
            }
            else
            {
                return Cell.Value.ToString().Trim();
            }
        }

        /// <summary>
        /// 设置选定单元格的条件格式
        /// </summary>
        /// <param name="SheetName"></param>
        /// <param name="IniRowIndex"></param>
        /// <param name="IniColIndex"></param>
        /// <param name="Cond1"></param>
        /// <param name="Cond2"></param>
        public void SetCellFormulaCondition(string SheetName, int IniRowIndex, int IniColIndex, string Cond1, string Cond2)
        {
            worksheet = workbook.Worksheets[SheetName];
            worksheet.Activate();
            Microsoft.Office.Interop.Excel.Range ColRange = worksheet.Range[worksheet.Cells[IniRowIndex, IniColIndex], worksheet.Cells[worksheet.Rows.Count, IniColIndex]];
            ColRange.FormatConditions.Delete(); // 删除已有的公式

            string Formula = "";
            XlFormatConditionOperator OprType = 0;
            if (Cond1 != "")
            {
                GetFormulaCondition(Cond1, ref OprType, ref Formula);
                if (OprType > 0 && Formula != "") ColRange.FormatConditions.Add(XlFormatConditionType.xlCellValue, OprType, Formula, Type.Missing);
            }
            if (Cond2 != "")
            {
                GetFormulaCondition(Cond2, ref OprType, ref Formula);
                if (OprType > 0 && Formula != "") ColRange.FormatConditions.Add(XlFormatConditionType.xlCellValue, OprType, Formula, Type.Missing);
            }
        }

        public void SetCellBKColor(string SheetName, int RowIndex, int ColIndex, Color BKColor)
        {
            worksheet = workbook.Worksheets[SheetName];
            worksheet.Activate();
            Microsoft.Office.Interop.Excel.Range ColRange = worksheet.Cells[RowIndex, ColIndex];
            ColRange.Interior.Color = BKColor;
        }

        private void GetFormulaCondition(string Cond, ref XlFormatConditionOperator OprType, ref string Formula)
        {
            string strOpr = "";
            string PreChr = "";
            for (int t = 0; t < Cond.Length; t++)
            {
                PreChr = Cond.Substring(t, 1);
                if (char.IsDigit(PreChr[0]) || PreChr == "\"" || PreChr == "-" || PreChr == "+")
                {
                    break;
                }
                else
                {
                    strOpr += PreChr;
                }
            }
            Formula = "=" + Cond.Substring(strOpr.Length);

            switch (strOpr)
            {
                case "<":
                    OprType = XlFormatConditionOperator.xlLess;
                    break;
                case "<=":
                    OprType = XlFormatConditionOperator.xlLessEqual;
                    break;
                case ">":
                    OprType = XlFormatConditionOperator.xlGreater;
                    break;
                case ">=":
                    OprType = XlFormatConditionOperator.xlGreaterEqual;
                    break;
                case "=":
                    OprType = XlFormatConditionOperator.xlEqual;
                    break;
                case "~":
                    OprType = XlFormatConditionOperator.xlBetween;
                    break;
            }
        }

        #endregion

        #region 产证专用

        private CertificateInformation _Certificate;
        /// <summary>
        /// 从产证模板中提取出来的信息
        /// </summary>
        /// <returns></returns>
        public CertificateInformation Certificate
        {
            get { return _Certificate; }
            set { _Certificate = value; }
        }

        /// <summary>
        /// 加载产证模板中被标黄的所有单元格信息，并获取原始测量表中所有的序列码信息
        /// </summary>
        /// <returns></returns>
        public bool AnalyzeCertificate(bool NeedVerifyTemplate = false)
        {
            try
            {
                _Certificate = new CertificateInformation();

                OpenFileDialog myDiag = new OpenFileDialog
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    Filter = "*.xlsx|*.xlsx|*.*|*.*"
                };
                if (myDiag.ShowDialog() == DialogResult.OK)
                {
                    _Certificate.CertificateFilePath = myDiag.FileName; // 操作人员打开的产证模板文件

                    // 读取该模板文件中的信息
                    using (ExcelPackage package = new ExcelPackage(new System.IO.FileInfo(_Certificate.CertificateFilePath)))
                    {
                        //1.获取当前要使用的表格
                        ExcelWorksheet worksheet = package.Workbook.Worksheets["产证模板"];
                        if (worksheet != null)
                        {
                            // 获取主要的产品信息
                            this._Certificate.CertificateNo = GetEPPlusCellValue(worksheet.Cells[1, 6]);
                            this._Certificate.ProdCode = GetEPPlusCellValue(worksheet.Cells[5, 10]);
                            this._Certificate.ProdName = GetEPPlusCellValue(worksheet.Cells[7, 10]);
                            this._Certificate.SampleSN = GetEPPlusCellValue(worksheet.Cells[9, 10]);

                            //2.遍历所有行，获取具有引用关系的单元格及其公式
                            string PrePageNo = "1";
                            string PrePageTitle = "产品证明书";
                            int TotalRow = worksheet.Dimension.End.Row;
                            int TotalCol = worksheet.Dimension.End.Column;
                            string PreCellValue = "";
                            for (int t = 1; t <= TotalRow; t++)
                            {
                                for (int s = 1; s <= TotalCol; s++)
                                {
                                    // 提取出页码及下页标题
                                    PreCellValue = GetEPPlusCellValue(worksheet.Cells[t, 1]);
                                    if (PreCellValue == "第" || PreCellValue == "共")
                                    {
                                        bool FoundPage = false;
                                        for (int j = 1; j <= TotalCol; j++)
                                        {
                                            if (GetEPPlusCellValue(worksheet.Cells[t, j + 1]) == "页" || GetEPPlusCellValue(worksheet.Cells[t, j + 2]) == "页")
                                            {
                                                FoundPage = true;
                                                if (PreCellValue == "共") break;
                                                PrePageNo = (int.Parse(GetEPPlusCellValue(worksheet.Cells[t, j])) + 1).ToString();
                                                PrePageTitle = GetEPPlusCellValue(worksheet.Cells[t + 1, 1]);
                                                break;
                                            }
                                        }
                                        if (FoundPage) break;
                                    }

                                    var Cell = worksheet.Cells[t, s];
                                    if (Cell.Formula != "")
                                    {
                                        //FormulaCellItem NewItem = new FormulaCellItem
                                        //{
                                        //    PageNo = PrePageNo,
                                        //    PageTitle = PrePageTitle,
                                        //    RowIndex = t,
                                        //    ColIndex = s,
                                        //    Address = Cell.Address,
                                        //    IsMerged = Cell.Merge,
                                        //    Formula = Cell.Formula,
                                        //    CellValue = Cell.Value
                                        //};
                                        //if (NewItem.ExtCellRange != "") GetRowColNumber(NewItem.ExtCellRange, out NewItem.ExtCellRowIndex, out NewItem.ExtCellColIndex);

                                        //// 去掉所有的合并单元格（已经合并，且值为空），并且只保留具有公式的单元格
                                        //if (NewItem.IsMerged && NewItem.CellValue != null && NewItem.CellValue.ToString() != "")
                                        //{
                                        //    _Certificate.FormulaCells.Add(NewItem);
                                        //}
                                    }
                                }
                            }

                            //3.对数据按照表格，单元格的顺序进行排序
                            var ExtCells = from p in _Certificate.FormulaCells
                                           where p.ExtSheetName != ""
                                           orderby p.ExtSheetName
                                           select p;

                            //4.依据提取到的单元格公式，确定所有需要引用的表名称及所有被引用单元格坐标
                            string LastExtSheetName = "";
                            string PreExtSheetName = "";
                            string PreExtCellRange = "";
                            SourceSheet LastSrcSheet = null;
                            foreach (var ExtCell in ExtCells)
                            {
                                PreExtSheetName = ExtCell.ExtSheetName;
                                if (PreExtSheetName != LastExtSheetName)
                                {
                                    LastExtSheetName = PreExtSheetName;
                                    LastSrcSheet = new SourceSheet();
                                    LastSrcSheet.SheetName = PreExtSheetName;
                                    this._Certificate.SourceSheets.Add(LastSrcSheet);
                                }
                                LastSrcSheet.RefCells.Add(ExtCell.ExtCellRange);
                            }

                            //5.逐个解析被引用表，首先确定样本码的位置，然后再确定数据行起始截止位置信息（行排列格式还是列排列格式），以便进行后续单元格的复制
                            foreach (var SourceSheet in this._Certificate.SourceSheets)
                            {
                                ExcelWorksheet preSheet = package.Workbook.Worksheets[SourceSheet.SheetName]; // 切换至当前被引用的Sheet
                                if (preSheet == null)
                                {
                                    MessageBox.Show("未找到表:" + SourceSheet.SheetName, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    continue;
                                }
                                // 根据单元格特点，大致确定一下向左还是向上查找SN
                                // 选定第一个单元格，然后向左,向上寻找，直到第一行/列，看能否找到与SampleSN匹配的值（个别产品不是序列码，而是写出1，2，3）
                                SourceSheet.RefCells.Sort(); // 按照单元格顺序进行排序

                                // 获取涉及到的所有单元格的最大行列
                                int MaxRowIndex = 0, MaxColIndex = 0;
                                foreach (var Cell in SourceSheet.RefCells)
                                {
                                    int PreRow, PreCol;
                                    GetRowColNumber(Cell, out PreRow, out PreCol);
                                    if (MaxRowIndex < PreRow) MaxRowIndex = PreRow;
                                    if (MaxColIndex < PreCol) MaxColIndex = PreCol;
                                }

                                //5.1 首先从当前行开始，水平向左查找，直至A列,如果该行没有查询到，则向上一行查找
                                int StartRow = MaxRowIndex;     // 当前行号
                                int StartCol = MaxColIndex;     // 获取当前字母所代表的列号
                                bool FoundSN = false;
                                for (int r = StartRow; r >= 1; r--)
                                {
                                    for (int c = StartCol; c >= 1; c--)
                                    {
                                        if (preSheet.Cells[r, c].Value == null) continue;
                                        PreCellValue = preSheet.Cells[r, c].Value.ToString().Trim();
                                        if (PreCellValue == this._Certificate.SampleSN) // 找到对应的码，便可以确定是行模式
                                        {
                                            // 确定跨越的行数（通过合并的单元格来确定）
                                            SampleRange PreSampleRange = new SampleRange();
                                            PreSampleRange.RangeType = RangeTypes.Row; // 按行排列方式
                                            PreSampleRange.PSampleSN.RowIndex = r; // 当前样本码所处位置
                                            PreSampleRange.PSampleSN.ColIndex = c;

                                            PreSampleRange.P0.RowIndex = r;
                                            PreSampleRange.P0.ColIndex = 1;  // 行模式下，一次性复制整行数据

                                            PreSampleRange.P1.RowIndex = r;
                                            PreSampleRange.P1.ColIndex = preSheet.Dimension.End.Column; // 当前Sheet的全部有效列

                                            // 向下找到合并单元格的截止行
                                            for (int s = r; s <= preSheet.Dimension.End.Row; s++)
                                            {
                                                if (preSheet.Cells[s, c].Merge)
                                                {
                                                    if (preSheet.Cells[s, c].Value != null)
                                                    {
                                                        PreCellValue = preSheet.Cells[s, c].Value.ToString();
                                                        if (PreCellValue != "" && PreCellValue != this._Certificate.SampleSN)
                                                        {
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        PreSampleRange.P1.RowIndex = s;
                                                    }
                                                }
                                            }

                                            FoundSN = true;
                                            if (SourceSheet.MaxRowIndex < PreSampleRange.P1.RowIndex) SourceSheet.MaxRowIndex = PreSampleRange.P1.RowIndex; // 用于判断是否有错误的引用
                                            if (SourceSheet.MaxColIndex < PreSampleRange.P1.ColIndex) SourceSheet.MaxColIndex = PreSampleRange.P1.ColIndex;
                                            SourceSheet.SampleRanges.Add(PreSampleRange);
                                            break;
                                        }
                                    }
                                    if (FoundSN) break;
                                }

                                //5.3.todo: 如果左侧没有查到，则从当前行开始，垂直向上查询，直至第一行，如果查到，则为列模式
                                if (!FoundSN)
                                {
                                    // TODO: 实现列模式查找
                                }

                                //5.4.如果查到了样本码，则加载该sheet中的所有SN
                                if (FoundSN)
                                {
                                    var PreSampleRange = SourceSheet.SampleRanges[0];
                                    string PreSN = "";
                                    if (PreSampleRange.RangeType == RangeTypes.Row)
                                    {
                                        // 横向模式（SN在左侧，测量数据在右侧）
                                        for (int s = PreSampleRange.PSampleSN.RowIndex; s <= preSheet.Dimension.End.Row; s++)
                                        {
                                            if (preSheet.Cells[s, PreSampleRange.PSampleSN.ColIndex].Value != null)
                                            {
                                                PreSN = preSheet.Cells[s, PreSampleRange.PSampleSN.ColIndex].Value.ToString().Trim();
                                                SourceSheet.SNs.Add(PreSN);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // 纵向模式（SN在上方，测量数据在下方）
                                        for (int s = PreSampleRange.PSampleSN.ColIndex; s <= preSheet.Dimension.End.Column; s++)
                                        {
                                            if (preSheet.Cells[PreSampleRange.PSampleSN.RowIndex, s].Value != null)
                                            {
                                                PreSN = preSheet.Cells[PreSampleRange.PSampleSN.RowIndex, s].Value.ToString().Trim();
                                                SourceSheet.SNs.Add(PreSN);
                                            }
                                        }
                                    }
                                }

                                //5.5如果查到了样本码，则获取该表格对应的所有列标题信息(标题，单位，上下限),用于判断是否引用正确及条件格式是否设置正常
                                if (FoundSN)
                                {
                                    var PreSampleRange = SourceSheet.SampleRanges[0];
                                    string PreSN = "";
                                    if (PreSampleRange.RangeType == RangeTypes.Row)
                                    {
                                        // 横向模式（SN在左侧，测量数据在右侧）
                                        int IniRow = PreSampleRange.PSampleSN.RowIndex - 1;
                                        int EndRow = PreSampleRange.PSampleSN.RowIndex - 3;
                                        int IniCol = PreSampleRange.PSampleSN.ColIndex + 1;
                                        int EndCol = preSheet.Dimension.End.Column;
                                        ExcelRange PreCell;

                                        // 逐列分析
                                        for (int c = IniCol; c <= EndCol; c++)
                                        {
                                            PreCell = preSheet.Cells[PreSampleRange.PSampleSN.RowIndex, c];
                                            if (PreCell.Value != null) // 仅判断测量值不为空的列
                                            {
                                                MesHeaderItem MesHeader = new MesHeaderItem();
                                                MesHeader.CellRowIndex = PreSampleRange.PSampleSN.RowIndex;
                                                MesHeader.CellColIndex = c;

                                                // 从当前单元格开始向上查找列标题及范围
                                                //1.获取已经设定的条件格式公式
                                                var CFs = from CF in preSheet.ConditionalFormatting
                                                          where CF.Address.Start.Address == PreCell.Address
                                                          select CF;
                                                if (CFs.Count() > 0)
                                                {
                                                    string EqualFormula = "";
                                                    foreach (var CF in CFs)
                                                    {
                                                        //switch (CF.Type)
                                                        //{
                                                        //    case ConditionalFormatting.eExcelConditionalFormattingRuleType.LessThan: // <
                                                        //        MesHeader.LowFormula = "<" + CF.Node.InnerText;
                                                        //        break;
                                                        //    case ConditionalFormatting.eExcelConditionalFormattingRuleType.LessThanOrEqual: // <=
                                                        //        MesHeader.LowFormula = "<=" + CF.Node.InnerText;
                                                        //        break;
                                                        //    case ConditionalFormatting.eExcelConditionalFormattingRuleType.GreaterThan: // >
                                                        //        MesHeader.HighFormula = ">" + CF.Node.InnerText;
                                                        //        break;
                                                        //    case ConditionalFormatting.eExcelConditionalFormattingRuleType.GreaterThanOrEqual: // >=
                                                        //        MesHeader.HighFormula = ">=" + CF.Node.InnerText;
                                                        //        break;
                                                        //    case ConditionalFormatting.eExcelConditionalFormattingRuleType.Equal:
                                                        //        EqualFormula = "=" + CF.Node.InnerText;
                                                        //        break;
                                                        //    case ConditionalFormatting.eExcelConditionalFormattingRuleType.Between:
                                                        //        MesHeader.LowFormula = "<" + CF.Node.ChildNodes[0].InnerText;
                                                        //        MesHeader.HighFormula = ">" + CF.Node.ChildNodes[1].InnerText;
                                                        //        break;
                                                        //}
                                                    }
                                                    // 如果发现了=号表达式
                                                    if (EqualFormula != "")
                                                    {
                                                        if (MesHeader.LowFormula == "") MesHeader.LowFormula = EqualFormula;
                                                        if (MesHeader.HighFormula == "") MesHeader.HighFormula = EqualFormula;
                                                    }
                                                }

                                                //2.获取范围
                                                PreCellValue = GetEPPlusCellValue(preSheet.Cells[IniRow, c]);
                                                if (PreCellValue.Contains("±") || PreCellValue.Contains("≤") || PreCellValue.Contains("≥") || PreCellValue.Contains(">") || PreCellValue.Contains("<") || PreCellValue.Contains("＜") || PreCellValue.Contains("＞") || PreCellValue.Contains("~"))
                                                {
                                                    MesHeader.Limit = PreCellValue;
                                                }

                                                //3.获取单位及列标题
                                                int TitleRowIndex = 0;
                                                if (MesHeader.Limit != "")
                                                {
                                                    TitleRowIndex = 1;
                                                }
                                                if (IniRow - 1 >= TitleRowIndex)
                                                {
                                                    PreCellValue = GetEPPlusCellValue(preSheet.Cells[IniRow - TitleRowIndex, c]);
                                                    if (PreCellValue == "")
                                                    {
                                                        if (IniRow >= 2) PreCellValue = GetEPPlusCellValue(preSheet.Cells[IniRow - TitleRowIndex - 1, c]);
                                                    }

                                                    if (PreCellValue != "")
                                                    {
                                                        if (PreCellValue.Contains("(") || PreCellValue.Contains("（")) // 括号中为单位
                                                        {
                                                            int IniPos = GetUnitPos(PreCellValue, "(,（");
                                                            int EndPos = GetUnitPos(PreCellValue, "),）");
                                                            if (IniPos > 0)
                                                            {
                                                                MesHeader.Title = PreCellValue.Substring(0, IniPos).TrimEnd('\n');
                                                                MesHeader.Unit = PreCellValue.Substring(IniPos + 1, EndPos - IniPos - 1);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            MesHeader.Title = PreCellValue;
                                                        }
                                                        if (MesHeader.Title.Contains('\n')) MesHeader.Title = MesHeader.Title.Trim('\n');
                                                    }
                                                }

                                                if (MesHeader.Title == "")
                                                {
                                                    MesHeader.IsFoundError = true;
                                                    MesHeader.ErrorDescription = "没有解析出列标题";
                                                }
                                                else if (MesHeader.Unit == "")
                                                {
                                                    MesHeader.IsFoundError = true;
                                                    MesHeader.ErrorDescription = "没有解析出单位";
                                                }
                                                else if (MesHeader.Limit == "")
                                                {
                                                    MesHeader.IsFoundError = true;
                                                    MesHeader.ErrorDescription = "没有解析出测量范围";
                                                }
                                                else if (MesHeader.HighFormula == "" && MesHeader.LowFormula == "") // 没有设置单元格的格式
                                                {
                                                    // TODO: 多行情况下，其余行如果没有公式时的判断
                                                    MesHeader.IsFoundError = true;
                                                    MesHeader.ErrorDescription = "没有设置条件格式:" + PreCell.Address;
                                                }

                                                SourceSheet.MesHeaders.Add(MesHeader);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // 纵向模式（SN在上方，测量数据在下方）
                                        for (int s = PreSampleRange.PSampleSN.ColIndex; s <= preSheet.Dimension.End.Column; s++)
                                        {
                                            if (preSheet.Cells[PreSampleRange.PSampleSN.RowIndex, s].Value != null)
                                            {
                                                PreSN = preSheet.Cells[PreSampleRange.PSampleSN.RowIndex, s].Value.ToString().Trim();
                                                SourceSheet.SNs.Add(PreSN);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    this._Certificate.IsAnalyzeSuccessd = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                this._Certificate.IsAnalyzeSuccessd = false;
                this.ErrorMessage = ex.Message;
            }

            return false;
        }

        private int GetUnitPos(string StrHeader, string StrSpliter)
        {
            int IntPos = 0;
            string[] Splits = StrSpliter.Split(',');
            foreach (string SP in Splits)
            {
                if (StrHeader.Contains(SP))
                {
                    IntPos = StrHeader.IndexOf(SP);
                    return IntPos;
                }
            }
            return -1;
        }

        /// <summary>
        /// 将模板文件复制一份至输出目录，然后将每个表单的数据复制到第一行/列，供模板自动调用生成最终的产证报告
        /// </summary>
        /// <param name="ReportRootPath"></param>
        /// <param name="SN"></param>
        /// <returns></returns>
        public string GenerateCertifcate(string ReportRootPath, string SN)
        {
            Application ExcelApp = new Application();
            Workbook ExcelWorkBook = null;
            Worksheet ExcelSheet = null;
            string DesFile = "";

            try
            {
                //1.复制一份模板文件到输出目录中
                string DesRootPath = ReportRootPath + $"\\{this._Certificate.ProdCode}\\{DateTime.Now.ToString("yyyy-MM-dd")}";
                if (!System.IO.Directory.Exists(DesRootPath))
                {
                    System.IO.Directory.CreateDirectory(DesRootPath);
                }
                DesFile = DesRootPath + $"\\{SN}.xlsx";
                System.IO.File.Copy(this._Certificate.CertificateFilePath, DesFile, true);

                //2.将每个所引用数据的表单里指定SN的信息行或者列，copy至样本所在行列（通常第一行为样本SN）
                this._Certificate.NGCells.Clear();

                ExcelWorkBook = ExcelApp.Workbooks.Open(DesFile);
                foreach (var extSheet in this._Certificate.SourceSheets)
                {
                    //1.获取当前要使用的表格
                    ExcelSheet = ExcelWorkBook.Worksheets[extSheet.SheetName];
                    if (ExcelSheet != null)
                    {
                        int RowCount = ExcelSheet.UsedRange.Rows.Count;
                        int ColCount = ExcelSheet.UsedRange.Columns.Count;

                        //2.定位到需要生成产证的SN
                        foreach (var SampleRange in extSheet.SampleRanges)
                        {
                            if (SampleRange.RangeType == RangeTypes.Row)
                            {
                                // 横向模式下，从样本码所在行开始，向下查找到目标SN
                                for (int s = SampleRange.PSampleSN.RowIndex; s <= RowCount; s++)
                                {
                                    if (GetOfficeRangeValue(ExcelSheet.Cells[s, SampleRange.PSampleSN.ColIndex]) == SN)
                                    {
                                        // 找到需要复制的SN所在行，开始复制各单元格信息至样本所在行
                                        for (int i = 0; i <= SampleRange.P1.RowIndex - SampleRange.P0.RowIndex; i++)     // 要复制的单元格行数
                                        {
                                            for (int j = 0; j <= SampleRange.P1.ColIndex - SampleRange.P0.ColIndex; j++) // 要复制的单元格列数
                                            {
                                                // 复制单元格的值到样本码所在位置
                                                ExcelSheet.Cells[SampleRange.P0.RowIndex + i, SampleRange.P0.ColIndex + j].Value = ExcelSheet.Cells[s + i, SampleRange.P0.ColIndex + j].Value;
                                                // 判断单元格背景色是否为红色，表示该值超过了误差范围，应该再最终的结果报告中予以体现
                                                if (ExcelSheet.Cells[SampleRange.P0.RowIndex + i, SampleRange.P0.ColIndex + j].DisplayFormat.Interior.Color != 16777215) // 16777215=0xFFFFFF 为白色背景色
                                                {
                                                    this._Certificate.NGCells.Add(new FormulaCellItem()
                                                    {
                                                        RowIndex = SampleRange.P0.RowIndex + i,
                                                        ColIndex = SampleRange.P0.ColIndex + j,
                                                        Address = ExcelSheet.Cells[SampleRange.P0.RowIndex + i, SampleRange.P0.ColIndex + j].Address,
                                                        CellValue = ExcelSheet.Cells[SampleRange.P0.RowIndex + i, SampleRange.P0.ColIndex + j].Value,
                                                        ExtSheetName = extSheet.SheetName
                                                    });
                                                }
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                //3.纵向模式下，从样本码所在列开始，向右查找到目标SN
                                for (int c = SampleRange.PSampleSN.ColIndex; c <= ColCount; c++)
                                {
                                    if (GetOfficeRangeValue(ExcelSheet.Cells[SampleRange.PSampleSN.RowIndex, c]) == SN)
                                    {
                                        // 找到需要复制的SN所在列，开始复制各单元格信息至样本所在列
                                        for (int i = 0; i <= SampleRange.P1.ColIndex - SampleRange.P0.ColIndex; i++)       // 要复制的单元格列数
                                        {
                                            for (int j = 0; j <= SampleRange.P1.RowIndex - SampleRange.P0.RowIndex; j++)   // 要复制的单元格行数
                                            {
                                                // 复制单元格的值到样本码所在位置
                                                ExcelSheet.Cells[SampleRange.P0.RowIndex + i, SampleRange.P0.ColIndex + j].Value = ExcelSheet.Cells[SampleRange.P0.RowIndex + j, c + i].Value;
                                                // 判断单元格背景色是否为红色，表示该值超过了误差范围，应该再最终的结果报告中予以体现
                                                if (ExcelSheet.Cells[SampleRange.P0.RowIndex + i, SampleRange.P0.ColIndex + j].DisplayFormat.Interior.Color != 16777215) // 16777215=0xFFFFFF 为白色背景色
                                                {
                                                    this._Certificate.NGCells.Add(new FormulaCellItem()
                                                    {
                                                        RowIndex = SampleRange.P0.RowIndex + j,
                                                        ColIndex = SampleRange.P0.ColIndex + i,
                                                        Address = ExcelSheet.Cells[SampleRange.P0.RowIndex + j, SampleRange.P0.ColIndex + i].Address,
                                                        CellValue = ExcelSheet.Cells[SampleRange.P0.RowIndex + j, SampleRange.P0.ColIndex + i].Value,
                                                        ExtSheetName = extSheet.SheetName
                                                    });
                                                }
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                // 将最终的产证报告的黄色背景设置为无填充
                ExcelSheet = ExcelWorkBook.Worksheets["产证模板"];
                ExcelSheet.Cells.Interior.ColorIndex = -4142;

                // 单独判断一下产证编号有无缺失
                string CertificateNo = GetOfficeRangeValue(ExcelSheet.Range["F1"]);
                if (CertificateNo.Length <= 1)
                {
                    FormulaCellItem NGCell = new FormulaCellItem() { RowIndex = 1, ColIndex = 6, Address = "$F:$1", ExtSheetName = "产证模板", ExtCellRange = "F1" };
                    this._Certificate.NGCells.Add(NGCell);
                    ExcelSheet.Cells[NGCell.RowIndex, NGCell.ColIndex].Interior.Color = 0xFF;
                }

                // 将有问题的单元格标红
                if (this._Certificate.NGCells.Count > 0)
                {
                    foreach (var NGCell in this._Certificate.NGCells)
                    {
                        foreach (var Cell in this._Certificate.FormulaCells)
                        {
                            if (Cell.ExtSheetName == NGCell.ExtSheetName && Cell.ExtCellRange == NGCell.Address.Replace("$", ""))
                            {
                                ExcelSheet.Cells[Cell.RowIndex, Cell.ColIndex].Interior.Color = 0xFF;
                            }
                        }
                    }
                }

                ExcelWorkBook.Save();
            }
            catch (Exception ex)
            {
                this.ErrorMessage = ex.Message;
                return "";
            }
            finally
            {
                if (ExcelWorkBook != null) ExcelWorkBook.Close();
                if (ExcelApp != null) ExcelApp.Quit();
                if (ExcelSheet != null) Marshal.ReleaseComObject(ExcelSheet);
                if (ExcelWorkBook != null) Marshal.ReleaseComObject(ExcelWorkBook);
                if (ExcelApp != null) Marshal.ReleaseComObject(ExcelApp);
            }

            // 如果当前为NG品，则将其移入NG目录中
            if (this._Certificate.NGCells.Count > 0 && DesFile != "")
            {
                string SrcFile = DesFile;
                string DesNGFolder = System.IO.Path.GetDirectoryName(SrcFile) + "\\NG";
                if (!System.IO.Directory.Exists(DesNGFolder)) System.IO.Directory.CreateDirectory(DesNGFolder);
                DesFile = DesNGFolder + "\\" + System.IO.Path.GetFileName(SrcFile);
                if (System.IO.File.Exists(DesFile)) System.IO.File.Delete(DesFile);
                File.Move(SrcFile, DesFile);
            }

            return DesFile;
        }

        /// <summary>
        /// 从产证模板文件中给定的单元格开始，向前查找出对应的单位，范围，标题，以便与引用表进行核对
        /// </summary>
        /// <param name="RowIndex"></param>
        /// <param name="ColIndex"></param>
        /// <returns></returns>
        public List<string> GetVerifyItems(int RowIndex, int ColIndex)
        {
            List<string> Items = new List<string>();
            Worksheet PreSheet = this.workbook.Sheets["产证模板"];
            string PreCellValue;
            for (int col = ColIndex - 1; col >= 1; col--)
            {
                PreCellValue = GetOfficeRangeValue(PreSheet.Cells[RowIndex, col]);
                if (PreCellValue != "") Items.Add(PreCellValue);
            }

            return Items;
        }

        /// <summary>
        /// 保存Excel
        /// </summary>
        /// <param name="ClearBK"></param>
        public void SaveCertificate(bool ClearBK)
        {
            if (ClearBK)
            {
                workbook.Worksheets["产证模板"].Cells.Interior.ColorIndex = -4142;
            }
            workbook.Save();
            MessageBox.Show("保存成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region 各种实验报告专用（word文档）

        public Document Doc;

        /// <summary>
        /// 获取当前活动的document
        /// </summary>
        public void GetWordActiveDocument()
        {
            Doc = (Document)m_Document;
        }

        /// <summary>
        /// 测试报告基本信息类
        /// </summary>
        public class TestReportInformation
        {
            private string _ReportPath;
            public string ReportPath
            {
                get { return _ReportPath; }
                set { _ReportPath = value; }
            }

            private string _ReportNo = "";
            /// <summary>
            /// 报告编号
            /// </summary>
            public string ReportNo
            {
                get { return _ReportNo; }
                set { _ReportNo = value; }
            }

            private string _ProdName = "";
            /// <summary>
            /// 产品名称
            /// </summary>
            public string ProdName
            {
                get { return _ProdName; }
                set { _ProdName = value; }
            }

            private string _ProdCode = "";
            /// <summary>
            /// 产品代码
            /// </summary>
            public string ProdCode
            {
                get { return _ProdCode; }
                set { _ProdCode = value; }
            }

            private string _LotCode = "";
            /// <summary>
            /// 生产批次号
            /// </summary>
            public string LotCode
            {
                get { return _LotCode; }
                set { _LotCode = value; }
            }

            private int _TestQty = 0;
            /// <summary>
            /// 生产数量
            /// </summary>
            public int TestQty
            {
                get { return _TestQty; }
                set { _TestQty = value; }
            }

            private List<string> _SNs = new List<string>();
            /// <summary>
            /// 所有的产品编号
            /// </summary>
            public List<string> SNs
            {
                get { return _SNs; }
                set { _SNs = value; }
            }

            public string GetAllSN()
            {
                System.Text.StringBuilder strSNs = new System.Text.StringBuilder();
                foreach (string SN in this._SNs)
                {
                    strSNs.AppendLine(SN);
                }
                return strSNs.ToString();
            }
        }

        private TestReportInformation _TestReport;
        /// <summary>
        /// 从测试报告中提取出来的信息
        /// </summary>
        public TestReportInformation TestReport
        {
            get { return _TestReport; }
            set { _TestReport = value; }
        }

        /// <summary>
        /// 选择测试报告，并提取其中的表头信息，主要包含产品型号，批次号，日期，及各个产品编号及数量信息
        /// </summary>
        /// <returns></returns>
        public bool AnalyzeTestReport()
        {
            _TestReport = new TestReportInformation();

            OpenFileDialog myDiag = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Filter = "*.docx|*.docx|*.*|*.*"
            };
            if (myDiag.ShowDialog() == DialogResult.OK)
            {
                _TestReport.ReportPath = myDiag.FileName;
                Microsoft.Office.Interop.Word.Application wordApp = new Microsoft.Office.Interop.Word.Application();
                Document doc = null;
                try
                {
                    // 打开Word文档
                    doc = wordApp.Documents.Open(myDiag.FileName);

                    // 读取文档全部内容
                    string content = doc.Content.Text;
                    string[] Values = content.Split('\r');

                    // 提取出关键信息
                    bool FoundAll = false;
                    for (int t = 0; t < Values.Length; t++)
                    {
                        if (Values[t].Contains("编号："))
                        {
                            if (_TestReport.ReportNo == "")
                            {
                                _TestReport.ReportNo = Values[t].Substring(Values[t].LastIndexOf("：") + 1).Trim();
                            }
                        }
                        else if (Values[t].Contains("产品名称："))
                        {
                            if (_TestReport.ProdName == "")
                            {
                                _TestReport.ProdName = Values[t].Substring(Values[t].IndexOf("：") + 1).Trim();
                            }
                        }
                        else if (Values[t].Contains("产品型号："))
                        {
                            if (_TestReport.ProdCode == "")
                            {
                                _TestReport.ProdCode = Values[t].Substring(Values[t].IndexOf("：") + 1).Trim();
                            }
                        }
                        else if (Values[t].Contains("生产批号："))
                        {
                            if (_TestReport.LotCode == "")
                            {
                                _TestReport.LotCode = Values[t].Substring(Values[t].IndexOf("：") + 1).Trim();
                            }
                        }
                        else if (Values[t].Contains("试验数量"))
                        {
                            if (_TestReport.TestQty == 0)
                            {
                                try
                                {
                                    string qtyStr = Values[t + 1].TrimStart('\a');
                                    _TestReport.TestQty = int.Parse(qtyStr);
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                        else if (Values[t].Contains("产品编号"))
                        {
                            string SN = "";
                            for (int s = t + 1; s < Values.Length; s++)
                            {
                                SN = Values[s].TrimStart('\a');
                                if (SN.Length <= 4) break;
                                _TestReport.SNs.Add(SN);
                            }
                            FoundAll = true;
                        }
                        if (FoundAll) break;
                    }
                }
                catch (Exception ex)
                {
                    this.ErrorMessage = ex.Message;
                    return false;
                }
                finally
                {
                    // 清理资源
                    if (doc != null)
                    {
                        doc.Close();
                    }
                    wordApp.Quit();

                    // 释放COM对象
                    if (doc != null) Marshal.ReleaseComObject(doc);
                    if (wordApp != null) Marshal.ReleaseComObject(wordApp);
                }
            }

            return true;
        }

        #endregion
    }

    // 辅助类定义
    public class CertificateInformation
    {
        public string CertificateFilePath { get; set; }
        public string CertificateNo { get; set; }
        public string ProdCode { get; set; }
        public string ProdName { get; set; }
        public string SampleSN { get; set; }
        public bool IsAnalyzeSuccessd { get; set; }
        public List<FormulaCellItem> FormulaCells { get; set; } = new List<FormulaCellItem>();
        public List<SourceSheet> SourceSheets { get; set; } = new List<SourceSheet>();
        public List<FormulaCellItem> NGCells { get; set; } = new List<FormulaCellItem>();
    }

    public class FormulaCellItem
    {
        public string PageNo { get; set; }
        public string PageTitle { get; set; }
        public int RowIndex { get; set; }
        public int ColIndex { get; set; }
        public string Address { get; set; }
        public bool IsMerged { get; set; }
        public string Formula { get; set; }
        public object CellValue { get; set; }
        public string ExtSheetName { get; set; }
        public string ExtCellRange { get; set; }
        public int ExtCellRowIndex { get; set; }
        public int ExtCellColIndex { get; set; }
    }

    public class SourceSheet
    {
        public string SheetName { get; set; }
        public List<string> RefCells { get; set; } = new List<string>();
        public List<string> SNs { get; set; } = new List<string>();
        public List<SampleRange> SampleRanges { get; set; } = new List<SampleRange>();
        public List<MesHeaderItem> MesHeaders { get; set; } = new List<MesHeaderItem>();
        public int MaxRowIndex { get; set; }
        public int MaxColIndex { get; set; }
    }

    public enum RangeTypes
    {
        Row,
        Column
    }

    public class SampleRange
    {
        public RangeTypes RangeType { get; set; }
        public CellLocation PSampleSN { get; set; } = new CellLocation();
        public CellLocation P0 { get; set; } = new CellLocation();
        public CellLocation P1 { get; set; } = new CellLocation();
    }

    public class CellLocation
    {
        public int RowIndex { get; set; }
        public int ColIndex { get; set; }
    }

    public class MesHeaderItem
    {
        public int CellRowIndex { get; set; }
        public int CellColIndex { get; set; }
        public string Title { get; set; }
        public string Unit { get; set; }
        public string Limit { get; set; }
        public string LowFormula { get; set; }
        public string HighFormula { get; set; }
        public bool IsFoundError { get; set; }
        public string ErrorDescription { get; set; }
    }

}

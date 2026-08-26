using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

using ZT.Cloud.SupplierReport.Models;
using ZT.ZTBaseFrame;
using ZT.ZTDataTracer;
using ZT.Cloud.ZTCloudHelper;
using ZT.Cloud.SupplierReport.Views;
using Microsoft.Office.Interop.Excel;

namespace ZT.Cloud.SupplierReport
{

    //*********************************************
    //名称： SupplierReport入库检验模块
    //作者:  李祥付
    //日期： 2026-06-30
    //功能： 管理SupplierReport入库检验报告相关文档。
    //*********************************************
    public class SupplierReportHelper : IZTCloudHelper
    {
        internal string AppStartPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        internal ClassConfigure Configure;
        internal ClassHistory History;
        internal DataTraceHelper DataTracer;
        internal ClassSQLServer SQLServer;
        internal string CfgPath = "";

        protected override Form GetMainForm()
        {
            return new FrmMain(this);
        }

        protected override bool Initial()
        {
            try
            {
                CfgPath = @"C:\ZTCloud\" + Assembly.GetExecutingAssembly().GetName().Name;
                ZT.ZTDataTracer.DataTraceHelper.AutoCreateDirectory(CfgPath);


                //2.加载Configure
                Configure = new ClassConfigure();
                Configure.XMLPath = CfgPath + @"\Configure.xml";
                ClassConfigure.LoadFromXML<ClassConfigure>(ref Configure);
                Configure.IsNetWorking = !LogInUser.IsOffLine; //设置是否离线运行

                //3.加载History
                History = new ClassHistory();
                History.XMLPath = CfgPath + @"\History.xml";
                ClassHistory.LoadFromXML<ClassHistory>(ref History);

                //4.远程SQL Server
                if (Configure.IsNetWorking)
                {
                    DataTracer = new DataTraceHelper(LogInUser.Plant, LogInUser.GetLanguageDescription(LogInUser.UILanguage));
                    if (DataTracer.Inital(Configure.LineID, Configure.ProcID, Configure.EqptID) == false)
                    {
                        throw new Exception("连接至MES服务器异常,请检查网络连接!" + DataTracer.ErrorMessage);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                this.ErrorMessage = ex.Message;
                return false;
            }
        }

        internal void SaveConfigure()
        {
            ClassConfigure.SaveToXML(ref Configure);
        }
        internal void SaveHistory()
        {
            ClassHistory.SaveToXML(ref History);
        }
    }
}

using System;
using System.Windows.Forms;
using System.Drawing;

using ZT.ZTDataTracer;
using System.Net.NetworkInformation;
using ZT.Cloud.SupplierReport.Models;

namespace ZT.Cloud.SupplierReport.Views
{
    internal partial class FrmMain : Form
    {
        #region "变量声明区"
        private bool _LoadOK = false;
        private SupplierReportHelper myHelper;
        #endregion

        public FrmMain(SupplierReportHelper Helper)
        {
            // 此调用是设计器所必需的。
            InitializeComponent();

            // 在 InitializeComponent() 调用之后添加任何初始化。
            myHelper = Helper;
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            //1.配置主界面
            //1.1网络连接状态
            if (myHelper.Configure.IsNetWorking)
            {
                ToolLblNetStatusValue.Text = "在线";
                ToolLblNetStatusValue.BackColor = Color.LightGreen;
            }
            else
            {
                ToolLblNetStatusValue.Text = "离线";
                ToolLblNetStatusValue.BackColor = Color.Red;
            }

            //1.2操作者信息
            ShowWorkerInfo();
        }

        private void ToolStripStatusLblOperator_Click(object sender, EventArgs e)
        {
            SelectedUserInformation Worker = myHelper.DataTracer.SelectUser(true);
            if (Worker != null)
            {
                myHelper.Configure.WorkerID = Worker.UserID;
                myHelper.Configure.WorkerName = Worker.UserName;
                myHelper.Configure.WorkerCardSN = Worker.CardSN;
                ShowWorkerInfo();
            }
        }

        private void ShowWorkerInfo()
        {
            ToolStripStatusLblOperator.Text = myHelper.Configure.WorkerName + $"({myHelper.Configure.WorkerCardSN})";
        }

        /// <summary>
        /// 调用选项配置界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DiagOptions myDiag=new DiagOptions(myHelper);
            if(myDiag.ShowDialog()==DialogResult.OK)
            {
                myHelper.SaveConfigure();
                MessageBox.Show("请重启程序生效!","提示");
            }
        }


        /// <summary>
        /// SupplierReport入库检验
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolStripButSupplierReportReport_Click(object sender, EventArgs e)
        {
            userSupplierReportReport1.Initial(myHelper);
            userSupplierReportReport1.BringToFront();
        }

        /// <summary>
        /// 供应商质量表现
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolStripButSupplierPerformance_Click(object sender, EventArgs e)
        {
            userSupplierScore1.Initial(myHelper);
            userSupplierScore1.BringToFront();
        }

        /// <summary>
        /// 供应商质量评分
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolStripButSupplierScore_Click(object sender, EventArgs e)
        {
            userSupplierEvaluate1.Initial(myHelper);
            userSupplierEvaluate1.BringToFront();
        }

        /// <summary>
        /// 供应商月度趋势分析
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolStripButSupplierTrend_Click(object sender, EventArgs e)
        {
            userSupplierTrend1.Initial(myHelper);
            userSupplierTrend1.BringToFront();
        }

        /// <summary>
        /// 单尺寸CPK分析
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolStripButCPKAnalysis_Click(object sender, EventArgs e)
        {
            userCPKAnalysis1.Initial(myHelper);
            userCPKAnalysis1.BringToFront();
        }

    }
}
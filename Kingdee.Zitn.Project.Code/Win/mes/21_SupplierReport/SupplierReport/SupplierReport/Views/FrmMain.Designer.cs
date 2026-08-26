
namespace ZT.Cloud.SupplierReport.Views
{
    partial class FrmMain
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.StatusStrip1 = new System.Windows.Forms.StatusStrip();
            this.ToolLblNetStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.ToolLblNetStatusValue = new System.Windows.Forms.ToolStripStatusLabel();
            this.ToolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.ToolStripStatusLblOperator = new System.Windows.Forms.ToolStripStatusLabel();
            this.ToolStripStatusLblErrorMsg = new System.Windows.Forms.ToolStripStatusLabel();
            this.ToolStrip1 = new System.Windows.Forms.ToolStrip();
            this.ToolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.ToolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.userSupplierReportReport1 = new ZT.Cloud.SupplierReport.Views.UserSupplierReportReport();
            this.userSupplierScore1 = new ZT.Cloud.SupplierReport.Views.UserSupplierScore();
            this.userSupplierEvaluate1 = new ZT.Cloud.SupplierReport.Views.UserSupplierEvaluate();
            this.userSupplierTrend1 = new ZT.Cloud.SupplierReport.Views.UserSupplierTrend();
            this.userCPKAnalysis1 = new ZT.Cloud.SupplierReport.Views.UserCPKAnalysis();
            this.ToolStripButSupplierReportReport = new System.Windows.Forms.ToolStripButton();
            this.ToolStripButSupplierScore = new System.Windows.Forms.ToolStripButton();
            this.ToolStripButSupplierPerformance = new System.Windows.Forms.ToolStripButton();
            this.ToolStripButSupplierTrend = new System.Windows.Forms.ToolStripButton();
            this.ToolStripButCPKAnalysis = new System.Windows.Forms.ToolStripButton();
            this.ToolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.ToolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.ConfigureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.StatusStrip1.SuspendLayout();
            this.ToolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // StatusStrip1
            // 
            this.StatusStrip1.BackColor = System.Drawing.Color.LightBlue;
            this.StatusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripDropDownButton1,
            this.ToolLblNetStatus,
            this.ToolLblNetStatusValue,
            this.ToolStripStatusLabel1,
            this.ToolStripStatusLblOperator,
            this.ToolStripStatusLblErrorMsg});
            this.StatusStrip1.Location = new System.Drawing.Point(0, 593);
            this.StatusStrip1.Name = "StatusStrip1";
            this.StatusStrip1.Size = new System.Drawing.Size(1002, 26);
            this.StatusStrip1.TabIndex = 3;
            this.StatusStrip1.Text = "StatusStrip1";
            // 
            // ToolLblNetStatus
            // 
            this.ToolLblNetStatus.Name = "ToolLblNetStatus";
            this.ToolLblNetStatus.Size = new System.Drawing.Size(32, 21);
            this.ToolLblNetStatus.Text = "网络";
            // 
            // ToolLblNetStatusValue
            // 
            this.ToolLblNetStatusValue.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.ToolLblNetStatusValue.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.ToolLblNetStatusValue.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.ToolLblNetStatusValue.ForeColor = System.Drawing.Color.Black;
            this.ToolLblNetStatusValue.Name = "ToolLblNetStatusValue";
            this.ToolLblNetStatusValue.Size = new System.Drawing.Size(50, 21);
            this.ToolLblNetStatusValue.Text = "Offline";
            // 
            // ToolStripStatusLabel1
            // 
            this.ToolStripStatusLabel1.Name = "ToolStripStatusLabel1";
            this.ToolStripStatusLabel1.Size = new System.Drawing.Size(44, 21);
            this.ToolStripStatusLabel1.Text = "操作人";
            // 
            // ToolStripStatusLblOperator
            // 
            this.ToolStripStatusLblOperator.BackColor = System.Drawing.Color.White;
            this.ToolStripStatusLblOperator.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.ToolStripStatusLblOperator.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.ToolStripStatusLblOperator.ForeColor = System.Drawing.Color.Blue;
            this.ToolStripStatusLblOperator.IsLink = true;
            this.ToolStripStatusLblOperator.Name = "ToolStripStatusLblOperator";
            this.ToolStripStatusLblOperator.Size = new System.Drawing.Size(27, 21);
            this.ToolStripStatusLblOperator.Text = "---";
            this.ToolStripStatusLblOperator.Click += new System.EventHandler(this.ToolStripStatusLblOperator_Click);
            // 
            // ToolStripStatusLblErrorMsg
            // 
            this.ToolStripStatusLblErrorMsg.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ToolStripStatusLblErrorMsg.Name = "ToolStripStatusLblErrorMsg";
            this.ToolStripStatusLblErrorMsg.Size = new System.Drawing.Size(773, 21);
            this.ToolStripStatusLblErrorMsg.Spring = true;
            this.ToolStripStatusLblErrorMsg.Text = "---";
            this.ToolStripStatusLblErrorMsg.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ToolStrip1
            // 
            this.ToolStrip1.BackColor = System.Drawing.Color.SteelBlue;
            this.ToolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripButSupplierReportReport,
            this.ToolStripSeparator1,
            this.ToolStripButSupplierScore,
            this.ToolStripSeparator2,
            this.ToolStripButSupplierPerformance,
            this.ToolStripSeparator3,
            this.ToolStripButSupplierTrend,
            this.ToolStripButCPKAnalysis});
            this.ToolStrip1.Location = new System.Drawing.Point(0, 0);
            this.ToolStrip1.Name = "ToolStrip1";
            this.ToolStrip1.Size = new System.Drawing.Size(1002, 25);
            this.ToolStrip1.TabIndex = 7;
            this.ToolStrip1.Text = "ToolStrip1";
            // 
            // ToolStripSeparator1
            // 
            this.ToolStripSeparator1.Name = "ToolStripSeparator1";
            this.ToolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // ToolStripSeparator2
            // 
            this.ToolStripSeparator2.Name = "ToolStripSeparator2";
            this.ToolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // userSupplierReportReport1
            // 
            this.userSupplierReportReport1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.userSupplierReportReport1.Location = new System.Drawing.Point(0, 25);
            this.userSupplierReportReport1.Name = "userSupplierReportReport1";
            this.userSupplierReportReport1.Size = new System.Drawing.Size(1002, 568);
            this.userSupplierReportReport1.TabIndex = 10;
            // 
            // userSupplierScore1
            // 
            this.userSupplierScore1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.userSupplierScore1.Location = new System.Drawing.Point(0, 25);
            this.userSupplierScore1.Name = "userSupplierScore1";
            this.userSupplierScore1.Size = new System.Drawing.Size(1002, 568);
            this.userSupplierScore1.TabIndex = 9;
            // 
            // userSupplierEvaluate1
            //
            this.userSupplierEvaluate1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.userSupplierEvaluate1.Location = new System.Drawing.Point(0, 25);
            this.userSupplierEvaluate1.Name = "userSupplierEvaluate1";
            this.userSupplierEvaluate1.Size = new System.Drawing.Size(1002, 568);
            this.userSupplierEvaluate1.TabIndex = 8;
            //
            // userSupplierTrend1
            //
            this.userSupplierTrend1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.userSupplierTrend1.Location = new System.Drawing.Point(0, 25);
            this.userSupplierTrend1.Name = "userSupplierTrend1";
            this.userSupplierTrend1.Size = new System.Drawing.Size(1002, 568);
            this.userSupplierTrend1.TabIndex = 11;
            //
            // userCPKAnalysis1
            //
            this.userCPKAnalysis1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.userCPKAnalysis1.Location = new System.Drawing.Point(0, 25);
            this.userCPKAnalysis1.Name = "userCPKAnalysis1";
            this.userCPKAnalysis1.Size = new System.Drawing.Size(1002, 568);
            this.userCPKAnalysis1.TabIndex = 12;
            // 
            // ToolStripButSupplierReportReport
            // 
            this.ToolStripButSupplierReportReport.Image = global::ZT.Cloud.SupplierReport.Properties.Resources.document_mark_as_final;
            this.ToolStripButSupplierReportReport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolStripButSupplierReportReport.Name = "ToolStripButSupplierReportReport";
            this.ToolStripButSupplierReportReport.Size = new System.Drawing.Size(98, 22);
            this.ToolStripButSupplierReportReport.Text = "SupplierReport入库检验";
            this.ToolStripButSupplierReportReport.Click += new System.EventHandler(this.ToolStripButSupplierReportReport_Click);
            // 
            // ToolStripButSupplierScore
            // 
            this.ToolStripButSupplierScore.Image = global::ZT.Cloud.SupplierReport.Properties.Resources.award_star_bronze_2;
            this.ToolStripButSupplierScore.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolStripButSupplierScore.Name = "ToolStripButSupplierScore";
            this.ToolStripButSupplierScore.Size = new System.Drawing.Size(112, 22);
            this.ToolStripButSupplierScore.Text = "供应商质量评分";
            this.ToolStripButSupplierScore.Click += new System.EventHandler(this.ToolStripButSupplierScore_Click);
            // 
            // ToolStripButSupplierPerformance
            // 
            this.ToolStripButSupplierPerformance.Image = global::ZT.Cloud.SupplierReport.Properties.Resources.chart_stock;
            this.ToolStripButSupplierPerformance.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolStripButSupplierPerformance.Name = "ToolStripButSupplierPerformance";
            this.ToolStripButSupplierPerformance.Size = new System.Drawing.Size(112, 22);
            this.ToolStripButSupplierPerformance.Text = "供应商质量表现";
            this.ToolStripButSupplierPerformance.Click += new System.EventHandler(this.ToolStripButSupplierPerformance_Click);
            //
            // ToolStripSeparator3
            //
            this.ToolStripSeparator3.Name = "ToolStripSeparator3";
            this.ToolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            //
            // ToolStripButSupplierTrend
            //
            this.ToolStripButSupplierTrend.Image = global::ZT.Cloud.SupplierReport.Properties.Resources.chart_stock;
            this.ToolStripButSupplierTrend.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolStripButSupplierTrend.Name = "ToolStripButSupplierTrend";
            this.ToolStripButSupplierTrend.Size = new System.Drawing.Size(136, 22);
            this.ToolStripButSupplierTrend.Text = "供应商月度趋势分析";
            this.ToolStripButSupplierTrend.Click += new System.EventHandler(this.ToolStripButSupplierTrend_Click);
            //
            // ToolStripButCPKAnalysis
            //
            this.ToolStripButCPKAnalysis.Image = global::ZT.Cloud.SupplierReport.Properties.Resources.chart_stock;
            this.ToolStripButCPKAnalysis.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolStripButCPKAnalysis.Name = "ToolStripButCPKAnalysis";
            this.ToolStripButCPKAnalysis.Size = new System.Drawing.Size(124, 22);
            this.ToolStripButCPKAnalysis.Text = "单尺寸CPK分析";
            this.ToolStripButCPKAnalysis.Click += new System.EventHandler(this.ToolStripButCPKAnalysis_Click);
            //
            // ToolStripDropDownButton1
            // 
            this.ToolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ConfigureToolStripMenuItem});
            this.ToolStripDropDownButton1.Image = global::ZT.Cloud.SupplierReport.Properties.Resources.Options;
            this.ToolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolStripDropDownButton1.Name = "ToolStripDropDownButton1";
            this.ToolStripDropDownButton1.Size = new System.Drawing.Size(61, 21);
            this.ToolStripDropDownButton1.Text = "选项";
            // 
            // ConfigureToolStripMenuItem
            // 
            this.ConfigureToolStripMenuItem.Image = global::ZT.Cloud.SupplierReport.Properties.Resources.gear_in;
            this.ConfigureToolStripMenuItem.Name = "ConfigureToolStripMenuItem";
            this.ConfigureToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.ConfigureToolStripMenuItem.Text = "设置";
            this.ConfigureToolStripMenuItem.Click += new System.EventHandler(this.ConfigureToolStripMenuItem_Click);
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.LightBlue;
            this.ClientSize = new System.Drawing.Size(1002, 619);
            this.Controls.Add(this.userSupplierReportReport1);
            this.Controls.Add(this.userSupplierScore1);
            this.Controls.Add(this.userSupplierEvaluate1);
            this.Controls.Add(this.userSupplierTrend1);
            this.Controls.Add(this.userCPKAnalysis1);
            this.Controls.Add(this.ToolStrip1);
            this.Controls.Add(this.StatusStrip1);
            this.Name = "FrmMain";
            this.Text = "主窗体";
            this.StatusStrip1.ResumeLayout(false);
            this.StatusStrip1.PerformLayout();
            this.ToolStrip1.ResumeLayout(false);
            this.ToolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal System.Windows.Forms.StatusStrip StatusStrip1;
        internal System.Windows.Forms.ToolStripDropDownButton ToolStripDropDownButton1;
        internal System.Windows.Forms.ToolStripMenuItem ConfigureToolStripMenuItem;
        internal System.Windows.Forms.ToolStripStatusLabel ToolLblNetStatus;
        internal System.Windows.Forms.ToolStripStatusLabel ToolLblNetStatusValue;
        internal System.Windows.Forms.ToolStripStatusLabel ToolStripStatusLabel1;
        internal System.Windows.Forms.ToolStripStatusLabel ToolStripStatusLblOperator;
        internal System.Windows.Forms.ToolStripStatusLabel ToolStripStatusLblErrorMsg;
        internal System.Windows.Forms.ToolStrip ToolStrip1;
        internal System.Windows.Forms.ToolStripButton ToolStripButSupplierReportReport;
        internal System.Windows.Forms.ToolStripSeparator ToolStripSeparator1;
        internal System.Windows.Forms.ToolStripButton ToolStripButSupplierScore;
        internal System.Windows.Forms.ToolStripSeparator ToolStripSeparator2;
        internal System.Windows.Forms.ToolStripButton ToolStripButSupplierPerformance;
        private SupplierReport.Views.UserSupplierEvaluate userSupplierEvaluate1;
        private SupplierReport.Views.UserSupplierScore userSupplierScore1;
        private SupplierReport.Views.UserSupplierReportReport userSupplierReportReport1;
        private SupplierReport.Views.UserSupplierTrend userSupplierTrend1;
        private SupplierReport.Views.UserCPKAnalysis userCPKAnalysis1;
        internal System.Windows.Forms.ToolStripButton ToolStripButSupplierTrend;
        internal System.Windows.Forms.ToolStripButton ToolStripButCPKAnalysis;
        internal System.Windows.Forms.ToolStripSeparator ToolStripSeparator3;
    }
}

namespace ZT.Cloud.SupplierReport.Views
{
    partial class UserCPKAnalysis
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        private void InitializeComponent()
        {
            this.WebView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.ToolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btn_upload_data = new System.Windows.Forms.ToolStripButton();
            this.ToolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.ToolStripLblDataStatus = new System.Windows.Forms.ToolStripLabel();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.ToolStripStatusLblErrorMsg = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)(this.WebView21)).BeginInit();
            this.ToolStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            //
            // WebView21
            //
            this.WebView21.AllowExternalDrop = true;
            this.WebView21.CreationProperties = null;
            this.WebView21.DefaultBackgroundColor = System.Drawing.Color.White;
            this.WebView21.Dock = System.Windows.Forms.DockStyle.Fill;
            this.WebView21.Location = new System.Drawing.Point(0, 25);
            this.WebView21.Name = "WebView21";
            this.WebView21.Size = new System.Drawing.Size(931, 666);
            this.WebView21.TabIndex = 17;
            this.WebView21.ZoomFactor = 1D;
            //
            // ToolStrip1
            //
            this.ToolStrip1.BackColor = System.Drawing.Color.LightBlue;
            this.ToolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btn_upload_data,
            this.ToolStripSeparator1,
            this.ToolStripLblDataStatus});
            this.ToolStrip1.Location = new System.Drawing.Point(0, 0);
            this.ToolStrip1.Name = "ToolStrip1";
            this.ToolStrip1.Size = new System.Drawing.Size(931, 25);
            this.ToolStrip1.TabIndex = 18;
            //
            // btn_upload_data
            //
            this.btn_upload_data.Image = global::ZT.Cloud.SupplierReport.Properties.Resources.file_extension_xls;
            this.btn_upload_data.Name = "btn_upload_data";
            this.btn_upload_data.Size = new System.Drawing.Size(112, 22);
            this.btn_upload_data.Text = "导入数据文件";
            this.btn_upload_data.Click += new System.EventHandler(this.btn_upload_data_Click);
            //
            // ToolStripSeparator1
            //
            this.ToolStripSeparator1.Name = "ToolStripSeparator1";
            this.ToolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            //
            // ToolStripLblDataStatus
            //
            this.ToolStripLblDataStatus.Name = "ToolStripLblDataStatus";
            this.ToolStripLblDataStatus.Size = new System.Drawing.Size(23, 22);
            this.ToolStripLblDataStatus.Text = "---";
            //
            // statusStrip1
            //
            this.statusStrip1.BackColor = System.Drawing.Color.LightBlue;
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripStatusLblErrorMsg});
            this.statusStrip1.Location = new System.Drawing.Point(0, 669);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(931, 22);
            this.statusStrip1.TabIndex = 19;
            //
            // ToolStripStatusLblErrorMsg
            //
            this.ToolStripStatusLblErrorMsg.BackColor = System.Drawing.Color.White;
            this.ToolStripStatusLblErrorMsg.Name = "ToolStripStatusLblErrorMsg";
            this.ToolStripStatusLblErrorMsg.Size = new System.Drawing.Size(885, 17);
            this.ToolStripStatusLblErrorMsg.Spring = true;
            this.ToolStripStatusLblErrorMsg.Text = "---";
            //
            // UserCPKAnalysis
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.WebView21);
            this.Controls.Add(this.ToolStrip1);
            this.Name = "UserCPKAnalysis";
            this.Size = new System.Drawing.Size(931, 691);
            ((System.ComponentModel.ISupportInitialize)(this.WebView21)).EndInit();
            this.ToolStrip1.ResumeLayout(false);
            this.ToolStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal Microsoft.Web.WebView2.WinForms.WebView2 WebView21;
        internal System.Windows.Forms.ToolStrip ToolStrip1;
        internal System.Windows.Forms.ToolStripButton btn_upload_data;
        internal System.Windows.Forms.ToolStripSeparator ToolStripSeparator1;
        internal System.Windows.Forms.ToolStripLabel ToolStripLblDataStatus;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel ToolStripStatusLblErrorMsg;
    }
}

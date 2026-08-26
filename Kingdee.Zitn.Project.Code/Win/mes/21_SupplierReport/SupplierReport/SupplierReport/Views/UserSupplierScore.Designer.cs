namespace ZT.Cloud.SupplierReport.Views
{
    partial class UserSupplierScore
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

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.WebView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.ToolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btn_upload_inspection = new System.Windows.Forms.ToolStripButton();
            this.btn_upload_supplier = new System.Windows.Forms.ToolStripButton();
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
            this.WebView21.Size = new System.Drawing.Size(888, 651);
            this.WebView21.TabIndex = 15;
            this.WebView21.ZoomFactor = 1D;
            // 
            // ToolStrip1
            // 
            this.ToolStrip1.BackColor = System.Drawing.Color.LightBlue;
            this.ToolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btn_upload_inspection,
            this.btn_upload_supplier,
            this.ToolStripSeparator1,
            this.ToolStripLblDataStatus});
            this.ToolStrip1.Location = new System.Drawing.Point(0, 0);
            this.ToolStrip1.Name = "ToolStrip1";
            this.ToolStrip1.Size = new System.Drawing.Size(888, 25);
            this.ToolStrip1.TabIndex = 19;
            this.ToolStrip1.Text = "ToolStrip1";
            // 
            // btn_upload_inspection
            // 
            this.btn_upload_inspection.Image = global::ZT.Cloud.SupplierReport.Properties.Resources.file_extension_xls;
            this.btn_upload_inspection.Name = "btn_upload_inspection";
            this.btn_upload_inspection.Size = new System.Drawing.Size(112, 22);
            this.btn_upload_inspection.Text = "上传月度来料表";
            this.btn_upload_inspection.Click += new System.EventHandler(this.btn_shangchuan_Click);
            // 
            // btn_upload_supplier
            // 
            this.btn_upload_supplier.Image = global::ZT.Cloud.SupplierReport.Properties.Resources.file_extension_xls;
            this.btn_upload_supplier.Name = "btn_upload_supplier";
            this.btn_upload_supplier.Size = new System.Drawing.Size(124, 22);
            this.btn_upload_supplier.Text = "上传供应商对应表";
            this.btn_upload_supplier.Click += new System.EventHandler(this.btn_shangchuan_supplier_Click);
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
            this.statusStrip1.Location = new System.Drawing.Point(0, 654);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(888, 22);
            this.statusStrip1.TabIndex = 20;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // ToolStripStatusLblErrorMsg
            // 
            this.ToolStripStatusLblErrorMsg.BackColor = System.Drawing.Color.White;
            this.ToolStripStatusLblErrorMsg.Name = "ToolStripStatusLblErrorMsg";
            this.ToolStripStatusLblErrorMsg.Size = new System.Drawing.Size(842, 17);
            this.ToolStripStatusLblErrorMsg.Spring = true;
            this.ToolStripStatusLblErrorMsg.Text = "---";
            // 
            // UserSupplierScore
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.WebView21);
            this.Controls.Add(this.ToolStrip1);
            this.Name = "UserSupplierScore";
            this.Size = new System.Drawing.Size(888, 676);
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
        internal System.Windows.Forms.ToolStripButton btn_upload_inspection;
        internal System.Windows.Forms.ToolStripButton btn_upload_supplier;
        internal System.Windows.Forms.ToolStripSeparator ToolStripSeparator1;
        internal System.Windows.Forms.ToolStripLabel ToolStripLblDataStatus;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel ToolStripStatusLblErrorMsg;
    }
}

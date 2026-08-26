
namespace ZT.Cloud.MesModel.Views
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            this.StatusStrip1 = new System.Windows.Forms.StatusStrip();
            this.ToolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.ConfigureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolLblNetStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.ToolLblNetStatusValue = new System.Windows.Forms.ToolStripStatusLabel();
            this.ToolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.ToolStripStatusLblOperator = new System.Windows.Forms.ToolStripStatusLabel();
            this.ToolStripStatusLblErrorMsg = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButMainPlan = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButMainDevice = new System.Windows.Forms.ToolStripButton();
            this.userDevices1 = new ZT.Cloud.PEManager.Views.UserDevices();
            this.userMaintPlan1 = new ZT.Cloud.PEManager.Views.UserMaintPlan();
            this.StatusStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
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
            this.StatusStrip1.Location = new System.Drawing.Point(0, 689);
            this.StatusStrip1.Name = "StatusStrip1";
            this.StatusStrip1.Size = new System.Drawing.Size(1046, 26);
            this.StatusStrip1.TabIndex = 3;
            this.StatusStrip1.Text = "StatusStrip1";
            // 
            // ToolStripDropDownButton1
            // 
            this.ToolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ConfigureToolStripMenuItem});
            this.ToolStripDropDownButton1.Image = global::ZT.Cloud.PEManager.Properties.Resources.Options;
            this.ToolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolStripDropDownButton1.Name = "ToolStripDropDownButton1";
            this.ToolStripDropDownButton1.Size = new System.Drawing.Size(61, 24);
            this.ToolStripDropDownButton1.Text = "选项";
            // 
            // ConfigureToolStripMenuItem
            // 
            this.ConfigureToolStripMenuItem.Image = global::ZT.Cloud.PEManager.Properties.Resources.gear_in;
            this.ConfigureToolStripMenuItem.Name = "ConfigureToolStripMenuItem";
            this.ConfigureToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.ConfigureToolStripMenuItem.Text = "设置";
            this.ConfigureToolStripMenuItem.Click += new System.EventHandler(this.ConfigureToolStripMenuItem_Click);
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
            this.ToolStripStatusLblErrorMsg.Size = new System.Drawing.Size(817, 21);
            this.ToolStripStatusLblErrorMsg.Spring = true;
            this.ToolStripStatusLblErrorMsg.Text = "---";
            this.ToolStripStatusLblErrorMsg.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // toolStrip1
            // 
            this.toolStrip1.BackColor = System.Drawing.Color.SteelBlue;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButMainPlan,
            this.toolStripButton2,
            this.toolStripButMainDevice});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1046, 25);
            this.toolStrip1.TabIndex = 7;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButMainPlan
            // 
            this.toolStripButMainPlan.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButMainPlan.Image")));
            this.toolStripButMainPlan.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButMainPlan.Name = "toolStripButMainPlan";
            this.toolStripButMainPlan.Size = new System.Drawing.Size(100, 22);
            this.toolStripButMainPlan.Text = "设备保养计划";
            this.toolStripButMainPlan.Click += new System.EventHandler(this.toolStripButMainPlan_Click);
            // 
            // toolStripButton2
            // 
            this.toolStripButton2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
            this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton2.Name = "toolStripButton2";
            this.toolStripButton2.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton2.Text = "引入文件";
            // 
            // toolStripButMainDevice
            // 
            this.toolStripButMainDevice.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButMainDevice.Image")));
            this.toolStripButMainDevice.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButMainDevice.Name = "toolStripButMainDevice";
            this.toolStripButMainDevice.Size = new System.Drawing.Size(100, 22);
            this.toolStripButMainDevice.Text = "设备管理台账";
            this.toolStripButMainDevice.Click += new System.EventHandler(this.toolStripButMainDevice_Click);
            // 
            // userDevices1
            // 
            this.userDevices1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.userDevices1.Location = new System.Drawing.Point(0, 25);
            this.userDevices1.Name = "userDevices1";
            this.userDevices1.Size = new System.Drawing.Size(1046, 664);
            this.userDevices1.TabIndex = 9;
            // 
            // userMaintPlan1
            // 
            this.userMaintPlan1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.userMaintPlan1.Location = new System.Drawing.Point(0, 25);
            this.userMaintPlan1.Name = "userMaintPlan1";
            this.userMaintPlan1.Size = new System.Drawing.Size(1046, 664);
            this.userMaintPlan1.TabIndex = 8;
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1046, 715);
            this.Controls.Add(this.userDevices1);
            this.Controls.Add(this.userMaintPlan1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.StatusStrip1);
            this.Name = "FrmMain";
            this.Text = "主窗体";
            this.StatusStrip1.ResumeLayout(false);
            this.StatusStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
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
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButMainPlan;
        private System.Windows.Forms.ToolStripButton toolStripButton2;
        private System.Windows.Forms.ToolStripButton toolStripButMainDevice;
        private PEManager.Views.UserMaintPlan userMaintPlan1;
        private PEManager.Views.UserDevices userDevices1;
    }
}

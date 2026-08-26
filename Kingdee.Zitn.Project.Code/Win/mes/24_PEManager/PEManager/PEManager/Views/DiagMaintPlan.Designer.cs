namespace ZT.Cloud.MesModel.Views
{
    partial class DiagMaintPlan
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

        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblName = new System.Windows.Forms.Label();
            this.lblFileNo = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.lblQty = new System.Windows.Forms.Label();
            this.lblCycle = new System.Windows.Forms.Label();
            this.txtEquipmentName = new System.Windows.Forms.TextBox();
            this.txtFileNo = new System.Windows.Forms.TextBox();
            this.txtVersion = new System.Windows.Forms.TextBox();
            this.txtQuantity = new System.Windows.Forms.TextBox();
            this.txtMaintCycle = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lblJan = new System.Windows.Forms.Label();
            this.txtJan = new System.Windows.Forms.TextBox();
            this.lblFeb = new System.Windows.Forms.Label();
            this.txtFeb = new System.Windows.Forms.TextBox();
            this.lblMar = new System.Windows.Forms.Label();
            this.txtMar = new System.Windows.Forms.TextBox();
            this.lblApr = new System.Windows.Forms.Label();
            this.txtApr = new System.Windows.Forms.TextBox();
            this.lblMay = new System.Windows.Forms.Label();
            this.txtMay = new System.Windows.Forms.TextBox();
            this.lblJun = new System.Windows.Forms.Label();
            this.txtJun = new System.Windows.Forms.TextBox();
            this.lblJul = new System.Windows.Forms.Label();
            this.txtJul = new System.Windows.Forms.TextBox();
            this.lblAug = new System.Windows.Forms.Label();
            this.txtAug = new System.Windows.Forms.TextBox();
            this.lblSep = new System.Windows.Forms.Label();
            this.txtSep = new System.Windows.Forms.TextBox();
            this.lblOct = new System.Windows.Forms.Label();
            this.txtOct = new System.Windows.Forms.TextBox();
            this.lblNov = new System.Windows.Forms.Label();
            this.txtNov = new System.Windows.Forms.TextBox();
            this.lblDec = new System.Windows.Forms.Label();
            this.txtDec = new System.Windows.Forms.TextBox();
            this.OK_Button = new System.Windows.Forms.Button();
            this.Cancel_Button = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            //
            // groupBox1
            //
            this.groupBox1.BackColor = System.Drawing.Color.Transparent;
            this.groupBox1.Controls.Add(this.lblName);
            this.groupBox1.Controls.Add(this.txtEquipmentName);
            this.groupBox1.Controls.Add(this.lblVersion);
            this.groupBox1.Controls.Add(this.txtVersion);
            this.groupBox1.Controls.Add(this.lblFileNo);
            this.groupBox1.Controls.Add(this.txtFileNo);
            this.groupBox1.Controls.Add(this.lblQty);
            this.groupBox1.Controls.Add(this.txtQuantity);
            this.groupBox1.Controls.Add(this.lblCycle);
            this.groupBox1.Controls.Add(this.txtMaintCycle);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.groupBox1.Location = new System.Drawing.Point(10, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(565, 130);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "基本信息";
            //
            // lblName
            //
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(14, 25);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(56, 17);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "设备名称:";
            //
            // txtEquipmentName
            //
            this.txtEquipmentName.Location = new System.Drawing.Point(80, 22);
            this.txtEquipmentName.Name = "txtEquipmentName";
            this.txtEquipmentName.Size = new System.Drawing.Size(180, 23);
            this.txtEquipmentName.TabIndex = 1;
            //
            // lblVersion
            //
            this.lblVersion.AutoSize = true;
            this.lblVersion.Location = new System.Drawing.Point(280, 25);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(35, 17);
            this.lblVersion.TabIndex = 2;
            this.lblVersion.Text = "版次:";
            //
            // txtVersion
            //
            this.txtVersion.Location = new System.Drawing.Point(320, 22);
            this.txtVersion.Name = "txtVersion";
            this.txtVersion.Size = new System.Drawing.Size(70, 23);
            this.txtVersion.TabIndex = 3;
            //
            // lblFileNo
            //
            this.lblFileNo.AutoSize = true;
            this.lblFileNo.Location = new System.Drawing.Point(14, 56);
            this.lblFileNo.Name = "lblFileNo";
            this.lblFileNo.Size = new System.Drawing.Size(56, 17);
            this.lblFileNo.TabIndex = 4;
            this.lblFileNo.Text = "文件编号:";
            //
            // txtFileNo
            //
            this.txtFileNo.Location = new System.Drawing.Point(80, 53);
            this.txtFileNo.Name = "txtFileNo";
            this.txtFileNo.Size = new System.Drawing.Size(180, 23);
            this.txtFileNo.TabIndex = 5;
            //
            // lblQty
            //
            this.lblQty.AutoSize = true;
            this.lblQty.Location = new System.Drawing.Point(280, 56);
            this.lblQty.Name = "lblQty";
            this.lblQty.Size = new System.Drawing.Size(35, 17);
            this.lblQty.TabIndex = 6;
            this.lblQty.Text = "数量:";
            //
            // txtQuantity
            //
            this.txtQuantity.Location = new System.Drawing.Point(320, 53);
            this.txtQuantity.Name = "txtQuantity";
            this.txtQuantity.Size = new System.Drawing.Size(70, 23);
            this.txtQuantity.TabIndex = 7;
            //
            // lblCycle
            //
            this.lblCycle.AutoSize = true;
            this.lblCycle.Location = new System.Drawing.Point(14, 88);
            this.lblCycle.Name = "lblCycle";
            this.lblCycle.Size = new System.Drawing.Size(56, 17);
            this.lblCycle.TabIndex = 8;
            this.lblCycle.Text = "维护周期:";
            //
            // txtMaintCycle
            //
            this.txtMaintCycle.Location = new System.Drawing.Point(80, 85);
            this.txtMaintCycle.Multiline = true;
            this.txtMaintCycle.Name = "txtMaintCycle";
            this.txtMaintCycle.Size = new System.Drawing.Size(470, 35);
            this.txtMaintCycle.TabIndex = 9;
            //
            // groupBox2
            //
            this.groupBox2.BackColor = System.Drawing.Color.Transparent;
            this.groupBox2.Controls.Add(this.lblJan);
            this.groupBox2.Controls.Add(this.txtJan);
            this.groupBox2.Controls.Add(this.lblFeb);
            this.groupBox2.Controls.Add(this.txtFeb);
            this.groupBox2.Controls.Add(this.lblMar);
            this.groupBox2.Controls.Add(this.txtMar);
            this.groupBox2.Controls.Add(this.lblApr);
            this.groupBox2.Controls.Add(this.txtApr);
            this.groupBox2.Controls.Add(this.lblMay);
            this.groupBox2.Controls.Add(this.txtMay);
            this.groupBox2.Controls.Add(this.lblJun);
            this.groupBox2.Controls.Add(this.txtJun);
            this.groupBox2.Controls.Add(this.lblJul);
            this.groupBox2.Controls.Add(this.txtJul);
            this.groupBox2.Controls.Add(this.lblAug);
            this.groupBox2.Controls.Add(this.txtAug);
            this.groupBox2.Controls.Add(this.lblSep);
            this.groupBox2.Controls.Add(this.txtSep);
            this.groupBox2.Controls.Add(this.lblOct);
            this.groupBox2.Controls.Add(this.txtOct);
            this.groupBox2.Controls.Add(this.lblNov);
            this.groupBox2.Controls.Add(this.txtNov);
            this.groupBox2.Controls.Add(this.lblDec);
            this.groupBox2.Controls.Add(this.txtDec);
            this.groupBox2.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.groupBox2.Location = new System.Drawing.Point(10, 142);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(565, 110);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "月度保养计划（填写每月保养类型: 月/季/半年/年）";
            //
            // Row 1: 一月~四月 (y=22)
            //
            this.lblJan.AutoSize = true;
            this.lblJan.Location = new System.Drawing.Point(14, 25);
            this.lblJan.Name = "lblJan";
            this.lblJan.Size = new System.Drawing.Size(32, 17);
            this.lblJan.TabIndex = 0;
            this.lblJan.Text = "一月:";
            //
            this.txtJan.Location = new System.Drawing.Point(50, 22);
            this.txtJan.Name = "txtJan";
            this.txtJan.Size = new System.Drawing.Size(75, 23);
            this.txtJan.TabIndex = 10;
            //
            this.lblFeb.AutoSize = true;
            this.lblFeb.Location = new System.Drawing.Point(142, 25);
            this.lblFeb.Name = "lblFeb";
            this.lblFeb.Size = new System.Drawing.Size(32, 17);
            this.lblFeb.TabIndex = 2;
            this.lblFeb.Text = "二月:";
            //
            this.txtFeb.Location = new System.Drawing.Point(178, 22);
            this.txtFeb.Name = "txtFeb";
            this.txtFeb.Size = new System.Drawing.Size(75, 23);
            this.txtFeb.TabIndex = 11;
            //
            this.lblMar.AutoSize = true;
            this.lblMar.Location = new System.Drawing.Point(270, 25);
            this.lblMar.Name = "lblMar";
            this.lblMar.Size = new System.Drawing.Size(32, 17);
            this.lblMar.TabIndex = 4;
            this.lblMar.Text = "三月:";
            //
            this.txtMar.Location = new System.Drawing.Point(306, 22);
            this.txtMar.Name = "txtMar";
            this.txtMar.Size = new System.Drawing.Size(75, 23);
            this.txtMar.TabIndex = 12;
            //
            this.lblApr.AutoSize = true;
            this.lblApr.Location = new System.Drawing.Point(398, 25);
            this.lblApr.Name = "lblApr";
            this.lblApr.Size = new System.Drawing.Size(32, 17);
            this.lblApr.TabIndex = 6;
            this.lblApr.Text = "四月:";
            //
            this.txtApr.Location = new System.Drawing.Point(434, 22);
            this.txtApr.Name = "txtApr";
            this.txtApr.Size = new System.Drawing.Size(75, 23);
            this.txtApr.TabIndex = 13;
            //
            // Row 2: 五月~八月 (y=52)
            //
            this.lblMay.AutoSize = true;
            this.lblMay.Location = new System.Drawing.Point(14, 55);
            this.lblMay.Name = "lblMay";
            this.lblMay.Size = new System.Drawing.Size(32, 17);
            this.lblMay.TabIndex = 8;
            this.lblMay.Text = "五月:";
            //
            this.txtMay.Location = new System.Drawing.Point(50, 52);
            this.txtMay.Name = "txtMay";
            this.txtMay.Size = new System.Drawing.Size(75, 23);
            this.txtMay.TabIndex = 14;
            //
            this.lblJun.AutoSize = true;
            this.lblJun.Location = new System.Drawing.Point(142, 55);
            this.lblJun.Name = "lblJun";
            this.lblJun.Size = new System.Drawing.Size(32, 17);
            this.lblJun.TabIndex = 10;
            this.lblJun.Text = "六月:";
            //
            this.txtJun.Location = new System.Drawing.Point(178, 52);
            this.txtJun.Name = "txtJun";
            this.txtJun.Size = new System.Drawing.Size(75, 23);
            this.txtJun.TabIndex = 15;
            //
            this.lblJul.AutoSize = true;
            this.lblJul.Location = new System.Drawing.Point(270, 55);
            this.lblJul.Name = "lblJul";
            this.lblJul.Size = new System.Drawing.Size(32, 17);
            this.lblJul.TabIndex = 12;
            this.lblJul.Text = "七月:";
            //
            this.txtJul.Location = new System.Drawing.Point(306, 52);
            this.txtJul.Name = "txtJul";
            this.txtJul.Size = new System.Drawing.Size(75, 23);
            this.txtJul.TabIndex = 16;
            //
            this.lblAug.AutoSize = true;
            this.lblAug.Location = new System.Drawing.Point(398, 55);
            this.lblAug.Name = "lblAug";
            this.lblAug.Size = new System.Drawing.Size(32, 17);
            this.lblAug.TabIndex = 14;
            this.lblAug.Text = "八月:";
            //
            this.txtAug.Location = new System.Drawing.Point(434, 52);
            this.txtAug.Name = "txtAug";
            this.txtAug.Size = new System.Drawing.Size(75, 23);
            this.txtAug.TabIndex = 17;
            //
            // Row 3: 九月~十二月 (y=82)
            //
            this.lblSep.AutoSize = true;
            this.lblSep.Location = new System.Drawing.Point(14, 85);
            this.lblSep.Name = "lblSep";
            this.lblSep.Size = new System.Drawing.Size(32, 17);
            this.lblSep.TabIndex = 16;
            this.lblSep.Text = "九月:";
            //
            this.txtSep.Location = new System.Drawing.Point(50, 82);
            this.txtSep.Name = "txtSep";
            this.txtSep.Size = new System.Drawing.Size(75, 23);
            this.txtSep.TabIndex = 18;
            //
            this.lblOct.AutoSize = true;
            this.lblOct.Location = new System.Drawing.Point(142, 85);
            this.lblOct.Name = "lblOct";
            this.lblOct.Size = new System.Drawing.Size(32, 17);
            this.lblOct.TabIndex = 18;
            this.lblOct.Text = "十月:";
            //
            this.txtOct.Location = new System.Drawing.Point(178, 82);
            this.txtOct.Name = "txtOct";
            this.txtOct.Size = new System.Drawing.Size(75, 23);
            this.txtOct.TabIndex = 19;
            //
            this.lblNov.AutoSize = true;
            this.lblNov.Location = new System.Drawing.Point(270, 85);
            this.lblNov.Name = "lblNov";
            this.lblNov.Size = new System.Drawing.Size(44, 17);
            this.lblNov.TabIndex = 20;
            this.lblNov.Text = "十一月:";
            //
            this.txtNov.Location = new System.Drawing.Point(318, 82);
            this.txtNov.Name = "txtNov";
            this.txtNov.Size = new System.Drawing.Size(75, 23);
            this.txtNov.TabIndex = 20;
            //
            this.lblDec.AutoSize = true;
            this.lblDec.Location = new System.Drawing.Point(410, 85);
            this.lblDec.Name = "lblDec";
            this.lblDec.Size = new System.Drawing.Size(44, 17);
            this.lblDec.TabIndex = 22;
            this.lblDec.Text = "十二月:";
            //
            this.txtDec.Location = new System.Drawing.Point(458, 82);
            this.txtDec.Name = "txtDec";
            this.txtDec.Size = new System.Drawing.Size(75, 23);
            this.txtDec.TabIndex = 21;
            //
            // OK_Button
            //
            this.OK_Button.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.OK_Button.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.OK_Button.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.OK_Button.Location = new System.Drawing.Point(428, 262);
            this.OK_Button.Name = "OK_Button";
            this.OK_Button.Size = new System.Drawing.Size(70, 28);
            this.OK_Button.TabIndex = 22;
            this.OK_Button.Text = "确定";
            this.OK_Button.UseVisualStyleBackColor = false;
            this.OK_Button.Click += new System.EventHandler(this.OK_Button_Click);
            //
            // Cancel_Button
            //
            this.Cancel_Button.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.Cancel_Button.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.Cancel_Button.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel_Button.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.Cancel_Button.Location = new System.Drawing.Point(505, 262);
            this.Cancel_Button.Name = "Cancel_Button";
            this.Cancel_Button.Size = new System.Drawing.Size(70, 28);
            this.Cancel_Button.TabIndex = 23;
            this.Cancel_Button.Text = "取消";
            this.Cancel_Button.UseVisualStyleBackColor = false;
            this.Cancel_Button.Click += new System.EventHandler(this.Cancel_Button_Click);
            //
            // DiagMaintPlan
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.LightBlue;
            this.ClientSize = new System.Drawing.Size(585, 300);
            this.Controls.Add(this.Cancel_Button);
            this.Controls.Add(this.OK_Button);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DiagMaintPlan";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "设备保养计划";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtEquipmentName;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.TextBox txtVersion;
        private System.Windows.Forms.Label lblFileNo;
        private System.Windows.Forms.TextBox txtFileNo;
        private System.Windows.Forms.Label lblQty;
        private System.Windows.Forms.TextBox txtQuantity;
        private System.Windows.Forms.Label lblCycle;
        private System.Windows.Forms.TextBox txtMaintCycle;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label lblJan;
        private System.Windows.Forms.TextBox txtJan;
        private System.Windows.Forms.Label lblFeb;
        private System.Windows.Forms.TextBox txtFeb;
        private System.Windows.Forms.Label lblMar;
        private System.Windows.Forms.TextBox txtMar;
        private System.Windows.Forms.Label lblApr;
        private System.Windows.Forms.TextBox txtApr;
        private System.Windows.Forms.Label lblMay;
        private System.Windows.Forms.TextBox txtMay;
        private System.Windows.Forms.Label lblJun;
        private System.Windows.Forms.TextBox txtJun;
        private System.Windows.Forms.Label lblJul;
        private System.Windows.Forms.TextBox txtJul;
        private System.Windows.Forms.Label lblAug;
        private System.Windows.Forms.TextBox txtAug;
        private System.Windows.Forms.Label lblSep;
        private System.Windows.Forms.TextBox txtSep;
        private System.Windows.Forms.Label lblOct;
        private System.Windows.Forms.TextBox txtOct;
        private System.Windows.Forms.Label lblNov;
        private System.Windows.Forms.TextBox txtNov;
        private System.Windows.Forms.Label lblDec;
        private System.Windows.Forms.TextBox txtDec;
        private System.Windows.Forms.Button OK_Button;
        private System.Windows.Forms.Button Cancel_Button;
    }
}

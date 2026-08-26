namespace ZT.Cloud.MesModel.Views
{
    partial class DiagDevice
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
            this.lblDeviceCode = new System.Windows.Forms.Label();
            this.txtDeviceCode = new System.Windows.Forms.TextBox();
            this.lblDeviceName = new System.Windows.Forms.Label();
            this.txtDeviceName = new System.Windows.Forms.TextBox();
            this.lblSpec = new System.Windows.Forms.Label();
            this.txtSpecification = new System.Windows.Forms.TextBox();
            this.lblBrand = new System.Windows.Forms.Label();
            this.txtBrand = new System.Windows.Forms.TextBox();
            this.lblDeviceStatus = new System.Windows.Forms.Label();
            this.txtDeviceStatus = new System.Windows.Forms.TextBox();
            this.lblUsageScope = new System.Windows.Forms.Label();
            this.txtUsageScope = new System.Windows.Forms.TextBox();
            this.lblUsageCategory = new System.Windows.Forms.Label();
            this.txtUsageCategory = new System.Windows.Forms.TextBox();
            this.lblCurrentDept = new System.Windows.Forms.Label();
            this.txtCurrentDept = new System.Windows.Forms.TextBox();
            this.lblCurrentLocation = new System.Windows.Forms.Label();
            this.txtCurrentLocation = new System.Windows.Forms.TextBox();
            this.lblCurrentPerson = new System.Windows.Forms.Label();
            this.txtCurrentPerson = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lblCalDate = new System.Windows.Forms.Label();
            this.txtCalibrationDate = new System.Windows.Forms.TextBox();
            this.lblNextCalDate = new System.Windows.Forms.Label();
            this.txtNextCalibrationDate = new System.Windows.Forms.TextBox();
            this.lblQualDate = new System.Windows.Forms.Label();
            this.txtQualificationDate = new System.Windows.Forms.TextBox();
            this.OK_Button = new System.Windows.Forms.Button();
            this.Cancel_Button = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            //
            // groupBox1
            //
            this.groupBox1.BackColor = System.Drawing.Color.Transparent;
            this.groupBox1.Controls.Add(this.lblDeviceCode);
            this.groupBox1.Controls.Add(this.txtDeviceCode);
            this.groupBox1.Controls.Add(this.lblDeviceName);
            this.groupBox1.Controls.Add(this.txtDeviceName);
            this.groupBox1.Controls.Add(this.lblSpec);
            this.groupBox1.Controls.Add(this.txtSpecification);
            this.groupBox1.Controls.Add(this.lblBrand);
            this.groupBox1.Controls.Add(this.txtBrand);
            this.groupBox1.Controls.Add(this.lblDeviceStatus);
            this.groupBox1.Controls.Add(this.txtDeviceStatus);
            this.groupBox1.Controls.Add(this.lblUsageScope);
            this.groupBox1.Controls.Add(this.txtUsageScope);
            this.groupBox1.Controls.Add(this.lblUsageCategory);
            this.groupBox1.Controls.Add(this.txtUsageCategory);
            this.groupBox1.Controls.Add(this.lblCurrentDept);
            this.groupBox1.Controls.Add(this.txtCurrentDept);
            this.groupBox1.Controls.Add(this.lblCurrentLocation);
            this.groupBox1.Controls.Add(this.txtCurrentLocation);
            this.groupBox1.Controls.Add(this.lblCurrentPerson);
            this.groupBox1.Controls.Add(this.txtCurrentPerson);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.groupBox1.Location = new System.Drawing.Point(10, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(620, 160);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "基本信息";
            //
            // Row 1
            //
            this.lblDeviceCode.AutoSize = true;
            this.lblDeviceCode.Location = new System.Drawing.Point(14, 25);
            this.lblDeviceCode.Name = "lblDeviceCode";
            this.lblDeviceCode.Size = new System.Drawing.Size(56, 17);
            this.lblDeviceCode.TabIndex = 0;
            this.lblDeviceCode.Text = "设备编号:";
            //
            this.txtDeviceCode.Location = new System.Drawing.Point(80, 22);
            this.txtDeviceCode.Name = "txtDeviceCode";
            this.txtDeviceCode.Size = new System.Drawing.Size(140, 23);
            this.txtDeviceCode.TabIndex = 1;
            //
            this.lblDeviceName.AutoSize = true;
            this.lblDeviceName.Location = new System.Drawing.Point(240, 25);
            this.lblDeviceName.Name = "lblDeviceName";
            this.lblDeviceName.Size = new System.Drawing.Size(56, 17);
            this.lblDeviceName.TabIndex = 2;
            this.lblDeviceName.Text = "设备名称:";
            //
            this.txtDeviceName.Location = new System.Drawing.Point(306, 22);
            this.txtDeviceName.Name = "txtDeviceName";
            this.txtDeviceName.Size = new System.Drawing.Size(140, 23);
            this.txtDeviceName.TabIndex = 2;
            //
            this.lblSpec.AutoSize = true;
            this.lblSpec.Location = new System.Drawing.Point(465, 25);
            this.lblSpec.Name = "lblSpec";
            this.lblSpec.Size = new System.Drawing.Size(35, 17);
            this.lblSpec.TabIndex = 4;
            this.lblSpec.Text = "规格:";
            //
            this.txtSpecification.Location = new System.Drawing.Point(505, 22);
            this.txtSpecification.Name = "txtSpecification";
            this.txtSpecification.Size = new System.Drawing.Size(100, 23);
            this.txtSpecification.TabIndex = 3;
            //
            // Row 2
            //
            this.lblBrand.AutoSize = true;
            this.lblBrand.Location = new System.Drawing.Point(14, 56);
            this.lblBrand.Name = "lblBrand";
            this.lblBrand.Size = new System.Drawing.Size(35, 17);
            this.lblBrand.TabIndex = 6;
            this.lblBrand.Text = "品牌:";
            //
            this.txtBrand.Location = new System.Drawing.Point(80, 53);
            this.txtBrand.Name = "txtBrand";
            this.txtBrand.Size = new System.Drawing.Size(140, 23);
            this.txtBrand.TabIndex = 4;
            //
            this.lblDeviceStatus.AutoSize = true;
            this.lblDeviceStatus.Location = new System.Drawing.Point(240, 56);
            this.lblDeviceStatus.Name = "lblDeviceStatus";
            this.lblDeviceStatus.Size = new System.Drawing.Size(56, 17);
            this.lblDeviceStatus.TabIndex = 8;
            this.lblDeviceStatus.Text = "设备状态:";
            //
            this.txtDeviceStatus.Location = new System.Drawing.Point(306, 53);
            this.txtDeviceStatus.Name = "txtDeviceStatus";
            this.txtDeviceStatus.Size = new System.Drawing.Size(140, 23);
            this.txtDeviceStatus.TabIndex = 5;
            //
            this.lblUsageScope.AutoSize = true;
            this.lblUsageScope.Location = new System.Drawing.Point(465, 56);
            this.lblUsageScope.Name = "lblUsageScope";
            this.lblUsageScope.Size = new System.Drawing.Size(35, 17);
            this.lblUsageScope.TabIndex = 10;
            this.lblUsageScope.Text = "用途:";
            //
            this.txtUsageScope.Location = new System.Drawing.Point(505, 53);
            this.txtUsageScope.Name = "txtUsageScope";
            this.txtUsageScope.Size = new System.Drawing.Size(100, 23);
            this.txtUsageScope.TabIndex = 6;
            //
            // Row 3
            //
            this.lblUsageCategory.AutoSize = true;
            this.lblUsageCategory.Location = new System.Drawing.Point(14, 87);
            this.lblUsageCategory.Name = "lblUsageCategory";
            this.lblUsageCategory.Size = new System.Drawing.Size(56, 17);
            this.lblUsageCategory.TabIndex = 12;
            this.lblUsageCategory.Text = "使用类别:";
            //
            this.txtUsageCategory.Location = new System.Drawing.Point(80, 84);
            this.txtUsageCategory.Name = "txtUsageCategory";
            this.txtUsageCategory.Size = new System.Drawing.Size(140, 23);
            this.txtUsageCategory.TabIndex = 7;
            //
            this.lblCurrentDept.AutoSize = true;
            this.lblCurrentDept.Location = new System.Drawing.Point(240, 87);
            this.lblCurrentDept.Name = "lblCurrentDept";
            this.lblCurrentDept.Size = new System.Drawing.Size(56, 17);
            this.lblCurrentDept.TabIndex = 14;
            this.lblCurrentDept.Text = "当前部门:";
            //
            this.txtCurrentDept.Location = new System.Drawing.Point(306, 84);
            this.txtCurrentDept.Name = "txtCurrentDept";
            this.txtCurrentDept.Size = new System.Drawing.Size(140, 23);
            this.txtCurrentDept.TabIndex = 8;
            //
            this.lblCurrentLocation.AutoSize = true;
            this.lblCurrentLocation.Location = new System.Drawing.Point(465, 87);
            this.lblCurrentLocation.Name = "lblCurrentLocation";
            this.lblCurrentLocation.Size = new System.Drawing.Size(35, 17);
            this.lblCurrentLocation.TabIndex = 16;
            this.lblCurrentLocation.Text = "位置:";
            //
            this.txtCurrentLocation.Location = new System.Drawing.Point(505, 84);
            this.txtCurrentLocation.Name = "txtCurrentLocation";
            this.txtCurrentLocation.Size = new System.Drawing.Size(100, 23);
            this.txtCurrentLocation.TabIndex = 9;
            //
            // Row 4
            //
            this.lblCurrentPerson.AutoSize = true;
            this.lblCurrentPerson.Location = new System.Drawing.Point(14, 118);
            this.lblCurrentPerson.Name = "lblCurrentPerson";
            this.lblCurrentPerson.Size = new System.Drawing.Size(56, 17);
            this.lblCurrentPerson.TabIndex = 18;
            this.lblCurrentPerson.Text = "责任人:";
            //
            this.txtCurrentPerson.Location = new System.Drawing.Point(80, 115);
            this.txtCurrentPerson.Name = "txtCurrentPerson";
            this.txtCurrentPerson.Size = new System.Drawing.Size(140, 23);
            this.txtCurrentPerson.TabIndex = 10;
            //
            // groupBox2
            //
            this.groupBox2.BackColor = System.Drawing.Color.Transparent;
            this.groupBox2.Controls.Add(this.lblCalDate);
            this.groupBox2.Controls.Add(this.txtCalibrationDate);
            this.groupBox2.Controls.Add(this.lblNextCalDate);
            this.groupBox2.Controls.Add(this.txtNextCalibrationDate);
            this.groupBox2.Controls.Add(this.lblQualDate);
            this.groupBox2.Controls.Add(this.txtQualificationDate);
            this.groupBox2.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.groupBox2.Location = new System.Drawing.Point(10, 172);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(620, 55);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "校准/检定信息";
            //
            this.lblCalDate.AutoSize = true;
            this.lblCalDate.Location = new System.Drawing.Point(14, 25);
            this.lblCalDate.Name = "lblCalDate";
            this.lblCalDate.Size = new System.Drawing.Size(56, 17);
            this.lblCalDate.TabIndex = 0;
            this.lblCalDate.Text = "校准日期:";
            //
            this.txtCalibrationDate.Location = new System.Drawing.Point(80, 22);
            this.txtCalibrationDate.Name = "txtCalibrationDate";
            this.txtCalibrationDate.Size = new System.Drawing.Size(110, 23);
            this.txtCalibrationDate.TabIndex = 11;
            //
            this.lblNextCalDate.AutoSize = true;
            this.lblNextCalDate.Location = new System.Drawing.Point(210, 25);
            this.lblNextCalDate.Name = "lblNextCalDate";
            this.lblNextCalDate.Size = new System.Drawing.Size(68, 17);
            this.lblNextCalDate.TabIndex = 2;
            this.lblNextCalDate.Text = "下次校准:";
            //
            this.txtNextCalibrationDate.Location = new System.Drawing.Point(282, 22);
            this.txtNextCalibrationDate.Name = "txtNextCalibrationDate";
            this.txtNextCalibrationDate.Size = new System.Drawing.Size(110, 23);
            this.txtNextCalibrationDate.TabIndex = 12;
            //
            this.lblQualDate.AutoSize = true;
            this.lblQualDate.Location = new System.Drawing.Point(412, 25);
            this.lblQualDate.Name = "lblQualDate";
            this.lblQualDate.Size = new System.Drawing.Size(56, 17);
            this.lblQualDate.TabIndex = 4;
            this.lblQualDate.Text = "检定日期:";
            //
            this.txtQualificationDate.Location = new System.Drawing.Point(478, 22);
            this.txtQualificationDate.Name = "txtQualificationDate";
            this.txtQualificationDate.Size = new System.Drawing.Size(110, 23);
            this.txtQualificationDate.TabIndex = 13;
            //
            // OK_Button
            //
            this.OK_Button.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.OK_Button.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.OK_Button.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.OK_Button.Location = new System.Drawing.Point(472, 237);
            this.OK_Button.Name = "OK_Button";
            this.OK_Button.Size = new System.Drawing.Size(70, 28);
            this.OK_Button.TabIndex = 14;
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
            this.Cancel_Button.Location = new System.Drawing.Point(560, 237);
            this.Cancel_Button.Name = "Cancel_Button";
            this.Cancel_Button.Size = new System.Drawing.Size(70, 28);
            this.Cancel_Button.TabIndex = 15;
            this.Cancel_Button.Text = "取消";
            this.Cancel_Button.UseVisualStyleBackColor = false;
            this.Cancel_Button.Click += new System.EventHandler(this.Cancel_Button_Click);
            //
            // DiagDevice
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.LightBlue;
            this.ClientSize = new System.Drawing.Size(640, 275);
            this.Controls.Add(this.Cancel_Button);
            this.Controls.Add(this.OK_Button);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DiagDevice";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "设备管理台账";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lblDeviceCode;
        private System.Windows.Forms.TextBox txtDeviceCode;
        private System.Windows.Forms.Label lblDeviceName;
        private System.Windows.Forms.TextBox txtDeviceName;
        private System.Windows.Forms.Label lblSpec;
        private System.Windows.Forms.TextBox txtSpecification;
        private System.Windows.Forms.Label lblBrand;
        private System.Windows.Forms.TextBox txtBrand;
        private System.Windows.Forms.Label lblDeviceStatus;
        private System.Windows.Forms.TextBox txtDeviceStatus;
        private System.Windows.Forms.Label lblUsageScope;
        private System.Windows.Forms.TextBox txtUsageScope;
        private System.Windows.Forms.Label lblUsageCategory;
        private System.Windows.Forms.TextBox txtUsageCategory;
        private System.Windows.Forms.Label lblCurrentDept;
        private System.Windows.Forms.TextBox txtCurrentDept;
        private System.Windows.Forms.Label lblCurrentLocation;
        private System.Windows.Forms.TextBox txtCurrentLocation;
        private System.Windows.Forms.Label lblCurrentPerson;
        private System.Windows.Forms.TextBox txtCurrentPerson;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label lblCalDate;
        private System.Windows.Forms.TextBox txtCalibrationDate;
        private System.Windows.Forms.Label lblNextCalDate;
        private System.Windows.Forms.TextBox txtNextCalibrationDate;
        private System.Windows.Forms.Label lblQualDate;
        private System.Windows.Forms.TextBox txtQualificationDate;
        private System.Windows.Forms.Button OK_Button;
        private System.Windows.Forms.Button Cancel_Button;
    }
}

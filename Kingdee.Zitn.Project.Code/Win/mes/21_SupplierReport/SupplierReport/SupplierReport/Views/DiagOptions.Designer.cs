namespace ZT.Cloud.SupplierReport.Views
{
    partial class DiagOptions
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.TableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.OK_Button = new System.Windows.Forms.Button();
            this.Cancel_Button = new System.Windows.Forms.Button();
            this.GroupBox1 = new System.Windows.Forms.GroupBox();
            this.LinkLblLine = new System.Windows.Forms.LinkLabel();
            this.Label4 = new System.Windows.Forms.Label();
            this.ChkEnableSound = new System.Windows.Forms.CheckBox();
            this.TableLayoutPanel1.SuspendLayout();
            this.GroupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // TableLayoutPanel1
            // 
            this.TableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.TableLayoutPanel1.ColumnCount = 2;
            this.TableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.TableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.TableLayoutPanel1.Controls.Add(this.OK_Button, 0, 0);
            this.TableLayoutPanel1.Controls.Add(this.Cancel_Button, 1, 0);
            this.TableLayoutPanel1.Location = new System.Drawing.Point(306, 67);
            this.TableLayoutPanel1.Name = "TableLayoutPanel1";
            this.TableLayoutPanel1.RowCount = 1;
            this.TableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.TableLayoutPanel1.Size = new System.Drawing.Size(146, 27);
            this.TableLayoutPanel1.TabIndex = 3;
            // 
            // OK_Button
            // 
            this.OK_Button.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.OK_Button.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.OK_Button.Location = new System.Drawing.Point(3, 3);
            this.OK_Button.Name = "OK_Button";
            this.OK_Button.Size = new System.Drawing.Size(67, 21);
            this.OK_Button.TabIndex = 0;
            this.OK_Button.Text = "确定";
            this.OK_Button.UseVisualStyleBackColor = false;
            this.OK_Button.Click += new System.EventHandler(this.OK_Button_Click);
            // 
            // Cancel_Button
            // 
            this.Cancel_Button.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.Cancel_Button.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.Cancel_Button.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel_Button.Location = new System.Drawing.Point(76, 3);
            this.Cancel_Button.Name = "Cancel_Button";
            this.Cancel_Button.Size = new System.Drawing.Size(67, 21);
            this.Cancel_Button.TabIndex = 1;
            this.Cancel_Button.Text = "取消";
            this.Cancel_Button.UseVisualStyleBackColor = false;
            this.Cancel_Button.Click += new System.EventHandler(this.Cancel_Button_Click);
            // 
            // GroupBox1
            // 
            this.GroupBox1.Controls.Add(this.LinkLblLine);
            this.GroupBox1.Controls.Add(this.Label4);
            this.GroupBox1.Controls.Add(this.ChkEnableSound);
            this.GroupBox1.Location = new System.Drawing.Point(8, 5);
            this.GroupBox1.Name = "GroupBox1";
            this.GroupBox1.Size = new System.Drawing.Size(446, 55);
            this.GroupBox1.TabIndex = 4;
            this.GroupBox1.TabStop = false;
            this.GroupBox1.Text = "基本";
            // 
            // LinkLblLine
            // 
            this.LinkLblLine.AutoSize = true;
            this.LinkLblLine.Cursor = System.Windows.Forms.Cursors.Hand;
            this.LinkLblLine.Location = new System.Drawing.Point(42, 25);
            this.LinkLblLine.Name = "LinkLblLine";
            this.LinkLblLine.Size = new System.Drawing.Size(23, 12);
            this.LinkLblLine.TabIndex = 9;
            this.LinkLblLine.TabStop = true;
            this.LinkLblLine.Text = "---";
            // 
            // Label4
            // 
            this.Label4.AutoSize = true;
            this.Label4.Location = new System.Drawing.Point(6, 25);
            this.Label4.Name = "Label4";
            this.Label4.Size = new System.Drawing.Size(35, 12);
            this.Label4.TabIndex = 7;
            this.Label4.Text = "线体:";
            // 
            // ChkEnableSound
            // 
            this.ChkEnableSound.AutoSize = true;
            this.ChkEnableSound.Location = new System.Drawing.Point(123, 24);
            this.ChkEnableSound.Name = "ChkEnableSound";
            this.ChkEnableSound.Size = new System.Drawing.Size(96, 16);
            this.ChkEnableSound.TabIndex = 6;
            this.ChkEnableSound.Text = "启用语言提示";
            this.ChkEnableSound.UseVisualStyleBackColor = true;
            // 
            // DiagOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.LightBlue;
            this.ClientSize = new System.Drawing.Size(460, 100);
            this.Controls.Add(this.TableLayoutPanel1);
            this.Controls.Add(this.GroupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DiagOptions";
            this.Text = "配置";
            this.Load += new System.EventHandler(this.DiagOptions_Load);
            this.TableLayoutPanel1.ResumeLayout(false);
            this.GroupBox1.ResumeLayout(false);
            this.GroupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.TableLayoutPanel TableLayoutPanel1;
        internal System.Windows.Forms.Button OK_Button;
        internal System.Windows.Forms.Button Cancel_Button;
        internal System.Windows.Forms.GroupBox GroupBox1;
        internal System.Windows.Forms.LinkLabel LinkLblLine;
        internal System.Windows.Forms.Label Label4;
        internal System.Windows.Forms.CheckBox ChkEnableSound;
    }
}
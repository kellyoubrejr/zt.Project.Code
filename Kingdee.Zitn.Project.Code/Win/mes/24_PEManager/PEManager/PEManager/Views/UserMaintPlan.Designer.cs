namespace ZT.Cloud.PEManager.Views
{
    partial class UserMaintPlan
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserMaintPlan));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButAdd = new System.Windows.Forms.ToolStripButton();
            this.toolStripButEdit = new System.Windows.Forms.ToolStripButton();
            this.toolStripButDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButQuery = new System.Windows.Forms.ToolStripButton();
            this.toolStripTextBox1 = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.BackColor = System.Drawing.Color.LightBlue;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButAdd,
            this.toolStripButEdit,
            this.toolStripButDelete,
            this.toolStripSeparator1,
            this.toolStripButQuery,
            this.toolStripTextBox1,
            this.toolStripButton1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1018, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButAdd
            // 
            this.toolStripButAdd.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButAdd.Image")));
            this.toolStripButAdd.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButAdd.Name = "toolStripButAdd";
            this.toolStripButAdd.Size = new System.Drawing.Size(52, 22);
            this.toolStripButAdd.Text = "新增";
            this.toolStripButAdd.Click += new System.EventHandler(this.toolStripButAdd_Click);
            // 
            // toolStripButEdit
            // 
            this.toolStripButEdit.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButEdit.Image")));
            this.toolStripButEdit.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButEdit.Name = "toolStripButEdit";
            this.toolStripButEdit.Size = new System.Drawing.Size(52, 22);
            this.toolStripButEdit.Text = "编辑";
            this.toolStripButEdit.Click += new System.EventHandler(this.toolStripButEdit_Click);
            // 
            // toolStripButDelete
            // 
            this.toolStripButDelete.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButDelete.Image")));
            this.toolStripButDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButDelete.Name = "toolStripButDelete";
            this.toolStripButDelete.Size = new System.Drawing.Size(52, 22);
            this.toolStripButDelete.Text = "删除";
            this.toolStripButDelete.Click += new System.EventHandler(this.toolStripButDelete_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButQuery
            // 
            this.toolStripButQuery.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButQuery.Image")));
            this.toolStripButQuery.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButQuery.Name = "toolStripButQuery";
            this.toolStripButQuery.Size = new System.Drawing.Size(52, 22);
            this.toolStripButQuery.Text = "过滤";
            this.toolStripButQuery.Click += new System.EventHandler(this.toolStripButQuery_Click);
            // 
            // toolStripTextBox1
            // 
            this.toolStripTextBox1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.toolStripTextBox1.Name = "toolStripTextBox1";
            this.toolStripTextBox1.Size = new System.Drawing.Size(100, 25);
            this.toolStripTextBox1.TextChanged += new System.EventHandler(this.toolStripTextBox1_TextChanged);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton1.Text = "toolStripButton1";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 25);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowTemplate.Height = 23;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(1018, 688);
            this.dataGridView1.TabIndex = 1;
            // 
            // UserMaintPlan
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "UserMaintPlan";
            this.Size = new System.Drawing.Size(1018, 713);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButAdd;
        private System.Windows.Forms.ToolStripButton toolStripButEdit;
        private System.Windows.Forms.ToolStripButton toolStripButDelete;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBox1;
        private System.Windows.Forms.ToolStripButton toolStripButQuery;
    }
}

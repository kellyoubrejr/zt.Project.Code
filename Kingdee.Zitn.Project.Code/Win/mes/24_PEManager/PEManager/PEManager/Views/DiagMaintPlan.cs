using System;
using System.Windows.Forms;
using ZT.Cloud.MesModel.Models;

namespace ZT.Cloud.MesModel.Views
{
    public partial class DiagMaintPlan : Form
    {
        private PEManagerHelper myHelper;
        private bool _isEditMode;
        private int _editId;

        public ClassMaintPlan ResultData { get; private set; }

        /// <summary>
        /// 新增模式
        /// </summary>
        public DiagMaintPlan(PEManagerHelper helper)
        {
            InitializeComponent();
            myHelper = helper;
            _isEditMode = false;
            this.Text = "新增设备保养计划";
        }

        /// <summary>
        /// 编辑模式
        /// </summary>
        public DiagMaintPlan(PEManagerHelper helper, ClassMaintPlan data)
        {
            InitializeComponent();
            myHelper = helper;
            _isEditMode = true;
            _editId = data.Id;
            this.Text = "编辑设备保养计划";

            txtEquipmentName.Text = data.EquipmentName;
            txtFileNo.Text = data.FileNo;
            txtVersion.Text = data.Version;
            txtMaintCycle.Text = data.MaintCycle;
            txtQuantity.Text = data.Quantity.ToString();
            txtJan.Text = data.Jan;
            txtFeb.Text = data.Feb;
            txtMar.Text = data.Mar;
            txtApr.Text = data.Apr;
            txtMay.Text = data.May;
            txtJun.Text = data.Jun;
            txtJul.Text = data.Jul;
            txtAug.Text = data.Aug;
            txtSep.Text = data.Sep;
            txtOct.Text = data.Oct;
            txtNov.Text = data.Nov;
            txtDec.Text = data.Dec;
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtEquipmentName.Text))
            {
                MessageBox.Show("请输入设备名称!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEquipmentName.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtFileNo.Text))
            {
                MessageBox.Show("请输入文件编号!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFileNo.Focus();
                return false;
            }
            if (!int.TryParse(txtQuantity.Text.Trim(), out _))
            {
                MessageBox.Show("数量请输入有效数字!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtQuantity.Focus();
                return false;
            }
            return true;
        }

        private ClassMaintPlan BuildData()
        {
            var data = new ClassMaintPlan();
            if (_isEditMode)
                data.Id = _editId;

            int.TryParse(txtQuantity.Text.Trim(), out int qty);
            data.EquipmentName = txtEquipmentName.Text.Trim();
            data.FileNo = txtFileNo.Text.Trim();
            data.Version = txtVersion.Text.Trim();
            data.MaintCycle = txtMaintCycle.Text.Trim();
            data.Quantity = qty;
            data.Jan = txtJan.Text.Trim();
            data.Feb = txtFeb.Text.Trim();
            data.Mar = txtMar.Text.Trim();
            data.Apr = txtApr.Text.Trim();
            data.May = txtMay.Text.Trim();
            data.Jun = txtJun.Text.Trim();
            data.Jul = txtJul.Text.Trim();
            data.Aug = txtAug.Text.Trim();
            data.Sep = txtSep.Text.Trim();
            data.Oct = txtOct.Text.Trim();
            data.Nov = txtNov.Text.Trim();
            data.Dec = txtDec.Text.Trim();
            return data;
        }

        private void OK_Button_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
                return;

            ResultData = BuildData();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void Cancel_Button_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}

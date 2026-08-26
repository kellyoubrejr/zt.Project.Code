using System;
using System.Windows.Forms;
using ZT.Cloud.MesModel.Models;

namespace ZT.Cloud.MesModel.Views
{
    public partial class DiagDevice : Form
    {
        private PEManagerHelper myHelper;
        private bool _isEditMode;
        private int _editId;

        public ClassDevice ResultData { get; private set; }

        public DiagDevice(PEManagerHelper helper)
        {
            InitializeComponent();
            myHelper = helper;
            _isEditMode = false;
            this.Text = "新增设备";
        }

        public DiagDevice(PEManagerHelper helper, ClassDevice data)
        {
            InitializeComponent();
            myHelper = helper;
            _isEditMode = true;
            _editId = data.Id;
            this.Text = "编辑设备";

            txtDeviceCode.Text = data.DeviceCode;
            txtDeviceName.Text = data.DeviceName;
            txtSpecification.Text = data.Specification;
            txtBrand.Text = data.Brand;
            txtDeviceStatus.Text = data.DeviceStatus;
            txtUsageScope.Text = data.UsageScope;
            txtUsageCategory.Text = data.UsageCategory;
            txtCurrentDept.Text = data.CurrentDept;
            txtCurrentLocation.Text = data.CurrentLocation;
            txtCurrentPerson.Text = data.CurrentPerson;
            txtCalibrationDate.Text = data.CalibrationDate;
            txtNextCalibrationDate.Text = data.NextCalibrationDate;
            txtQualificationDate.Text = data.QualificationDate;
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtDeviceCode.Text))
            {
                MessageBox.Show("请输入设备编号!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDeviceCode.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtDeviceName.Text))
            {
                MessageBox.Show("请输入设备名称!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDeviceName.Focus();
                return false;
            }
            return true;
        }

        private ClassDevice BuildData()
        {
            var data = new ClassDevice();
            if (_isEditMode)
                data.Id = _editId;

            data.DeviceCode = txtDeviceCode.Text.Trim();
            data.DeviceName = txtDeviceName.Text.Trim();
            data.Specification = txtSpecification.Text.Trim();
            data.Brand = txtBrand.Text.Trim();
            data.DeviceStatus = txtDeviceStatus.Text.Trim();
            data.UsageScope = txtUsageScope.Text.Trim();
            data.UsageCategory = txtUsageCategory.Text.Trim();
            data.CurrentDept = txtCurrentDept.Text.Trim();
            data.CurrentLocation = txtCurrentLocation.Text.Trim();
            data.CurrentPerson = txtCurrentPerson.Text.Trim();
            data.CalibrationDate = txtCalibrationDate.Text.Trim();
            data.NextCalibrationDate = txtNextCalibrationDate.Text.Trim();
            data.QualificationDate = txtQualificationDate.Text.Trim();
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

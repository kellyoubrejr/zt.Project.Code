using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZT.Cloud.MesModel.Models;
using ZT.Cloud.MesModel.Views;

namespace ZT.Cloud.PEManager.Views
{
    public partial class UserDevices : UserControl
    {
        PEManagerHelper myHelper;
        private bool _isInitilized = false;
        private DataView DV;

        public UserDevices()
        {
            InitializeComponent();
        }

        public void Initial(PEManagerHelper helper)
        {
            if (!_isInitilized)
            {
                myHelper = helper;
                _isInitilized = true;
                LoadData();
            }
        }

        private void LoadData()
        {
            if (myHelper.SQLServer == null) return;

            DV = myHelper.SQLServer.QueryDevices().DefaultView;
            dataGridView1.DataSource = DV;

            var colMap = new Dictionary<string, string>
            {
                {"Id", "ID"},
                {"SeqNo", "序号"},
                {"DeviceCode", "设备编号"},
                {"DeviceName", "设备名称"},
                {"Specification", "规格"},
                {"Brand", "品牌"},
                {"DeviceStatus", "设备状态"},
                {"UsageScope", "用途"},
                {"UsageCategory", "使用类别"},
                {"CurrentDept", "当前部门"},
                {"CurrentLocation", "当前位置"},
                {"CurrentPerson", "责任人"},
                {"CalibrationDate", "校准日期"},
                {"NextCalibrationDate", "下次校准日期"},
                {"QualificationDate", "检定日期"}
            };
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                if (colMap.ContainsKey(col.Name))
                    col.HeaderText = colMap[col.Name];
            }
        }

        private void toolStripButQuery_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (DV == null) return;

            string search = toolStripTextBox1.Text.Trim().Replace("'", "''");
            if (string.IsNullOrEmpty(search))
            {
                DV.RowFilter = "";
            }
            else
            {
                DV.RowFilter = string.Format(
                    "DeviceName like '%{0}%' or DeviceCode like '%{0}%' or CurrentDept like '%{0}%' or CurrentPerson like '%{0}%'",
                    search);
            }
        }

        private void toolStripButAdd_Click(object sender, EventArgs e)
        {
            DiagDevice dlg = new DiagDevice(myHelper);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var data = dlg.ResultData;
                    int maxSeq = GetMaxSeqNo();
                    data.SeqNo = maxSeq + 1;

                    int newId = myHelper.SQLServer.InsertDevice(data);
                    myHelper.SQLServer.LogDeviceOperation(
                        "INSERT", newId, data.DeviceName, data.DeviceCode,
                        myHelper.Configure.WorkerName,
                        string.Format("新增设备: {0}({1})", data.DeviceName, data.DeviceCode));

                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("新增失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void toolStripButEdit_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("请先选择一条记录!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataRowView rowView = dataGridView1.CurrentRow.DataBoundItem as DataRowView;
            if (rowView == null) return;

            ClassDevice current = RowToDevice(rowView.Row);

            DiagDevice dlg = new DiagDevice(myHelper, current);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var data = dlg.ResultData;
                    myHelper.SQLServer.UpdateDevice(data);
                    myHelper.SQLServer.LogDeviceOperation(
                        "UPDATE", data.Id, data.DeviceName, data.DeviceCode,
                        myHelper.Configure.WorkerName,
                        string.Format("编辑设备: {0}({1})", data.DeviceName, data.DeviceCode));

                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("编辑失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void toolStripButDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("请先选择一条记录!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataRowView rowView = dataGridView1.CurrentRow.DataBoundItem as DataRowView;
            if (rowView == null) return;

            ClassDevice current = RowToDevice(rowView.Row);

            string msg = string.Format("确认删除设备 [{0}] 吗?\n设备编号: {1}",
                current.DeviceName, current.DeviceCode);

            if (MessageBox.Show(msg, "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    myHelper.SQLServer.DeleteDevice(current.Id);
                    myHelper.SQLServer.LogDeviceOperation(
                        "DELETE", current.Id, current.DeviceName, current.DeviceCode,
                        myHelper.Configure.WorkerName,
                        string.Format("删除设备: {0}({1})", current.DeviceName, current.DeviceCode));

                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("删除失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private int GetMaxSeqNo()
        {
            DataTable dt = myHelper.SQLServer.QueryDevices();
            if (dt.Rows.Count == 0) return 0;
            int max = 0;
            foreach (DataRow row in dt.Rows)
            {
                if (int.TryParse(row["SeqNo"]?.ToString(), out int seq))
                {
                    if (seq > max) max = seq;
                }
            }
            return max;
        }

        private ClassDevice RowToDevice(DataRow row)
        {
            var device = new ClassDevice();
            int.TryParse(row["Id"]?.ToString(), out int id);
            int.TryParse(row["SeqNo"]?.ToString(), out int seq);

            device.Id = id;
            device.SeqNo = seq;
            device.DeviceCode = row["DeviceCode"]?.ToString() ?? "";
            device.DeviceName = row["DeviceName"]?.ToString() ?? "";
            device.Specification = row["Specification"]?.ToString() ?? "";
            device.Brand = row["Brand"]?.ToString() ?? "";
            device.DeviceStatus = row["DeviceStatus"]?.ToString() ?? "";
            device.UsageScope = row["UsageScope"]?.ToString() ?? "";
            device.UsageCategory = row["UsageCategory"]?.ToString() ?? "";
            device.CurrentDept = row["CurrentDept"]?.ToString() ?? "";
            device.CurrentLocation = row["CurrentLocation"]?.ToString() ?? "";
            device.CurrentPerson = row["CurrentPerson"]?.ToString() ?? "";
            device.CalibrationDate = row["CalibrationDate"]?.ToString() ?? "";
            device.NextCalibrationDate = row["NextCalibrationDate"]?.ToString() ?? "";
            device.QualificationDate = row["QualificationDate"]?.ToString() ?? "";
            return device;
        }
    }
}

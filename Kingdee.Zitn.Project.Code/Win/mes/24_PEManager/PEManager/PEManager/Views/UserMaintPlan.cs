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
    public partial class UserMaintPlan : UserControl
    {
        PEManagerHelper myHelper;
        private bool _isInitilized = false;
        private DataView DV;

        public UserMaintPlan()
        {
            InitializeComponent();
        }

        public void Initial(PEManagerHelper helper)
        {
            if(!_isInitilized)
            {
                myHelper = helper;
                _isInitilized = true;
                LoadData();
            }
        }

        private void LoadData()
        {
            if (myHelper.SQLServer == null) return;

            DV = myHelper.SQLServer.QueryMaintPlans().DefaultView;
            dataGridView1.DataSource = DV;

            // 设置中文列名
            var colMap = new Dictionary<string, string>
            {
                {"Id", "ID"},
                {"SeqNo", "序号"},
                {"EquipmentName", "设备名称"},
                {"FileNo", "文件编号"},
                {"Version", "版次"},
                {"MaintCycle", "维护周期"},
                {"Quantity", "数量"},
                {"Jan", "一月"}, {"Feb", "二月"}, {"Mar", "三月"},
                {"Apr", "四月"}, {"May", "五月"}, {"Jun", "六月"},
                {"Jul", "七月"}, {"Aug", "八月"}, {"Sep", "九月"},
                {"Oct", "十月"}, {"Nov", "十一月"}, {"Dec", "十二月"}
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
                    "EquipmentName like '%{0}%' or FileNo like '%{0}%' or MaintCycle like '%{0}%'",
                    search);
            }
        }

        private void toolStripButAdd_Click(object sender, EventArgs e)
        {
            DiagMaintPlan dlg = new DiagMaintPlan(myHelper);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var data = dlg.ResultData;
                    int maxSeq = GetMaxSeqNo();
                    data.SeqNo = maxSeq + 1;

                    int newId = myHelper.SQLServer.InsertMaintPlan(data);
                    myHelper.SQLServer.LogMaintPlanOperation(
                        "INSERT", newId, data.EquipmentName, data.FileNo,
                        myHelper.Configure.WorkerName,
                        string.Format("新增设备保养计划: {0}({1})", data.EquipmentName, data.FileNo));

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

            ClassMaintPlan current = RowToPlan(rowView.Row);

            DiagMaintPlan dlg = new DiagMaintPlan(myHelper, current);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var data = dlg.ResultData;
                    myHelper.SQLServer.UpdateMaintPlan(data);
                    myHelper.SQLServer.LogMaintPlanOperation(
                        "UPDATE", data.Id, data.EquipmentName, data.FileNo,
                        myHelper.Configure.WorkerName,
                        string.Format("编辑设备保养计划: {0}({1})", data.EquipmentName, data.FileNo));

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

            ClassMaintPlan current = RowToPlan(rowView.Row);

            string msg = string.Format("确认删除设备 [{0}] 的保养计划吗?\n文件编号: {1}",
                current.EquipmentName, current.FileNo);

            if (MessageBox.Show(msg, "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    myHelper.SQLServer.DeleteMaintPlan(current.Id);
                    myHelper.SQLServer.LogMaintPlanOperation(
                        "DELETE", current.Id, current.EquipmentName, current.FileNo,
                        myHelper.Configure.WorkerName,
                        string.Format("删除设备保养计划: {0}({1})", current.EquipmentName, current.FileNo));

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
            DataTable dt = myHelper.SQLServer.QueryMaintPlans();
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

        private ClassMaintPlan RowToPlan(DataRow row)
        {
            var plan = new ClassMaintPlan();
            int.TryParse(row["Id"]?.ToString(), out int id);
            int.TryParse(row["SeqNo"]?.ToString(), out int seq);
            int.TryParse(row["Quantity"]?.ToString(), out int qty);

            plan.Id = id;
            plan.SeqNo = seq;
            plan.EquipmentName = row["EquipmentName"]?.ToString() ?? "";
            plan.FileNo = row["FileNo"]?.ToString() ?? "";
            plan.Version = row["Version"]?.ToString() ?? "";
            plan.MaintCycle = row["MaintCycle"]?.ToString() ?? "";
            plan.Quantity = qty;
            plan.Jan = row["Jan"]?.ToString() ?? "";
            plan.Feb = row["Feb"]?.ToString() ?? "";
            plan.Mar = row["Mar"]?.ToString() ?? "";
            plan.Apr = row["Apr"]?.ToString() ?? "";
            plan.May = row["May"]?.ToString() ?? "";
            plan.Jun = row["Jun"]?.ToString() ?? "";
            plan.Jul = row["Jul"]?.ToString() ?? "";
            plan.Aug = row["Aug"]?.ToString() ?? "";
            plan.Sep = row["Sep"]?.ToString() ?? "";
            plan.Oct = row["Oct"]?.ToString() ?? "";
            plan.Nov = row["Nov"]?.ToString() ?? "";
            plan.Dec = row["Dec"]?.ToString() ?? "";
            return plan;
        }
    }
}

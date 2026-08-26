using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZT.Cloud.MesModel.Views
{
    public partial class DiagOptions : Form
    {
        private PEManagerHelper myHelper;
        public DiagOptions(PEManagerHelper Helper)
        {
            InitializeComponent();

            myHelper = Helper;
        }

        private void DiagOptions_Load(object sender, EventArgs e)
        {
            //加载当前配置信息
            LinkLblLine.Text = myHelper.Configure.LineName;
            ChkEnableSound.Checked = myHelper.Configure.EnableSound;
        }

        private bool ApplyNewSettings()
        {
            myHelper.Configure.EnableSound = ChkEnableSound.Checked;    



            return true;
        }

        private void OK_Button_Click(object sender, EventArgs e)
        {
            if(ApplyNewSettings())
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        private void Cancel_Button_Click(object sender, EventArgs e)
        {
            this.DialogResult= DialogResult.Cancel;
            this.Close();
        }
    }
}

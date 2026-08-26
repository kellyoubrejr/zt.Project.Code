using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZT.Cloud.ZTCloudHelper;

namespace ZTCloud
{
    internal static class Program
    {
        private static PEManagerHelper myAppHelper = new PEManagerHelper();

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                //首次初始化
                myAppHelper.LogInUser = new LogInCloudUser()
                {
                    UserID = 273,
                    DeptID = 13,
                    PostID = 55,
                    UserLevel = 4,
                    UserName = "李祥付",
                    Plant = ZT.ZTDataTracer.Plants.PlantDebug
                };

                if (myAppHelper.AppInitial())
                {
                    Application.Run(myAppHelper.FrmMain);
                }
                else
                {
                    MessageBox.Show(myAppHelper.ErrorMessage, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}

using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdee.Zitn.Project.Code.plugin.PO
{
    [Description("【表单插件】采购订单】带入供应商付款条件字段")]
    [HotUpdate]
    public class PoSupplier_FK : AbstractBillPlugIn
    {
        private static readonly string LogPath = @"D:\金蝶自定义日志文件\采购订单审核推送BPM合同.txt";

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            if (e.BarItemKey.Equals("tbSplitSubmit", StringComparison.OrdinalIgnoreCase) ||
                e.BarItemKey.Equals("tbSubmit", StringComparison.OrdinalIgnoreCase) ||
                e.BarItemKey.Equals("tbSplitSave", StringComparison.OrdinalIgnoreCase) ||
                e.BarItemKey.Equals("tbSave", StringComparison.OrdinalIgnoreCase))
            {
                var supplierObj = this.View.Model.GetValue("FSupplierId") as DynamicObject;
                if (supplierObj != null)
                {
                    var supplierId = supplierObj["Id"];
                    WriteLog($"GYSID: {supplierId}");

                    if (supplierId != null)
                    {

                        var gysOther = string.Format($"/*dialect*/SELECT F_UNW_COMBO_ZC5 FROM T_BD_SUPPLIER WHERE FSUPPLIERID = {supplierId}");
                        var DT = DBUtils.ExecuteDynamicObject(this.Context, gysOther);
                        if (DT != null && DT.Count > 0)
                        {
                            for (int ii = 0; ii < DT.Count; ii++)
                            {
                                var fkfs = DT[ii]["F_UNW_COMBO_ZC5"].ToString();
                                WriteLog($"FKFS: {fkfs}");

                                if (fkfs != null && !fkfs.Equals(""))
                                {
                                    this.View.Model.SetValue("F_ZMER_Combo_qtr", fkfs);
                                }
                            }
                        }

                    }
                }
                this.View.UpdateView("F_ZMER_Combo_qtr");
                this.View.Model.Save();
                WriteLog($"保存成功,fkfs: {this.View.Model.GetValue("F_ZMER_Combo_qtr")}");
                this.View.ShowMessage("供应商付款条件已带入成功！");
            }
        }

        private static void WriteLog(string message)
        {
            try
            {
                string dir = Path.GetDirectoryName(LogPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}");
            }
            catch
            {
                // 日志写入失败不抛异常，避免影响主流程
            }
        }
    }
}

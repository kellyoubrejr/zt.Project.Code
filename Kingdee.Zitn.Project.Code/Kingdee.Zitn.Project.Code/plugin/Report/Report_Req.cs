using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.K3.SCM.App.Purchase.Report;
using System;
using System.ComponentModel;
using System.Text;

namespace Kingdee.Zitn.Project.Code.plugin.Report
{
    [Description("【采购申请报表插件】标准报表二次开发逻辑自定义（新加字段，同步加筛选条件）")]
    [HotUpdate]
    public class Report_Req : PurReqExecuteRpt
    {
        private string[] _customTempTableNames;

        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            IDBService dbService = Kingdee.BOS.App.ServiceHelper.GetService<IDBService>();
            _customTempTableNames = dbService.CreateTemporaryTableName(this.Context, 1);

            string tempTable = _customTempTableNames[0];

            base.BuilderReportSqlAndTempTable(filter, tempTable);

            DynamicObject customFilter = filter.FilterParameter.CustomFilter;

            string note = "";

            if (customFilter != null)
            {
                note = Convert.ToString(customFilter["FNOTE"]);//! 增加备注自定义字段
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("SELECT T1.*, TCH.FNOTE");
            sb.AppendFormat(" INTO {0} ", tableName);
            sb.AppendFormat(" FROM {0} T1 ", tempTable);

            sb.AppendLine(" LEFT JOIN T_PUR_REQENTRY TCT ON T1.FREQID = TCT.FENTRYID");
            sb.AppendLine(" LEFT JOIN T_PUR_REQUISITION TCH ON TCT.FID = TCH.FID");

            sb.AppendLine(" WHERE 1=1 ");

            if (!string.IsNullOrWhiteSpace(note))
            {
                note = note.Replace("'", "''");

                sb.AppendFormat(" AND TCH.FNOTE LIKE '%{0}%'", note);
            }

            DBUtils.Execute(this.Context, sb.ToString());
        }



        public override void CloseReport()
        {
            if (!_customTempTableNames.IsNullOrEmptyOrWhiteSpace())
            {
                IDBService dbService = Kingdee.BOS.App.ServiceHelper.GetService<IDBService>();
                dbService.DeleteTemporaryTableName(this.Context, _customTempTableNames);
            }
            base.CloseReport();
        }
    }
}

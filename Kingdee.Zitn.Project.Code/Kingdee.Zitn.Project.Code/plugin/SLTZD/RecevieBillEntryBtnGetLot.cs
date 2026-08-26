//using Kingdee.BOS.App.Data;
//using Kingdee.BOS.Core.Bill.PlugIn;
//using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
//using Kingdee.BOS.Util;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using Kingdee.BOS.Orm.DataEntity;

//namespace Kingdee.Zitn.Project.Code.plugin.SLTZD
//{
//    [Description("【收料通知单单据插件】收料通知单点击单据体获取批号事件-终极版"),HotUpdate]
//    public class RecevieBillEntryBtnGetLot : AbstractBillPlugIn
//    {
//        private const string PREFIX = "R";

//        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
//        {
//            base.AfterEntryBarItemClick(e);

//            if (!e.BarItemKey.Equals("tbGetLot", StringComparison.OrdinalIgnoreCase))
//                return;

//            var entity = this.View.Model.GetEntityDataObject(this.View.BusinessInfo.GetEntryEntity("FDetailEntity"));

//            var selectedRows = this.View.GetControl(entity.EntityId).GetSelectedRows();
//            List<int> rowIndexes = selectedRows.Count > 0 ? selectedRows : GetAllRowIndexes(entity);

//            if (rowIndexes.Count == 0)
//                return;

//            string dateStr = DateTime.Now.ToString("yyyyMMdd");

//            int seq = GetMaxSeq(dateStr);

//            // 缓存物料批号，同物料同批号
//            Dictionary<long, string> materialLotMap = new Dictionary<long, string>();

//            foreach (int i in rowIndexes)
//            {
//                string lot = Convert.ToString(this.View.Model.GetValue("FLot", i));

//                if (!string.IsNullOrWhiteSpace(lot))
//                    continue;

//                DynamicObject materialObj = this.View.Model.GetValue("FMaterialId", i) as DynamicObject;
//                if (materialObj == null)
//                    continue;

//                long materialId = Convert.ToInt64(materialObj["Id"]);

//                // 同物料使用已有批号
//                if (materialLotMap.ContainsKey(materialId))
//                {
//                    this.View.Model.SetValue("FLot", materialLotMap[materialId], i);
//                    continue;
//                }

//                // 新流水号
//                seq++;
//                string seqStr = seq.ToString("D3");

//                string lotNumber = PREFIX + dateStr + "-" + seqStr;

//                // 有效期
//                object expiryObj = this.View.Model.GetValue("F_YOUXIAOQIZHI", i);
//                if (expiryObj != null)
//                {
//                    DateTime expiry = Convert.ToDateTime(expiryObj);
//                    lotNumber += "有效期至" + expiry.ToString("yyyyMMdd");
//                }

//                // 缓存物料批号
//                materialLotMap[materialId] = lotNumber;

//                // 设置批号
//                this.View.Model.SetValue("FLot", lotNumber, i);
//            }

//            this.View.UpdateView("FDetailEntity");
//        }

//        /// <summary>
//        /// 获取所有行索引
//        /// </summary>
//        private List<int> GetAllRowIndexes(DynamicObject entity)
//        {
//            var dataObjs = this.View.Model.GetEntityDataObject(this.View.BusinessInfo.GetEntryEntity(entity));
//            List<int> indexes = new List<int>();
//            if (dataObjs != null)
//            {
//                for (int i = 0; i < dataObjs.Count; i++)
//                    indexes.Add(i);
//            }
//            return indexes;
//        }

//        /// <summary>
//        /// 获取当天最大流水号
//        /// </summary>
//        private int GetMaxSeq(string dateStr)
//        {
//            string sql = $@"
//    SELECT MAX(FLOT)
//    FROM T_PUR_ReceiveEntry
//    WHERE FLOT LIKE 'R{dateStr}-%' AND FLOT LIKE 'R{dateStr}-[0-9][0-9][0-9]%'";

//            object result = DBUtils.ExecuteDynamicObject(this.Context, sql);

//            if (result == null || string.IsNullOrWhiteSpace(result.ToString()))
//                return 0;

//            string lot = result.ToString();
//            int index = lot.IndexOf("-") + 1;
//            string seqStr = lot.Substring(index, 3);
//            return Convert.ToInt32(seqStr);
//        }
//    }
//}
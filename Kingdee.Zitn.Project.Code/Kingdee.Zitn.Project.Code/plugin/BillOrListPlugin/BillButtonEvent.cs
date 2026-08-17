using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdee.Zitn.Project.Code.plugin.BillOrListPlugin
{
    [Description("【单据按钮插件】点击【一键去免审】按钮,删除已经存在的免审记录，更改按钮名称，再次点击一键免审选择所有信息生成免审记录")]
    [HotUpdate]
    public class BillButtonEvent : AbstractBillPlugIn
    {
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);

            if (!string.Equals(e.Key, "F_ZMER_Button_qtr", System.StringComparison.OrdinalIgnoreCase))
                return;

            string flag = this.Model.GetValue("FUNAUDITFLAG")?.ToString();

            #region  还原免审逻辑

            if (flag == "Y")
            {
                string gysName = GetSupplierName();

                // TODO:设置全部信息免审
                //Audit();

                var entity = this.View.BillBusinessInfo.GetEntity("FPOOrderEntry");
                var entityObjs = this.View.Model.GetEntityDataObject(entity);

                //物料id存储列表字符串形式用英文逗号间隔，比如'A001','A002'
                //string materialIds = string.Join("','", entityObjs.Select(x => x["MaterialId"]?.ToString()).ToArray());

                for (int k = 0; k < entityObjs.Count; k++)
                {
                    string wlid = GetMaterialId(k);
                    string wlnum = GetMaterialNumber(k);
                    int entryId = Convert.ToInt32(entityObjs[k]["ID"]);
                    int qty = Convert.ToInt32(entityObjs[k]["Qty"]);
                    decimal price = Convert.ToDecimal(entityObjs[k]["taxprice"]);

                    var judgeQuery = string.Format(@"                                                    
                                                    SELECT DISTINCT FBILLNO
                                                    FROM ZMER_t_Cust100025 A
                                                    JOIN ZMER_t_Cust_Entry100101 B ON A.FID = B.FID
                                                    JOIN T_BD_MATERIAL M1 ON B.FMATERIALID = M1.FMATERIALID
                                                    WHERE M1.FNUMBER = '{0}'
                                                     AND CONVERT(DECIMAL(18,6), B.FQTY) >= {1}
  AND CONVERT(DECIMAL(18,6), B.FTAXPRICE)
        * (1 + CONVERT(DECIMAL(18,6), B.F_ZMER_DISCOUNTRATE_83G) / 100) >= {2} AND A.FDOCUMENTSTATUS = 'D' AND FUNAUDIT = '去除免审' AND B.FSUPPLIER = '{3}'",
                                                                        wlnum,
                                                                        qty,
                                                                        price, gysName);

                    DynamicObjectCollection judgeResult =
                        DBUtils.ExecuteDynamicObject(this.Context, judgeQuery);
                    if (judgeResult != null && judgeResult.Count > 0)
                    {
                        string msddBillNo1 = judgeResult[0]["FBILLNO"].ToString();
                        // 设置绿色
                        SetEntryIdBackColor("FPOOrderEntry", entryId, "#C6EFCE");

                        var sql = string.Format("/*dialect*/UPDATE ZMER_t_Cust100025 SET FDOCUMENTSTATUS = 'C',FUNAUDIT = ' ' WHERE FBILLNO = '{0}'", msddBillNo1);
                        DBUtils.Execute(this.Context, sql);
                    }
                }

                this.Model.SetValue("FUNAUDITFLAG", "N");
                this.View.GetControl<Button>("F_ZMER_Button_qtr").Text = "去除免审";

                var delSql = string.Format("/*dialect*/DELETE FROM ZMER_t_Cust100025 WHERE FDOCUMENTSTATUS = 'D' AND FUNAUDIT = '去除免审'");
                DBUtils.Execute(this.Context, delSql);

                return;
            }

            #endregion

            #region 去免审逻辑

            if (flag != "Y")
            {
                // TODO: 去免审逻辑删除已经存在的免审记录
                UnAudit();

                this.Model.SetValue("FUNAUDITFLAG", "Y");
                this.View.GetControl<Button>("F_ZMER_Button_qtr").Text = "还原免审";

                // 清空背景色
                SetEntryBackColor("FPOOrderEntry", null);
            }

            #endregion
        }

        private void SetEntryIdBackColor(string entryKey, long entryId, string color)
        {
            var grid = this.View.GetControl<EntryGrid>(entryKey);
            if (grid == null)
                return;

            var entity = this.View.BusinessInfo.GetEntity(entryKey);
            if (entity == null)
                return;

            int rowCount = this.Model.GetEntryRowCount(entryKey);

            for (int i = 0; i < rowCount; i++)
            {
                var rowObj = this.Model.GetEntityDataObject(entity, i);
                if (rowObj == null)
                    continue;

                long currentEntryId = Convert.ToInt64(rowObj["Id"]);

                if (currentEntryId == entryId)
                {
                    grid.SetRowBackcolor(color, i);
                    break;
                }
            }
        }

        private void SetEntryBackColor(string entryKey, string color)
        {
            var grid = this.View.GetControl<EntryGrid>(entryKey);
            if (grid == null)
                return;

            int rowCount = this.Model.GetEntryRowCount(entryKey);

            for (int i = 0; i < rowCount; i++)
            {
                grid.SetRowBackcolor(color, i);
            }
        }

        /// <summary>
        /// 免审
        /// </summary>
        /*private void Audit()
        {
            string gysName = GetSupplierName();
            string purBillno = this.Model.GetValue("FBillNo")?.ToString();

            var entity = this.View.BillBusinessInfo.GetEntity("FPOOrderEntry");
            var entityObjs = this.View.Model.GetEntityDataObject(entity);

            // 构造数据集合
            List<Dictionary<string, object>> entryList = new List<Dictionary<string, object>>();

            for (int i = 0; i < entityObjs.Count; i++)
            {
                //string wlnum = GetMaterialNumber(i);
                string wlid = GetMaterialId(i);
                decimal qty = Convert.ToDecimal(entityObjs[i]["Qty"]);
                decimal price = Convert.ToDecimal(entityObjs[i]["TaxPrice"]);

                var row = new Dictionary<string, object>
                {
                    { "MaterialNumber", wlid },
                    { "Qty", qty },
                    { "Price", price }
                };

                entryList.Add(row);
            }

            var showParam = new BillShowParameter
            {
                FormId = "k7078466f851f47eca6565bdb352045d5",
                Status = OperationStatus.ADDNEW,
                Width = 1200,
                Height = 400
            };

            showParam.CustomParams.Add("SupplierName", gysName);
            showParam.CustomParams.Add("PurBillNo", purBillno);

            showParam.CustomParams.Add("EntryData", Newtonsoft.Json.JsonConvert.SerializeObject(entryList));

            this.View.ShowForm(showParam);
        }*/
        private string GetMaterialId(int i)
        {
            DynamicObject materialObj = this.View.Model.GetValue("FMaterialId", i) as DynamicObject;
            if (materialObj == null)
                return string.Empty;

            string materialId = Convert.ToString(materialObj["Id"]);
            return materialId;
        }


        /// <summary>
        /// 去免审
        /// </summary>
        private void UnAudit()
        {
            string gysName = GetSupplierName();

            var entity = this.View.BillBusinessInfo.GetEntity("FPOOrderEntry");
            var entityObjs = this.View.Model.GetEntityDataObject(entity);

            for (int i = 0; i < entityObjs.Count; i++)
            {
                string wlnum = GetMaterialNumber(i);

                var checkQuery = string.Format(@"/*dialect*/SELECT DISTINCT FNUMBER,S1.FNAME,FEntryID,FBILLNO
                                                    FROM ZMER_t_Cust100025 A
                                                    JOIN ZMER_t_Cust_Entry100101 B ON A.FID = B.FID
                                                    JOIN T_BD_MATERIAL M1 ON B.FMATERIALID = M1.FMATERIALID
                                                    JOIN T_BD_SUPPLIER_L S1 ON B.FSUPPLIER = S1.FNAME
                                                    WHERE FNUMBER = '{0}' AND S1.FNAME = '{1}' AND A.FDOCUMENTSTATUS = 'C'", wlnum, gysName);
                DynamicObjectCollection checkResult = DBUtils.ExecuteDynamicObject(this.Context, checkQuery);
                if (checkResult != null && checkResult.Count > 0)
                {
                    for (int j = 0; j < checkResult.Count; j++)
                    {
                        string msddBillNo = Convert.ToString(checkResult[j]["FBILLNO"]);

                        //var updSql = string.Format("/*dialect*/DELETE FROM ZMER_t_Cust100025  WHERE FBILLNO = '{0}'", msddBillNo);

                        var updSql = string.Format("/*dialect*/UPDATE ZMER_t_Cust100025 SET FDOCUMENTSTATUS = 'D',FUNAUDIT = '去除免审' WHERE FBILLNO = '{0}'", msddBillNo);
                        DBUtils.Execute(this.Context, updSql);
                    }


                }


            }
        }

        /// <summary>
        /// 根据单据体行号获取物料编码
        /// </summary>
        /// <param name="rowIndex">单据体行号</param>
        /// <returns>物料编码，未找到返回空字符串</returns>
        private string GetMaterialNumber(int rowIndex)
        {
            DynamicObject materialObj = this.View.Model.GetValue("FMaterialId", rowIndex) as DynamicObject;
            if (materialObj == null)
                return string.Empty;

            string materialId = Convert.ToString(materialObj["Id"]);
            if (string.IsNullOrWhiteSpace(materialId))
                return string.Empty;

            string sql = string.Format(
                "/*dialect*/SELECT FNUMBER FROM T_BD_MATERIAL WHERE FMATERIALID = '{0}'",
                materialId);

            DynamicObjectCollection result = DBUtils.ExecuteDynamicObject(this.Context, sql);

            if (result == null || result.Count == 0)
                return string.Empty;

            return Convert.ToString(result[0]["FNUMBER"]);
        }

        /// <summary>
        /// 获取当前单据上的供应商名称
        /// </summary>
        /// <returns>供应商名称，未找到返回空字符串</returns>
        private string GetSupplierName()
        {
            DynamicObject gysObj = this.View.Model.GetValue("FSupplierId") as DynamicObject;
            if (gysObj == null) return string.Empty;

            string gysId = Convert.ToString(gysObj["Id"]);
            if (string.IsNullOrWhiteSpace(gysId)) return string.Empty;

            string sql = string.Format(
                "/*dialect*/SELECT FNAME FROM T_BD_SUPPLIER_L WHERE FSUPPLIERID = '{0}'",
                gysId);

            DynamicObjectCollection result = DBUtils.ExecuteDynamicObject(this.Context, sql);

            if (result == null || result.Count == 0)
                return string.Empty;

            return Convert.ToString(result[0]["FNAME"]);
        }
    }

    [System.ComponentModel.Description("k7078466f851f47eca6565bdb352045d5 单据插件：填充展示数据")]
    [HotUpdate]
    public class WriteInfo : AbstractBillPlugIn
    {
        public override void AfterCreateNewData(EventArgs e)
        {
            base.AfterCreateNewData(e);

            var param = this.View.OpenParameter;

            string supplierName = param.GetCustomParameter("SupplierName") as string;
            string purBillno = param.GetCustomParameter("PurBillNo") as string;

            /*if (!string.IsNullOrWhiteSpace(supplierName))
            {
                this.View.Model.SetValue("FSupplier", supplierName);
            }*/

            string entryJson = param.GetCustomParameter("EntryData") as string;

            if (!string.IsNullOrWhiteSpace(entryJson))
            {
                var entryList = Newtonsoft.Json.JsonConvert
                    .DeserializeObject<List<Dictionary<string, object>>>(entryJson);

                for (int i = 0; i < entryList.Count; i++)
                {

                    this.View.Model.CreateNewEntryRow("F_ZMER_Entity_ne1");

                    this.View.Model.SetValue("FPURNO", purBillno, i);

                    //string wlid = Convert.ToString(entryList[i]["MaterialId"]);

                    var materialMeta = MetaDataServiceHelper.Load(this.Context, "BD_MATERIAL") as FormMetadata;

                    DynamicObjectType materialType = materialMeta.BusinessInfo.GetDynamicObjectType();

                    DynamicObject materialObj = BusinessDataServiceHelper.Load(
                        this.Context,
                        new object[] { entryList[i]["MaterialNumber"] },
                        materialType
                    ).FirstOrDefault();

                    this.Model.SetValue("FMATERIALID", materialObj, i);

                    this.View.Model.SetValue("FSupplier", supplierName, i);
                    this.View.Model.SetValue("FQty", entryList[i]["Qty"], i);
                    this.View.Model.SetValue("FTaxPrice", entryList[i]["Price"], i);
                }
            }

            // this.View.UpdateView();
        }
    }
}

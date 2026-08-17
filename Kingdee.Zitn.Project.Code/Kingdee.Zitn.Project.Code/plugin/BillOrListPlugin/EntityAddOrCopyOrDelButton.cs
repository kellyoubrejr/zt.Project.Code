using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
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

namespace Kingdee.Zitn.Project.Code.plugin.BillOrListPlugin
{
    [Description("【单据插件】自定义单据体按钮事件（新增、复制、删除行）"), HotUpdate]
    public class EntityAddOrCopyOrDelButton : AbstractBillPlugIn
    {
        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);

            if (e.BarItemKey.Equals("ZMER_tbButton_5"))
            {
                string gys = Convert.ToString(this.View.Model.GetValue("FSUPPLIER", 0));

                var entity = this.View.BusinessInfo.GetEntity("F_ZMER_Entity_ne1");

                int rowCount = this.Model.GetEntryRowCount(entity.Key);

                List<string> materialIds = new List<string>();

                for (int i = 0; i < rowCount; i++)
                {
                    DynamicObject matObj = this.Model.GetValue("FMATERIALID", i) as DynamicObject;

                    if (matObj != null)
                    {
                        string id = matObj["Id"].ToString().Trim('\'', '"');
                        if (!string.IsNullOrEmpty(id))
                        {
                            materialIds.Add($"'{id}'");
                        }
                    }
                }
                /*string excludeIds = string.Join(",", materialIds);*/
                string excludeIds = materialIds.Count > 0 ? string.Join(",", materialIds) : "";

                var showParam = new BillShowParameter
                {
                    FormId = "k71bdcee16a944e70a47ed5c6a9abf37d",
                    Status = OperationStatus.ADDNEW,
                    Width = 900,
                    Height = 800
                };
                showParam.CustomParams.Add("F_ZMER_Text_re5", gys);
                showParam.CustomParams.Add("ExcludeMaterialIds", excludeIds);

                //this.View.ShowForm(showParam);
                this.View.ShowForm(showParam, new Action<FormResult>(result =>
                {
                    if (result == null || result.ReturnData == null)
                        return;

                    var list = result.ReturnData as List<DynamicObject>;
                    if (list == null || list.Count == 0)
                        return;

                    BatchAddEntry(list);
                }));
            }


            /*var entity1 = this.View.BusinessInfo.GetEntity("F_ZMER_Entity_ne1");
            var entryKey = entity1.Key;

            // 获取表体控件
            var entryGrid = this.View.GetControl<EntryGrid>(entryKey);
            var selectedRows = entryGrid.GetSelectedRows();
            int rowIndex = selectedRows[0];

            var rowCount1 = this.Model.GetEntryRowCount("F_ZMER_Entity_ne1");*/

            var entryKey = "F_ZMER_Entity_ne1";

            /*int index = this.View.Model.GetEntryCurrentRowIndex("F_ZMER_Entity_ne1");
            DynamicObject obj = this.Model.DataObject;
            DynamicObjectCollection rows = obj["ZMER_Cust_Entry100101"] as DynamicObjectCollection;
            var aa = rows[index];*/

            //当前选中行行号

            int[] selectedIndexsR = this.View.GetControl<EntryGrid>("F_ZMER_Entity_ne1").GetSelectedRows();

            //单据体数据

            DynamicObjectCollection selectedRowsDy = this.Model.DataObject["ZMER_Cust_Entry100101"] as DynamicObjectCollection;

            //选中行数据

            DynamicObject selectedRow = selectedRowsDy[selectedIndexsR[0]];


            var dqsj = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var rowIndex = selectedIndexsR[0];
            var purno = selectedRow["FPURNO"];
            var gysname = selectedRow["FSUPPLIER"];
            var wlid = selectedRow["FMATERIALID"];
            var fqty = selectedRow["FQTY"];
            var ftaxprice = selectedRow["FTAXPRICE"];
            var shangxian = selectedRow["F_ZMER_Text_qtr"];
            var xiaxian = selectedRow["F_ZMER_Text_83g"];
            var danwei = selectedRow["FDANWEI"];


            // =========================
            // 1️⃣ 新增行（按钮：ZMER_tbButton）
            // =========================
            if (e.BarItemKey.Equals("ZMER_tbButton"))
            {
                int rowIndex1 = this.Model.GetEntryRowCount(entryKey);

                // 在当前行下面插入
                this.Model.InsertEntryRow(entryKey, rowIndex1);
                this.View.Model.SetValue("FPURNO", "新增行", rowIndex1);
                this.View.Model.SetValue("FSUPPLIER", gysname, rowIndex1);
                this.View.Model.SetValue("FMATERIALID", wlid, rowIndex1);
                this.View.Model.SetValue("FQTY", fqty, rowIndex1);
                this.View.Model.SetValue("FTAXPRICE", null, rowIndex1);
                this.View.Model.SetValue("F_ZMER_Text_qtr", null, rowIndex1);
                this.View.Model.SetValue("F_ZMER_Text_83g", null, rowIndex1);
                this.View.Model.SetValue("fmodtime", null, rowIndex1);
                this.View.Model.SetValue("fdanwei", danwei, rowIndex1);


                this.View.UpdateView(entryKey);
            }
            // =========================
            // 2️⃣ 插入行（按钮：ZMER_tbButton_4）
            // =========================
            if (e.BarItemKey.Equals("ZMER_tbButton_4"))
            {
                this.Model.InsertEntryRow(entryKey, rowIndex + 1);
                this.View.Model.SetValue("FPURNO", "插入行", rowIndex + 1);

                this.View.Model.SetValue("FSUPPLIER", gysname, rowIndex + 1);
                this.View.Model.SetValue("FMATERIALID", wlid, rowIndex + 1);
                this.View.Model.SetValue("FQTY", fqty, rowIndex + 1);
                this.View.Model.SetValue("FTAXPRICE", ftaxprice, rowIndex + 1);
                this.View.Model.SetValue("F_ZMER_Text_qtr", null, rowIndex + 1);
                this.View.Model.SetValue("F_ZMER_Text_83g", null, rowIndex + 1);
                this.View.Model.SetValue("fmodtime", dqsj, rowIndex + 1);
                this.View.Model.SetValue("fdanwei", danwei, rowIndex + 1);

                this.View.UpdateView(entryKey);
            }
            // =========================
            // 3️⃣ 复制行（按钮：ZMER_tbButton_1）
            // =========================
            if (e.BarItemKey.Equals("ZMER_tbButton_1"))
            {
                int newRow = rowIndex + 1;

                // 插入新行
                this.Model.InsertEntryRow(entryKey, newRow);
                this.View.Model.SetValue("FPURNO", "复制行", rowIndex + 1);

                this.View.Model.SetValue("FSUPPLIER", gysname, rowIndex + 1);
                this.View.Model.SetValue("FMATERIALID", wlid, rowIndex + 1);
                this.View.Model.SetValue("FQTY", fqty, rowIndex + 1);
                this.View.Model.SetValue("FTAXPRICE", ftaxprice, rowIndex + 1);
                this.View.Model.SetValue("F_ZMER_Text_qtr", null, rowIndex + 1);
                this.View.Model.SetValue("F_ZMER_Text_83g", null, rowIndex + 1);
                this.View.Model.SetValue("fmodtime", dqsj, rowIndex + 1);
                this.View.Model.SetValue("fdanwei", danwei, rowIndex + 1);

                this.View.UpdateView(entryKey);
            }
            if (e.BarItemKey.Equals("ZMER_tbButton_2"))
            {
                if (rowIndex == 0)
                {
                    this.View.ShowErrMessage("不能删除第一行,无法保存");
                }
                else
                {
                    this.Model.DeleteEntryRow(entryKey, rowIndex);
                }

            }
        }

        #region 回填信息

        private void BatchAddEntry(List<DynamicObject> list)
        {
            var entity = this.View.BusinessInfo.GetEntity("F_ZMER_Entity_ne1");

            string supplier = Convert.ToString(this.Model.GetValue("FSUPPLIER"));

            foreach (var data in list)
            {
                int newRow = this.Model.GetEntryRowCount(entity.Key);
                this.Model.CreateNewEntryRow(entity.Key);

                this.Model.SetValue("FPURNO", "回填", newRow);
                this.Model.SetValue("FSUPPLIER", supplier, newRow);
                this.Model.SetValue("FMATERIALID", data["FMATERIALID"], newRow);
                this.Model.SetValue("FQTY", data["FQTY"], newRow);
                this.Model.SetValue("FTAXPRICE", data["FTAXPRICE"], newRow);
            }

            this.View.UpdateView("F_ZMER_Entity_ne1");
        }

        #endregion
    }

    [Description("供应商历史采购物料情况单据插件：接收并填充数据"), HotUpdate]
    public class Fill : AbstractBillPlugIn
    {
        #region 填充数据信息

        public override void AfterCreateNewData(EventArgs e)
        {
            base.AfterCreateNewData(e);

            var param = this.View.OpenParameter;

            string gysName = param.GetCustomParameter("F_ZMER_Text_re5").ToString();
            string excludeIds = param.GetCustomParameter("ExcludeMaterialIds").ToString().Trim('\'', '"');
            this.View.Model.SetValue("F_ZMER_Text_re5", gysName);

            if (excludeIds.IsNullOrEmptyOrWhiteSpace())
            {
                var wlQuery = string.Format(@"/*dialect*/  SELECT distinct
                                                      B.FMATERIALID AS wlid,
                                                      B.FQTY,
                                                      B1.FTAXPRICE 
                                                    FROM
                                                      T_PUR_POORDER A
                                                      JOIN T_PUR_POORDERENTRY B ON A.FID = B.FID
                                                      JOIN T_BD_SUPPLIER_L S1 ON A.FSUPPLIERID = S1.FSUPPLIERID
                                                      JOIN T_PUR_POORDERENTRY_F B1 ON B.FENTRYID = B1.FENTRYID 
                                                    WHERE
                                                      S1.FNAME = '{0}'", gysName);
                DynamicObjectCollection wlResult = DBUtils.ExecuteDynamicObject(this.Context, wlQuery);
                if (wlResult != null && wlResult.Count > 0)
                {
                    this.Model.DeleteEntryData("F_ZMER_Entity_apv");

                    for (int j = 0; j < wlResult.Count; j++)
                    {
                        string materialId = wlResult[j]["wlid"].ToString();
                        int materialQty = Convert.ToInt32(wlResult[j]["FQTY"]);
                        decimal materialPrice = Convert.ToDecimal(wlResult[j]["FTAXPRICE"]);

                        this.Model.CreateNewEntryRow("F_ZMER_Entity_apv");

                        var materialMeta = MetaDataServiceHelper.Load(this.Context, "BD_MATERIAL") as FormMetadata;
                        DynamicObjectType materialType = materialMeta.BusinessInfo.GetDynamicObjectType();

                        DynamicObject materialObj = BusinessDataServiceHelper.Load(
                            this.Context,
                            new object[] { materialId },
                            materialType
                        ).FirstOrDefault();

                        this.Model.SetValue("FMATERIALID", materialObj, j);
                        this.Model.SetValue("FQTY", materialQty, j);
                        this.Model.SetValue("FTAXPRICE", materialPrice, j);
                    }
                }
            }
            else
            {
                var wlSql = string.Format(@"/*dialect*/  SELECT distinct
                                                      B.FMATERIALID AS wlid,
                                                      B.FQTY,
                                                      B1.FTAXPRICE 
                                                    FROM
                                                      T_PUR_POORDER A
                                                      JOIN T_PUR_POORDERENTRY B ON A.FID = B.FID
                                                      JOIN T_BD_SUPPLIER_L S1 ON A.FSUPPLIERID = S1.FSUPPLIERID
                                                      JOIN T_PUR_POORDERENTRY_F B1 ON B.FENTRYID = B1.FENTRYID 
                                                    WHERE
                                                      S1.FNAME = '{0}' AND  B.FMATERIALID NOT IN ('{1}')", gysName, excludeIds);
                DynamicObjectCollection Result = DBUtils.ExecuteDynamicObject(this.Context, wlSql);
                if (Result != null && Result.Count > 0)
                {
                    this.Model.DeleteEntryData("F_ZMER_Entity_apv");

                    for (int i = 0; i < Result.Count; i++)
                    {
                        string wlid2 = Result[i]["wlid"].ToString();
                        int wlsl = Convert.ToInt32(Result[i]["FQTY"]);
                        decimal wljg = Convert.ToDecimal(Result[i]["FTAXPRICE"]);

                        this.Model.CreateNewEntryRow("F_ZMER_Entity_apv");

                        var materialMeta = MetaDataServiceHelper.Load(this.Context, "BD_MATERIAL") as FormMetadata;
                        DynamicObjectType materialType = materialMeta.BusinessInfo.GetDynamicObjectType();

                        DynamicObject materialObj = BusinessDataServiceHelper.Load(
                            this.Context,
                            new object[] { wlid2 },
                            materialType
                        ).FirstOrDefault();

                        this.Model.SetValue("FMATERIALID", materialObj, i);
                        this.Model.SetValue("FQTY", wlsl, i);
                        this.Model.SetValue("FTAXPRICE", wljg, i);

                    }
                }
            }
        }

        #endregion      

        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);

            if (!e.Key.Equals("F_ZMER_Button_qtr", System.StringComparison.OrdinalIgnoreCase))
                return;

            ReturnSelectedData();
        }

        #region  父窗体返回数据填充

        private void ReturnSelectedData()
        {
            var entity = this.View.BusinessInfo.GetEntity("F_ZMER_Entity_apv");

            int rowCount = this.Model.GetEntryRowCount(entity.Key);

            var resultList = new List<DynamicObject>();

            for (int i = 0; i < rowCount; i++)
            {
                bool isSelected = Convert.ToBoolean(this.Model.GetValue("F_ZMER_CheckBox_tzk", i));

                if (!isSelected)
                    continue;

                var material = this.Model.GetValue("FMATERIALID", i);
                var qty = this.Model.GetValue("FQTY", i);
                var price = this.Model.GetValue("FTAXPRICE", i);

                DynamicObject obj = new DynamicObject(entity.DynamicObjectType);
                obj["FMATERIALID"] = material;
                obj["FQTY"] = qty;
                obj["FTAXPRICE"] = price;

                resultList.Add(obj);
            }

            this.View.ReturnToParentWindow(resultList);
        }

        #endregion        
    }
}

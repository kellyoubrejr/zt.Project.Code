using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZT.ZTDataTracer;
using System.Data;

namespace ZT.Cloud.MesModel.Models
{
    internal class ClassSQLServer:ZT.ZTDataTracer.ClassBaseSQLServer
    {
        public ClassSQLServer(Plants Plant):base("127.0.0.1", "ZTCloud", "sa", "Kd@123")
        {


        }

        #region DeviceLedger CRUD

        public DataTable QueryDevices()
        {
            return this.FillDatable("sp_DeviceLedger_QueryAll", null, CommandType.StoredProcedure);
        }

        public int InsertDevice(ClassDevice data)
        {
            IDbDataParameter[] parms = new IDbDataParameter[]
            {
                MakeInParam("@SeqNo", DbType.Int32, 0, data.SeqNo),
                MakeInParam("@DeviceCode", DbType.String, 50, data.DeviceCode ?? ""),
                MakeInParam("@DeviceName", DbType.String, 100, data.DeviceName ?? ""),
                MakeInParam("@Specification", DbType.String, 100, data.Specification ?? ""),
                MakeInParam("@Brand", DbType.String, 100, data.Brand ?? ""),
                MakeInParam("@DeviceStatus", DbType.String, 20, data.DeviceStatus ?? ""),
                MakeInParam("@UsageScope", DbType.String, 50, data.UsageScope ?? ""),
                MakeInParam("@UsageCategory", DbType.String, 50, data.UsageCategory ?? ""),
                MakeInParam("@CurrentDept", DbType.String, 50, data.CurrentDept ?? ""),
                MakeInParam("@CurrentLocation", DbType.String, 100, data.CurrentLocation ?? ""),
                MakeInParam("@CurrentPerson", DbType.String, 50, data.CurrentPerson ?? ""),
                MakeInParam("@CalibrationDate", DbType.String, 20, data.CalibrationDate ?? ""),
                MakeInParam("@NextCalibrationDate", DbType.String, 20, data.NextCalibrationDate ?? ""),
                MakeInParam("@QualificationDate", DbType.String, 20, data.QualificationDate ?? "")
            };

            object result = this.ExecuteScalar("sp_DeviceLedger_Insert", parms, CommandType.StoredProcedure);
            return Convert.ToInt32(result);
        }

        public int UpdateDevice(ClassDevice data)
        {
            IDbDataParameter[] parms = new IDbDataParameter[]
            {
                MakeInParam("@Id", DbType.Int32, 0, data.Id),
                MakeInParam("@SeqNo", DbType.Int32, 0, data.SeqNo),
                MakeInParam("@DeviceCode", DbType.String, 50, data.DeviceCode ?? ""),
                MakeInParam("@DeviceName", DbType.String, 100, data.DeviceName ?? ""),
                MakeInParam("@Specification", DbType.String, 100, data.Specification ?? ""),
                MakeInParam("@Brand", DbType.String, 100, data.Brand ?? ""),
                MakeInParam("@DeviceStatus", DbType.String, 20, data.DeviceStatus ?? ""),
                MakeInParam("@UsageScope", DbType.String, 50, data.UsageScope ?? ""),
                MakeInParam("@UsageCategory", DbType.String, 50, data.UsageCategory ?? ""),
                MakeInParam("@CurrentDept", DbType.String, 50, data.CurrentDept ?? ""),
                MakeInParam("@CurrentLocation", DbType.String, 100, data.CurrentLocation ?? ""),
                MakeInParam("@CurrentPerson", DbType.String, 50, data.CurrentPerson ?? ""),
                MakeInParam("@CalibrationDate", DbType.String, 20, data.CalibrationDate ?? ""),
                MakeInParam("@NextCalibrationDate", DbType.String, 20, data.NextCalibrationDate ?? ""),
                MakeInParam("@QualificationDate", DbType.String, 20, data.QualificationDate ?? "")
            };

            return this.ExecuteNonQuery("sp_DeviceLedger_Update", parms, CommandType.StoredProcedure);
        }

        public int DeleteDevice(int id)
        {
            IDbDataParameter[] parms = new IDbDataParameter[]
            {
                MakeInParam("@Id", DbType.Int32, 0, id)
            };
            return this.ExecuteNonQuery("sp_DeviceLedger_Delete", parms, CommandType.StoredProcedure);
        }

        public void LogDeviceOperation(string operationType, int recordId, string deviceName, string deviceCode, string operatorName, string details)
        {
            IDbDataParameter[] parms = new IDbDataParameter[]
            {
                MakeInParam("@OperationType", DbType.String, 20, operationType),
                MakeInParam("@RecordId", DbType.Int32, 0, recordId),
                MakeInParam("@DeviceName", DbType.String, 100, deviceName ?? ""),
                MakeInParam("@DeviceCode", DbType.String, 50, deviceCode ?? ""),
                MakeInParam("@Operator", DbType.String, 50, operatorName ?? ""),
                MakeInParam("@Details", DbType.String, 500, details ?? "")
            };

            this.ExecuteNonQuery("sp_DeviceLedger_LogInsert", parms, CommandType.StoredProcedure);
        }

        #endregion

        public DataTable QueryMaintPlans()
        {
            return this.FillDatable("sp_EquipmentMaintPlan_QueryAll", null, CommandType.StoredProcedure);
        }

        public int InsertMaintPlan(ClassMaintPlan data)
        {
            IDbDataParameter[] parms = new IDbDataParameter[]
            {
                MakeInParam("@SeqNo", DbType.Int32, 0, data.SeqNo),
                MakeInParam("@EquipmentName", DbType.String, 100, data.EquipmentName ?? ""),
                MakeInParam("@FileNo", DbType.String, 50, data.FileNo ?? ""),
                MakeInParam("@Version", DbType.String, 20, data.Version ?? ""),
                MakeInParam("@MaintCycle", DbType.String, 200, data.MaintCycle ?? ""),
                MakeInParam("@Quantity", DbType.Int32, 0, data.Quantity),
                MakeInParam("@Jan", DbType.String, 50, data.Jan ?? ""),
                MakeInParam("@Feb", DbType.String, 50, data.Feb ?? ""),
                MakeInParam("@Mar", DbType.String, 50, data.Mar ?? ""),
                MakeInParam("@Apr", DbType.String, 50, data.Apr ?? ""),
                MakeInParam("@May", DbType.String, 50, data.May ?? ""),
                MakeInParam("@Jun", DbType.String, 50, data.Jun ?? ""),
                MakeInParam("@Jul", DbType.String, 50, data.Jul ?? ""),
                MakeInParam("@Aug", DbType.String, 50, data.Aug ?? ""),
                MakeInParam("@Sep", DbType.String, 50, data.Sep ?? ""),
                MakeInParam("@Oct", DbType.String, 50, data.Oct ?? ""),
                MakeInParam("@Nov", DbType.String, 50, data.Nov ?? ""),
                MakeInParam("@Dec", DbType.String, 50, data.Dec ?? "")
            };

            object result = this.ExecuteScalar("sp_EquipmentMaintPlan_Insert", parms, CommandType.StoredProcedure);
            return Convert.ToInt32(result);
        }

        public int UpdateMaintPlan(ClassMaintPlan data)
        {
            IDbDataParameter[] parms = new IDbDataParameter[]
            {
                MakeInParam("@Id", DbType.Int32, 0, data.Id),
                MakeInParam("@SeqNo", DbType.Int32, 0, data.SeqNo),
                MakeInParam("@EquipmentName", DbType.String, 100, data.EquipmentName ?? ""),
                MakeInParam("@FileNo", DbType.String, 50, data.FileNo ?? ""),
                MakeInParam("@Version", DbType.String, 20, data.Version ?? ""),
                MakeInParam("@MaintCycle", DbType.String, 200, data.MaintCycle ?? ""),
                MakeInParam("@Quantity", DbType.Int32, 0, data.Quantity),
                MakeInParam("@Jan", DbType.String, 50, data.Jan ?? ""),
                MakeInParam("@Feb", DbType.String, 50, data.Feb ?? ""),
                MakeInParam("@Mar", DbType.String, 50, data.Mar ?? ""),
                MakeInParam("@Apr", DbType.String, 50, data.Apr ?? ""),
                MakeInParam("@May", DbType.String, 50, data.May ?? ""),
                MakeInParam("@Jun", DbType.String, 50, data.Jun ?? ""),
                MakeInParam("@Jul", DbType.String, 50, data.Jul ?? ""),
                MakeInParam("@Aug", DbType.String, 50, data.Aug ?? ""),
                MakeInParam("@Sep", DbType.String, 50, data.Sep ?? ""),
                MakeInParam("@Oct", DbType.String, 50, data.Oct ?? ""),
                MakeInParam("@Nov", DbType.String, 50, data.Nov ?? ""),
                MakeInParam("@Dec", DbType.String, 50, data.Dec ?? "")
            };

            return this.ExecuteNonQuery("sp_EquipmentMaintPlan_Update", parms, CommandType.StoredProcedure);
        }

        public int DeleteMaintPlan(int id)
        {
            IDbDataParameter[] parms = new IDbDataParameter[]
            {
                MakeInParam("@Id", DbType.Int32, 0, id)
            };
            return this.ExecuteNonQuery("sp_EquipmentMaintPlan_Delete", parms, CommandType.StoredProcedure);
        }

        public void LogMaintPlanOperation(string operationType, int recordId, string equipmentName, string fileNo, string operatorName, string details)
        {
            IDbDataParameter[] parms = new IDbDataParameter[]
            {
                MakeInParam("@OperationType", DbType.String, 20, operationType),
                MakeInParam("@RecordId", DbType.Int32, 0, recordId),
                MakeInParam("@EquipmentName", DbType.String, 100, equipmentName ?? ""),
                MakeInParam("@FileNo", DbType.String, 50, fileNo ?? ""),
                MakeInParam("@Operator", DbType.String, 50, operatorName ?? ""),
                MakeInParam("@Details", DbType.String, 500, details ?? "")
            };

            this.ExecuteNonQuery("sp_EquipmentMaintPlan_LogInsert", parms, CommandType.StoredProcedure);
        }
    }
}

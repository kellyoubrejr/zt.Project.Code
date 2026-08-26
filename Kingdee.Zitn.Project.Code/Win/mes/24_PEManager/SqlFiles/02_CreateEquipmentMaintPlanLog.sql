-- ============================================
-- 设备保养计划操作日志表
-- 数据库: ZTCloudData
-- 说明: 记录设备保养计划的增删改操作
-- ============================================

CREATE TABLE dbo.EquipmentMaintPlanLog
(
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    OperationType   NVARCHAR(20)    NOT NULL,   -- INSERT / UPDATE / DELETE
    RecordId        INT             NOT NULL,   -- 对应保养计划记录的Id
    EquipmentName   NVARCHAR(100)   NULL,       -- 设备名称(快照)
    FileNo          NVARCHAR(50)    NULL,       -- 文件编号(快照)
    OperationTime   DATETIME        NOT NULL DEFAULT GETDATE(),
    Operator        NVARCHAR(50)    NULL,       -- 操作人
    Details         NVARCHAR(500)   NULL        -- 变更描述
);

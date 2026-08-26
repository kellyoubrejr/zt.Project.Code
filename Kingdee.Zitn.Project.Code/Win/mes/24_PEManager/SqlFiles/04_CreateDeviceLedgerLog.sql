-- ============================================
-- 设备管理台账操作日志表
-- 数据库: ZTCloudData
-- 说明: 记录设备管理台账的增删改操作
-- ============================================

USE [ZTCloudData]
GO

IF OBJECT_ID(N'dbo.DeviceLedgerLog', N'U') IS NOT NULL
    DROP TABLE dbo.DeviceLedgerLog;
GO

CREATE TABLE dbo.DeviceLedgerLog
(
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    OperationType   NVARCHAR(20)    NOT NULL,   -- INSERT / UPDATE / DELETE
    RecordId        INT             NOT NULL,   -- 对应台账记录的Id
    DeviceName      NVARCHAR(100)   NULL,       -- 设备名称(快照)
    DeviceCode      NVARCHAR(50)    NULL,       -- 设备编号(快照)
    OperationTime   DATETIME        NOT NULL DEFAULT GETDATE(),
    Operator        NVARCHAR(50)    NULL,       -- 操作人
    Details         NVARCHAR(500)   NULL        -- 变更描述
);
GO

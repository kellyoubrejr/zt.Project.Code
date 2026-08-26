-- ============================================
-- 设备保养计划表 + 存储过程
-- 数据库: ZTCloud
-- 说明: 存储设备年度保养计划数据
-- ============================================

USE [ZTCloud]
GO

-- ============================================
-- 1. 创建表
-- ============================================
IF OBJECT_ID(N'dbo.EquipmentMaintPlan', N'U') IS NOT NULL
    DROP TABLE dbo.EquipmentMaintPlan;
GO

CREATE TABLE dbo.EquipmentMaintPlan
(
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    SeqNo           INT             NOT NULL,
    EquipmentName   NVARCHAR(100)   NOT NULL,
    FileNo          NVARCHAR(50)    NOT NULL,
    Version         NVARCHAR(20)    NOT NULL,
    MaintCycle      NVARCHAR(200)   NOT NULL,
    Quantity        INT             NOT NULL,
    Jan             NVARCHAR(50)    NULL,
    Feb             NVARCHAR(50)    NULL,
    Mar             NVARCHAR(50)    NULL,
    Apr             NVARCHAR(50)    NULL,
    May             NVARCHAR(50)    NULL,
    Jun             NVARCHAR(50)    NULL,
    Jul             NVARCHAR(50)    NULL,
    Aug             NVARCHAR(50)    NULL,
    Sep             NVARCHAR(50)    NULL,
    Oct             NVARCHAR(50)    NULL,
    Nov             NVARCHAR(50)    NULL,
    Dec             NVARCHAR(50)    NULL
);
GO

-- ============================================
-- 2. 存储过程: 查询全部
-- ============================================
IF OBJECT_ID(N'dbo.sp_EquipmentMaintPlan_QueryAll', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_EquipmentMaintPlan_QueryAll;
GO

CREATE PROCEDURE dbo.sp_EquipmentMaintPlan_QueryAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.EquipmentMaintPlan ORDER BY SeqNo;
END
GO

-- ============================================
-- 3. 存储过程: 新增
-- ============================================
IF OBJECT_ID(N'dbo.sp_EquipmentMaintPlan_Insert', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_EquipmentMaintPlan_Insert;
GO

CREATE PROCEDURE dbo.sp_EquipmentMaintPlan_Insert
    @SeqNo          INT,
    @EquipmentName  NVARCHAR(100),
    @FileNo         NVARCHAR(50),
    @Version        NVARCHAR(20),
    @MaintCycle     NVARCHAR(200),
    @Quantity       INT,
    @Jan            NVARCHAR(50),
    @Feb            NVARCHAR(50),
    @Mar            NVARCHAR(50),
    @Apr            NVARCHAR(50),
    @May            NVARCHAR(50),
    @Jun            NVARCHAR(50),
    @Jul            NVARCHAR(50),
    @Aug            NVARCHAR(50),
    @Sep            NVARCHAR(50),
    @Oct            NVARCHAR(50),
    @Nov            NVARCHAR(50),
    @Dec            NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.EquipmentMaintPlan
        (SeqNo, EquipmentName, FileNo, Version, MaintCycle, Quantity,
         Jan, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec)
    VALUES
        (@SeqNo, @EquipmentName, @FileNo, @Version, @MaintCycle, @Quantity,
         @Jan, @Feb, @Mar, @Apr, @May, @Jun, @Jul, @Aug, @Sep, @Oct, @Nov, @Dec);
    SELECT SCOPE_IDENTITY() AS NewId;
END
GO

-- ============================================
-- 4. 存储过程: 编辑
-- ============================================
IF OBJECT_ID(N'dbo.sp_EquipmentMaintPlan_Update', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_EquipmentMaintPlan_Update;
GO

CREATE PROCEDURE dbo.sp_EquipmentMaintPlan_Update
    @Id             INT,
    @SeqNo          INT,
    @EquipmentName  NVARCHAR(100),
    @FileNo         NVARCHAR(50),
    @Version        NVARCHAR(20),
    @MaintCycle     NVARCHAR(200),
    @Quantity       INT,
    @Jan            NVARCHAR(50),
    @Feb            NVARCHAR(50),
    @Mar            NVARCHAR(50),
    @Apr            NVARCHAR(50),
    @May            NVARCHAR(50),
    @Jun            NVARCHAR(50),
    @Jul            NVARCHAR(50),
    @Aug            NVARCHAR(50),
    @Sep            NVARCHAR(50),
    @Oct            NVARCHAR(50),
    @Nov            NVARCHAR(50),
    @Dec            NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.EquipmentMaintPlan SET
        SeqNo           = @SeqNo,
        EquipmentName   = @EquipmentName,
        FileNo          = @FileNo,
        Version         = @Version,
        MaintCycle      = @MaintCycle,
        Quantity        = @Quantity,
        Jan = @Jan, Feb = @Feb, Mar = @Mar,
        Apr = @Apr, May = @May, Jun = @Jun,
        Jul = @Jul, Aug = @Aug, Sep = @Sep,
        Oct = @Oct, Nov = @Nov, Dec = @Dec
    WHERE Id = @Id;
END
GO

-- ============================================
-- 5. 存储过程: 删除
-- ============================================
IF OBJECT_ID(N'dbo.sp_EquipmentMaintPlan_Delete', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_EquipmentMaintPlan_Delete;
GO

CREATE PROCEDURE dbo.sp_EquipmentMaintPlan_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.EquipmentMaintPlan WHERE Id = @Id;
END
GO

-- ============================================
-- 6. 存储过程: 写入操作日志 (跨库写入 ZTCloudData)
-- ============================================
IF OBJECT_ID(N'dbo.sp_EquipmentMaintPlan_LogInsert', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_EquipmentMaintPlan_LogInsert;
GO

CREATE PROCEDURE dbo.sp_EquipmentMaintPlan_LogInsert
    @OperationType  NVARCHAR(20),
    @RecordId       INT,
    @EquipmentName  NVARCHAR(100),
    @FileNo         NVARCHAR(50),
    @Operator       NVARCHAR(50),
    @Details        NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [ZTCloudData].[dbo].[EquipmentMaintPlanLog]
        (OperationType, RecordId, EquipmentName, FileNo, Operator, Details)
    VALUES
        (@OperationType, @RecordId, @EquipmentName, @FileNo, @Operator, @Details);
END
GO

-- ============================================
-- 7. 插入初始数据 (来源: 设备保养计划MES.xlsx)
-- ============================================
INSERT INTO dbo.EquipmentMaintPlan (SeqNo, EquipmentName, FileNo, Version, MaintCycle, Quantity, Jan, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec) VALUES
(1,  N'超净工作台',           N'QZT-EB-001', N'B1', N'班前/后维护；半年维护；年度维护；三年维护',              7,  N'',  N'年', N'',  N'',  N'',  N'半年', N'',  N'',  N'',  N'',  N'',  N'半年'),
(2,  N'电子防潮箱',           N'QZT-EB-002', N'B1', N'班前/后维护；年度维护',                                47, N'',  N'年', N'',  N'',  N'',  N'',   N'',  N'',  N'',  N'',  N'',  N''),
(3,  N'氮气干燥柜',           N'QZT-EB-002', N'B1', N'班前/后维护；年度维护',                                85, N'',  N'年', N'',  N'',  N'',  N'',   N'',  N'',  N'',  N'',  N'',  N''),
(4,  N'活塞式压力计',         N'QZT-EB-003', N'B1', N'班前/后维护；半年维护；两年维护',                       4,  N'',  N'',   N'',  N'',  N'',  N'半年', N'',  N'',  N'',  N'',  N'',  N'半年'),
(5,  N'平行缝焊机',           N'QZT-EB-004', N'B1', N'班前/后维护；月度维护；季度维护；年度维护',             2,  N'月', N'月', N'月/季', N'月', N'月', N'月/季', N'月', N'月', N'月/季/年', N'月', N'月', N'月/季'),
(6,  N'干燥箱',               N'QZT-EB-005', N'B1', N'班前/后维护；季度维护；半年维护；年度维护',             69, N'',  N'季', N'年', N'',  N'季', N'',   N'',  N'季/半年', N'',  N'',  N'季', N''),
(7,  N'真空烘箱',             N'QZT-EB-005', N'B1', N'班前/后维护；季度维护；半年维护；年度维护',             1,  N'',  N'季', N'年', N'',  N'季', N'',   N'',  N'季/半年', N'',  N'',  N'季', N''),
(8,  N'超声金/铝丝焊机',      N'QZT-EB-006', N'B1', N'班前/后维护；每周维护；月度维护；季度维护；半年维护',   13, N'月', N'月', N'月/季', N'月', N'月', N'月/季/半年', N'月', N'月', N'月/季', N'月', N'月', N'月/季'),
(9,  N'车，铣，钻床',         N'QZT-EB-007', N'B1', N'班前/后维护；季度维护',                                3,  N'',  N'',   N'季', N'',  N'',  N'季', N'',  N'',  N'季', N'',  N'',  N'季'),
(10, N'充磁电源',             N'QZT-EB-008', N'B1', N'班前/后维护；月度维护',                                1,  N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月'),
(11, N'等离子清洗机',         N'QZT-EB-009', N'B1', N'班前/后维护；月度维护；季度维护',                       2,  N'月', N'月/季', N'月', N'月', N'月', N'月/季', N'月', N'月', N'月/季', N'月', N'月', N'月/季'),
(12, N'高低温快速温变试验箱', N'QZT-EB-010', N'B1', N'班前/后维护；季度维护；半年维护；年度维护',             32, N'',  N'',   N'季/半年/年', N'', N'', N'季', N'', N'', N'季/半年', N'', N'', N'季'),
(13, N'高低温试验箱',         N'QZT-EB-011', N'B1', N'班前/后维护；季度维护；半年维护；年度维护',             1,  N'',  N'季/半年/年', N'', N'', N'季', N'', N'', N'季/半年', N'', N'', N'季', N''),
(14, N'压力循环试验台',       N'QZT-EB-012', N'B1', N'班前/后维护；年度维护',                                2,  N'',  N'年', N'',  N'',  N'',  N'',   N'',  N'',  N'',  N'',  N'',  N''),
(15, N'交流点焊机',           N'QZT-EB-013', N'B1', N'班前/后维护；月度维护，年度维护',                       1,  N'月', N'月/年', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月'),
(16, N'恒温槽',               N'QZT-EB-014', N'B1', N'班前/后维护',                                          2,  N'',  N'',   N'',  N'',  N'',  N'',   N'',  N'',  N'',  N'',  N'',  N''),
(17, N'测斜仪校验台',         N'QZT-EB-015', N'B1', N'班前/后维护；季度维护；年度维护',                       4,  N'',  N'',   N'季', N'',  N'',  N'季', N'',  N'',  N'季', N'',  N'',  N'季/年'),
(18, N'激光打标机',           N'QZT-EB-016', N'B1', N'班前/后维护；半年维护',                                2,  N'',  N'半年', N'', N'',  N'',  N'',   N'',  N'半年', N'',  N'',  N'',  N''),
(19, N'恒温恒湿试验箱',       N'QZT-EB-017', N'B1', N'班前/后维护；季度维护；半年维护；年度维护',             2,  N'',  N'季/半年/年', N'', N'', N'季', N'', N'', N'季/半年', N'', N'', N'季', N''),
(20, N'氦质谱检漏仪',         N'QZT-EB-018', N'B1', N'班前/后维护；半年维护；年度维护',                       2,  N'',  N'半年/年度', N'', N'', N'', N'', N'', N'半年', N'', N'', N'', N''),
(21, N'氦气氟油加压平台',     N'QZT-EB-019', N'B1', N'班前/后维护；季度维护；年度维护',                       2,  N'季', N'年', N'',  N'季', N'',  N'',   N'季', N'',  N'',  N'季', N'',  N''),
(22, N'超声波清洗机',         N'QZT-EB-020', N'B1', N'班前/后维护；季度维护',                                7,  N'',  N'季', N'',  N'',  N'季', N'',   N'',  N'季', N'',  N'',  N'季', N''),
(23, N'激光焊接机',           N'QZT-EB-021', N'B1', N'班前/后维护；每周维护；月度维护；半年维护',             2,  N'月', N'月', N'月', N'月', N'月', N'月/半年', N'月', N'月', N'月', N'月', N'月', N'月/半年'),
(24, N'绕线机',               N'QZT-EB-022', N'B',  N'班前/后维护；半年维护',                                1,  N'',  N'',   N'',  N'',  N'',  N'半年', N'',  N'',  N'',  N'',  N'',  N'半年'),
(25, N'精密数控线切割机床',   N'QZT-EB-024', N'B',  N'班前/后维护；每周维护；月维护；半年维护',               1,  N'月', N'月', N'月', N'月/半年', N'月', N'月', N'月', N'月', N'月', N'月/半年', N'月', N'月'),
(26, N'充氮烘箱',             N'QZT-EB-025', N'B',  N'班前/后维护；季度维护；半年维护；年度维护',             1,  N'',  N'季', N'',  N'',  N'季', N'半年', N'',  N'季', N'',  N'',  N'季', N'半年/年'),
(27, N'颗粒碰撞噪声检测仪',   N'QZT-EB-027', N'A',  N'班前/后维护；半年维护',                                2,  N'半年', N'', N'',  N'',  N'',  N'', N'半年', N'',  N'',  N'',  N'',  N''),
(28, N'空气压缩机',           N'QZT-EB-028', N'A',  N'班前/后维护；每周维护；月度维护；季度维护；半年维护',   2,  N'月', N'月', N'月/季', N'月/半年', N'月', N'月/季', N'月', N'月', N'月/季', N'月/半年', N'月', N'月/季'),
(29, N'真空共晶炉',           N'QZT-EB-030', N'A',  N'班前/后维护；月度维护；半年维护；年度维护',             1,  N'月', N'月', N'月/半年/年', N'月', N'月', N'月', N'月', N'月', N'月/半年', N'月', N'月', N'月'),
(30, N'冲击试验台',           N'QZT-EB-031', N'A',  N'班前/后维护；每周维护；月度维护；季度维护；年度维护',   1,  N'月', N'月', N'月/季/年', N'月', N'月', N'月/季', N'月', N'月', N'月/季', N'月', N'月', N'月/季'),
(31, N'金属带锯床',           N'QZT-EB-034', N'A',  N'班前/后维护；月度维护；半年维护',                       1,  N'月/半年', N'月', N'月', N'月', N'月', N'月', N'月/半年', N'月', N'月', N'月', N'月', N'月'),
(32, N'数控车床',             N'QZT-EB-035', N'A',  N'班前/后维护；月度维护；半年维护；年度维护',             1,  N'月/半年/年', N'月', N'月', N'月', N'月', N'月', N'月/半年', N'月', N'月', N'月', N'月', N'月'),
(33, N'平面磨床',             N'QZT-EB-037', N'A',  N'班前/后维护；月度维护；季度维护；年度维护',             1,  N'月/季/年', N'月', N'月', N'月/季', N'月', N'月', N'月/季', N'月', N'月', N'月/季', N'月', N'月'),
(34, N'加工中心',             N'QZT-EB-038', N'A',  N'班前/后维护；月度维护；半年维护；年度维护',             1,  N'月', N'月', N'月', N'月/半年/年', N'月', N'月', N'月', N'月', N'月', N'月/半年', N'月', N'月'),
(35, N'烧结炉',               N'QZT-EB-045', N'A',  N'班前/后维护；月度维护；半年维护；年度维护',             1,  N'月/半年/年', N'月', N'月', N'月', N'月', N'月', N'月/半年', N'月', N'月', N'月', N'月', N'月'),
(36, N'红外干燥炉',           N'QZT-EB-046', N'A',  N'班前/后维护；月度维护；半年维护；年度维护',             1,  N'月/半年/年', N'月', N'月', N'月', N'月', N'月', N'月/半年', N'月', N'月', N'月', N'月', N'月'),
(37, N'刻板机',               N'QZT-EB-048', N'A',  N'班前/后维护；半年维护',                                2,  N'半年', N'', N'',  N'',  N'',  N'', N'半年', N'',  N'',  N'',  N'',  N''),
(38, N'八段回流焊机',         N'QZT-EB-049', N'A',  N'班前/后维护；月度维护；半年维护',                       1,  N'月/半年', N'月', N'月', N'月', N'月', N'月', N'月/半年', N'月', N'月', N'月', N'月', N'月'),
(39, N'激光调阻机',           N'QZT-EB-050', N'A',  N'班前/后维护；半年维护',                                1,  N'半年', N'', N'',  N'',  N'',  N'', N'半年', N'',  N'',  N'',  N'',  N''),
(40, N'双轴速率转台',         N'QZT-EB-051', N'A',  N'班前/后维护；季度维护；半年维护；年度维护',             1,  N'季/半年/年', N'', N'', N'季', N'', N'', N'季/半年', N'', N'', N'季', N'', N'季'),
(41, N'交流点焊机PW20Q',      N'QZT-EB-054', N'B1', N'班前/后维护；月度维护，年度维护',                       1,  N'月', N'月/年', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月'),
(42, N'金丝自动键合机',       N'QZT-EB-036', N'A',  N'班前/后维护；月度维护；季度维护',                       3,  N'月', N'月/季度', N'月', N'月', N'月/季度', N'月', N'月', N'月/季度', N'月', N'月', N'月/季度', N'月'),
(43, N'导电胶脱泡搅拌机',     N'QZT-EB-058', N'A',  N'班前/后维护；月度维护；季度维护；年度维护',             3,  N'月', N'月', N'月/季', N'月', N'月', N'月/季', N'月', N'月', N'月/季/年', N'月', N'月', N'月/季'),
(44, N'自动点胶机',           N'QZT-EB-059', N'A',  N'班前/后维护；月度维护；半年维护',                       3,  N'月', N'月/半年', N'月', N'月', N'月', N'月', N'月', N'月/半年', N'月', N'月', N'月', N'月'),
(45, N'自动贴片机',           N'QZT-EB-060', N'A',  N'班前/后维护；月度维护；半年维护',                       2,  N'月', N'月/半年', N'月', N'月', N'月', N'月', N'月', N'月/半年', N'月', N'月', N'月', N'月'),
(46, N'智能低温槽',           N'QZT-EB-063', N'A',  N'班前/后维护；月度维护',                                4,  N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月'),
(47, N'水清洗机',             N'QZT-EB-064', N'A',  N'班前/后维护；周维护；月度维护',                         1,  N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月'),
(48, N'3D SPI检测设备',       N'QZT-EB-065', N'A',  N'班前/后维护；月度维护；季维护',                         1,  N'月/季度', N'月', N'月', N'月/季度', N'月', N'月', N'月/季度', N'月', N'月', N'月/季度', N'月', N'月'),
(49, N'AOI检测设备',          N'QZT-EB-066', N'A',  N'班前/后维护；月度维护；季维护',                         1,  N'月/季度', N'月', N'月', N'月/季度', N'月', N'月', N'月/季度', N'月', N'月', N'月/季度', N'月', N'月'),
(50, N'高加速度离心试验机',   N'QZT-EB-067', N'A',  N'班前/后维护；月度维护；半年维护',                       1,  N'月/年', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月'),
(51, N'厚膜印刷机',           N'QZT-EB-069', N'A',  N'班前/后维护；周维护；月度维护；季维护',                 2,  N'月', N'月/季度', N'月', N'月', N'月/季度', N'月', N'月', N'月/季度', N'月', N'月', N'月/季度', N'月'),
(52, N'箱式实验炉',           N'QZT-EB-075', N'A',  N'班前/后维护；月度维护；季度维护',                       2,  N'月', N'月', N'月/季度', N'月', N'月', N'月/季度', N'月', N'月', N'月/季度', N'月', N'月', N'月/季度'),
(53, N'铣刀式分板机',         N'QZT-EB-076', N'A',  N'班前/后维护；周维护；月度维护',                         1,  N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月'),
(54, N'汽相清洗机',           N'QZT-EB-078', N'A',  N'班前/后维护；周维护；月度维护',                         1,  N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月', N'月'),
(55, N'电动振动试验台',       N'QZT-EB-082', N'A',  N'班前/后维护；月度维护；季度维护',                       2,  N'月', N'月', N'月/季度', N'月', N'月', N'月/季度', N'月', N'月', N'月/季度', N'月', N'月', N'月/季度'),
(56, N'桌面式焊接机',         N'QZT-EB-089', N'A',  N'班前/后维护；月度维护；半年维护',                       4,  N'月', N'月/半年', N'月', N'月', N'月', N'月', N'月', N'月/半年', N'月', N'月', N'月', N'月'),
(57, N'X-Ray透视检测设备',    N'QZT-EB-091', N'A',  N'班前/后维护；月度维护；半年维护；年度维护',             1,  N'月', N'月', N'月/半年/年', N'月', N'月', N'月', N'月', N'月', N'月/半年', N'月', N'月', N'月'),
(58, N'高低温冲击试验箱',     N'QZT-EB-093', N'A',  N'班前/后维护；每月维护；季度维护；半年维护；年度维护',   1,  N'月/年', N'月', N'月/季', N'月/半年', N'月', N'月/季', N'月', N'月', N'月/季', N'月/半年', N'月', N'月/季');
GO

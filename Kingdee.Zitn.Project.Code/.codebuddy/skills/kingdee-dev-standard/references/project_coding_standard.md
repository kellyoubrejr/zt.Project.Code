# 金蝶ERP项目编码规范

## 项目概述
本项目基于金蝶BOS平台，使用.NET Framework 4.8开发，采用插件式架构。

## 技术栈
- 框架：.NET Framework 4.8
- 平台：金蝶BOS平台
- 开发语言：C#
- 数据库：SQL Server
- JSON处理：Newtonsoft.Json

## 命名空间规范
所有插件代码必须使用以下命名空间：
```
Kingdee.Zitn.Project.Code.plugin.{模块名}
```

示例：
- `Kingdee.Zitn.Project.Code.plugin.PO`
- `Kingdee.Zitn.Project.Code.plugin.SFBill`
- `Kingdee.Zitn.Project.Code.plugin.OutStock`

## 插件继承规范

### 表单插件
继承 `AbstractBillPlugIn`，用于标准单据的表单逻辑。
```csharp
[Description("功能描述")]
[HotUpdate]
public class MyBillPlugin : AbstractBillPlugIn
{
    // 实现方法
}
```

### 动态表单插件
继承 `AbstractDynamicFormPlugIn`，用于自定义动态表单。
```csharp
[Description("功能描述")]
[HotUpdate]
public class MyDynamicFormPlugin : AbstractDynamicFormPlugIn
{
    // 实现方法
}
```

### 操作服务插件
继承 `AbstractOperationServicePlugIn`，用于审核、提交等操作服务。
```csharp
[Description("功能描述")]
[HotUpdate]
public class MyOperationService : AbstractOperationServicePlugIn
{
    // 实现方法
}
```

### 列表插件
继承 `AbstractListPlugIn`，用于列表页面逻辑。
```csharp
[Description("功能描述")]
[HotUpdate]
public class MyListPlugin : AbstractListPlugIn
{
    // 实现方法
}
```

## 代码结构规范

### 文件头
每个插件文件应包含以下结构：
```csharp
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
// 其他必要的using

namespace Kingdee.Zitn.Project.Code.plugin.{模块名}
{
    [Description("功能描述")]
    [HotUpdate]
    public class {ClassName} : {基类}
    {
        // 插件实现代码
    }
}
```

### 特性标记
- `[Description("功能描述")]`：描述插件功能
- `[HotUpdate]`：支持热更新

## 配置文件使用规范

### 日志配置（CustomLog.cs）
位于 `conf/CustomLog.cs`：
```csharp
private static readonly CustomLog.LogWriter _log = CustomLog.For("模块名称");
_log.WriteLog($"日志内容");
_log.Section($"分段日志");
_log.Error($"错误日志");
```

### ERP登录配置（ErpLogin.cs）
位于 `conf/ErpLogin.cs`：
```csharp
var ctx = ErpLogin.GetContext();
```

### 顺丰配置（SFConfig.cs）
位于 `conf/SFConfig.cs`：
```csharp
var sfConfig = new SFConfig();
```

### 企业微信配置（WeComConfig.cs）
位于 `conf/WeComConfig.cs`：
```csharp
var weComConfig = new WeComConfig();
```

## 工具类使用规范

### 日志工具（CustomLog）
```csharp
private static readonly CustomLog.LogWriter _log = CustomLog.For("模块名称");
_log.WriteLog($"处理单据：{billNo}");
_log.Section($"开始处理：{billNo}");
_log.Error($"处理单据异常，FID={fid}");
```

### 消息通知工具（SendMsg）
位于 `Util/SendMsg.cs`：
```csharp
SendMsg.Send($"【模块名称】操作成功：{billNo}");
SendMsg.Send($"【模块名称】操作失败：{billNo}", ex);
```

### 数据库工具（DBUtils）
```csharp
var result = DBUtils.ExecuteDynamicObject(this.Context, sql);
DBUtils.Execute(this.Context, sql);
```

### 安全处理工具（Tools）
位于 `Util/Tools.cs`：
```csharp
private static string SafeStr(DynamicObject obj, string field)
{
    var val = obj[field];
    if (val == null || val == DBNull.Value) return "";
    return val.ToString();
}

private static string Sql(string s)
{
    return string.IsNullOrEmpty(s) ? "" : s.Replace("'", "''");
}

private static string Str(object obj)
{
    return obj?.ToString() ?? "";
}
```

## 系统集成接口规范

### BPM系统集成
位于 `Interface/ToBPM/BpmService.cs`：
```csharp
var bpmService = new BpmService();
bpmService.SubmitToBpm(billNo, billType, data);
```

### MES系统集成
位于 `Interface/ToMES/MesService.cs`：
```csharp
var mesService = new MesService();
mesService.PushToMes(data);
```

### WMS系统集成
位于 `Interface/ToWMS/WmsService.cs`：
```csharp
var wmsService = new WmsService();
wmsService.PushToWms(data);
```

## 数据模型规范

### 实体类（Entity）
位于 `models/` 目录：
```csharp
public class ErpDataDict
{
    public long FID { get; set; }
    public string FBILLNO { get; set; }
    public string FDOCUMENTSTATUS { get; set; }
}
```

### 数据传输对象（DTO）
用于插件间数据传递：
```csharp
public class BillInfo
{
    public string BillNo { get; set; }
    public string Status { get; set; }
}

public class ReqEntryInfo : BillInfo
{
    public long EntryId { get; set; }
    public string MaterialNumber { get; set; }
}
```

### 视图对象（VO）
用于前端展示：
```csharp
public class BillDisplayInfo
{
    public string BillNo { get; set; }
    public string Status { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreatorName { get; set; }
}
```

## 调度任务规范

### 继承关系
继承 `Kingdee.BOS.Schedule.IRepairService`：
```csharp
using Kingdee.BOS.Schedule;

namespace Kingdee.Zitn.Project.Code.Schedule
{
    [Description("调度任务描述")]
    [HotUpdate]
    public class MyScheduleTask : IRepairService
    {
        public void Execute(Context ctx)
        {
            // 任务执行逻辑
        }
    }
}
```

### 常用方法
- `Execute(Context ctx)`：任务执行入口
- 支持定时执行、手动触发
- 需要处理异常和日志记录

## 插件方法规范

### 表单插件（AbstractBillPlugIn）常用方法
- `AfterCreateNewData(EventArgs e)`：新建数据后
- `BeforeSave(CancelEventArgs e)`：保存前
- `AfterSave(SaveEventArgs e)`：保存后
- `ButtonClick(ButtonClickEventArgs e)`：按钮点击
- `DataChanged(DataChangedEventArgs e)`：数据变更
- `BeforeClose(BeforeClosedEventArgs e)`：关闭前

### 动态表单插件（AbstractDynamicFormPlugIn）常用方法
- `EntryButtonCellClick(EntryButtonCellClickEventArgs e)`：分录按钮点击
- `ButtonClick(ButtonClickEventArgs e)`：按钮点击
- `DataChanged(DataChangedEventArgs e)`：数据变更
- `AfterCreateNewData(EventArgs e)`：新建数据后

### 操作服务插件（AbstractOperationServicePlugIn）常用方法
- `OnPrepareOperateOption(OnPrepareOperateOptionEventArgs e)`：准备操作选项
- `BeginOperationTransaction(BeginOperationTransactionEventArgs e)`：操作事务开始
- `EndOperationTransaction(EndOperationTransactionEventArgs e)`：操作事务结束

### 列表插件（AbstractListPlugIn）常用方法
- `ItemClick(ItemClickEventArgs e)`：列表项点击
- `FormatCellValue(FormatCellValueEventArgs e)`：格式化单元格值
- `FormatCellPostData(FormatCellPostDataEventArgs e)`：格式化单元格后数据

## 开发流程规范

### 1. 开发前准备
- 分析需求，明确功能目标
- 查看现有类似代码，了解实现模式
- 规划代码结构，确定插件类型

### 2. 编码过程
- 遵循编码规范，保持代码风格一致
- 先实现核心逻辑，再添加异常处理
- 使用统一的日志和消息通知方式
- 避免随意删除或修改现有代码

### 3. 代码审查
- 检查继承关系是否正确
- 验证特性标记是否完整
- 确认日志记录是否充分
- 测试异常处理是否完善

### 4. 问题处理
- 发现问题先咨询用户，同意后再处理
- 不随意修改现有功能
- 保持代码的稳定性和可维护性

## 代码质量要求
1. 遵循C#编码规范
2. 保持代码简洁明了
3. 注释不要随便增加
4. 异常处理要完善
5. 日志记录要充分

## 安全规范
1. 使用参数化查询防止SQL注入
2. 对用户输入进行验证
3. 敏感信息要加密处理
4. 遵循最小权限原则
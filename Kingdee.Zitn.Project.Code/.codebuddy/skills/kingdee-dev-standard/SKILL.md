---
name: 金蝶ERP开发规范
description: 用于金蝶ERP项目开发的标准化skills，确保代码风格、开发流程和技术实现的一致性。适用于表单插件、动态表单插件、操作服务插件、调度任务等各类开发。
---

# 金蝶ERP开发规范

## 概述
本skills为金蝶ERP项目开发提供标准化的编码规范、开发流程和技术实现指南。确保代码质量、可维护性和团队协作效率。

## 触发条件
当用户进行以下操作时自动触发：
- 编写新的金蝶插件代码
- 修改现有插件功能
- 进行代码审查
- 需要生成标准化代码模板
- 开发调度任务
- 集成外部系统接口

## 核心开发规范

### 1. 插件继承规范
根据插件类型选择正确的基类：

**表单插件**：
- 继承 `AbstractBillPlugIn`
- 用于标准单据的表单逻辑
- 常用方法：`AfterCreateNewData`, `BeforeSave`, `AfterSave`, `ButtonClick`, `DataChanged`

**动态表单插件**：
- 继承 `AbstractDynamicFormPlugIn`
- 用于自定义动态表单
- 常用方法：`EntryButtonCellClick`, `ButtonClick`, `DataChanged`

**操作服务插件**：
- 继承 `AbstractOperationServicePlugIn`
- 用于审核、提交等操作服务
- 常用方法：`OnPrepareOperateOption`, `BeginOperationTransaction`, `EndOperationTransaction`

**列表插件**：
- 继承 `AbstractListPlugIn`
- 用于列表页面逻辑
- 常用方法： `ItemClick`, `FormatCellValue`, `FormatCellPostData`

### 2. 代码结构规范
每个插件文件应包含：

```csharp
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
// 其他必要的using

namespace Kingdee.Zitn.Project.Code.plugin.{模块名}
{
    [Description("【功能描述】")]
    [HotUpdate]
    public class {ClassName} : {基类}
    {
        // 插件实现代码
    }
}
```

### 3. 配置文件规范
项目配置文件位于 `conf/` 目录：

**CustomLog.cs** - 日志配置：
```csharp
// 使用方式
private static readonly CustomLog.LogWriter _log = CustomLog.For("模块名称");
_log.WriteLog($"日志内容");
_log.Section($"分段日志");
_log.Error($"错误日志");
```

**ErpLogin.cs** - ERP登录配置：
```csharp
// 获取ERP上下文
var ctx = ErpLogin.GetContext();
```

**SFConfig.cs** - 顺丰配置：
```csharp
// 顺丰API配置
var sfConfig = new SFConfig();
```

**WeComConfig.cs** - 企业微信配置：
```csharp
// 企业微信配置
var weComConfig = new WeComConfig();
```

### 4. 日志记录规范
统一使用 `CustomLog` 类（位于 `conf/CustomLog.cs`）：

```csharp
// 创建日志实例
private static readonly CustomLog.LogWriter _log = CustomLog.For("模块名称");

// 写入普通日志
_log.WriteLog($"处理单据：{billNo}");

// 写入分段日志
_log.Section($"开始处理：{billNo}");

// 记录异常
_log.Error($"处理单据异常，FID={fid}");
_log.Error(ex);
```

### 5. 消息通知规范
统一使用 `SendMsg.Send()` 发送企业微信消息（位于 `Util/SendMsg.cs`）：

```csharp
// 发送普通消息
SendMsg.Send($"【模块名称】操作成功：{billNo}");

// 发送异常消息
SendMsg.Send($"【模块名称】操作失败：{billNo}", ex);

// 发送格式化消息
SendMsg.Send($@"【模块名称】操作结果：
• 操作单据：{billType}
• 单据编号：{billNo}
• 时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}
• 结果：{result}");
```

### 6. 数据库操作规范
使用 `DBUtils` 进行数据库操作：

```csharp
// 查询数据
var result = DBUtils.ExecuteDynamicObject(this.Context, sql);

// 执行SQL
DBUtils.Execute(this.Context, sql);

// 使用参数化查询防止SQL注入
var sql = $@"/*dialect*/SELECT * FROM {tableName} WHERE FID = {fid}";
```

### 7. 安全处理规范
使用统一的工具方法处理数据（位于 `Util/Tools.cs`）：

```csharp
// 安全获取字符串值
private static string SafeStr(DynamicObject obj, string field)
{
    var val = obj[field];
    if (val == null || val == DBNull.Value) return "";
    return val.ToString();
}

// SQL字符串安全处理
private static string Sql(string s)
{
    return string.IsNullOrEmpty(s) ? "" : s.Replace("'", "''");
}

// 字符串转换
private static string Str(object obj)
{
    return obj?.ToString() ?? "";
}
```

## 系统集成接口规范

### 1. BPM系统集成
接口位于 `Interface/ToBPM/BpmService.cs`：

```csharp
// BPM服务调用
var bpmService = new BpmService();
bpmService.SubmitToBpm(billNo, billType, data);
```

### 2. MES系统集成
接口位于 `Interface/ToMES/MesService.cs`：

```csharp
// MES服务调用
var mesService = new MesService();
mesService.PushToMes(data);
```

### 3. WMS系统集成
接口位于 `Interface/ToWMS/WmsService.cs`：

```csharp
// WMS服务调用
var wmsService = new WmsService();
wmsService.PushToWms(data);
```

## 数据模型规范

### 1. 实体类（Entity）
位于 `models/` 目录，用于数据库表映射：

```csharp
// 示例：ErpDataDict.cs
public class ErpDataDict
{
    public long FID { get; set; }
    public string FBILLNO { get; set; }
    public string FDOCUMENTSTATUS { get; set; }
}
```

### 2. 数据传输对象（DTO）
用于插件间数据传递：

```csharp
// 示例：BillInfo.cs
public class BillInfo
{
    public string BillNo { get; set; }
    public string Status { get; set; }
}

// 继承示例：ReqEntryInfo.cs
public class ReqEntryInfo : BillInfo
{
    public long EntryId { get; set; }
    public string MaterialNumber { get; set; }
}
```

### 3. 视图对象（VO）
用于前端展示：

```csharp
// 示例：BillDisplayInfo.cs
public class BillDisplayInfo
{
    public string BillNo { get; set; }
    public string Status { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreatorName { get; set; }
}
```

## 调度任务规范

### 1. 继承关系
调度任务继承 `Kingdee.BOS.Schedule.IRepairService`：

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

### 2. 常用方法
- `Execute(Context ctx)`：任务执行入口
- 支持定时执行、手动触发
- 需要处理异常和日志记录

## 插件方法规范

### 1. 表单插件（AbstractBillPlugIn）常用方法
- `AfterCreateNewData(EventArgs e)`：新建数据后
- `BeforeSave(CancelEventArgs e)`：保存前
- `AfterSave(SaveEventArgs e)`：保存后
- `ButtonClick(ButtonClickEventArgs e)`：按钮点击
- `DataChanged(DataChangedEventArgs e)`：数据变更
- `BeforeClose(BeforeClosedEventArgs e)`：关闭前

### 2. 动态表单插件（AbstractDynamicFormPlugIn）常用方法
- `EntryButtonCellClick(EntryButtonCellClickEventArgs e)`：分录按钮点击
- `ButtonClick(ButtonClickEventArgs e)`：按钮点击
- `DataChanged(DataChangedEventArgs e)`：数据变更
- `AfterCreateNewData(EventArgs e)`：新建数据后

### 3. 操作服务插件（AbstractOperationServicePlugIn）常用方法
- `OnPrepareOperateOption(OnPrepareOperateOptionEventArgs e)`：准备操作选项
- `BeginOperationTransaction(BeginOperationTransactionEventArgs e)`：操作事务开始
- `EndOperationTransaction(EndOperationTransactionEventArgs e)`：操作事务结束

### 4. 列表插件（AbstractListPlugIn）常用方法
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

## 技术栈要求
- 框架：.NET Framework 4.8
- 平台：金蝶BOS平台
- 日志：CustomLog（位于conf/CustomLog.cs）
- 消息：SendMsg（位于Util/SendMsg.cs）
- 数据库：DBUtils、SQLServerHelper
- JSON：Newtonsoft.Json
- 配置：ErpLogin、SFConfig、WeComConfig

## 资源使用指南

### 代码模板
在 `assets/templates/` 目录下：
- `bill_plugin_template.cs`：表单插件模板
- `dynamic_form_plugin_template.cs`：动态表单插件模板
- `operation_service_template.cs`：操作服务插件模板

### 参考文档
在 `references/` 目录下：
- `kingdee_api_reference.md`：金蝶API参考文档
- `project_coding_standard.md`：项目详细编码规范
- `plugin_types.md`：插件类型详细说明

### 检查脚本
在 `scripts/` 目录下：
- `check_coding_standard.py`：编码规范检查工具
- `generate_plugin_template.py`：模板生成工具

## 自定义扩展
用户可以在以下方面进行扩展：
1. 添加新的插件类型模板
2. 扩展编码规范检查规则
3. 增加项目特定的工具方法
4. 添加更多示例代码

## 使用示例
```bash
# 生成表单插件代码
python scripts/generate_plugin_template.py --type bill --name MyBillPlugin --module PO

# 检查编码规范
python scripts/check_coding_standard.py --path ./plugin/PO/MyBillPlugin.cs

# 查看插件类型说明
cat references/plugin_types.md
```

## 注意事项
1. 始终使用项目规定的命名空间：`Kingdee.Zitn.Project.Code.plugin.{模块名}`
2. 保持代码简洁，注释不要随便增加
3. 遵循先计划、再实现、后测试的开发流程
4. 发现问题先咨询用户，不要随意修改现有代码
5. 使用项目统一的配置文件、日志和消息通知方式
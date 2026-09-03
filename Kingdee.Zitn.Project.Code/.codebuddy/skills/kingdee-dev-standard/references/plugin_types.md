# 金蝶ERP插件类型说明

## 概述
本项目基于金蝶BOS平台，采用插件式架构。不同类型的插件用于处理不同的业务场景。

## 插件类型分类

### 1. 表单插件（Bill Plugin）
**继承关系**：`AbstractBillPlugIn`
**适用场景**：标准单据的表单逻辑
**命名空间**：`Kingdee.Zitn.Project.Code.plugin.{模块名}`

**常用方法**：
- `AfterCreateNewData(EventArgs e)`：新建数据后
- `BeforeSave(CancelEventArgs e)`：保存前
- `AfterSave(SaveEventArgs e)`：保存后
- `ButtonClick(ButtonClickEventArgs e)`：按钮点击
- `DataChanged(DataChangedEventArgs e)`：数据变更
- `BeforeClose(BeforeClosedEventArgs e)`：关闭前

**示例文件**：
- `plugin/PO/PoAuditToBPMHT.cs`
- `plugin/SFBill/SFBillAuditOpe.cs`

### 2. 动态表单插件（Dynamic Form Plugin）
**继承关系**：`AbstractDynamicFormPlugIn`
**适用场景**：自定义动态表单
**命名空间**：`Kingdee.Zitn.Project.Code.plugin.{模块名}`

**常用方法**：
- `EntryButtonCellClick(EntryButtonCellClickEventArgs e)`：分录按钮点击
- `ButtonClick(ButtonClickEventArgs e)`：按钮点击
- `DataChanged(DataChangedEventArgs e)`：数据变更
- `AfterCreateNewData(EventArgs e)`：新建数据后

**示例文件**：
- `plugin/PO/btnPoToBPMHT.cs`
- `plugin/PoReq/PoReqClickMenuButtonOpenForm.cs`

### 3. 操作服务插件（Operation Service Plugin）
**继承关系**：`AbstractOperationServicePlugIn`
**适用场景**：审核、提交等操作服务
**命名空间**：`Kingdee.Zitn.Project.Code.plugin.{模块名}`

**常用方法**：
- `OnPrepareOperateOption(OnPrepareOperateOptionEventArgs e)`：准备操作选项
- `BeginOperationTransaction(BeginOperationTransactionEventArgs e)`：操作事务开始
- `EndOperationTransaction(EndOperationTransactionEventArgs e)`：操作事务结束

**示例文件**：
- `plugin/PO/PoAuditToBPMHT.cs`
- `plugin/SFBill/SFBillAuditOpe.cs`

### 4. 列表插件（List Plugin）
**继承关系**：`AbstractListPlugIn`
**适用场景**：列表页面逻辑
**命名空间**：`Kingdee.Zitn.Project.Code.plugin.{模块名}`

**常用方法**：
- `ItemClick(ItemClickEventArgs e)`：列表项点击
- `FormatCellValue(FormatCellValueEventArgs e)`：格式化单元格值
- `FormatCellPostData(FormatCellPostDataEventArgs e)`：格式化单元格后数据

**示例文件**：
- `plugin/BillOrListPlugin/BillButtonEvent.cs`

### 5. 调度任务（Schedule Task）
**继承关系**：`Kingdee.BOS.Schedule.IRepairService`
**适用场景**：定时任务、计划任务
**命名空间**：`Kingdee.Zitn.Project.Code.Schedule`

**常用方法**：
- `Execute(Context ctx)`：任务执行入口

**示例文件**：
- `Schedule/JiaZhiFenLei.cs`

## 插件文件结构规范

### 标准文件结构
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

## 插件模块分类

### 1. 采购模块（PO）
- 采购订单审核
- 采购订单按钮事件
- 采购订单数据变更

### 2. 销售模块（SFBill）
- 销售单审核
- 销售单查询按钮
- 销售单保存生成订单

### 3. 库存模块（OutStock）
- 出库数据变更
- 出库审核

### 4. 生产模块（PrdInstock）
- 生产入库按钮
- 生产入库审核

### 5. 采购申请模块（PoReq）
- 流程进度单据插件
- 数据接收和填充

### 6. 调拨模块（TransferDirect）
- 直接调拨数据变更

### 7. 通用模块（BillOrListPlugin）
- 单据按钮事件
- 列表按钮事件

## 插件开发流程

### 1. 确定插件类型
根据业务需求选择合适的插件类型：
- 表单逻辑 → 表单插件
- 动态表单 → 动态表单插件
- 操作服务 → 操作服务插件
- 列表页面 → 列表插件
- 定时任务 → 调度任务

### 2. 创建插件文件
在对应模块目录下创建插件文件：
```
plugin/{模块名}/{插件类名}.cs
```

### 3. 实现插件逻辑
根据业务需求实现相应的方法。

### 4. 注册插件
在金蝶BOS平台中注册插件。

## 插件方法详细说明

### 表单插件方法
1. **AfterCreateNewData**
   - 触发时机：新建数据后
   - 用途：初始化默认值、设置默认状态

2. **BeforeSave**
   - 触发时机：保存前
   - 用途：数据验证、业务规则检查

3. **AfterSave**
   - 触发时机：保存后
   - 用途：后续处理、日志记录

4. **ButtonClick**
   - 触发时机：按钮点击
   - 用途：自定义按钮逻辑

5. **DataChanged**
   - 触发时机：数据变更
   - 用途：联动计算、数据验证

### 动态表单插件方法
1. **EntryButtonCellClick**
   - 触发时机：分录按钮点击
   - 用途：分录行操作

2. **ButtonClick**
   - 触发时机：按钮点击
   - 用途：表单级操作

3. **DataChanged**
   - 触发时机：数据变更
   - 用途：动态计算、联动显示

### 操作服务插件方法
1. **OnPrepareOperateOption**
   - 触发时机：准备操作选项
   - 用途：设置操作参数

2. **BeginOperationTransaction**
   - 触发时机：操作事务开始
   - 用途：业务逻辑处理

3. **EndOperationTransaction**
   - 触发时机：操作事务结束
   - 用途：后续处理、通知

### 列表插件方法
1. **ItemClick**
   - 触发时机：列表项点击
   - 用途：自定义点击逻辑

2. **FormatCellValue**
   - 触发时机：格式化单元格值
   - 用途：自定义显示格式

3. **FormatCellPostData**
   - 触发时机：格式化单元格后数据
   - 用途：数据转换处理

## 插件注册说明

### 注册位置
在金蝶BOS平台中，插件需要在对应单据的插件列表中注册。

### 注册步骤
1. 打开金蝶BOS开发平台
2. 找到对应的单据
3. 进入插件管理界面
4. 添加新的插件引用
5. 配置插件参数

### 注册信息
- 插件名称：完整的类名
- 插件位置：DLL文件路径
- 排序号：执行顺序

## 常见问题解决

### 1. 插件不生效
- 检查插件是否正确注册
- 检查插件DLL是否在正确位置
- 检查插件类名是否正确

### 2. 方法不触发
- 检查方法签名是否正确
- 检查插件是否继承正确的基类
- 检查插件是否标记了`[HotUpdate]`特性

### 3. 数据访问问题
- 检查数据库连接字符串
- 检查SQL语法是否正确
- 检查数据表和字段是否存在

## 性能优化建议

### 1. 数据库操作
- 使用参数化查询
- 避免在循环中执行数据库操作
- 使用批量操作提高性能

### 2. 内存管理
- 及时释放资源
- 避免内存泄漏
- 使用适当的数据结构

### 3. 异步处理
- 耗时操作使用异步处理
- 避免阻塞主线程
- 使用任务并行库
# 金蝶API参考文档

## 概述
本文档提供金蝶BOS平台开发中常用的API参考，包括插件基类、数据操作、界面控制等。

## 插件基类

### AbstractBillPlugIn
表单插件基类，用于标准单据的表单逻辑控制。

**常用事件**：
- `AfterCreateNewData`：新增数据后
- `AfterLoadData`：加载数据后
- `BeforeSave`：保存前
- `AfterSave`：保存后
- `BeforeSubmit`：提交前
- `AfterSubmit`：提交后
- `BeforeAudit`：审核前
- `AfterAudit`：审核后

**常用属性**：
- `this.Model`：数据模型
- `this.View`：视图控制
- `this.Context`：上下文信息

**示例**：
```csharp
[Description("定价依据信息预览")]
[HotUpdate]
public class PriceBasisBillPreview : AbstractBillPlugIn
{
    public override void AfterCreateNewData(EventObject e)
    {
        base.AfterCreateNewData(e);
        // 新增数据后的处理逻辑
    }
}
```

### AbstractDynamicFormPlugIn
动态表单插件基类，用于自定义动态表单。

**常用事件**：
- `OnInitialize`：初始化事件
- `ButtonClick`：按钮点击事件
- `TextChanged`：文本变化事件
- `SelectedIndexChanged`：下拉选择变化事件

**示例**：
```csharp
[Description("采购订单按钮")]
[HotUpdate]
public class btnPoToBPMHT : AbstractDynamicFormPlugIn
{
    public override void OnInitialize(OnInitializeEventArgs e)
    {
        base.OnInitialize(e);
        // 初始化逻辑
    }
    
    public override void ButtonClick(ButtonClickEventArgs e)
    {
        base.ButtonClick(e);
        // 按钮点击处理
    }
}
```

### AbstractOperationServicePlugIn
操作服务插件基类，用于审核、提交等操作服务。

**常用事件**：
- `BeforeExecuteOperationTransaction`：执行操作事务前
- `AfterExecuteOperationTransaction`：执行操作事务后
- `OnPrepareOperationServiceHelper`：准备操作服务帮助器

**示例**：
```csharp
[Description("【采购订单审核服务】")]
[HotUpdate]
public class PoAuditToBPMHT : AbstractOperationServicePlugIn
{
    public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
    {
        base.BeforeExecuteOperationTransaction(e);
        // 审核前的处理逻辑
    }
}
```

## 数据操作API

### DBUtils
数据库操作工具类。

**常用方法**：
- `ExecuteDynamicObject`：执行SQL返回动态对象集合
- `Execute`：执行SQL语句
- `ExecuteScalar`：执行SQL返回单个值

**示例**：
```csharp
// 查询数据
string sql = "SELECT FID, FBILLNO FROM T_PUR_POORDER WHERE FID = {0}";
var result = DBUtils.ExecuteDynamicObject(this.Context, sql, fid);

// 执行SQL
string updateSql = "UPDATE T_PUR_POORDER SET FSTATUS = 1 WHERE FID = {0}";
DBUtils.Execute(this.Context, updateSql, fid);
```

### DynamicObject
金蝶动态对象，用于表示单据数据。

**常用属性**：
- `DynamicObjectCollection`：动态对象集合
- `DynamicObject`：动态对象

**示例**：
```csharp
// 获取单据数据
DynamicObject billData = this.Model.DataEntity;

// 获取字段值
string billNo = billData["FBILLNO"]?.ToString();
decimal amount = Convert.ToDecimal(billData["FALLAMOUNT"]);
```

## 界面控制API

### View
视图控制对象，用于界面交互。

**常用方法**：
- `ShowMessage`：显示消息
- `ShowError`：显示错误
- `UpdateView`：更新界面
- `Close`：关闭界面

**示例**：
```csharp
// 显示成功消息
this.View.ShowMessage("操作成功");

// 显示错误信息
this.View.ShowError("操作失败，请检查");

// 更新界面
this.View.UpdateView();
```

## 工具类API

### CustomLog
自定义日志工具类。

**使用方法**：
```csharp
// 创建日志实例
private static readonly CustomLog.LogWriter _log = CustomLog.For("模块名称");

// 写入日志
_log.WriteLog($"处理单据：{billNo}");

// 记录异常
_log.Error($"处理单据异常，FID={fid}");
_log.Error(ex);
```

### SendMsg
企业微信消息发送工具类。

**使用方法**：
```csharp
// 发送普通消息
SendMsg.Send($"【模块名称】操作成功：{billNo}");

// 发送异常消息
SendMsg.Send($"【模块名称】操作失败：{billNo}", ex);
```

## SQL语法参考

### 金蝶专用SQL
- `/*dialect*/`：标识为金蝶专用SQL语法
- `{0}`：参数占位符

**示例**：
```csharp
string sql = $@"/*dialect*/SELECT 
    A.FID,
    A.FBILLNO,
    B.FMATERIALID
FROM T_PUR_POORDER A
LEFT JOIN T_PUR_POORDERENTRY B ON A.FID = B.FID
WHERE A.FID IN ({ids})";
```

### 常用查询模式
1. 主表+明细表关联查询
2. 多组织数据查询
3. 状态筛选查询
4. 时间范围查询

## 异常处理

### KDBusinessException
业务异常类，用于抛出业务逻辑错误。

**使用方法**：
```csharp
throw new KDBusinessException("错误编码", "错误信息");
```

### 标准异常处理模式
```csharp
try
{
    // 业务逻辑
}
catch (KDBusinessException ex)
{
    // 业务异常，直接抛出
    throw;
}
catch (Exception ex)
{
    // 系统异常，记录日志并抛出
    _log.Error($"系统异常：{ex.Message}", ex);
    throw new KDBusinessException("SYSTEM_ERROR", $"系统异常：{ex.Message}");
}
```

## 最佳实践
1. 使用统一的日志和消息通知方式
2. 采用参数化查询防止SQL注入
3. 使用SafeStr等安全方法处理数据
4. 遵循先计划、再实现、后测试的开发流程
5. 发现问题先咨询用户，不要随意修改现有代码
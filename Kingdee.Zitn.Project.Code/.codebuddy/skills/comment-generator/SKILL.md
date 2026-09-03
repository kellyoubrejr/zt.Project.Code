---
name: Comment
description: 为C#代码文件自动生成XML文档注释，支持文档注释、功能注释、模型注释、实体类注释等。
---

# 代码注释生成器

## 概述
本skills为C#代码文件自动生成标准化的XML文档注释，支持多种注释类型，提升代码可读性和维护性。

## 触发条件
当用户进行以下操作时自动触发：
- 为代码文件添加注释
- 生成API文档
- 创建实体类注释
- 添加功能说明

## 使用方式

### 1. 基本用法
```
/Comment [文件路径]
```

### 2. 带参数用法
```
/Comment [文件路径] --type [注释类型]
```

### 3. 示例
```
/Comment ./plugin/PO/PoAuditToBPMHT.cs
/Comment ./models/ErpDataDict.cs --type entity
/Comment ./Util/Tools.cs --type utility
```

## 支持的注释类型

### 1. 文档注释 (doc)
- **用途**：为类、方法、属性生成标准XML文档注释
- **格式**：`<summary>`、`<param>`、`<returns>`、`<remarks>`
- **适用**：所有C#代码文件

### 2. 功能注释 (function)
- **用途**：为业务逻辑方法生成功能说明
- **格式**：包含功能描述、参数说明、返回值说明
- **适用**：插件方法、服务方法、工具方法

### 3. 模型注释 (model)
- **用途**：为数据模型类生成详细注释
- **格式**：包含模型用途、属性说明、使用场景
- **适用**：DTO、VO、实体类

### 4. 实体类注释 (entity)
- **用途**：为数据库实体类生成注释
- **格式**：包含表名、字段说明、关系说明
- **适用**：数据库映射类

### 5. 插件注释 (plugin)
- **用途**：为金蝶插件生成特定注释
- **格式**：包含插件类型、继承关系、重写方法说明
- **适用**：所有金蝶插件类

## 注释生成规则

### 1. 类注释
```csharp
/// <summary>
/// [类功能描述]
/// </summary>
/// <remarks>
/// [详细说明、使用场景、注意事项]
/// </remarks>
```

### 2. 方法注释
```csharp
/// <summary>
/// [方法功能描述]
/// </summary>
/// <param name="paramName">[参数说明]</param>
/// <returns>[返回值说明]</returns>
/// <example>
/// [使用示例]
/// </example>
```

### 3. 属性注释
```csharp
/// <summary>
/// [属性功能描述]
/// </summary>
/// <value>[属性值说明]</value>
```

### 4. 实体类注释
```csharp
/// <summary>
/// [实体类名称] - [功能描述]
/// </summary>
/// <remarks>
/// 对应数据库表：[表名]
/// 主键：[主键字段]
/// [其他说明]
/// </remarks>
```

## 实现流程

### 1. 文件分析
- 解析C#代码文件
- 识别类、方法、属性、字段
- 分析继承关系和特性标记

### 2. 注释生成
- 根据代码结构生成对应注释
- 保持与现有代码风格一致
- 遵循XML文档注释标准

### 3. 文件更新
- 在适当位置插入注释
- 保持代码格式不变
- 避免重复注释

## 集成开发规范

### 1. 与金蝶开发规范集成
- 遵循项目命名空间规范
- 保持与现有注释风格一致
- 支持插件特定的注释格式

### 2. 代码质量检查
- 验证生成的注释格式
- 检查注释与代码的一致性
- 确保不破坏现有代码结构

## 使用示例

### 示例1：为插件类生成注释
```
/Comment ./plugin/PO/PoAuditToBPMHT.cs --type plugin
```

**生成的注释示例**：
```csharp
/// <summary>
/// 采购订单审核服务插件 - 推送到BPM系统
/// </summary>
/// <remarks>
/// 继承自AbstractOperationServicePlugIn，用于采购订单审核后的BPM推送
/// 功能：将审核通过的采购订单数据推送到BPM系统进行审批
/// </remarks>
[Description("采购订单审核推送BPM")]
[HotUpdate]
public class PoAuditToBPMHT : AbstractOperationServicePlugIn
{
    /// <summary>
    /// 准备操作选项
    /// </summary>
    /// <param name="e">操作选项事件参数</param>
    protected override void OnPrepareOperateOption(OnPrepareOperateOptionEventArgs e)
    {
        // 实现逻辑
    }
}
```

### 示例2：为实体类生成注释
```
/Comment ./models/ErpDataDict.cs --type entity
```

**生成的注释示例**：
```csharp
/// <summary>
/// ERP数据字典实体类 - 系统配置数据映射
/// </summary>
/// <remarks>
/// 对应数据库表：T_ERP_DATADICT
/// 主键：FID
/// 用途：存储ERP系统配置数据，包括基础资料、编码规则等
/// </remarks>
public class ErpDataDict
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public long FID { get; set; }
    
    /// <summary>
    /// 单据编号
    /// </summary>
    public string FBILLNO { get; set; }
}
```

## 注意事项

1. **保持代码简洁**：注释不要过于冗长，突出关键信息
2. **避免重复**：不要生成与现有注释重复的内容
3. **格式一致**：保持与项目现有注释风格一致
4. **及时更新**：代码修改时同步更新相关注释
5. **遵循规范**：遵循金蝶开发规范中的注释要求

## 自定义扩展

### 1. 添加新的注释类型
在脚本中添加新的注释模板和生成逻辑。

### 2. 自定义注释格式
修改注释模板，适应项目特定的注释风格。

### 3. 集成其他工具
与代码分析工具、文档生成工具集成。

## 常见问题

### Q1: 注释会破坏现有代码吗？
A1: 不会，注释生成器会智能分析代码结构，在适当位置插入注释，保持代码格式不变。

### Q2: 支持哪些C#版本？
A2: 支持.NET Framework 4.8及以上的C#语法，包括最新特性。

### Q3: 如何处理已有的注释？
A3: 会检测已有注释，避免重复生成。如果已有注释不完整，会进行补充完善。

### Q4: 能否批量处理多个文件？
A4: 可以，支持目录批量处理，使用通配符或目录路径。
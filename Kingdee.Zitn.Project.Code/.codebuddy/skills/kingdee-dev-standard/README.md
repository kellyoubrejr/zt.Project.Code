# 金蝶ERP开发规范Skills

## 概述
本skills为金蝶ERP项目开发提供标准化的编码规范、开发流程和技术实现指南，确保代码质量、可维护性和团队协作效率。

## 目录结构
```
kingdee-dev-standard/
├── SKILL.md                    # 主说明文件（Skills定义）
├── README.md                   # 本说明文件
├── scripts/                    # 辅助脚本
│   ├── check_coding_standard.py    # 编码规范检查工具
│   └── generate_plugin_template.py # 模板生成工具
├── references/                 # 参考文档
│   ├── kingdee_api_reference.md    # 金蝶API参考文档
│   ├── project_coding_standard.md  # 项目编码规范文档
│   └── plugin_types.md             # 插件类型说明文档
└── assets/                     # 资源文件
    ├── templates/                   # 代码模板
    │   ├── bill_plugin_template.cs      # 表单插件模板
    │   ├── dynamic_form_plugin_template.cs  # 动态表单插件模板
    │   ├── operation_service_template.cs    # 操作服务插件模板
    │   └── schedule_task_template.cs        # 调度任务模板
    └── examples/                    # 示例代码
        ├── po_audit_example.cs      # 采购订单审核示例
        └── sf_bill_example.cs      # 销售单审核示例
```

## 功能特性

### 1. 开发规范检查
使用 `check_coding_standard.py` 脚本检查代码是否符合项目规范：
```bash
# 检查单个文件
python scripts/check_coding_standard.py --path ./plugin/PO/MyPlugin.cs

# 检查整个目录
python scripts/check_coding_standard.py --path ./plugin/

# 输出检查结果到文件
python scripts/check_coding_standard.py --path ./plugin/ --output check_result.txt
```

**检查内容**：
- 命名空间规范
- 插件继承规范
- 特性标记规范
- 日志记录规范
- 消息通知规范

### 2. 模板生成
使用 `generate_plugin_template.py` 脚本生成标准化的插件代码：
```bash
# 生成表单插件模板
python scripts/generate_plugin_template.py --type bill --name MyBillPlugin --module PO

# 生成动态表单插件模板
python scripts/generate_plugin_template.py --type dynamic --name MyDynamicForm --module PO

# 生成操作服务插件模板
python scripts/generate_plugin_template.py --type operation --name MyOperationService --module PO

# 生成列表插件模板
python scripts/generate_plugin_template.py --type list --name MyListPlugin --module PO

# 指定输出路径
python scripts/generate_plugin_template.py --type bill --name MyBillPlugin --module PO --output ./plugin/PO/MyBillPlugin.cs
```

**支持的插件类型**：
- `bill`：表单插件（继承 `AbstractBillPlugIn`）
- `dynamic`：动态表单插件（继承 `AbstractDynamicFormPlugIn`）
- `operation`：操作服务插件（继承 `AbstractOperationServicePlugIn`）
- `list`：列表插件（继承 `AbstractListPlugIn`）

### 3. 参考文档
- **kingdee_api_reference.md**：金蝶BOS平台API参考文档
- **project_coding_standard.md**：项目详细的编码规范文档
- **plugin_types.md**：各类插件的详细说明和使用场景

### 4. 代码模板
- **bill_plugin_template.cs**：表单插件标准化模板
- **dynamic_form_plugin_template.cs**：动态表单插件标准化模板
- **operation_service_template.cs**：操作服务插件标准化模板
- **schedule_task_template.cs**：调度任务标准化模板

### 5. 示例代码
- **po_audit_example.cs**：采购订单审核服务插件示例
- **sf_bill_example.cs**：销售单审核操作插件示例

## 使用指南

### 1. 开发新插件
1. 确定插件类型（表单、动态表单、操作服务、列表）
2. 使用模板生成工具生成基础代码
3. 根据业务需求实现具体逻辑
4. 运行编码规范检查确保代码质量

### 2. 修改现有插件
1. 查看相关参考文档了解规范
2. 修改代码时遵循编码规范
3. 使用检查工具验证修改后的代码
4. 参考示例代码学习最佳实践

### 3. 代码审查
1. 使用编码规范检查工具
2. 对照参考文档验证规范遵循情况
3. 参考示例代码检查实现模式

## 开发规范要点

### 1. 插件继承规范
- 表单插件：继承 `AbstractBillPlugIn`
- 动态表单插件：继承 `AbstractDynamicFormPlugIn`
- 操作服务插件：继承 `AbstractOperationServicePlugIn`
- 列表插件：继承 `AbstractListPlugIn`

### 2. 代码结构规范
- 命名空间：`Kingdee.Zitn.Project.Code.plugin.{模块名}`
- 特性标记：`[Description("功能描述")]` 和 `[HotUpdate]`
- 日志记录：使用 `CustomLog` 类
- 消息通知：使用 `SendMsg.Send()` 方法

### 3. 开发流程规范
- 先计划再修改，不随意删除代码
- 发现问题先咨询用户，同意后再处理
- 注释不要随便增加，保持代码简洁
- 遵循统一的日志和消息通知方式

## 自定义扩展

### 1. 添加新的插件类型模板
在 `assets/templates/` 目录下添加新的模板文件，并更新 `generate_plugin_template.py` 脚本。

### 2. 扩展编码规范检查规则
在 `scripts/check_coding_standard.py` 中添加新的检查规则。

### 3. 增加项目特定的工具方法
在参考文档中添加项目特定的工具方法说明。

### 4. 添加更多示例代码
在 `assets/examples/` 目录下添加更多业务场景的示例代码。

## 如何使用Skills

### 1. 使用斜杠命令触发Skills
在CodeBuddy对话中，您可以使用以下方式触发skills：

**方式一：完整命令**
```
/金蝶ERP开发规范 我需要开发一个PO模块的审核插件
```

**方式二：交互式触发**
1. 输入 `/` 触发命令菜单
2. 选择 `金蝶ERP开发规范`
3. 然后输入您的具体需求

**方式三：简化命令**
```
/金蝶开发规范 创建一个销售单保存生成订单的插件
```

### 2. 使用场景示例

#### 场景1：开发新的表单插件
```
/金蝶ERP开发规范 我需要开发一个采购订单的表单插件，实现保存前验证逻辑
```

#### 场景2：修改现有插件
```
/金蝶ERP开发规范 修改 SFBillAuditOpe.cs 插件，增加新的审核条件判断
```

#### 场景3：创建调度任务
```
/金蝶ERP开发规范 创建一个定时任务，每天同步销售单图片
```

#### 场景4：集成外部系统
```
/金蝶ERP开发规范 开发BPM系统集成接口，推送采购订单数据
```

### 3. Skills执行流程
当您使用斜杠命令后，系统会自动：
1. 加载 `SKILL.md` 中的开发规范
2. 根据您的需求生成标准化的代码
3. 遵循项目特定的编码规范
4. 使用统一的日志、消息通知和数据库操作方式
5. 提供符合项目架构的代码结构

### 4. 结合脚本工具使用

#### 生成插件代码
```bash
# 使用模板生成工具
python .codebuddy/skills/kingdee-dev-standard/scripts/generate_plugin_template.py \
  --type bill \
  --name MyBillPlugin \
  --module PO \
  --output ./plugin/PO/MyBillPlugin.cs
```

#### 检查代码规范
```bash
# 检查单个文件
python .codebuddy/skills/kingdee-dev-standard/scripts/check_coding_standard.py \
  --path ./plugin/PO/MyBillPlugin.cs

# 检查整个目录
python .codebuddy/skills/kingdee-dev-standard/scripts/check_coding_standard.py \
  --path ./plugin/PO/
```

### 5. 自定义Skills
您可以随时在skills目录中添加或修改内容：

**添加新的模板文件**：
```bash
# 在 assets/templates/ 目录下添加新的模板
# 例如：添加报表插件模板
```

**扩展参考文档**：
```bash
# 在 references/ 目录下添加新的规范文档
# 例如：添加数据库设计规范
```

**添加新的脚本工具**：
```bash
# 在 scripts/ 目录下添加新的辅助工具
# 例如：添加数据库迁移脚本
```

### 6. 最佳实践建议
1. **先计划后实现**：在开始编码前，先描述您的需求
2. **遵循规范**：使用skills确保代码符合项目规范
3. **测试验证**：使用检查脚本验证代码质量
4. **持续改进**：根据使用情况不断完善skills

## 常见问题

### Q1: 如何开始使用这个skills？
A1: 在CodeBuddy对话中输入 `/金蝶ERP开发规范` 然后描述您的需求，系统会自动应用开发规范。

### Q2: 如何确保代码符合规范？
A2: 使用 `check_coding_standard.py` 脚本检查代码规范，或在开发时直接使用skills，系统会自动应用规范。

### Q3: 如何添加新的插件类型？
A3: 在 `assets/templates/` 目录下创建新的模板文件，并在 `generate_plugin_template.py` 中添加对应的生成逻辑。

### Q4: 如何扩展编码规范？
A4: 修改 `references/project_coding_standard.md` 文档，添加新的规范要求，并在检查脚本中实现对应的检查逻辑。

### Q5: Skills支持哪些开发场景？
A5: 支持表单插件、动态表单插件、操作服务插件、列表插件、调度任务、系统集成接口等各类开发场景。

### Q6: 如何修改已有的开发规范？
A6: 直接编辑 `SKILL.md` 或 `references/` 目录下的文档文件，保存后即可生效。

## 更新日志
- **v1.0**：初始版本，包含基础的插件模板和编码规范
- 后续版本将根据项目需求持续更新和完善

## 联系方式
如有任何问题或建议，请联系项目负责人或在项目仓库中提交Issue。
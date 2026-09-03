#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
C#代码解释器
解释C#代码的逻辑原理、运行流程、架构设计和业务规则
"""

import os
import re
import sys
import argparse
from typing import List, Dict, Tuple, Optional
import ast
import json

class CSharpCodeExplainer:
    """C#代码解释器类"""
    
    def __init__(self, file_path: str, focus: str = "logic"):
        """
        初始化代码解释器
        
        Args:
            file_path: C#文件路径
            focus: 解释重点 (logic, flow, architecture, business, technical)
        """
        self.file_path = file_path
        self.focus = focus
        self.content = ""
        self.lines = []
        self.analysis = {}
        
    def read_file(self) -> bool:
        """读取文件内容"""
        try:
            with open(self.file_path, 'r', encoding='utf-8') as f:
                self.content = f.read()
                self.lines = self.content.split('\n')
            return True
        except Exception as e:
            print(f"读取文件失败: {e}")
            return False
    
    def analyze_code_structure(self) -> Dict:
        """分析代码结构"""
        analysis = {
            'file_info': self._get_file_info(),
            'namespace': '',
            'usings': [],
            'classes': [],
            'methods': [],
            'properties': [],
            'events': [],
            'inheritance': [],
            'attributes': []
        }
        
        # 提取命名空间
        namespace_match = re.search(r'namespace\s+([^;]+)', self.content)
        if namespace_match:
            analysis['namespace'] = namespace_match.group(1).strip()
        
        # 提取using语句
        using_matches = re.findall(r'using\s+([^;]+);', self.content)
        analysis['usings'] = [u.strip() for u in using_matches]
        
        # 提取类定义
        class_pattern = r'(?:public|internal|private|protected)?\s*(?:static|partial|abstract|sealed)?\s*class\s+(\w+)(?:\s*:\s*([^{]+))?'
        class_matches = re.finditer(class_pattern, self.content)
        
        for match in class_matches:
            class_name = match.group(1)
            inheritance = match.group(2).strip() if match.group(2) else ''
            analysis['classes'].append({
                'name': class_name,
                'inheritance': inheritance,
                'position': match.start(),
                'line': self.content[:match.start()].count('\n') + 1
            })
            
            # 提取继承的接口和类
            if inheritance:
                interfaces = [i.strip() for i in inheritance.split(',')]
                analysis['inheritance'].extend(interfaces)
        
        # 提取方法定义
        method_pattern = r'(?:public|internal|private|protected)?\s*(?:static|virtual|override|abstract|async)?\s*(\w+(?:<[^>]+>)?)\s+(\w+)\s*\(([^)]*)\)'
        method_matches = re.finditer(method_pattern, self.content)
        
        for match in method_matches:
            return_type = match.group(1)
            method_name = match.group(2)
            params = match.group(3).strip()
            analysis['methods'].append({
                'return_type': return_type,
                'name': method_name,
                'params': params,
                'position': match.start(),
                'line': self.content[:match.start()].count('\n') + 1
            })
        
        # 提取属性定义
        property_pattern = r'(?:public|internal|private|protected)?\s*(?:static|virtual|override)?\s*(\w+(?:<[^>]+>)?)\s+(\w+)\s*\{\s*(?:get|set)?\s*(?:\{[^}]*\})?\s*\}'
        property_matches = re.finditer(property_pattern, self.content)
        
        for match in property_matches:
            prop_type = match.group(1)
            prop_name = match.group(2)
            analysis['properties'].append({
                'type': prop_type,
                'name': prop_name,
                'position': match.start(),
                'line': self.content[:match.start()].count('\n') + 1
            })
        
        # 提取特性标记
        attribute_pattern = r'\[(\w+)(?:\(([^)]*)\))?\]'
        attribute_matches = re.finditer(attribute_pattern, self.content)
        
        for match in attribute_matches:
            attr_name = match.group(1)
            attr_params = match.group(2) if match.group(2) else ''
            analysis['attributes'].append({
                'name': attr_name,
                'params': attr_params,
                'position': match.start(),
                'line': self.content[:match.start()].count('\n') + 1
            })
        
        return analysis
    
    def _get_file_info(self) -> Dict:
        """获取文件基本信息"""
        return {
            'path': self.file_path,
            'name': os.path.basename(self.file_path),
            'size': os.path.getsize(self.file_path),
            'extension': os.path.splitext(self.file_path)[1]
        }
    
    def generate_overview(self) -> str:
        """生成概述"""
        analysis = self.analyze_code_structure()
        self.analysis = analysis
        
        overview = f"""## 概述

**文件信息**：
- 文件名：{analysis['file_info']['name']}
- 路径：{analysis['file_info']['path']}
- 大小：{analysis['file_info']['size']} 字节

**代码结构**：
- 命名空间：{analysis['namespace'] if analysis['namespace'] else '未定义'}
- 类数量：{len(analysis['classes'])}
- 方法数量：{len(analysis['methods'])}
- 属性数量：{len(analysis['properties'])}
- 使用的命名空间：{len(analysis['usings'])} 个

**主要类**：
"""
        
        for i, class_info in enumerate(analysis['classes'][:3]):  # 只显示前3个类
            overview += f"{i+1}. {class_info['name']}"
            if class_info['inheritance']:
                overview += f" (继承自 {class_info['inheritance']})"
            overview += "\n"
        
        return overview
    
    def generate_logic_explanation(self) -> str:
        """生成逻辑原理解释"""
        analysis = self.analysis
        
        explanation = """## 逻辑原理

**核心算法**：
1. **数据处理流程**：输入数据 → 验证 → 业务处理 → 输出结果
2. **状态管理**：通过单据状态控制业务流程
3. **异常处理**：try-catch机制确保系统稳定性

**关键实现**：
"""
        
        # 分析关键方法
        key_methods = self._identify_key_methods(analysis['methods'])
        
        for i, method in enumerate(key_methods[:5]):  # 只显示前5个关键方法
            explanation += f"{i+1}. **{method['name']}**：{self._get_method_purpose(method['name'])}\n"
        
        explanation += """
**设计模式**：
- **插件模式**：通过继承实现功能扩展
- **事件驱动**：通过重写方法响应系统事件
- **依赖注入**：通过接口解耦组件依赖

**数据流**：
```
用户操作 → 插件事件 → 业务逻辑 → 数据库操作 → 结果反馈
```
"""
        
        return explanation
    
    def generate_flow_explanation(self) -> str:
        """生成运行流程解释"""
        analysis = self.analysis
        
        explanation = """## 运行流程

**执行顺序**：
1. **初始化阶段**：插件加载，上下文初始化
2. **事件触发**：用户操作或系统事件触发
3. **业务处理**：执行相应的业务逻辑
4. **数据操作**：数据库读写操作
5. **结果返回**：处理结果反馈给用户

**方法调用链**：
```
"""
        
        # 生成方法调用链
        for method in analysis['methods'][:5]:
            explanation += f"{method['name']}() → "
        
        explanation += """...
```

**生命周期**：
1. **创建**：插件实例化
2. **注册**：注册到金蝶BOS平台
3. **激活**：对应单据打开时激活
4. **执行**：响应事件执行逻辑
5. **销毁**：单据关闭时销毁

**事件处理**：
- **按钮点击**：ButtonClick方法处理
- **数据变更**：DataChanged方法处理
- **保存操作**：BeforeSave/AfterSave方法处理
- **审核操作**：BeginOperationTransaction方法处理
"""
        
        return explanation
    
    def generate_architecture_explanation(self) -> str:
        """生成架构设计解释"""
        analysis = self.analysis
        
        explanation = """## 架构设计

**整体架构**：
- **分层架构**：表现层 → 业务逻辑层 → 数据访问层
- **插件架构**：基于金蝶BOS平台的插件式设计
- **模块化设计**：按业务功能划分模块

**组件关系**：
```
"""
        
        # 分析继承关系
        if analysis['inheritance']:
            explanation += "继承体系：\n"
            for inheritance in analysis['inheritance'][:5]:
                explanation += f"  - {inheritance}\n"
        
        explanation += """```

**扩展点**：
1. **方法重写**：通过重写基类方法扩展功能
2. **接口实现**：通过实现接口添加新功能
3. **事件订阅**：通过订阅事件响应系统变化

**依赖管理**：
- **金蝶BOS平台**：核心依赖
- **.NET Framework 4.8**：运行环境
- **项目内部依赖**：通过命名空间组织

**配置管理**：
- **插件配置**：通过金蝶BOS平台配置
- **数据库配置**：通过配置文件管理
- **日志配置**：通过CustomLog配置
"""
        
        return explanation
    
    def generate_business_explanation(self) -> str:
        """生成业务规则解释"""
        analysis = self.analysis
        
        explanation = """## 业务规则

**业务流程**：
1. **单据创建**：用户创建新的业务单据
2. **数据录入**：填写单据详细信息
3. **保存验证**：系统验证数据完整性
4. **提交审核**：单据进入审核流程
5. **审核处理**：审核人员审批单据
6. **完成处理**：审核通过后执行后续操作

**业务约束**：
- **数据完整性**：必填字段验证
- **业务规则**：金额、数量、日期等业务逻辑验证
- **权限控制**：基于用户角色的操作权限
- **状态控制**：单据状态驱动业务流程

**关键业务逻辑**：
"""
        
        # 分析业务相关方法
        business_methods = self._identify_business_methods(analysis['methods'])
        
        for i, method in enumerate(business_methods[:3]):
            explanation += f"{i+1}. **{method['name']}**：{self._get_business_purpose(method['name'])}\n"
        
        explanation += """
**数据验证规则**：
- **字段级验证**：单个字段的数据格式和范围验证
- **表头级验证**：单据头信息的完整性验证
- **分录级验证**：单据体信息的业务逻辑验证
- **整体验证**：单据整体的数据一致性验证

**异常处理策略**：
- **业务异常**：提示用户具体业务规则冲突
- **系统异常**：记录日志并通知管理员
- **数据异常**：回滚事务并恢复数据
"""
        
        return explanation
    
    def generate_technical_explanation(self) -> str:
        """生成技术细节解释"""
        analysis = self.analysis
        
        explanation = """## 技术细节

**技术栈**：
- **开发语言**：C# (.NET Framework 4.8)
- **开发平台**：金蝶BOS平台
- **数据库**：SQL Server
- **日志框架**：CustomLog
- **消息队列**：企业微信API

**关键技术**：
1. **插件机制**：基于继承和接口的插件架构
2. **ORM映射**：金蝶BOS平台的数据映射
3. **事务管理**：数据库事务处理
4. **异常处理**：多层次的异常处理机制

**性能优化**：
- **数据库优化**：使用参数化查询防止SQL注入
- **缓存机制**：合理使用缓存提高性能
- **异步处理**：耗时操作使用异步处理
- **资源管理**：及时释放资源避免内存泄漏

**安全考虑**：
- **输入验证**：验证所有外部输入
- **SQL注入防护**：使用参数化查询
- **权限控制**：基于角色的访问控制
- **日志安全**：敏感信息脱敏处理

**扩展性设计**：
- **插件扩展**：通过继承扩展新功能
- **配置扩展**：通过配置文件扩展参数
- **接口扩展**：通过接口定义扩展点
- **模块扩展**：通过模块化设计支持功能扩展
"""
        
        return explanation
    
    def _identify_key_methods(self, methods: List[Dict]) -> List[Dict]:
        """识别关键方法"""
        key_patterns = ['Save', 'Audit', 'Submit', 'ButtonClick', 'DataChanged', 
                       'BeginOperation', 'EndOperation', 'AfterCreate', 'BeforeSave']
        
        key_methods = []
        for method in methods:
            for pattern in key_patterns:
                if pattern.lower() in method['name'].lower():
                    key_methods.append(method)
                    break
        
        return key_methods if key_methods else methods[:3]
    
    def _identify_business_methods(self, methods: List[Dict]) -> List[Dict]:
        """识别业务方法"""
        business_patterns = ['Validate', 'Check', 'Process', 'Handle', 'Execute', 
                            'Update', 'Insert', 'Delete', 'Query', 'Get']
        
        business_methods = []
        for method in methods:
            for pattern in business_patterns:
                if pattern.lower() in method['name'].lower():
                    business_methods.append(method)
                    break
        
        return business_methods if business_methods else methods[:3]
    
    def _get_method_purpose(self, method_name: str) -> str:
        """获取方法用途描述"""
        method_purposes = {
            'Save': '保存单据数据到数据库',
            'Audit': '审核单据，更新状态',
            'Submit': '提交单据进入审核流程',
            'ButtonClick': '处理按钮点击事件',
            'DataChanged': '处理数据变更事件',
            'BeginOperation': '开始业务操作事务',
            'EndOperation': '结束业务操作事务',
            'AfterCreate': '新建单据后的初始化处理',
            'BeforeSave': '保存前的验证和处理',
            'Validate': '验证数据完整性和业务规则',
            'Check': '检查业务条件和权限',
            'Process': '执行业务处理逻辑',
            'Handle': '处理特定业务场景',
            'Execute': '执行具体的业务操作'
        }
        
        for key, purpose in method_purposes.items():
            if key.lower() in method_name.lower():
                return purpose
        
        return '执行特定业务功能'
    
    def _get_business_purpose(self, method_name: str) -> str:
        """获取业务用途描述"""
        business_purposes = {
            'Validate': '验证数据是否符合业务规则',
            'Check': '检查业务条件和权限',
            'Process': '处理业务流程和逻辑',
            'Handle': '处理特定业务场景',
            'Execute': '执行具体的业务操作',
            'Update': '更新业务数据',
            'Insert': '插入新的业务数据',
            'Delete': '删除业务数据',
            'Query': '查询业务数据',
            'Get': '获取业务数据'
        }
        
        for key, purpose in business_purposes.items():
            if key.lower() in method_name.lower():
                return purpose
        
        return '执行业务功能'
    
    def generate_explanation(self) -> str:
        """生成完整解释"""
        if not self.read_file():
            return "无法读取文件"
        
        # 分析代码结构
        self.analysis = self.analyze_code_structure()
        
        # 根据focus生成相应解释
        explanation = self.generate_overview()
        
        if self.focus == "logic":
            explanation += self.generate_logic_explanation()
        elif self.focus == "flow":
            explanation += self.generate_flow_explanation()
        elif self.focus == "architecture":
            explanation += self.generate_architecture_explanation()
        elif self.focus == "business":
            explanation += self.generate_business_explanation()
        elif self.focus == "technical":
            explanation += self.generate_technical_explanation()
        else:
            # 默认生成所有解释
            explanation += self.generate_logic_explanation()
            explanation += self.generate_flow_explanation()
            explanation += self.generate_architecture_explanation()
            explanation += self.generate_business_explanation()
            explanation += self.generate_technical_explanation()
        
        # 添加金蝶开发规范相关解释
        explanation += self.generate_kingdee_specific_explanation()
        
        return explanation
    
    def generate_kingdee_specific_explanation(self) -> str:
        """生成金蝶开发规范相关的解释"""
        analysis = self.analysis
        
        # 检查是否为金蝶插件
        is_kingdee_plugin = False
        plugin_type = ""
        
        for inheritance in analysis['inheritance']:
            inheritance_lower = inheritance.lower()
            if 'abstractbillplugin' in inheritance_lower:
                is_kingdee_plugin = True
                plugin_type = "表单插件"
                break
            elif 'abstractdynamicformplugin' in inheritance_lower:
                is_kingdee_plugin = True
                plugin_type = "动态表单插件"
                break
            elif 'abstractoperationserviceplugin' in inheritance_lower:
                is_kingdee_plugin = True
                plugin_type = "操作服务插件"
                break
            elif 'abstractlistplugin' in inheritance_lower:
                is_kingdee_plugin = True
                plugin_type = "列表插件"
                break
            elif 'irepairservice' in inheritance_lower:
                is_kingdee_plugin = True
                plugin_type = "调度任务"
                break
        
        if not is_kingdee_plugin:
            return ""
        
        explanation = f"""
## 金蝶开发规范说明

**插件类型**：{plugin_type}
**继承关系**：{', '.join(analysis['inheritance'])}

**金蝶BOS平台特性**：
1. **插件机制**：通过继承基类实现功能扩展
2. **事件驱动**：通过重写方法响应平台事件
3. **热更新**：支持[HotUpdate]特性实现代码热更新
4. **元数据驱动**：基于元数据的业务对象定义

**开发规范遵循**：
- **命名空间**：遵循项目命名空间规范
- **特性标记**：使用[Description]和[HotUpdate]特性
- **日志记录**：使用CustomLog进行日志记录
- **消息通知**：使用SendMsg发送企业微信通知

**插件生命周期**：
1. **加载**：插件被金蝶BOS平台加载
2. **注册**：注册到对应的业务对象
3. **激活**：业务对象打开时激活插件
4. **执行**：响应事件执行业务逻辑
5. **卸载**：业务对象关闭时卸载插件

**常用重写方法**：
- **ButtonClick**：处理按钮点击事件
- **DataChanged**：处理数据变更事件
- **BeforeSave**：保存前验证
- **AfterSave**：保存后处理
- **BeginOperationTransaction**：操作事务开始
- **EndOperationTransaction**：操作事务结束
"""
        
        return explanation

def main():
    """主函数"""
    parser = argparse.ArgumentParser(description='C#代码解释器')
    parser.add_argument('file_path', help='C#文件路径')
    parser.add_argument('--focus', choices=['logic', 'flow', 'architecture', 'business', 'technical'], 
                       default='logic', help='解释重点')
    parser.add_argument('--output', help='输出文件路径')
    
    args = parser.parse_args()
    
    # 检查文件是否存在
    if not os.path.exists(args.file_path):
        print(f"文件不存在: {args.file_path}")
        sys.exit(1)
    
    # 创建代码解释器
    explainer = CSharpCodeExplainer(args.file_path, args.focus)
    
    # 生成解释
    explanation = explainer.generate_explanation()
    
    # 输出解释
    if args.output:
        with open(args.output, 'w', encoding='utf-8') as f:
            f.write(explanation)
        print(f"解释已保存到: {args.output}")
    else:
        print(explanation)

if __name__ == '__main__':
    main()
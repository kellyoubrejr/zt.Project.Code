#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
C#代码注释生成器
为C#代码文件自动生成XML文档注释
"""

import os
import re
import sys
import argparse
from typing import List, Dict, Tuple, Optional
import xml.etree.ElementTree as ET

class CSharpCommentGenerator:
    """C#代码注释生成器类"""
    
    def __init__(self, file_path: str, comment_type: str = "doc"):
        """
        初始化注释生成器
        
        Args:
            file_path: C#文件路径
            comment_type: 注释类型 (doc, function, model, entity, plugin)
        """
        self.file_path = file_path
        self.comment_type = comment_type
        self.content = ""
        self.lines = []
        
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
    
    def analyze_code(self) -> Dict:
        """分析代码结构"""
        analysis = {
            'namespace': '',
            'usings': [],
            'classes': [],
            'methods': [],
            'properties': [],
            'fields': []
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
                'position': match.start()
            })
        
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
                'position': match.start()
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
                'position': match.start()
            })
        
        return analysis
    
    def generate_class_comment(self, class_info: Dict) -> str:
        """生成类注释"""
        class_name = class_info['name']
        inheritance = class_info['inheritance']
        
        if self.comment_type == 'plugin':
            # 插件类注释
            plugin_type = self._get_plugin_type(inheritance)
            comment = f"""/// <summary>
/// {class_name} - {plugin_type}插件
/// </summary>
/// <remarks>
/// 继承自{inheritance if inheritance else '基类'}
/// 用于处理特定业务逻辑
/// </remarks>"""
        elif self.comment_type == 'entity':
            # 实体类注释
            comment = f"""/// <summary>
/// {class_name} - 数据实体类
/// </summary>
/// <remarks>
/// 数据模型：{class_name}
/// 用途：数据传输和存储
/// </remarks>"""
        elif self.comment_type == 'model':
            # 模型类注释
            comment = f"""/// <summary>
/// {class_name} - 数据模型
/// </summary>
/// <remarks>
/// 模型类型：{self.comment_type}
/// 用途：数据封装和传递
/// </remarks>"""
        else:
            # 默认文档注释
            comment = f"""/// <summary>
/// {class_name} 类
/// </summary>
/// <remarks>
/// 功能说明：{class_name}类的主要功能
/// 使用场景：相关业务场景
/// </remarks>"""
        
        return comment
    
    def generate_method_comment(self, method_info: Dict) -> str:
        """生成方法注释"""
        method_name = method_info['name']
        return_type = method_info['return_type']
        params = method_info['params']
        
        # 解析参数
        param_comments = []
        if params:
            for param in params.split(','):
                param = param.strip()
                if param:
                    # 提取参数名
                    param_parts = param.split()
                    if len(param_parts) >= 2:
                        param_name = param_parts[-1]
                        param_type = ' '.join(param_parts[:-1])
                        param_comments.append(f"/// <param name=\"{param_name}\">{param_type}参数</param>")
        
        # 生成方法注释
        if self.comment_type == 'function':
            # 功能注释
            comment = f"""/// <summary>
/// {method_name} 功能方法
/// </summary>
/// <remarks>
/// 功能描述：{method_name}方法的主要功能
/// 实现逻辑：具体的业务逻辑实现
/// </remarks>"""
        else:
            # 默认文档注释
            comment = f"""/// <summary>
/// {method_name} 方法
/// </summary>"""
        
        # 添加参数注释
        for param_comment in param_comments:
            comment += f"\n{param_comment}"
        
        # 添加返回值注释
        if return_type and return_type.lower() != 'void':
            comment += f"\n/// <returns>{return_type} 返回值说明</returns>"
        
        return comment
    
    def generate_property_comment(self, prop_info: Dict) -> str:
        """生成属性注释"""
        prop_name = prop_info['name']
        prop_type = prop_info['type']
        
        if self.comment_type == 'entity':
            # 实体属性注释
            comment = f"""/// <summary>
/// {prop_name} - {prop_type}类型属性
/// </summary>
/// <value>
/// {prop_name}属性值
/// </value>"""
        else:
            # 默认属性注释
            comment = f"""/// <summary>
/// {prop_name} 属性
/// </summary>
/// <value>{prop_type} 类型</value>"""
        
        return comment
    
    def _get_plugin_type(self, inheritance: str) -> str:
        """根据继承关系判断插件类型"""
        if not inheritance:
            return "通用"
        
        inheritance_lower = inheritance.lower()
        
        if 'abstractbillplugin' in inheritance_lower:
            return "表单"
        elif 'abstractdynamicformplugin' in inheritance_lower:
            return "动态表单"
        elif 'abstractoperationserviceplugin' in inheritance_lower:
            return "操作服务"
        elif 'abstractlistplugin' in inheritance_lower:
            return "列表"
        elif 'irepairservice' in inheritance_lower:
            return "调度任务"
        else:
            return "自定义"
    
    def insert_comments(self, analysis: Dict) -> str:
        """在代码中插入注释"""
        # 这里简化实现，实际应该更复杂
        # 需要处理代码位置、缩进、已有注释等
        
        # 简单示例：在类定义前插入注释
        new_lines = self.lines.copy()
        
        # 按位置倒序处理，避免索引问题
        insertions = []
        
        # 处理类注释
        for class_info in analysis['classes']:
            comment = self.generate_class_comment(class_info)
            # 找到类定义的行
            for i, line in enumerate(new_lines):
                if f"class {class_info['name']}" in line:
                    # 检查是否已有注释
                    if i > 0 and '///' in new_lines[i-1]:
                        continue
                    insertions.append((i, comment))
                    break
        
        # 处理方法注释
        for method_info in analysis['methods']:
            comment = self.generate_method_comment(method_info)
            # 找到方法定义的行
            for i, line in enumerate(new_lines):
                if f"{method_info['name']}(" in line and method_info['return_type'] in line:
                    # 检查是否已有注释
                    if i > 0 and '///' in new_lines[i-1]:
                        continue
                    insertions.append((i, comment))
                    break
        
        # 按位置倒序插入
        insertions.sort(key=lambda x: x[0], reverse=True)
        for line_num, comment in insertions:
            new_lines.insert(line_num, comment)
        
        return '\n'.join(new_lines)
    
    def generate_comments(self) -> bool:
        """生成注释的主方法"""
        if not self.read_file():
            return False
        
        # 分析代码
        analysis = self.analyze_code()
        
        # 生成注释
        new_content = self.insert_comments(analysis)
        
        # 保存文件
        try:
            with open(self.file_path, 'w', encoding='utf-8') as f:
                f.write(new_content)
            print(f"成功为文件生成注释: {self.file_path}")
            return True
        except Exception as e:
            print(f"保存文件失败: {e}")
            return False

def main():
    """主函数"""
    parser = argparse.ArgumentParser(description='C#代码注释生成器')
    parser.add_argument('file_path', help='C#文件路径')
    parser.add_argument('--type', choices=['doc', 'function', 'model', 'entity', 'plugin'], 
                       default='doc', help='注释类型')
    parser.add_argument('--output', help='输出文件路径（默认覆盖原文件）')
    
    args = parser.parse_args()
    
    # 检查文件是否存在
    if not os.path.exists(args.file_path):
        print(f"文件不存在: {args.file_path}")
        sys.exit(1)
    
    # 创建注释生成器
    generator = CSharpCommentGenerator(args.file_path, args.type)
    
    # 生成注释
    if generator.generate_comments():
        print("注释生成完成")
    else:
        print("注释生成失败")
        sys.exit(1)

if __name__ == '__main__':
    main()
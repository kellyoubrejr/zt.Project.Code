#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
金蝶ERP项目编码规范检查工具

检查C#代码文件是否符合项目编码规范，包括：
1. 命名空间规范
2. 插件继承规范
3. 特性标记规范
4. 日志记录规范
5. 消息通知规范
"""

import os
import re
import sys
import argparse
from typing import List, Dict, Tuple

class CodingStandardChecker:
    """编码规范检查器"""
    
    def __init__(self):
        # 命名空间规范
        self.namespace_pattern = r'^namespace\s+Kingdee\.Zitn\.Project\.Code\.plugin\.[A-Za-z_][A-Za-z0-9_]*$'
        
        # 插件继承规范
        self.plugin_bases = {
            'AbstractBillPlugIn': '表单插件',
            'AbstractDynamicFormPlugIn': '动态表单插件',
            'AbstractOperationServicePlugIn': '操作服务插件',
            'AbstractListPlugIn': '列表插件'
        }
        
        # 特性标记规范
        self.required_attributes = ['Description', 'HotUpdate']
        
        # 日志记录规范
        self.log_patterns = [
            r'CustomLog\.LogWriter',
            r'_log\.WriteLog',
            r'_log\.Error',
            r'_log\.Section'
        ]
        
        # 消息通知规范
        self.msg_patterns = [
            r'SendMsg\.Send'
        ]
    
    def check_file(self, file_path: str) -> List[str]:
        """检查单个文件"""
        errors = []
        
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                lines = content.split('\n')
        except Exception as e:
            errors.append(f"无法读取文件：{e}")
            return errors
        
        # 检查命名空间
        errors.extend(self.check_namespace(lines, file_path))
        
        # 检查插件继承
        errors.extend(self.check_plugin_inheritance(lines, file_path))
        
        # 检查特性标记
        errors.extend(self.check_attributes(lines, file_path))
        
        # 检查日志记录
        errors.extend(self.check_logging(content, file_path))
        
        # 检查消息通知
        errors.extend(self.check_messaging(content, file_path))
        
        return errors
    
    def check_namespace(self, lines: List[str], file_path: str) -> List[str]:
        """检查命名空间规范"""
        errors = []
        
        for i, line in enumerate(lines, 1):
            line = line.strip()
            if line.startswith('namespace '):
                if not re.match(self.namespace_pattern, line):
                    errors.append(f"第{i}行：命名空间不符合规范 '{line}'")
                    errors.append(f"  规范格式：namespace Kingdee.Zitn.Project.Code.plugin.{{模块名}}")
                break
        
        return errors
    
    def check_plugin_inheritance(self, lines: List[str], file_path: str) -> List[str]:
        """检查插件继承规范"""
        errors = []
        found_class = False
        
        for i, line in enumerate(lines, 1):
            line = line.strip()
            
            # 查找类定义
            if line.startswith('public class ') and ':' in line:
                found_class = True
                class_part = line.split('{')[0].strip()
                
                # 检查是否继承了正确的基类
                valid_inheritance = False
                for base_class in self.plugin_bases.keys():
                    if base_class in class_part:
                        valid_inheritance = True
                        break
                
                if not valid_inheritance and 'PlugIn' in file_path:
                    errors.append(f"第{i}行：插件类未继承正确的基类 '{class_part}'")
                    errors.append(f"  可选基类：{', '.join(self.plugin_bases.keys())}")
        
        return errors
    
    def check_attributes(self, lines: List[str], file_path: str) -> List[str]:
        """检查特性标记规范"""
        errors = []
        found_class = False
        attribute_lines = []
        
        for i, line in enumerate(lines, 1):
            line = line.strip()
            
            # 查找类定义
            if line.startswith('public class '):
                found_class = True
                continue
            
            # 在类定义前查找特性
            if not found_class and line.startswith('[') and 'Description' in line:
                attribute_lines.append((i, line))
            if not found_class and line.startswith('[') and 'HotUpdate' in line:
                attribute_lines.append((i, line))
        
        # 检查是否缺少必要的特性
        found_description = any('Description' in line for _, line in attribute_lines)
        found_hotupdate = any('HotUpdate' in line for _, line in attribute_lines)
        
        if not found_description and 'PlugIn' in file_path:
            errors.append("缺少[Description]特性标记")
        
        if not found_hotupdate and 'PlugIn' in file_path:
            errors.append("缺少[HotUpdate]特性标记")
        
        return errors
    
    def check_logging(self, content: str, file_path: str) -> List[str]:
        """检查日志记录规范"""
        errors = []
        
        # 检查是否使用了正确的日志记录方式
        uses_custom_log = any(re.search(pattern, content) for pattern in self.log_patterns)
        uses_console_log = 'Console.WriteLine' in content or 'Console.Write(' in content
        
        if uses_console_log and not uses_custom_log:
            errors.append("使用了Console.WriteLine进行日志记录，应使用CustomLog")
        
        # 检查是否定义了日志实例
        if 'PlugIn' in file_path and not uses_custom_log:
            if 'private static readonly CustomLog.LogWriter' not in content:
                errors.append("插件类应定义私有静态日志实例：private static readonly CustomLog.LogWriter _log = CustomLog.For(\"模块名称\");")
        
        return errors
    
    def check_messaging(self, content: str, file_path: str) -> List[str]:
        """检查消息通知规范"""
        errors = []
        
        # 检查是否使用了正确的消息发送方式
        uses_sendmsg = any(re.search(pattern, content) for pattern in self.msg_patterns)
        uses_other_msg = 'MessageBox' in content or 'ShowMessage' in content
        
        if uses_other_msg and not uses_sendmsg:
            errors.append("使用了其他消息发送方式，应使用SendMsg.Send")
        
        return errors
    
    def check_directory(self, dir_path: str) -> Dict[str, List[str]]:
        """检查目录下的所有C#文件"""
        results = {}
        
        for root, dirs, files in os.walk(dir_path):
            for file in files:
                if file.endswith('.cs'):
                    file_path = os.path.join(root, file)
                    errors = self.check_file(file_path)
                    if errors:
                        results[file_path] = errors
        
        return results
    
    def print_results(self, results: Dict[str, List[str]]):
        """打印检查结果"""
        if not results:
            print("✓ 所有文件都符合编码规范！")
            return
        
        print(f"✗ 发现 {len(results)} 个文件存在编码规范问题：\n")
        
        for file_path, errors in results.items():
            print(f"文件：{file_path}")
            for error in errors:
                print(f"  - {error}")
            print()
        
        print(f"共检查 {len(results)} 个文件，发现 {sum(len(errors) for errors in results.values())} 个问题。")

def main():
    """主函数"""
    parser = argparse.ArgumentParser(description='金蝶ERP项目编码规范检查工具')
    parser.add_argument('--path', '-p', required=True, help='要检查的文件或目录路径')
    parser.add_argument('--output', '-o', help='输出结果的文件路径')
    
    args = parser.parse_args()
    
    checker = CodingStandardChecker()
    
    if os.path.isfile(args.path):
        # 检查单个文件
        errors = checker.check_file(args.path)
        if errors:
            results = {args.path: errors}
        else:
            results = {}
    elif os.path.isdir(args.path):
        # 检查目录
        results = checker.check_directory(args.path)
    else:
        print(f"错误：路径不存在 - {args.path}")
        sys.exit(1)
    
    # 打印结果
    checker.print_results(results)
    
    # 输出到文件
    if args.output:
        with open(args.output, 'w', encoding='utf-8') as f:
            if not results:
                f.write("✓ 所有文件都符合编码规范！\n")
            else:
                f.write(f"✗ 发现 {len(results)} 个文件存在编码规范问题：\n\n")
                for file_path, errors in results.items():
                    f.write(f"文件：{file_path}\n")
                    for error in errors:
                        f.write(f"  - {error}\n")
                    f.write("\n")
                f.write(f"共检查 {len(results)} 个文件，发现 {sum(len(errors) for errors in results.values())} 个问题。\n")
    
    # 返回状态码
    sys.exit(0 if not results else 1)

if __name__ == '__main__':
    main()
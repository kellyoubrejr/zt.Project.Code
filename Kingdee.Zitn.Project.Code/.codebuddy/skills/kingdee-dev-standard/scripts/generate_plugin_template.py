#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
金蝶ERP插件模板生成工具

根据指定的插件类型和参数生成标准化的插件代码模板。
"""

import os
import argparse
from datetime import datetime
from typing import Dict, List

class PluginTemplateGenerator:
    """插件模板生成器"""
    
    def __init__(self):
        # 插件类型配置
        self.plugin_types = {
            'bill': {
                'name': '表单插件',
                'base_class': 'AbstractBillPlugIn',
                'description': '表单插件',
                'events': [
                    'AfterCreateNewData',
                    'AfterLoadData',
                    'BeforeSave',
                    'AfterSave',
                    'BeforeSubmit',
                    'AfterSubmit',
                    'BeforeAudit',
                    'AfterAudit'
                ]
            },
            'dynamic': {
                'name': '动态表单插件',
                'base_class': 'AbstractDynamicFormPlugIn',
                'description': '动态表单插件',
                'events': [
                    'OnInitialize',
                    'ButtonClick',
                    'TextChanged',
                    'SelectedIndexChanged'
                ]
            },
            'operation': {
                'name': '操作服务插件',
                'base_class': 'AbstractOperationServicePlugIn',
                'description': '操作服务插件',
                'events': [
                    'BeforeExecuteOperationTransaction',
                    'AfterExecuteOperationTransaction'
                ]
            },
            'list': {
                'name': '列表插件',
                'base_class': 'AbstractListPlugIn',
                'description': '列表插件',
                'events': [
                    'OnInitialize',
                    'Query',
                    'ButtonClick'
                ]
            }
        }
    
    def generate_bill_plugin(self, class_name: str, module_name: str, description: str = "") -> str:
        """生成表单插件模板"""
        if not description:
            description = f"{class_name} 表单插件"
        
        template = f'''using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.Zitn.Project.Code.Util;
using System;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.plugin.{module_name}
{{
    [Description("{description}")]
    [HotUpdate]
    public class {class_name} : AbstractBillPlugIn
    {{
        private static readonly CustomLog.LogWriter _log = CustomLog.For("{class_name}");
        
        public override void AfterCreateNewData(EventObject e)
        {{
            base.AfterCreateNewData(e);
            _log.WriteLog($"新增数据后处理");
        }}
        
        public override void AfterLoadData(EventObject e)
        {{
            base.AfterLoadData(e);
            _log.WriteLog($"加载数据后处理");
        }}
        
        public override void BeforeSave(BeforeSaveEventArgs e)
        {{
            base.BeforeSave(e);
            _log.WriteLog($"保存前处理");
            
            // 数据验证
            if (!ValidateData())
            {{
                e.Cancel = true;
                this.View.ShowError("数据验证失败，请检查");
            }}
        }}
        
        public override void AfterSave(AfterSaveEventArgs e)
        {{
            base.AfterSave(e);
            _log.WriteLog($"保存后处理");
        }}
        
        public override void BeforeSubmit(BeforeSubmitEventArgs e)
        {{
            base.BeforeSubmit(e);
            _log.WriteLog($"提交前处理");
        }}
        
        public override void AfterSubmit(AfterEventArgs e)
        {{
            base.AfterSubmit(e);
            _log.WriteLog($"提交后处理");
            
            // 发送消息通知
            try
            {{
                string billNo = this.Model.GetValue("FBILLNO")?.ToString();
                SendMsg.Send($"【{module_name}】单据提交成功：{{billNo}}");
            }}
            catch (Exception ex)
            {{
                _log.Error($"发送消息失败：{{ex.Message}}", ex);
            }}
        }}
        
        public override void BeforeAudit(BeforeAuditEventArgs e)
        {{
            base.BeforeAudit(e);
            _log.WriteLog($"审核前处理");
        }}
        
        public override void AfterAudit(AfterAuditEventArgs e)
        {{
            base.AfterAudit(e);
            _log.WriteLog($"审核后处理");
            
            // 发送消息通知
            try
            {{
                string billNo = this.Model.GetValue("FBILLNO")?.ToString();
                SendMsg.Send($"【{module_name}】单据审核成功：{{billNo}}");
            }}
            catch (Exception ex)
            {{
                _log.Error($"发送消息失败：{{ex.Message}}", ex);
            }}
        }}
        
        /// <summary>
        /// 数据验证
        /// </summary>
        /// <returns>验证结果</returns>
        private bool ValidateData()
        {{
            try
            {{
                // TODO: 实现数据验证逻辑
                return true;
            }}
            catch (Exception ex)
            {{
                _log.Error($"数据验证异常：{{ex.Message}}", ex);
                return false;
            }}
        }}
    }}
}}'''
        return template
    
    def generate_dynamic_plugin(self, class_name: str, module_name: str, description: str = "") -> str:
        """生成动态表单插件模板"""
        if not description:
            description = f"{class_name} 动态表单插件"
        
        template = f'''using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.Zitn.Project.Code.Util;
using System;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.plugin.{module_name}
{{
    [Description("{description}")]
    [HotUpdate]
    public class {class_name} : AbstractDynamicFormPlugIn
    {{
        private static readonly CustomLog.LogWriter _log = CustomLog.For("{class_name}");
        
        public override void OnInitialize(OnInitializeEventArgs e)
        {{
            base.OnInitialize(e);
            _log.WriteLog($"动态表单初始化");
            
            // 初始化界面元素
            InitializeUI();
        }}
        
        public override void ButtonClick(ButtonClickEventArgs e)
        {{
            base.ButtonClick(e);
            _log.WriteLog($"按钮点击：{{e.Key}}");
            
            switch (e.Key)
            {{
                case "btnSubmit":
                    HandleSubmit();
                    break;
                case "btnCancel":
                    HandleCancel();
                    break;
                case "btnExport":
                    HandleExport();
                    break;
                default:
                    _log.WriteLog($"未知按钮：{{e.Key}}");
                    break;
            }}
        }}
        
        public override void TextChanged(TextChangedEventArgs e)
        {{
            base.TextChanged(e);
            _log.WriteLog($"文本变化：{{e.Key}}");
        }}
        
        public override void SelectedIndexChanged(SelectedIndexChangedEventArgs e)
        {{
            base.SelectedIndexChanged(e);
            _log.WriteLog($"下拉选择变化：{{e.Key}}");
        }}
        
        /// <summary>
        /// 初始化界面元素
        /// </summary>
        private void InitializeUI()
        {{
            try
            {{
                // TODO: 初始化界面元素
                _log.WriteLog("界面初始化完成");
            }}
            catch (Exception ex)
            {{
                _log.Error($"界面初始化异常：{{ex.Message}}", ex);
            }}
        }}
        
        /// <summary>
        /// 处理提交操作
        /// </summary>
        private void HandleSubmit()
        {{
            try
            {{
                _log.WriteLog("开始处理提交操作");
                
                // TODO: 实现提交逻辑
                
                _log.WriteLog("提交操作完成");
                this.View.ShowMessage("提交成功");
                
                // 发送消息通知
                SendMsg.Send($"【{module_name}】操作提交成功");
            }}
            catch (Exception ex)
            {{
                _log.Error($"提交处理异常：{{ex.Message}}", ex);
                this.View.ShowError($"提交失败：{{ex.Message}}");
                SendMsg.Send($"【{module_name}】操作提交失败：{{ex.Message}}");
            }}
        }}
        
        /// <summary>
        /// 处理取消操作
        /// </summary>
        private void HandleCancel()
        {{
            try
            {{
                _log.WriteLog("开始处理取消操作");
                
                // TODO: 实现取消逻辑
                
                _log.WriteLog("取消操作完成");
                this.View.Close();
            }}
            catch (Exception ex)
            {{
                _log.Error($"取消处理异常：{{ex.Message}}", ex);
                this.View.ShowError($"取消失败：{{ex.Message}}");
            }}
        }}
        
        /// <summary>
        /// 处理导出操作
        /// </summary>
        private void HandleExport()
        {{
            try
            {{
                _log.WriteLog("开始处理导出操作");
                
                // TODO: 实现导出逻辑
                
                _log.WriteLog("导出操作完成");
                this.View.ShowMessage("导出成功");
            }}
            catch (Exception ex)
            {{
                _log.Error($"导出处理异常：{{ex.Message}}", ex);
                this.View.ShowError($"导出失败：{{ex.Message}}");
            }}
        }}
    }}
}}'''
        return template
    
    def generate_operation_plugin(self, class_name: str, module_name: str, description: str = "") -> str:
        """生成操作服务插件模板"""
        if not description:
            description = f"{class_name} 操作服务插件"
        
        template = f'''using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.Zitn.Project.Code.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Kingdee.Zitn.Project.Code.plugin.{module_name}
{{
    [Description("{description}")]
    [HotUpdate]
    public class {class_name} : AbstractOperationServicePlugIn
    {{
        private static readonly CustomLog.LogWriter _log = CustomLog.For("{class_name}");
        
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {{
            base.BeforeExecuteOperationTransaction(e);
            
            try
            {{
                _log.WriteLog("开始执行操作服务");
                
                // 获取选中的单据ID
                var ids = string.Join(",", e.SelectedRows
                    .Select(row => row.DataEntity["Id"]?.ToString()));
                
                _log.WriteLog($"选中的单据ID：{{ids}}");
                
                // 执行业务逻辑
                ProcessOperation(ids, e.SelectedRows);
                
                _log.WriteLog("操作服务执行完成");
            }}
            catch (KDBusinessException ex)
            {{
                _log.Error($"业务异常：{{ex.Message}}");
                throw;
            }}
            catch (Exception ex)
            {{
                _log.Error($"系统异常：{{ex.Message}}", ex);
                throw new KDBusinessException("SYSTEM_ERROR", $"系统异常：{{ex.Message}}");
            }}
        }}
        
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {{
            base.AfterExecuteOperationTransaction(e);
            _log.WriteLog("操作事务执行完成");
        }}
        
        /// <summary>
        /// 处理操作逻辑
        /// </summary>
        /// <param name="ids">单据ID列表</param>
        /// <param name="selectedRows">选中的行</param>
        private void ProcessOperation(string ids, SelectedRowCollection selectedRows)
        {{
            try
            {{
                _log.WriteLog($"开始处理单据：{{ids}}");
                
                // TODO: 实现具体的业务逻辑
                
                // 获取单据信息
                var billInfo = GetBillInfo(ids);
                
                // 执行业务处理
                ExecuteBusinessLogic(billInfo);
                
                // 发送消息通知
                SendMsg.Send($"【{module_name}】操作处理完成：{{ids}}");
                
                _log.WriteLog($"单据处理完成：{{ids}}");
            }}
            catch (Exception ex)
            {{
                _log.Error($"处理单据异常：{{ids}}，错误：{{ex.Message}}", ex);
                SendMsg.Send($"【{module_name}】操作处理失败：{{ex.Message}}");
                throw;
            }}
        }}
        
        /// <summary>
        /// 获取单据信息
        /// </summary>
        /// <param name="ids">单据ID列表</param>
        /// <returns>单据信息</returns>
        private DynamicObjectCollection GetBillInfo(string ids)
        {{
            try
            {{
                string sql = $@"/*dialect*/SELECT 
                    FID,
                    FBILLNO,
                    FSTATUS
                FROM T_{module_name.ToUpper()}_ORDER
                WHERE FID IN ({{ids}})";
                
                return DBUtils.ExecuteDynamicObject(this.Context, sql);
            }}
            catch (Exception ex)
            {{
                _log.Error($"获取单据信息异常：{{ex.Message}}", ex);
                throw;
            }}
        }}
        
        /// <summary>
        /// 执行业务逻辑
        /// </summary>
        /// <param name="billInfo">单据信息</param>
        private void ExecuteBusinessLogic(DynamicObjectCollection billInfo)
        {{
            try
            {{
                foreach (var bill in billInfo)
                {{
                    string billId = bill["FID"]?.ToString();
                    string billNo = bill["FBILLNO"]?.ToString();
                    
                    _log.WriteLog($"处理单据：{{billNo}}");
                    
                    // TODO: 实现具体的业务逻辑
                    
                    _log.WriteLog($"单据处理完成：{{billNo}}");
                }}
            }}
            catch (Exception ex)
            {{
                _log.Error($"执行业务逻辑异常：{{ex.Message}}", ex);
                throw;
            }}
        }}
    }}
}}'''
        return template
    
    def generate_list_plugin(self, class_name: str, module_name: str, description: str = "") -> str:
        """生成列表插件模板"""
        if not description:
            description = f"{class_name} 列表插件"
        
        template = f'''using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.Zitn.Project.Code.Util;
using System;
using System.ComponentModel;

namespace Kingdee.Zitn.Project.Code.plugin.{module_name}
{{
    [Description("{description}")]
    [HotUpdate]
    public class {class_name} : AbstractListPlugIn
    {{
        private static readonly CustomLog.LogWriter _log = CustomLog.For("{class_name}");
        
        public override void OnInitialize(OnInitializeEventArgs e)
        {{
            base.OnInitialize(e);
            _log.WriteLog($"列表插件初始化");
            
            // 初始化列表
            InitializeList();
        }}
        
        public override void Query(QueryEventArgs e)
        {{
            base.Query(e);
            _log.WriteLog($"执行查询操作");
            
            // 查询处理
            ProcessQuery(e);
        }}
        
        public override void ButtonClick(ButtonClickEventArgs e)
        {{
            base.ButtonClick(e);
            _log.WriteLog($"按钮点击：{{e.Key}}");
            
            switch (e.Key)
            {{
                case "btnRefresh":
                    HandleRefresh();
                    break;
                case "btnExport":
                    HandleExport();
                    break;
                case "btnImport":
                    HandleImport();
                    break;
                default:
                    _log.WriteLog($"未知按钮：{{e.Key}}");
                    break;
            }}
        }}
        
        /// <summary>
        /// 初始化列表
        /// </summary>
        private void InitializeList()
        {{
            try
            {{
                // TODO: 初始化列表
                _log.WriteLog("列表初始化完成");
            }}
            catch (Exception ex)
            {{
                _log.Error($"列表初始化异常：{{ex.Message}}", ex);
            }}
        }}
        
        /// <summary>
        /// 处理查询
        /// </summary>
        /// <param name="e">查询参数</param>
        private void ProcessQuery(QueryEventArgs e)
        {{
            try
            {{
                // TODO: 处理查询逻辑
                _log.WriteLog("查询处理完成");
            }}
            catch (Exception ex)
            {{
                _log.Error($"查询处理异常：{{ex.Message}}", ex);
            }}
        }}
        
        /// <summary>
        /// 处理刷新操作
        /// </summary>
        private void HandleRefresh()
        {{
            try
            {{
                _log.WriteLog("开始刷新列表");
                
                // TODO: 实现刷新逻辑
                
                _log.WriteLog("列表刷新完成");
                this.View.ShowMessage("刷新成功");
            }}
            catch (Exception ex)
            {{
                _log.Error($"刷新处理异常：{{ex.Message}}", ex);
                this.View.ShowError($"刷新失败：{{ex.Message}}");
            }}
        }}
        
        /// <summary>
        /// 处理导出操作
        /// </summary>
        private void HandleExport()
        {{
            try
            {{
                _log.WriteLog("开始导出数据");
                
                // TODO: 实现导出逻辑
                
                _log.WriteLog("数据导出完成");
                this.View.ShowMessage("导出成功");
            }}
            catch (Exception ex)
            {{
                _log.Error($"导出处理异常：{{ex.Message}}", ex);
                this.View.ShowError($"导出失败：{{ex.Message}}");
            }}
        }}
        
        /// <summary>
        /// 处理导入操作
        /// </summary>
        private void HandleImport()
        {{
            try
            {{
                _log.WriteLog("开始导入数据");
                
                // TODO: 实现导入逻辑
                
                _log.WriteLog("数据导入完成");
                this.View.ShowMessage("导入成功");
            }}
            catch (Exception ex)
            {{
                _log.Error($"导入处理异常：{{ex.Message}}", ex);
                this.View.ShowError($"导入失败：{{ex.Message}}");
            }}
        }}
    }}
}}'''
        return template
    
    def generate_template(self, plugin_type: str, class_name: str, module_name: str, description: str = "") -> str:
        """生成插件模板"""
        if plugin_type == 'bill':
            return self.generate_bill_plugin(class_name, module_name, description)
        elif plugin_type == 'dynamic':
            return self.generate_dynamic_plugin(class_name, module_name, description)
        elif plugin_type == 'operation':
            return self.generate_operation_plugin(class_name, module_name, description)
        elif plugin_type == 'list':
            return self.generate_list_plugin(class_name, module_name, description)
        else:
            raise ValueError(f"不支持的插件类型：{plugin_type}")
    
    def save_template(self, content: str, output_path: str):
        """保存模板到文件"""
        try:
            # 确保目录存在
            os.makedirs(os.path.dirname(output_path), exist_ok=True)
            
            with open(output_path, 'w', encoding='utf-8') as f:
                f.write(content)
            
            print(f"✓ 模板已生成：{output_path}")
        except Exception as e:
            print(f"✗ 保存模板失败：{e}")
            raise

def main():
    """主函数"""
    parser = argparse.ArgumentParser(description='金蝶ERP插件模板生成工具')
    parser.add_argument('--type', '-t', required=True, 
                       choices=['bill', 'dynamic', 'operation', 'list'],
                       help='插件类型：bill(表单插件), dynamic(动态表单插件), operation(操作服务插件), list(列表插件)')
    parser.add_argument('--name', '-n', required=True, help='插件类名')
    parser.add_argument('--module', '-m', required=True, help='模块名称')
    parser.add_argument('--description', '-d', default='', help='插件描述')
    parser.add_argument('--output', '-o', help='输出文件路径')
    
    args = parser.parse_args()
    
    # 生成模板
    generator = PluginTemplateGenerator()
    template = generator.generate_template(args.type, args.name, args.module, args.description)
    
    # 确定输出路径
    if args.output:
        output_path = args.output
    else:
        # 默认输出路径
        output_path = f"plugin/{args.module}/{args.name}.cs"
    
    # 保存模板
    generator.save_template(template, output_path)
    
    # 显示使用说明
    print(f"\n生成信息：")
    print(f"  插件类型：{generator.plugin_types[args.type]['name']}")
    print(f"  类名：{args.name}")
    print(f"  模块：{args.module}")
    print(f"  描述：{args.description or '未指定'}")
    print(f"\n使用说明：")
    print(f"  1. 将生成的代码复制到项目对应目录")
    print(f"  2. 根据实际需求实现TODO部分")
    print(f"  3. 确保引用了必要的命名空间")
    print(f"  4. 运行编码规范检查：python scripts/check_coding_standard.py --path {output_path}")

if __name__ == '__main__':
    main()
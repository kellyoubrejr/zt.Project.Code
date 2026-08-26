# Kingdee.Zitn.Project.Code 开发文档

> 金蝶云星空（K3Cloud）二次开发插件项目，基于 C# / .NET Framework 4.8。

---

## 一、项目概述

本项目是 **青岛智腾微电子有限公司** 的金蝶云星空（K3Cloud）二次开发插件库，编译产物 `Kingdee.Zitn.Project.Code.dll` 部署到 K3Cloud 的 `WebSite\Bin\` 目录下，通过 BOS 设计器将插件类挂载到对应的表单、操作、列表或定时任务上运行。

项目主要覆盖以下几个业务方向：

| 业务方向 | 说明 |
|---------|------|
| 采购业务 | 采购订单、采购申请、收料通知单、供应商等插件 |
| 生产制造 | 生产订单、生产汇报单、计划订单（MRP）、生产用料清单 |
| 库存物流 | 出库、入库、调拨、生产领料/退料/补料 |
| 顺丰物流 | 物流面单下单、路由/运费同步、图片回传 |
| 系统集成 | 对接 BPM、MES、PLM 及其他系统的 WebAPI 接口 |
| 定时任务 | 价值分类跑批、采购周期计算、顺丰同步、调拨批量 |

---

## 二、技术栈与依赖

- **语言/框架**：C#、.NET Framework 4.8
- **运行环境**：金蝶云星空 K3Cloud（`D:\Program Files (x86)\Kingdee\K3Cloud\`）
- **核心依赖**：Kingdee.BOS 系列（Core/App/DataEntity/Orm/WebApi.Client 等）、Kingdee.K3 系列（MFG/SCM）
- **JSON 处理**：Newtonsoft.Json（13.0.1）
- **配置管理**：`System.Configuration`（App.config / Web.config 的 AppSettings）
- **日志**：项目自研 `CustomLog`（写本地文本文件）

---

## 三、目录结构总览

```
Kingdee.Zitn.Project.Code/
├── conf/                    # 全局配置类
├── Interface/               # 对外 WebAPI 接口服务
│   ├── ToBPM/               # 对接 BPM 系统
│   ├── ToMES/               # 对接 MES 系统
│   ├── ToPLM/               # 对接 PLM 系统（预留）
│   └── ToOther/             # 对接其他系统（预留）
├── plugin/                  # 业务插件（按业务域划分）
│   ├── Assem/               # 组装拆卸单
│   ├── BillOrListPlugin/    # 通用单据/列表插件
│   ├── BLPCZD/              # 备料/配料相关
│   ├── DeliveryNotice/      # 发货通知单
│   ├── FeedMtrl/            # 补料单
│   ├── Mo/                  # 生产订单
│   ├── MoRpt/               # 生产汇报单
│   ├── MRP/                 # 计划订单
│   ├── OutStock/            # 出库单
│   ├── PickMtrl/            # 生产领料单
│   ├── PO/                  # 采购订单
│   ├── PoReq/               # 采购申请
│   ├── PrdInstock/          # 生产入库单
│   ├── QC/                  # 质检单
│   ├── Report/              # 报表插件
│   ├── ReturnMtrl/          # 生产退料单
│   ├── SFBill/              # 物流面单
│   ├── SFCallBack/          # 顺丰回调接收
│   ├── SLTZD/               # 收料通知单
│   ├── StkInstock/          # 入库单
│   ├── TransferApply/       # 调拨申请单
│   └── TransferDirect/      # 直接调拨单
├── Schedule/                # 定时任务
├── Util/                    # 通用工具类
├── models/                  # 数据字典/模型
└── ApiDoc-SF/               # 顺丰开放平台接口文档
```

> 另有独立项目 `SFMiddleService/`（在仓库根目录，与主项目平级），是顺丰图片中转服务。

---

## 四、各模块详细说明

### 4.1 conf/ —— 全局配置

| 文件 | 功能 |
|------|------|
| `CustomLog.cs` | 项目统一日志库。提供 `WriteLog` / `Section` / `Error` 及 `For(tag)` 返回 `LogWriter`。日志写入 `D:\kingdeeLog.txt`，格式含时间戳、环境名（DEV/PROD）、标签。 |
| `ErpLogin.cs` | K3Cloud 登录配置。通过 `#if PROD` 编译宏切换 DEV/PROD 两套环境，字段：`K3CloudUrl`、`AppId`、`UserName`、`Password`、`Lcid`，支持从 AppSettings 覆盖。 |
| `SFConfig.cs` | 顺丰开放平台配置。同样支持 DEV/PROD 切换，含 `PartnerID`、`CheckWord`、`ApiUrl`、月结卡号、寄件人信息、图片解密密钥 `PictureSecret`、图片落盘目录、中转服务地址等。 |

### 4.2 Interface/ —— 对外 WebAPI 接口服务

这些类继承 `AbstractWebApiBusinessService`，通过 K3Cloud 的自定义 WebAPI 服务对外暴露，供第三方系统（BPM/MES/PLM 等）调用。

| 文件 | 功能 |
|------|------|
| `ToBPM/BpmService.cs` | 对接 BPM 系统：① `SalesProject` 销售线索同步（按 CRMID 新增/更新并提交审核）；② 采购看板统计类接口（采购需求、供应商付款、费用项目、采购检验、未下单/未到货/未检验/未入库数量），多数走存储过程或 SQL 聚合查询。 |
| `ToMES/MesService.cs` | 对接 MES 系统：① `GetTodoInfo` 获取生产订单信息；② `PushMoToMorpt` 生产订单下推生产汇报单填工时；③ `PushMoToMorptXLH` 下推生产汇报单填序列号；④ `Ppbom`/`PpbomList` 查询生产用料清单；⑤ `SaveBZXBQ` 保存包装箱标签。 |
| `ToPLM/PlmService.cs` | 对接 PLM 系统的服务类（当前为空壳，预留）。 |
| `ToOther/OtherService.cs` | 对接其他系统的服务类（当前为空壳，预留）。 |

### 4.3 plugin/ —— 业务插件

#### Assem/（组装拆卸单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `AssemSubmitJudgeOpe.cs` | 审核服务 | 校验是否存在在途组装拆卸，存在则拦截提交并提示。 |

#### BillOrListPlugin/（通用单据/列表插件）

| 文件 | 类型 | 功能 |
|------|------|------|
| `BillButtonEvent.cs` | 单据按钮 | 「一键去免审/一键免审」按钮：删除/生成免审记录。 |
| `BillControl.cs` | 表单插件 | 免审采购订单打开后隐藏相关控件。 |
| `BillLastRowSetColorEntity.cs` | 表单插件 | 单据体最后一行设置背景色。 |
| `CloseBill.cs` | 单据插件 | 关闭单据。 |
| `EntityAddOrCopyOrDelButton.cs` | 单据插件 | 单据体行增/复制/删除按钮。 |
| `OpenBill.cs` | 表单插件 | 打开单据。 |
| `BatchSelectedEntityPrintList.cs` | 列表插件 | 批量选中实体打印。 |
| `ListOpenFromTest.cs` | 列表插件 | 点击字段打开免审规则 form，设置新增模板并填充数据。 |
| `ListUnAuditListColor.cs` | 列表插件 | 列表颜色 + 排序。 |

#### BLPCZD/

| 文件 | 类型 | 功能 |
|------|------|------|
| `BLPCZDSubmitCheckMtrlNum.cs` | 提交服务 | 提交时校验物料数量。 |

#### DeliveryNotice/（发货通知单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `DeliveryNoticePushOutStock.cs` | 服务 | 发货通知单下推出库单。 |
| `DeliveryNoticePushTransferDirect.cs` | 服务 | 发货通知单下推直接调拨单。 |

#### FeedMtrl/（补料单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `FeedMtrlJiaZhiFenLeiUpsert.cs` | 提交服务 | 生产补料单提交时，按物料采购价格更新价值分类字段。 |
| `TransferApplyAuditFeedMtrl.cs` | 审核服务 | 调拨申请单审核后生成补料单。 |

#### Mo/（生产订单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `MoAutoFeelMtrlFormPpbom.cs` | 服务 | 生产订单自动生成补料单（基于用料清单 PPBOM）。 |
| `MoSelection.cs` | 单据插件 | 生产订单选单逻辑。 |

#### MoRpt/（生产汇报单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `MoRptAuditOpeCheck.cs` | 审核服务 | 生产汇报单提交/审核时的两项校验拦截。 |
| `MoRptSaveUpdateMaterialXLHInfo.cs` | 服务 | 生产汇报单保存时更新物料序列号信息。 |

#### MRP/（计划订单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `MRP_PlnForceCastSaveToPlanOrderForm.cs` | 表单插件 | 意向预测单保存，按多投数量生成计划订单（表单按钮触发）。 |
| `MRP_PlnForceCastSaveToPlanOrderOpe.cs` | 服务 | 意向预测单保存生成计划订单（服务端触发）。 |
| `MRP_SalOrderSaveToPlanOrderForm.cs` | 表单插件 | 销售订单保存，按多投数量分单据类型生成计划订单（表单按钮触发）。 |
| `MRP_SalOrderSaveToPlanOrderOpe.cs` | 服务 | 销售订单保存生成计划订单（服务端触发，回写销售订单分录）。 |
| `MRP_PlanHEBINGList.cs` | 列表插件 | 计划订单合并（列表）。 |
| `MRP_PlanHEBINGList1.cs` | 列表插件 | 计划订单合并（按物料+计划跟踪号+云枢单据号+标记分组，调用标准合并服务）。 |

#### OutStock/（出库单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `OutStockAuditSuanBox.cs` | 审核服务 | 出库审核时算箱。 |
| `OutStockEntryButtonSuanBox.cs` | 分录按钮 | 出库分录按钮算箱。 |
| `OutStockAuditSMVaildate.cs` | 单据插件 | 出库审核校验。 |
| `OutStockJiaZhiFenLeiUpsert.cs` | 提交服务 | 出库申请单提交时，按物料采购价格更新价值分类字段。 |

#### PO/（采购订单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `PoAfterBindData.cs` | 表单插件 | 采购订单打开 AfterBindData 染色。 |
| `PoAuditSetEntryBackColor.cs` | 单据插件 | 点击「确认」按钮，按物料信息设置单据体颜色。 |
| `PoAuditToBPMHT.cs` | 审核服务 | 采购订单审核后调用 BPM 接口传值。 |
| `PoBillBtnTIDAILIAO.cs` | 单据按钮 | 点击「替代料情况」按钮，重新计算并填充数据。 |
| `PoBillBtnXIANGSIMtrl.cs` | 单据按钮 | 点击「相似物料情况」按钮，重新计算并填充数据。 |
| `PoCGZQOperation.cs` | 单据插件 | 采购订单保存/提交后，手工/标准采购周期反写物料并留痕。 |
| `PoCloseTObpmHT.cs` | 服务 | 采购订单关闭，调用 BPM 接口反写。 |
| `PoListBtnOpenEXE.cs` | 列表插件 | 列表按钮打开外部 EXE。 |
| `PoOpenShowFrom.cs` | 单据插件 | 点击字段打开采购需求查看模板并填充数据。 |
| `PoPriceOperation.cs` | 服务 | 价格相关操作。 |
| `PoRejWriteSupplierInfos.cs` | 服务 | 驳回时写供应商信息。 |
| `PoSubmitJudgeFcolorflag.cs` | 服务 | 提交时判断颜色标记。 |
| `PoSupplierFKFSDataChanged.cs` | 表单插件 | DataChanged：采购按供应商带出付款方式到付款计划。 |
| `PoSupplierJudgeCount.cs` | 保存服务 | 采购单保存/提交时检查供应商。 |
| `PoSupplier_FK.cs` | 表单插件 | 采购订单带入供应商付款条件字段。 |
| `btnPoToBPMHT.cs` | 表单插件 | 按钮点击后调用合同 BPM Api。 |

#### PickMtrl/（生产领料单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `PickMtrlJiaZhiFenLeiUpsert.cs` | 提交服务 | 生产领料单提交时，按物料采购价格更新价值分类字段。 |
| `PickMtrlAuditTransferDirect.cs` | 审核服务 | 领料审核后生成调拨相关单据。 |

#### PoReq/（采购申请）

| 文件 | 类型 | 功能 |
|------|------|------|
| `PoReqAfterBindData.cs` | 表单插件 | 采购申请打开 AfterBindData 染色。 |
| `PoReqClickEntityButtonOpenForm.cs` | 单据插件 | 点击单据体「进度」按钮事件。 |
| `PoReqClickMenuButtonOpenForm.cs` | 表单插件 | 流程进度插件：接收数据。 |

#### PrdInstock/（生产入库单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `PrdInstockAuditToBPM.cs` | 审核服务 | 生产入库审核：客返(KF)直接推送 BPM；报检(BJD)校验最后一次入库后推送。 |
| `PrdInstockAuditToBPMKF.cs` | 审核服务 | 生产入库审核调 BPM 客返（**已弃用**）。 |
| `PrdInstockAuditToBPMSZ.cs` | 审核服务 | 生产入库审核调 BPM。 |
| `PrdInstockBtnToBPM.cs` | 表单按钮 | 按钮触发推送 BPM（客返/报检）。 |
| `PrdInstockSubmitUpsertSYB.cs` | 服务 | 提交时更新事业部字段。 |

#### QC/（质检单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `QCAuditUpsertDateTime.cs` | 审核服务 | 质检审核时更新日期字段。 |
| `QC_SaveOrSubmit_UpdateAutoFlag.cs` | 服务 | 质检保存/提交时更新自动标记。 |

#### Report/（报表）

| 文件 | 类型 | 功能 |
|------|------|------|
| `Report_Req.cs` | 报表插件 | 采购申请报表二次开发（新增字段 + 同步筛选条件）。 |

#### ReturnMtrl/（生产退料单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `ReturnMtrlJiaZhiFenLeiUpsert.cs` | 提交服务 | 生产退料单提交时，按物料采购价格更新价值分类字段。 |

#### SFBill/（物流面单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `SFExpressClient.cs` | 静态客户端 | 顺丰开放平台 BSP 接口客户端：MD5 签名 + form 表单 POST，封装下单、路由查询、费用查询、图片注册等接口。 |
| `SFBillAuditOpe.cs` | 审核服务 | 物流面单审核后推送顺丰下单（带单号下单 / 自动分配运单号）。 |
| `SFBillSaveGenOrderId.cs` | 保存服务 | 物流面单保存时自动生成客户订单号(FKHDDH)。 |
| `SFBillQueryEntryBtn.cs` | 单据插件 | 手动查询顺丰路由/费用、手动注册图片（拍照回传/纸质回单）。 |

#### SFCallBack/（顺丰回调接收）

| 文件 | 类型 | 功能 |
|------|------|------|
| `SFCallBack.cs` | IHttpHandler | 顺丰图片推送接收端：接收密文 content，AES/CBC 解密后落盘，回 ack 让顺丰继续增量推送。 |

#### SLTZD/（收料通知单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `ReceiveBillSaveOpe.cs` | 保存服务 | 收料单保存/提交时，设置超收信息审批流。 |
| `RecevieBillEntryBtnGetLot.cs` | 分录按钮 | 收料分录按钮获取批号。 |
| `SLTZDautoGetFLot.cs` | 保存服务 | 收料通知单保存/提交时自动获取批号（调用金蝶批号服务）。 |

#### StkInstock/（入库单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `StkInstockBtnToAssemble.cs` | 单据插件 | 入库单按钮下推组装拆卸单。 |
| `StkInstockPushAssemble.cs` | 服务 | 入库单下推组装拆卸单。 |

#### TransferApply/（调拨申请单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `TransferApplyAuditPickMtrl.cs` | 审核服务 | 调拨申请审核后生成生产领料单。 |
| `TransferApplyAuditTransferDirect.cs` | 审核服务 | 调拨申请审核后生成直接调拨单。 |
| `TransferApplyDataChangedMtrlQty.cs` | 表单插件 | DataChanged：点击物料按订单带出数量。 |
| `TransferApplyEntryBtnUpsertStockField.cs` | 表单插件 | 点击按钮获取「物料+仓库」即时库存可用量更新字段。 |
| `TransferApplySumbitUpsertTransferDirectStockAndLocid.cs` | 服务 | 调拨申请提交时，按物料+仓库更新直接调拨单库存与仓位。 |

#### TransferDirect/（直接调拨单）

| 文件 | 类型 | 功能 |
|------|------|------|
| `TransferDirectAuditSuanBox.cs` | 审核服务 | 直接调拨审核时算箱。 |
| `TransferDitectEntryButtonSuanBox.cs` | 分录按钮 | 直接调拨分录按钮算箱。 |
| `TransferDirectAuditSMVaildate.cs` | 单据插件 | 直接调拨审核校验。 |

### 4.4 Schedule/ —— 定时任务

这些类实现 `IScheduleService`，在 K3Cloud 中注册为计划任务定时执行。

| 文件 | 功能 |
|------|------|
| `JiaZhiFenLei.cs` | 跑批：生产领料、退料、补料、出库单，更新审核中的「价值分类」字段。 |
| `PowlCGZQWriteDBLogTable.cs` | 计算物料最快/最慢采购周期，写入日志表供导出核对。 |
| `PowlCGZQWritePo.cs` | 计算物料最快/最慢采购周期，并更新物料表。 |
| `SFBillSync.cs` | 跑批：物流面单顺丰路由轨迹 + 运费自动同步。 |
| `SFBillPictureSync.cs` | 跑批：物流面单顺丰图片注册（拍照回传/纸质回单）。 |
| `SFBillPicturePull.cs` | 跑批：从顺丰图片中转服务拉取图片并回写面单。 |
| `TransferApplyAndTransferDirectBatch.cs` | 定时任务：调拨申请单、直接调拨单批量脚本。 |

### 4.5 Util/ —— 通用工具

| 文件 | 功能 |
|------|------|
| `Tools.cs` | 通用工具类：① `CreateBillView` 按单据标识创建并加载单据视图（IBillView）；② `CreateOpenParameter` 构建视图加载参数；③ `GetItemArray` 按过滤条件查询单据返回 DynamicObject 数组。 |

### 4.6 models/ —— 数据字典

| 文件 | 功能 |
|------|------|
| `ErpDataDict.cs` | ERP 数据字典（编码/枚举 → 中文）：物料类型、发料方式、货主、单据状态、事业部、生产订单状态，并提供 `Map` 取中文方法。 |

### 4.7 ApiDoc-SF/ —— 顺丰接口文档

顺丰开放平台接口文档（MD），供开发参考，涵盖：下订单、路由查询、清单运费、路由推送、订单状态、订单查询、订单确认取消、拍照回传、图片注册与推送、纸质回单、增值服务产品表、鉴权方式（简易 MD5、数字签名）等。

---

## 五、独立项目：SFMiddleService

位于仓库根目录 `SFMiddleService/`，是一个独立的 .NET 控制台程序（`HttpListener` 自托管 HTTP 服务），部署在公网可达服务器上，作为顺丰图片推送的中转站：

| 文件 | 功能 |
|------|------|
| `Program.cs` | 提供三个路由：① `POST /sf/callback` 顺丰推送密文图片，原样落盘并回 ack；② `GET /sf/pull` K3 拉取未处理列表（需 token）；③ `POST /sf/ack` K3 确认处理完成，归档。服务不持有解密密钥、不解密，只转发密文。 |
| `App.config` | 配置端口、token、inbox/done 目录。 |

---

## 六、关键设计约定

1. **日志规范**：统一使用 `CustomLog.For("标签")` 获取 `LogWriter`，再调用 `WriteLog` / `Error` / `Section`；不要自行 `File.AppendAllText`。
2. **登录规范**：所有需要登录 K3Cloud 的地方统一用 `ErpLogin.*`（勿用已废弃的 `ApiConfig.*`），通过 `#if PROD` 切换环境。
3. **环境切换**：编译 Release（定义 `PROD`）切生产环境，Debug 默认开发环境。
4. **SQL 注入防护**：拼接 SQL 时对字符串字段做单引号转义（`Replace("'", "''")`）。
5. **热更新**：插件类标注 `[HotUpdate]`，便于运行时热加载。

---

## 七、编译与部署

- 编译工具：`F:\vs2022\MSBuild\Current\Bin\MSBuild.exe`
- 解决方案：`Kingdee.Zitn.Project.Code.sln`
- 输出目录：`D:\Program Files (x86)\Kingdee\K3Cloud\WebSite\Bin\Kingdee.Zitn.Project.Code.dll`
- 编译命令示例：

```bash
"F:/vs2022/MSBuild/Current/Bin/MSBuild.exe" \
  "Kingdee.Zitn.Project.Code/Kingdee.Zitn.Project.Code.csproj" \
  -p:Configuration=Debug
```

> 新增 `.cs` 文件后，需在 `Kingdee.Zitn.Project.Code.csproj` 中手动添加 `<Compile Include="...">`；新增金蝶 DLL 依赖时需添加对应 `<Reference>`。

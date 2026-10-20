# BPM 接口对接文档（金蝶集成通用模板）

> **版本：** v1.0
> **更新日期：** 2026-10-20
> **适用场景：** 金蝶 K3 Cloud 插件调用 BPM 系统发起审批流程

---

## 一、接口总览

| 序号 | 接口名称 | 方法 | 地址 | 用途 |
|------|----------|------|------|------|
| 1 | 获取认证 Token | POST | `http://10.0.32.10:8769/api/oauth/token` | OAuth2 客户端凭证模式获取 access_token |
| 2 | 发起工作流程 | POST | `http://10.0.32.10:8769/api/openapi/v3/workflow/start` | 启动 BPM 审批流程 |

---

## 二、接口详细说明

### 2.1 获取认证 Token

#### 基本信息

| 项目 | 说明 |
|------|------|
| **请求地址** | `http://10.0.32.10:8769/api/oauth/token` |
| **请求方式** | POST |
| **Content-Type** | `application/x-www-form-urlencoded` |
| **认证方式** | 无需认证（本接口即为获取认证凭证） |

#### 请求参数

| 参数名 | 类型 | 必填 | 固定值 | 说明 |
|--------|------|------|--------|------|
| `grant_type` | string | 是 | `client_credentials` | OAuth2 授权模式，固定为客户端凭证 |
| `client_id` | string | 是 | `xclient` | 客户端 ID |
| `client_secret` | string | 是 | `0a417ecce58c31b32364ce19ca8fcd15` | 客户端密钥 |

#### 请求示例

```
POST /api/oauth/token HTTP/1.1
Host: 10.0.32.10:8769
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=xclient&client_secret=0a417ecce58c31b32364ce19ca8fcd15
```

#### 响应参数

| 参数名 | 类型 | 说明 |
|--------|------|------|
| `access_token` | string | 认证令牌，后续接口调用需携带在 Authorization 请求头中 |
| `token_type` | string | 令牌类型，通常为 `bearer` |
| `expires_in` | int | 令牌有效期（秒） |

#### 响应示例

```json
{
    "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "token_type": "bearer",
    "expires_in": 3600
}
```

#### C# 调用示例

```csharp
var tokenParams = new Dictionary<string, string>
{
    { "grant_type", "client_credentials" },
    { "client_id", "xclient" },
    { "client_secret", "0a417ecce58c31b32364ce19ca8fcd15" }
};

using (var httpClient = new HttpClient())
{
    var tokenResponse = httpClient.PostAsync(
        "http://10.0.32.10:8769/api/oauth/token",
        new FormUrlEncodedContent(tokenParams)
    ).Result;
    var tokenJson = tokenResponse.Content.ReadAsStringAsync().Result;
    var tokenObj = JObject.Parse(tokenJson);
    string accessToken = tokenObj["access_token"].ToString();
}
```

---

### 2.2 发起工作流程

#### 基本信息

| 项目 | 说明 |
|------|------|
| **请求地址** | `http://10.0.32.10:8769/api/openapi/v3/workflow/start` |
| **请求方式** | POST |
| **Content-Type** | `application/json` |
| **认证方式** | Bearer Token（请求头 `Authorization: Bearer {access_token}`） |

#### 请求头

| Header | 值 | 说明 |
|--------|-----|------|
| `Authorization` | `Bearer {access_token}` | 通过 2.1 接口获取的 access_token |
| `Content-Type` | `application/json` | 请求体为 JSON 格式 |

#### 请求体结构

```json
{
    "data": {
        // 表单字段（根据流程定义的业务表单字段）
    },
    "departmentId": "发起部门ID",
    "userId": "发起人用户ID",
    "workflowCode": "流程编码",
    "finishStart": true,
    "nextParticipants": [
        {
            "activityCode": "",
            "participants": []
        }
    ],
    "trustor": ""
}
```

#### 请求参数说明

| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| `data` | object | 是 | 业务表单数据，字段根据具体流程定义而定（详见下方表单字段表） |
| `departmentId` | string | 是 | 发起部门 ID（BPM 系统中的部门 ID） |
| `userId` | string | 是 | 发起人用户 ID（BPM 系统中的用户 ID） |
| `workflowCode` | string | 是 | 流程编码，如 `trial_production_report_flow` |
| `finishStart` | boolean | 是 | 是否直接发起（`true`=直接发起，不经过草稿状态） |
| `nextParticipants` | array | 否 | 下一节点参与人配置，通常传空数组即可 |
| `trustor` | string | 否 | 委托人 ID，无委托时传空字符串 |

#### 响应参数

| 参数名 | 类型 | 说明 |
|--------|------|------|
| `errcode` | int | 错误码，`0` 表示成功 |
| `errmsg` | string | 错误信息，成功时为 `"流程启动成功"` |

#### 成功响应示例

```json
{
    "errcode": 0,
    "errmsg": "流程启动成功"
}
```

#### 失败响应示例

```json
{
    "errcode": 1001,
    "errmsg": "流程编码不存在"
}
```

#### 成功判断逻辑

```
errcode == 0 && errmsg == "流程启动成功"
```

---

## 三、试制报告流程表单字段（trial_production_report_flow）

### 3.1 data 表单字段

| BPM 字段名 | 中文名 | 类型 | 数据来源（金蝶字段） | 取值说明 |
|-------------|--------|------|----------------------|----------|
| `creater` | 入库登记人（责任人） | string | 生产车间 → 映射用户 ID | 见「车间-责任人 ID 映射表」 |
| `org` | 入库组织 | string | `T_PRD_INSTOCK.FSTOCKORGID` | 见「入库组织映射表」 |
| `shiyebu` | 产品事业部 | string | `T_PRD_INSTOCKENTRY.FCPSYB` → 映射组织 ID | 见「事业部-组织 ID 映射表」 |
| `productCode` | 物料编码 | string | `T_BD_MATERIAL.FNUMBER` | 物料编码 |
| `productName` | 物料名称 | string | `T_BD_MATERIAL_L.FNAME` | 物料名称 |
| `model` | 物料规格 | string | `T_BD_MATERIAL_L.FSPECIFICATION` | 物料规格型号 |
| `developmentPhase` | 研发阶段 | string | `T_PRD_INSTOCK.FSCGCPCH` 第 2 位字符 | 取生产产品批号的第 2 个字符（如 `BD001` → `D`） |
| `makeQty` | 生产订单数量 | string | `T_PRD_MOENTRY.FQTY` | 生产订单上的计划数量 |
| `inStoreQty` | 累计入库数量 | string | `T_PRD_MOENTRY_A.FSTOCKINQUASELAUXQTY` | 累计入库辅助数量 |
| `hgRkQty` | 合格入库数量 | string | `T_PRD_MOENTRY_A.FSTOCKINQUAAUXQTY` | 合格入库辅助数量 |
| `productDate` | 开工日期 | string | `T_PRD_MOENTRY_A.FSTARTDATE` | 生产订单开工日期 |
| `rkDate` | 入库日期 | string | `T_PRD_INSTOCK.FDATE` | 入库单日期 |
| `workShop` | 生产车间 | string | `T_BD_DEPARTMENT_L.FNAME` | 生产车间名称 |
| `scrkOrderNo` | 入库单号 | string | `T_PRD_INSTOCK.FBILLNO` | 生产入库单单号 |
| `LongText1780472941618` | 生产订单信息 | string | 拼接生成 | 格式见下方说明 |

### 3.2 LongText1780472941618 字段拼接规则

该字段由多行文本组成，每行对应一个关联的生产订单：

**格式：**
```
{生产订单号}:{百分比}%({合格数量}/{累计入库数量})
```

**规则：**

- 按生产订单创建日期倒序排列
- 按订单号前缀（`-` 前的部分）去重，最多取前 3 个不同前缀
- 同一前缀下的订单全部拼接
- 百分比 = 合格数量 / 累计入库数量 × 100，保留 2 位小数
- 数量去掉末尾的 0 和小数点

**示例：**
```
GZ2502-001:85.50%(171/200)
GZ2502-002:92.00%(184/200)
GZ2503-001:78.30%(157/200)
```

---

## 四、映射表

### 4.1 入库组织映射表

| 金蝶 FSTOCKORGID | 组织名称 |
|------------------|----------|
| `1` | 青岛智腾科技有限公司 |
| `101006` | 青岛智腾微电子有限公司 |
| `101007` | 青岛智腾电源有限公司 |
| `101050` | test |
| `1404303` | 青岛智腾烽行能源有限公司 |
| `1516310` | 青岛晶英电子科技有限公司 |
| `3149866` | 青岛智腾微电子有限公司北京分公司 |
| `3241152` | 青岛加速度智能科技有限公司 |
| `4032930` | 青岛智腾微电子有限公司西安分公司 |
| `4665868` | 青岛智导电子有限公司 |
| `4665869` | 青岛深科睿探技术有限公司 |
| `4852744` | 青岛智导电子有限公司北京分公司 |

### 4.2 事业部名称映射（金蝶 FCPSYB → 名称）

| FCPSYB 值 | 事业部名称 |
|-----------|-----------|
| `1` | 一部：民品事业部 |
| `2` | 二部：军品电子事业部 |
| `3` | 三部：传感技术事业部 |
| `4` | 事业四部 |
| `11` | 事业五部 |

> **回退逻辑：** 当入库单分录的 `FCPSYB` 为空时，取物料基础信息上的 `F_PAEZ_SYB` 字段，使用相同的映射规则。

### 4.3 事业部-组织 ID 映射表（BPM shiyebu 字段）

| 事业部名称 | BPM 组织 ID |
|-----------|-------------|
| `事业五部` | `ff8080818fd22d5b018fd271c1c40261` |
| `一部：民品事业部` | `ff8080818fd22d5b018fd271c56c028d` |
| `二部：军品电子事业部` | `ff8080818fd22d5b018fd271c6dd029d` |
| `三部：传感技术事业部` | `ff8080818fd22d5b018fd271c90202b9` |
| `事业四部` | `ff808081948850b401948d5a28531958` |

### 4.4 车间-责任人 ID 映射表（BPM userId / creater 字段）

| 生产车间 | BPM 用户 ID |
|----------|-------------|
| `电装车间` | `2c2c80849f8e4fa7019f91d1e1e65394` |
| `微电子车间` | `ff8080818fd2b96c018fd2bb32eb023e` |
| `传感器车间` | `ff8080818fd2b96c018fd2bbcd6a0627` |
| `加表车间` | `ff8080818fd2b96c018fd2bbcd6a0627` |
| `加速度计车间` | `ff8080818fd2b96c018fd2bbcd6a0627` |
| `机加工车间` | `ff8080818fd2b96c018fd2bb419002bc` |
| `电测车间` | `ff8080818fd2b96c018fd2bb32eb023e` |

### 4.5 车间-部门 ID 映射表（BPM departmentId 字段）

| 生产车间 | BPM 部门 ID |
|----------|-------------|
| `电装车间` | `ff80808194de9bc30194dfc15d712392` |
| `微电子车间` | `ff80808194de9bc30194dfc15fb32398` |
| `传感器车间` | `ff8080818fd22d5b018fd271c9ff02c6` |
| `加表车间` | `ff8080818fd22d5b018fd271c9ff02c6` |
| `加速度计车间` | `ff8080818fd22d5b018fd271c9ff02c6` |
| `机加工车间` | `ff80808194de9bc30194dfc16097239b` |
| `电测车间` | `ff80808194de9bc30194dfc15fb32398` |

---

## 五、业务触发规则

### 5.1 触发时机

金蝶插件 `AfterExecuteOperationTransaction` 事件（生产入库单批量审核操作执行后）。

### 5.2 前置过滤条件

| 序号 | 条件 | 说明 |
|------|------|------|
| 1 | 物料编码以 `90%` 开头 | `M.FNUMBER LIKE '90%'`，仅处理 90 开头的物料 |
| 2 | 累计入库比例 > 80% | `累计入库数量 / 生产订单数量 > 0.8` |
| 3 | 未发起过试制流程 | `T_PRD_MOENTRY.FSFFQSZ != 1` |

### 5.3 去重逻辑

- **分组维度：** 生产订单号 + 物料编码
- **合并规则：** 同一分组下的多张入库单合并处理，取创建时间最新的入库单号
- **防重复：** BPM 调用成功后，更新 `T_PRD_MOENTRY.FSFFQSZ = 1` 标记已发起

---

## 六、完整调用流程

```
┌──────────────────────────────────────────────────────────┐
│ 1. 金蝶入库单批量审核触发                                   │
│    AfterExecuteOperationTransaction                      │
└────────────────────┬─────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────┐
│ 2. 收集选中行入库单 FID，按(生产订单号+物料编码)分组汇总     │
└────────────────────┬─────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────┐
│ 3. 逐行校验                                              │
│    ├─ 物料编码以 90 开头？                                 │
│    ├─ 累计入库 / 订单数量 > 80%？                          │
│    └─ FSFFQSZ != 1（未发起过）？                           │
│    不满足则跳过                                            │
└────────────────────┬─────────────────────────────────────┘
                     │ 满足条件
                     ▼
┌──────────────────────────────────────────────────────────┐
│ 4. 查询入库单完整信息（大 SQL）                             │
│    匹配当前物料编码行，提取 BPM 所需字段                     │
└────────────────────┬─────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────┐
│ 5. 查询关联生产订单信息                                     │
│    按前缀去重取前 3 个，拼接合格数量百分比                     │
└────────────────────┬─────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────┐
│ 6. 获取 BPM Token                                        │
│    POST /api/oauth/token (client_credentials)            │
└────────────────────┬─────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────┐
│ 7. 调用 BPM 发起流程                                      │
│    POST /api/openapi/v3/workflow/start                   │
│    携带 Bearer Token + 表单数据                            │
└────────────────────┬─────────────────────────────────────┘
                     │
            ┌────────┴────────┐
            │                 │
        成功 ▼             失败 ▼
┌──────────────────┐  ┌──────────────────────────────┐
│ 更新 FSFFQSZ = 1 │  │ 记录日志 + 发送告警消息        │
│ 记录成功日志      │  │ 抛出 KDBusinessException      │
└──────────────────┘  └──────────────────────────────┘
```

---

## 七、完整请求 JSON 模板

```json
{
    "data": {
        "creater": "BPM用户ID（车间映射）",
        "org": "入库组织名称",
        "shiyebu": "事业部BPM组织ID",
        "productCode": "物料编码",
        "productName": "物料名称",
        "model": "物料规格",
        "developmentPhase": "研发阶段（SCGCPCH第2位）",
        "makeQty": "生产订单数量",
        "inStoreQty": "累计入库数量",
        "hgRkQty": "合格入库数量",
        "productDate": "开工日期",
        "rkDate": "入库日期",
        "workShop": "生产车间名称",
        "scrkOrderNo": "入库单号",
        "LongText1780472941618": "生产订单拼接信息"
    },
    "departmentId": "车间映射的BPM部门ID",
    "finishStart": true,
    "nextParticipants": [
        {
            "activityCode": "",
            "participants": []
        }
    ],
    "trustor": "",
    "userId": "BPM用户ID（同creater）",
    "workflowCode": "trial_production_report_flow"
}
```

---

## 八、C# 通用调用模板（可直接复用）

```csharp
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

/// <summary>
/// BPM 接口调用通用工具类
/// </summary>
public static class BpmApiClient
{
    private const string BpmBaseUrl = "http://10.0.32.10:8769";
    private const string ClientId = "xclient";
    private const string ClientSecret = "0a417ecce58c31b32364ce19ca8fcd15";

    /// <summary>
    /// 获取 BPM 认证 Token
    /// </summary>
    public static string GetAccessToken()
    {
        var tokenParams = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", ClientId },
            { "client_secret", ClientSecret }
        };

        using (var httpClient = new HttpClient())
        {
            var response = httpClient.PostAsync(
                $"{BpmBaseUrl}/api/oauth/token",
                new FormUrlEncodedContent(tokenParams)
            ).Result;

            var json = response.Content.ReadAsStringAsync().Result;
            var obj = JObject.Parse(json);
            return obj["access_token"]?.ToString();
        }
    }

    /// <summary>
    /// 发起 BPM 工作流程
    /// </summary>
    /// <param name="accessToken">认证令牌</param>
    /// <param name="workflowCode">流程编码</param>
    /// <param name="formData">表单数据（data 节点内容）</param>
    /// <param name="userId">发起人 BPM 用户 ID</param>
    /// <param name="departmentId">发起部门 BPM 部门 ID</param>
    /// <returns>(是否成功, 错误码, 错误信息, 完整响应)</returns>
    public static (bool success, int errcode, string errmsg, string rawResponse) StartWorkflow(
        string accessToken,
        string workflowCode,
        JObject formData,
        string userId,
        string departmentId)
    {
        var body = new JObject
        {
            ["data"] = formData,
            ["departmentId"] = departmentId,
            ["finishStart"] = true,
            ["nextParticipants"] = new JArray
            {
                new JObject
                {
                    ["activityCode"] = "",
                    ["participants"] = new JArray()
                }
            },
            ["trustor"] = "",
            ["userId"] = userId,
            ["workflowCode"] = workflowCode
        };

        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var content = new StringContent(
                body.ToString(), Encoding.UTF8, "application/json");

            var response = httpClient.PostAsync(
                $"{BpmBaseUrl}/api/openapi/v3/workflow/start",
                content
            ).Result;

            var result = response.Content.ReadAsStringAsync().Result;
            var resultObj = JObject.Parse(result);

            int errcode = resultObj["errcode"]?.Value<int>() ?? -1;
            string errmsg = resultObj["errmsg"]?.ToString() ?? "";

            return (errcode == 0 && errmsg == "流程启动成功", errcode, errmsg, result);
        }
    }
}
```

### 使用示例

```csharp
// 1. 获取 Token
string token = BpmApiClient.GetAccessToken();

// 2. 组装表单数据
var formData = new JObject
{
    ["creater"] = "BPM用户ID",
    ["org"] = "青岛智腾科技有限公司",
    ["shiyebu"] = "事业部组织ID",
    ["productCode"] = "9001001",
    ["productName"] = "XX传感器",
    ["model"] = "规格型号",
    ["developmentPhase"] = "D",
    ["makeQty"] = "100",
    ["inStoreQty"] = "85",
    ["hgRkQty"] = "80",
    ["productDate"] = "2026-01-15",
    ["rkDate"] = "2026-10-20",
    ["workShop"] = "传感器车间",
    ["scrkOrderNo"] = "SRK202610001",
    ["LongText1780472941618"] = "GZ2502-001:85.50%(171/200)"
};

// 3. 发起流程
var (success, errcode, errmsg, raw) = BpmApiClient.StartWorkflow(
    accessToken: token,
    workflowCode: "trial_production_report_flow",
    formData: formData,
    userId: "BPM用户ID",
    departmentId: "BPM部门ID"
);

if (success)
{
    // 流程发起成功，更新 FSFFQSZ = 1
}
else
{
    // 记录错误日志，发送告警
}
```

---

## 九、涉及的金蝶数据库表

| 表名 | 别名 | 说明 |
|------|------|------|
| `T_PRD_INSTOCK` | A | 生产入库单 - 主表 |
| `T_PRD_INSTOCKENTRY` | B | 生产入库单 - 明细表 |
| `T_PRD_INSTOCKENTRY_A` | BA | 生产入库单 - 明细扩展表 A |
| `T_BD_MATERIAL` | M | 物料基础表 |
| `T_BD_MATERIAL_L` | M1 | 物料基础表 - 多语言 |
| `T_PRD_MO` | A | 生产订单 - 主表 |
| `T_PRD_MOENTRY` | MO/MOE/B | 生产订单 - 分录表 |
| `T_PRD_MOENTRY_A` | MOA/BA | 生产订单 - 分录扩展表 A |
| `T_PRD_MOENTRY_Q` | BQ | 生产订单 - 分录扩展表 Q |
| `T_SEC_USER` | T | 用户表 |
| `T_BD_DEPARTMENT_L` | DEP | 部门表 - 多语言 |

---

## 十、关键关联关系

```
T_PRD_INSTOCK (入库单主表)
    │ FID
    ├──→ T_PRD_INSTOCKENTRY (入库单明细)
    │       │ FMOENTRYID
    │       ├──→ T_PRD_MOENTRY (生产订单分录)
    │       │       │ FENTRYID
    │       │       ├──→ T_PRD_MOENTRY_A (分录扩展A: 合格数量、累计入库、开工日期、状态)
    │       │       └──→ T_PRD_MOENTRY_Q (分录扩展Q: 非入库数量)
    │       │ FID
    │       └──→ T_PRD_MO (生产订单主表)
    │       │ FMATERIALID
    │       ├──→ T_BD_MATERIAL (物料)
    │       │       └──→ T_BD_MATERIAL_L (物料多语言)
    │       │ FWORKSHOPID
    │       └──→ T_BD_DEPARTMENT_L (生产车间)
    │ FCREATORID
    └──→ T_SEC_USER (创建人)
```

---

## 十一、告警通知

BPM 调用失败时，通过 `SendMsg.Send()` 发送钉钉/企微告警消息，格式如下：

```
🚨【紧急】【生产入库审核】BPM发起流程失败！

操作单据："试制总结"
入库单号：{billno}
时间：{yyyy-MM-dd HH:mm:ss}
当前步骤：{step}
异常信息：{ex.Message}
异常类型：{ex.GetType().Name}
提示：请核查kingdeeLog
```

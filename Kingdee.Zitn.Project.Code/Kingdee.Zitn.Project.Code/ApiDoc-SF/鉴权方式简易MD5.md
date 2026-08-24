# 鉴权方式（简易MD5 + OAuth2）

## 1. 概述

### 1.1 文档目的

- 本文档主要规范顺丰统一接入平台和合作伙伴之间的系统对接过程中，公共部分内容的说明。如基础参数、数据安全及统一错误编码等。
- 每个业务接口详情，请参考具体业务接口的数据定义。

### 1.2 名词说明

| 名称 | 说明 |
| ---- | ---- |
| SF   | 顺丰速运 |
| CP   | Cooperative partner，顺丰合作伙伴 |

---

## 2. 通信约定

### 2.1 CP 请求 SF

请求地址如下（只支持 HTTPS）：

| 环境 | 地址 |
| ---- | ---- |
| 正式环境 | `https://bspgw.sf-express.com/std/service` |
| 沙箱环境 | `https://sfapi-sbox.sf-express.com/std/service` |

CP 请求 SF，参数列表具体如下：

| 参数列表 | 类型 | 是否必传 | 含义 |
| -------- | ---- | -------- | ---- |
| `partnerID` | String(64) | Y | 顾客编码（即合作伙伴编码 CustomerCode） |
| `requestID` | String(40) | Y | 请求唯一号 UUID |
| `serviceCode` | String(50) | Y | 接口服务代码（到 API 接口详情查看具体服务代码） |
| `timestamp` | long | Y | 调用接口时间戳 |
| `msgDigest` | String(128) | 条件 | 数字签名；使用数字签名认证方式必填 |
| `accessToken` | String(100) | 条件 | 访问令牌；使用 OAuth2 认证方式必填 |
| `msgData` | String | Y | 业务数据报文 |

对应响应要求：

| 属性列表 | 类型 | 是否必传 | 含义 |
| -------- | ---- | -------- | ---- |
| `apiResultCode` | String(10) | Y | API 平台结果代码 |
| `apiErrorMsg` | String(200) | N | API 平台异常信息 |
| `apiResponseID` | String(40) | Y | API 响应唯一号 UUID |
| `apiResultData` | String | N | 业务处理详细结果 |

### 2.2 通讯协议与报文格式

1. 通讯双方采用 HTTPS 作为通讯协议，提交方式为 POST，请求头须添加 `Content-type: application/x-www-form-urlencoded`，字符集统一使用 UTF-8。
2. 参数需要通过 HTTP URL 编码传送。
3. 业务数据统一以字符串格式放在 `msgData` 字段中传送。

### 2.3 请求参数示例

#### 2.3.1 curl 请求示例

```bash
curl --request POST \
  --url https://sfapi.sf-express.com/std/service \
  --header 'content-type: application/x-www-form-urlencoded' \
  --data partnerID=XXXX \
  --data requestID=fe4be6fc-065d-4914-bf78-da366639ec80 \
  --data serviceCode=EXP_RECE_CREATE_ORDER \
  --data timestamp=1708235379205 \
  --data 'msgData={"extraInfoList":[],"parcelQty":1,"totalWeight":6,"monthlyCard":"123455678","language":"zh-CN","cargoDetails":[{"volume":1,"amount":0,"unit":"件","count":1,"name":"药品","weight":0.1}],"contactInfoList":[{"address":"...","province":"山西省","city":"太原市","contact":"张峰峰","county":"小店区"},{"address":"...","province":"山西省","city":"阳泉市","contact":"韩贵英","county":"盂县","mobile":"...","company":"","contactType":2}]}' \
  --data 'msgDigest=JzWVEE1cW/cZHfoM9+olLw=='
```

#### 2.3.2 Postman 请求示例

![Postman 工具 MD5 加密方式请求接口示例](https://qiao.sf-express.com/doc/download/picture/MD5%E6%96%B9%E5%BC%8F.png)

### 2.4 请求响应示例

```json
{
    "apiErrorMsg": "",
    "apiResponseID": "00016B0D59BA503FDF3C3333355F863F",
    "apiResultCode": "A1000",
    "apiResultData": "{\"success\":false,\"errorCode\":\"8016\",\"errorMsg\":\"重复下单\",\"msgData\":null}"
}
```

---

## 3. OAuth2 认证

### 3.1 概述

适用开发语言：JAVA、Python、C#、Node.js、PHP。

- `accessToken` 有效期只有 **2 小时**，需用户维护并定期重新获取令牌，避免接口调用失败。
- 2 小时内调用该接口获取的令牌不变，2 小时后调用该接口将返回新的令牌。
- [点击进入 OAuth2 鉴权测试工具](https://open.sf-express.com)

### 3.2 请求地址

| 环境 | 地址 |
| ---- | ---- |
| 正式环境 | `https://bspgw.sf-express.com/oauth2/accessToken` |
| 沙箱环境 | `https://sfapi-sbox.sf-express.com/oauth2/accessToken` |

### 3.3 通讯协议与报文格式

通讯双方采用 HTTP POST 方法作为通讯协议。请求头必须添加 `Content-type: application/x-www-form-urlencoded;charset=UTF-8`，字符集统一使用 UTF-8。

### 3.4 参数说明

#### 4.1 请求参数

| 参数列表 | 类型 | 是否必传 | 含义 |
| -------- | ---- | -------- | ---- |
| `partnerID` | String(64) | Y | 顾客编码（即合作伙伴编码 CustomerCode） |
| `secret` | String(50) | Y | 校验码（即合作伙伴密钥 checkWord） |
| `grantType` | String(50) | Y | 申请类型，填 `password` |

#### 4.2 响应参数

| 参数名 | 类型 | 是否必填 | 描述 |
| ------ | ---- | -------- | ---- |
| `apiResponseID` | String(40) | Y | 响应 ID |
| `apiResultCode` | String(10) | Y | 响应码 |
| `apiErrorMsg` | String(200) | N | 错误描述 |
| `accessToken` | String(100) | Y | 访问令牌 |
| `expiresIn` | Number | Y | accessToken 访问令牌过期时间，单位（秒），默认 7200 秒。申请成功后开始倒计时 7200s，令牌过期后（即 expiresIn=0）需重新获取 |

#### 4.3 OAuth2 认证响应码

| 响应码 | 异常信息 | 描述 |
| ------ | -------- | ---- |
| A1000 | success | 成功 |
| A1011 | `auth_error:${REASON}` | 认证失败：${原因} |

### 3.5 报文示例

#### 5.1 curl 请求示例

```bash
curl --request POST \
  --url https://sfapi.sf-express.com/oauth2/accessToken \
  --header 'content-type: application/x-www-form-urlencoded' \
  --data partnerID=yourclientcode \
  --data secret=yourcheckword \
  --data grantType=password
```

#### 5.2 响应报文

**成功报文**

```json
{
    "apiResultCode": "A1000",
    "apiErrorMsg": "success",
    "apiResponseID": "000180E0AC18933F963C52701B18C03F",
    "accessToken": "20D02FC4F63B4A4AA7C9A236EAD5B0A1",
    "expiresIn": 5150
}
```

**失败报文**

```json
{
    "apiResultCode": "A1011",
    "apiErrorMsg": "auth_error:partnerID:test010d_is_not_exist",
    "apiResponseID": "8VYUxU2eL9ZgBXtGXrj",
    "accessToken": null,
    "expiresIn": null
}
```

#### 5.3 Postman 请求示例

- 获取 token
- Token 方式请求接口

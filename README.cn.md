[English Version](./README.md) | 中文版

# LabAcacia.GrpcIngress

> ## ⚠️ 已废弃 —— 并入 Bridge 包（NPS-CR-0010）
>
> `v1.0.0-alpha.17` 是**最后一个 deprecated 版本**；alpha.18 起退出同步发布列车。
>
> [NPS-CR-0010](https://github.com/labacacia/NPS-Release/blob/main/spec/cr/NPS-CR-0010-bridge-bidirectional.md)
> 已将 **Bridge Node 定案为双向**。NWP 的节点分类表本来就是这么定义它的（`NPS ↔ 非-NPS`）；
> 那条"仅出向"的收窄——也正是本包当初必须**独立存在**的原因——唯一的存在理由是让 `Bridge` 这个名字
> 不与 `compat/*-ingress` 撞车。限制解除后，入向适配器就该回到 Bridge 包里：在那里它与出向共用
> 同一套翻译核、同一张错误映射表（NWP §16.3）、同一份合规 profile，而不是像现在这样两份手工维护
> 的副本各自漂移。
>
> **请迁移到 [`LabAcacia.NPS.NWP.Bridge`](https://www.nuget.org/packages/LabAcacia.NPS.NWP.Bridge)。**
> 替代品是本包的超集：它照样能通过 HTTP 顶在远程 NWP 节点前面（`NwpUpstream` → `BridgeInboundOptions.Upstreams`），
> 并且额外支持在进程内代理同宿主节点 —— 两者走同一个 `INwpBackend` 抽象。
>
> 另外注意：`nps-ingress`（第二层公网边缘守护进程）**保留原名**。它和本包毫无关系，从来就不是一回事 ——
> CR-0010 消解的正是这个词的撞车。



[![NuGet](https://img.shields.io/nuget/v/LabAcacia.GrpcIngress.svg)](https://www.nuget.org/packages/LabAcacia.GrpcIngress)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](../../LICENSE)
[![Release](https://img.shields.io/badge/release-v1.0.0--alpha.17-orange.svg)](CHANGELOG.cn.md)
[![NCP](https://img.shields.io/badge/NCP-v0.11-5b8cff.svg)]()
[![NWP](https://img.shields.io/badge/NWP-v0.20-4af0b0.svg)]()
[![NIP](https://img.shields.io/badge/NIP-v0.13-7b61ff.svg)]()
[![NDP](https://img.shields.io/badge/NDP-v0.12-f0a050.svg)]()
[![NOP](https://img.shields.io/badge/NOP-v0.9-ff8c42.svg)]()

一个 **ASP.NET Core 库**，把一个或多个 **NPS NWP 节点** 暴露成一个 **gRPC
服务**。任何有 protoc 插件的语言写的 gRPC / protobuf 客户端都能读 NWP
Memory Node、调用 NWP Action / Complex Node、列出可用 action，
而无需了解 NPS 原生 wire 格式。

- **协议**：gRPC over HTTP/2，服务包 `labacacia.grpc_ingress.v1`。
- **目标**：.NET 10，ASP.NET Core。
- **NWP 规范**：`spec/NPS-2-NWP.md` v0.14。

---

## 为什么是通用的 bytes 透传？

NWP 的 schema 在 **运行时** 通过 `AnchorFrame` + `/.schema` 声明。
传统的强类型 `.proto` 会把每个 NWP action 塞进一个在代码生成阶段根本
不存在的 schema 里——两头都不讨好。

本 bridge 走相反的路：只定义 **4 个小巧的通用 RPC**（`GetManifest`、
`Invoke`、`Query`、`ListActions`），payload 是 JSON 编码的 NWP 帧体，
以 `bytes` 透传。想要编译期强类型的调用方，可以从具体节点的
`AnchorFrame` 派生出自己的 `.proto`，叠在本服务之上。

| gRPC RPC      | NWP 调用                      | 说明                                                                  |
| ------------- | ----------------------------- | --------------------------------------------------------------------- |
| `GetManifest` | `GET /.nwm`                   | 返回原始 JSON + 便利字段 `node_type`。                                 |
| `Invoke`      | `POST /invoke`                | 把 `action_id` + 调用方的 `params_json` 包成 ActionFrame。            |
| `Query`       | `POST /query`                 | 原样透传 query JSON。                                                  |
| `ListActions` | `GET /actions`                | 返回 `/actions` 原始 body。                                            |

---

## 安装

```bash
dotnet add package LabAcacia.GrpcIngress
```

---

## 快速开始

```csharp
using LabAcacia.NPS.GrpcIngress;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpcIngress(o =>
{
    o.Upstreams = new[]
    {
        new NwpUpstream
        {
            Name    = "orders",
            BaseUrl = new Uri("https://api.example.com/orders"),
        },
        new NwpUpstream
        {
            Name    = "products",
            BaseUrl = new Uri("https://api.example.com/products"),
        },
    };
});

var app = builder.Build();
app.MapGrpcIngress();
app.Run();
```

客户端（C# 示例；其他语言编译同一个 `.proto` 即可）：

```csharp
using Grpc.Net.Client;
using LabAcacia.GrpcIngress.Generated;

using var channel = GrpcChannel.ForAddress("https://localhost:5001");
var client = new NwpIngress.NwpIngressClient(channel);

var resp = await client.InvokeAsync(new InvokeRequest
{
    Ctx        = new UpstreamContext { Upstream = "orders", AgentNid = "nid:ed25519:..." },
    ActionId   = "orders.create",
    ParamsJson = Google.Protobuf.ByteString.CopyFromUtf8("""{"sku":"ABC-123","qty":1}"""),
});

Console.WriteLine($"http={resp.HttpStatus}, body={resp.BodyJson.ToStringUtf8()}");
```

---

## 错误映射

传输层故障会以 `RpcException` 抛出，映射如下：

| 上游 HTTP     | gRPC 状态             |
| ------------- | --------------------- |
| 400 / 422     | `INVALID_ARGUMENT`    |
| 401 / 403     | `PERMISSION_DENIED`   |
| 404           | `NOT_FOUND`           |
| 408           | `DEADLINE_EXCEEDED`   |
| 409           | `ABORTED`             |
| 429           | `RESOURCE_EXHAUSTED`  |
| 5xx           | `UNAVAILABLE`         |

`Invoke` 和 `Query` **故意不在上游 4xx 时抛异常**——把 `http_status`
作为数据返回，让调用方自己区分业务拒绝（入参错、任务不存在、限速）
和传输层故障（上游挂了），不必靠异常。

`GetManifest` 和 `ListActions` **在非 2xx 时抛异常**：这两个调用属于
发现路径，没有合理的中性表示。

---

## 本次不做

- **Server-streaming / bidi**：首个 alpha 只做 unary。`AlignStream` 异步
  任务输出今天可以通过 `Invoke` 轮询 `system.task.status` 访问。
  Server-streaming 的 `InvokeStream` 计划在 `0.2.0-alpha` 中加入。
- **Reflection / gRPC descriptor**：服务本身小到 `.proto` 生成物足以
  使用；`grpcurl` 用户直接指向 `.proto` 即可。
- **鉴权**：桥从配置转发 `Authorization` / `X-NWP-Agent` header；
  host 级别鉴权（TLS、gRPC interceptor、API gateway）由部署方自行叠加。

---

## 扩展阅读

- [gRPC Ingress 详解](https://github.com/labacacia/NPS-Dev/blob/main/docs/compat/grpc-ingress.md) — bytes 透传原理、双错误映射策略、多语言 client、强类型 proto 叠加、部署注意
- [桥层总览](https://github.com/labacacia/NPS-Dev/blob/main/docs/compat/index.md) — MCP / A2A / gRPC 何时选哪个

---

## 许可证

Apache-2.0。参见 [`LICENSE`](./LICENSE) 和 [`NOTICE`](./NOTICE)。

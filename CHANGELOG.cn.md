[English Version](./CHANGELOG.md) | 中文版

# 更新日志 — gRPC Bridge (`LabAcacia.GrpcBridge`)

格式遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，
版本号遵循 [SemVer](https://semver.org/lang/zh-CN/spec/v2.0.0.html)。

NPS 进入 v1.0 稳定版之前，套件内所有仓库统一对齐到同一个 pre-release
版本号。

---

## [0.1.0-alpha.1] — 2026-04-21

### 新增

- `LabAcacia.GrpcBridge` 首次发布：ASP.NET Core 库，把一个或多个 NWP
  节点（Memory / Action / Complex / Gateway）暴露为 gRPC 服务。
- 通用透传 `.proto`（`nwp_bridge.proto`），payload 用 bytes 承载——
  回避了在 NWP 运行时 `AnchorFrame` 模型之上强加编译期 schema 的
  做法。
- Unary RPC：`GetManifest`、`Invoke`、`Query`、`ListActions`。
- DI 扩展：`AddGrpcBridge(...)` + `MapGrpcBridge()`。
- 从 `UpstreamContext` 把 `agent_nid`、`idempotency_key`、W3C
  `traceparent` 透传到上游 HTTP 调用。
- 把 NWP/HTTP 故障状态码映射到 gRPC status：
  `400/422 → INVALID_ARGUMENT`、`401/403 → PERMISSION_DENIED`、
  `404 → NOT_FOUND`、`429 → RESOURCE_EXHAUSTED`、`5xx → UNAVAILABLE`。
- 15 个单元测试覆盖各 RPC、header 透传、错误映射、构造 guard。

### 动机

回应 2026-04-20 的评审意见：NPS 应该能被现有 gRPC / protobuf 生态直接
消费。本桥采用**通用**形态——调用方把动态 NWP payload 继续以 `bytes`
携带——让两种哲学（编译期 vs 运行时 schema）共存，不强迫任一侧转换。

[0.1.0-alpha.1]: https://github.com/LabAcacia/nps/releases/tag/v0.1.0-alpha.1

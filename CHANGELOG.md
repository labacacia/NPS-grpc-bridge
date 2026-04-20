English | [中文版](./CHANGELOG.cn.md)

# Changelog — gRPC Bridge (`LabAcacia.GrpcBridge`)

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Until NPS reaches v1.0 stable, every repository in the suite is synchronized to
the same pre-release version tag.

---

## [0.1.0-alpha.1] — 2026-04-21

### Added

- Initial release of `LabAcacia.GrpcBridge`, an ASP.NET Core library that
  exposes one or more NWP nodes (Memory / Action / Complex / Gateway) as a
  gRPC service.
- Generic passthrough `.proto` (`nwp_bridge.proto`) with bytes-carrying
  payloads — avoids forcing a compile-time schema on top of NWP's runtime
  `AnchorFrame` model.
- Unary RPCs: `GetManifest`, `Invoke`, `Query`, `ListActions`.
- DI extensions: `AddGrpcBridge(...)` + `MapGrpcBridge()`.
- Forwards `agent_nid`, `idempotency_key`, and W3C `traceparent` from
  `UpstreamContext` to upstream HTTP calls.
- Maps NWP/HTTP failure statuses to gRPC status codes
  (`400/422 → INVALID_ARGUMENT`, `401/403 → PERMISSION_DENIED`,
  `404 → NOT_FOUND`, `429 → RESOURCE_EXHAUSTED`, `5xx → UNAVAILABLE`).
- 15 unit tests covering every RPC, header forwarding, error mapping,
  and construction guards.

### Motivation

Responds to a 2026-04-20 review comment arguing that NPS should be
approachable from the existing gRPC / protobuf ecosystem. The bridge is
**generic** — callers keep their dynamic NWP payloads as `bytes` — so
the two protocol philosophies (compile-time vs. runtime schema) coexist
without forcing a conversion on either side.

[0.1.0-alpha.1]: https://github.com/LabAcacia/nps/releases/tag/v0.1.0-alpha.1

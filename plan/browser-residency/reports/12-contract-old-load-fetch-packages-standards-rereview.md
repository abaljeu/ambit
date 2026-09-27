# 12 — Contract old Load Fetch packages Standards rereview

## 1. Standards

1. **No Standards findings** — The shared `wantAnswerForTargets` function removes the duplicated Load-target Want conversion. The changed F# functions remain below the 40-line limit.
No remaining Standards findings: [ResidentProjection](src/Shared/ResidentProjection.fs) `wantAnswerForTargets` is the single Load-target Want answer; [captureLoadResponse](src/Shared/ResidentProjection.fs) and [Api](src/Server/Api.fs) `loadWantAnswer` call it; scan sizes 17 and 14 lines are under the 40-line limit in [fsharp-source](.agents/rules/fsharp-source.md).

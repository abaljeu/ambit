# Independent code review: 09 — Migrate Server Sync doors

Range: `origin/staging...HEAD`

Sources: [09 — Migrate Server Sync doors](../issues/09-migrate-server-sync-doors.md), [Browser residency architecture](../arch.md), and [Browser residency specification](../spec.md).

## 1. Verdict — Must-fixes

1. **Good — Server door behavior** — Poll is POST-only and requires Want, Poll and post-Event return Changes with `nodes` and `childMap`, `wantAnswer` omits dangling edges and answers empty Want with empty collections, production State uses the saved-Zoom visible closure, and Load sends `nodes` plus `childMap`. No production Browser door changed.
2. **Must-fixes — Three findings** — Standards has two findings and Spec has one scope finding.
3. **Nice-to-haves — None** — No optional cleanup is part of this review.

## 2. Standards — Two must-fixes

### 2.1 Remove the orphaned bootstrap response door

[Core agent behavior](.agents/rules/core-agent-behavior.md) requires a change to remove functions that it makes unused. [Server Api](src/Server/Api.fs) replaces the only `ResidentProjection.bootstrapStateResponse` call with `visibleClosureGraph`, but [ResidentProjection](src/Shared/ResidentProjection.fs) keeps `bootstrapStateResponse` with no remaining F# caller. Remove this orphan or keep an actual caller.

### 2.2 Name the new snapshot answer results

[F# source rules](.agents/rules/fsharp-source.md) say not to invent a one-off tuple and to group related values in a named, reused type. [Server Api](src/Server/Api.fs) adds `wantAnswerFromHandle` with `State * Map<NodeId, ChildNode list> * Node list` and extends `loadPackages` to `State * Result<Node list * Map<NodeId, ChildNode list>, LoadRefuse>`. Give the snapshot and its answer one named result shape that both Server helpers can reuse.

The mechanical scan found no binding over 40 lines, no added F# line over 100 characters, and no changed file over 800 lines. The smell baseline produced no separate judgment-call finding.

## 3. Spec — One must-fix

### 3.1 Leave legacy Load package type contraction to 12 — Contract old Load Fetch packages

[12 — Contract old Load Fetch packages](../issues/12-contract-old-load-fetch-packages.md) owns “31.4 Delete package response — remove `LoadResponse.packages` and `packageChildMap`,” and [08 — Migrate Shared wire](../issues/08-migrate-shared-wire.md) records that those fields remain for the later contract. This range removes both fields from `LoadResponse` and rewires the Shared codec, `loadResponseToSync`, and Shared tests to the replacement field names. That Shared type contraction exceeds the Server-door scope of [09 — Migrate Server Sync doors](../issues/09-migrate-server-sync-doors.md). Keep the current `nodes` plus `childMap` Server wire behavior without taking the type-removal work from 12 — Contract old Load Fetch packages.

The remaining focus checks pass. [Route registration](src/Server/RouteRegistration.fs) has POST `/ambit/poll` and no GET Poll route. [Server Api](src/Server/Api.fs) decodes required Wants and returns Changes with the Want answer. [ResidentProjection](src/Shared/ResidentProjection.fs) omits an edge when a target Node is absent, returns empty collections for empty Want, and builds the reserved-plus-Zoom visible closure. `postLoad` encodes `nodes` plus `childMap`; keeping `packagesForTargets` and `installPackages` for 12 — Contract old Load Fetch packages follows the planned expand-contract boundary.

## 4. Verification — Focused proof passes

1. **Shared residency proof — Good** — `dotnet test tests/Shared.Tests/Gambol.Shared.Tests.fsproj --filter "FullyQualifiedName~WantTests|FullyQualifiedName~SerializationTests|FullyQualifiedName~LoadCaptureTests"` passed 75 of 75 tests.
2. **Server door proof — Good** — Focused `ApiGetStateTests`, `ApiPostLoadTests`, `CoreChangesTests`, `BrowserCredentialTests`, and `ChangeEndpointResilienceTests` passed 32 of 32 tests.
3. **Diff proof — Good** — `git diff --check origin/staging...HEAD` passed.

## 5. Status — Coded

[09 — Migrate Server Sync doors](../issues/09-migrate-server-sync-doors.md) stays `coded`. This report is not approval.

## 6. Summary — Two Standards, one Spec

Standards has two findings; the worst is the introduced one-off snapshot-answer tuple interface. Spec has one finding; the worst is taking the `LoadResponse` package-field contraction owned by ticket 12.

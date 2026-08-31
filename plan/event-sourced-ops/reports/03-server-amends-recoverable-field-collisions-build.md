# Report — Issue 03 server amends recoverable field collisions build

**Date:** 2026-08-22
**Branch:** `w/event-sourced-ops`
**Issue:** [[../issues/03-server-amends-recoverable-field-collisions.md]]

## Summary

Recoverable same-field compare-and-swap collisions on text, name, and classes now amend-and-succeed on the Server instead of HTTP 400 Reject. Text/name losers become an `amb-conflict` first child; classes merge as a set delta against the common prior. Amended POST responses set `externalChanges = true`. Same-parent Replace span CAS and auth/malformed requests still Reject. `Api.postChange` no longer forces `externalChanges = false` over agent acks.

## Files changed

| File | Change |
| --- | --- |
| [[../../../src/Shared/ChangeAmendment.fs]] | New pure amendment/merge module (`applyChange`, text/name amb-conflict child, class set-delta) |
| [[../../../src/Shared/Gambol.Shared.fsproj]] | Register `ChangeAmendment.fs` |
| [[../../../src/Server/FileAgent.fs]] | `applyBatch` uses `ChangeAmendment.applyChange`; propagate `externalChanges` |
| [[../../../src/Server/DbAgent.fs]] | Same as FileAgent |
| [[../../../src/Server/Api.fs]] | Preserve `externalChanges` from agent ack when enriching POST response |
| [[../../../tests/Server.Tests/StateEndpointTests.fs]] | Text/name/class amendment scenarios; helpers for `externalChanges` and `amb-conflict` child |
| [[../../../tests/Shared.Tests/ChangeAmendmentTests.fs]] | Unit coverage for SetText and SetClasses amendment |
| [[../../../tests/Shared.Tests/Gambol.Shared.Tests.fsproj]] | Register `ChangeAmendmentTests.fs` |
| [[../issues/03-server-amends-recoverable-field-collisions.md]] | Acceptance boxes checked; Status → done |

## Red / green evidence

**Red (before implementation):** replaced ``POST attribute collision with stale oldText returns 400`` expectation — would fail with 400 while graph unchanged.

**Green:**

```bash
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~StateEndpointTests"
```

Result: Failed 0, Passed 66, Skipped 0 (File + Db backends).

```bash
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~ChangeAmendmentTests"
```

Result: Failed 0, Passed 2, Skipped 0.

## Commands

```bash
bash ./status.sh
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~ChangeAmendmentTests"
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~StateEndpointTests"
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~StateEndpointTests&FullyQualifiedName~concurrent stale"
```

## Remaining concerns

- Ticket 04 (client rewind/replay) and ticket 05 (same-parent child-list Accept Both) not started.
- Amended Changes break confirmation-echo prefix contract (intentional per spec).
- No commit in this session (per instructions).

## Board mutation (for root)

- **remove** — [[../issues/03-server-amends-recoverable-field-collisions.md]] from Active (verified green on StateEndpointTests, both backends).

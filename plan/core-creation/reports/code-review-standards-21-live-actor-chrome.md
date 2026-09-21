# Standards axis: 21 — Client shows live Actor

Range `2f34407ebc14a3377d6fb7d7f555d062b26aaa6b...HEAD`. Commit `df9e281f` Show live Actor chrome from Poll ActorStart and ActorStop.

## 1. Hard violations

### 1.1 File size already over 400 and increased

Rule: [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) (do not grow a file already over 400; 800 max). [RowView.fs](src/Client/RowView.fs) 415→423; [Update.fs](src/Client/Update.fs) 405→411; [SyncLogic.fs](src/Shared/SyncLogic.fs) 402→413. Tests exempt: [SyncLogicTests.fs](tests/Shared.Tests/SyncLogicTests.fs) 666→707; [ViewModelRowStateTests.fs](tests/Shared.Tests/ViewModelRowStateTests.fs) 791→820.

### 1.2 Function over 40 lines and increased

Rule: [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) (40 lines or less per function). [RowView.fs](src/Client/RowView.fs) `applyRowPatches` 87–134 (48 lines); `SetClassName` grew. `buildRowElement` (from 137) was already long; +3 lines. New measured lets are fine: `applyActorLive` 8 lines; `state` 2 lines.

### 1.3 Argument list lengthened

Rule: [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) (extend a named type; do not lengthen the list). [ViewModel.fs](src/Shared/ViewModel.fs) `BootGraphApplied` adds `liveFocusIds` beside `graph * eventId * history * isReady`. Smell **Parameter Explosion** (judgement): same hunk. Those fields already exist on `ClientSyncState` and `VM`.

## 2. Judgement calls (SMELLS.md)

### 2.1 Duplicated Code

[RowView.fs](src/Client/RowView.fs):

```
if el.classList.contains "actor-live" then
    el.setAttribute("aria-busy", "true")
```

```
row.classList.add "actor-live"
row.setAttribute("aria-busy", "true")
```

[ViewModelDomPlan.fs](src/Shared/ViewModelDomPlan.fs) also adds class `actor-live` (same pattern as selected/focused).

### 2.2 Shotgun Surgery

`liveFocusIds` is copied through VM, ClientSyncState, Program, several [Update.fs](src/Client/Update.fs) arms, UpdateHelpers, RowView, ViewModelDomPlan, and tests.

### 2.3 Speculative Generality

`aria-busy` is extra to class + CSS outline. Tiny HTML in F# is allowed.

## 3. Clean

[.agents/rules/core-api.md](.agents/rules/core-api.md): Client does not mint stored EventId. [.agents/rules/core-agent-behavior.md](.agents/rules/core-agent-behavior.md): plumbing is on-ticket. [.agents/rules/markdown-writing.md](.agents/rules/markdown-writing.md), [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md): project note names [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md).

**Summary:** 3 hard (file size, function size, BootGraphApplied); 3 smell judgements. Worst: already-over-400 production files grew.

# Independent review — live Actor chrome

Reviewer did not write the implementation. Implementer review files in the range were not read. Ticket [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) Status stays `coded`.

**Range:** `origin/staging...HEAD` at `65bdbc404879e8b39ac23eb9ec64690898370b3f`. Command: `git diff origin/staging...HEAD`.

**Spec:** [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) (projection from ActorStart / ActorStop + visible chrome; no Graph lock-present; no Cancel UI).

**Verdict:** Needs work

**Parent extra checks** (not a merge of the axes): public field `actorLiveFocusIds` on `ClientSyncState` and `VM`; helper `applyActorLive` is `private`. No new module or source file name contains a ticket number. Poll, Load, boot novel, and catch-up all apply Events through [SyncLogic.fs](src/Shared/SyncLogic.fs) `foldProjectedEvents` / `applyServerTail` (same fold as Graph Changes). `EventBody.ActorStop(focusId, _)` removes the Focus for ActorSucceeded, ActorFailed, and ActorCancelled.

Axis reports follow. Source files: [independent-standards-live-actor-chrome.md](plan/core-creation/reports/independent-standards-live-actor-chrome.md), [independent-spec-live-actor-chrome.md](plan/core-creation/reports/independent-spec-live-actor-chrome.md).

## Standards

# Independent Standards review — live Actor chrome

Tip `65bdbc404879e8b39ac23eb9ec64690898370b3f`. Range `git diff origin/staging...HEAD`. Source, tests, [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md), and [project.md](plan/core-creation/project.md) only. Report files are out of scope. Scan lines that name a rule path are documented-standard findings. Tests are exempt from the file-size rule in [fsharp-source.md](.agents/rules/fsharp-source.md).

### Documented-standard violations (hard)

#### File size — [fsharp-source.md](.agents/rules/fsharp-source.md)

Rule: 800 lines or less per file; if a file is already longer, split when the change increases it; split in a later standalone commit. Scan:

- [RowView.fs](src/Client/RowView.fs) FILE 415→417
- [Update.fs](src/Client/Update.fs) FILE 405→411
- [SyncLogic.fs](src/Shared/SyncLogic.fs) FILE 402→413

This tip has no split commit. [SyncLogicTests.fs](tests/Shared.Tests/SyncLogicTests.fs) 666→707 and [ViewModelRowStateTests.fs](tests/Shared.Tests/ViewModelRowStateTests.fs) 791→820 are exempt.

#### Group related parameters — [fsharp-source.md](.agents/rules/fsharp-source.md)

Rule: reuse a named type; do not lengthen a one-off tuple. [ViewModel.fs](src/Shared/ViewModel.fs) `BootGraphApplied` adds `actorLiveFocusIds` as a fifth tuple field. [ClientSyncState](src/Shared/SyncLogic.fs) already holds `graph`, `eventId`, `history`, and `actorLiveFocusIds`. [Program.fs](src/Client/Program.fs) and [Update.fs](src/Client/Update.fs) pass the extra slot.

#### Labeled links — [markdown-writing.md](.agents/rules/markdown-writing.md) and [refer-by-name.md](.agents/rules/refer-by-name.md)

Rule: labeled links are `[label](path)` with a project-root path; the label includes number and name. New [project.md](plan/core-creation/project.md) note uses `issues/21-client-shows-lock-present.md` and `issues/22-client-cancels-a-job.md`, not `plan/core-creation/issues/...`. Touched checklist on [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) keeps `[[22-client-cancels-a-job.md|22]]` (Obsidian labeled wikilink; label is the id only).

### Checked, not a finding

`applyActorLive` is `private` (8 lines). Nested `state` in [UpdateHelpers.fs](src/Client/UpdateHelpers.fs) is 2 lines. Both are under 40 lines. No new module or source file name contains a ticket number. Public records expose `actorLiveFocusIds`; the new helper stays private. [core-api.md](.agents/rules/core-api.md): Client does not mint stored EventId; projection stays off Core `State`.

### Baseline smells (judgement, not hard)

**Shotgun Surgery** — one live-Focus field is copied through Poll, Load, and boot arms.

```
actorLiveFocusIds = newState.actorLiveFocusIds
```

**Duplicated Code** — the `actor-live` class is set in two render paths:

```
row.classList.add "actor-live"
```

```
|> CssClass.addIf (Set.contains entry.nodeId model.actorLiveFocusIds) "actor-live"
```

[core-agent-behavior.md](.agents/rules/core-agent-behavior.md) surgical match-style applies; this is the existing full-row versus patch split.

**Style** — neighbours are `.amb-selected` and `.amb-focused`; new rule is `.amb-row.actor-live` without the `amb-` prefix.

### Counts

Hard: 3 file-size, 1 parameter-group, 1 markdown/refer-by-name. Smells: Shotgun Surgery, Duplicated Code, CSS prefix. Most serious hard issue: [SyncLogic.fs](src/Shared/SyncLogic.fs) grew 402→413 with no split.

## Spec

# Independent spec review — 21 live Actor chrome

Range: `origin/staging...HEAD` at `65bdbc40`. Spec: [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md). Implementer review files in the range are ignored.

### (a) Missing or partial

1. Command response Events — Spec: "Client tracks live Focus ids from EventBody.ActorStart / EventBody.ActorStop applied through normal Poll / response Event apply (same path as Graph Changes)." Poll, Load, boot novel tails, and catch-up use [SyncLogic.fs](src/Shared/SyncLogic.fs) `foldProjectedEvents` (same fold as Change Events). [App.fs](src/Client/App.fs) `runSubmitCommand` logs the command POST and does not apply the Event list that [Api.fs](src/Server/Api.fs) `postCommand` already returns. Chrome waits for a later Poll.
2. `/state` boot — Spec: "A person needs to see that an Actor is live for a Focus." and "After ActorStart for a Focus, that Focus is live." [Update.fs](src/Client/Update.fs) `StateLoaded` sets `actorLiveFocusIds` to empty and does not apply EventLog. After `/state` at the tip, Poll does not resend a prior ActorStart. A live Actor has no chrome until a new ActorStart. Spec non-goals forbid a Graph lock-present field and a live registry Poll, so this path does not meet the person-facing need.

### (b) Scope creep

None in product behavior. Projection is a field on `ClientSyncState` beside Graph apply. Chrome is the `actor-live` row class plus CSS. Tests cover ActorStart / ActorStop and the class patch. No Cancel UI. No new Graph lock-present field. No span lock. No History UI. No extra Poll registry.

### (c) Implemented but wrong

None. `applyActorLive` matches `EventBody.ActorStop(focusId, _)`, so ActorFailed and ActorCancelled also remove the Focus. Chrome does not read `lockPresent`. The indicator is a row inset `box-shadow` (spec: "row / outline"). [ViewModelDomPlan.fs](src/Shared/ViewModelDomPlan.fs) treats a live-set change as a class patch, not a selection-only fast path.

## Summary

Standards: 5 hard findings (3 file-size, 1 parameter-group, 1 markdown/refer-by-name); worst: [SyncLogic.fs](src/Shared/SyncLogic.fs) 402→413 with no split. Spec: 2 missing/partial, 0 creep, 0 wrong; worst: `/state` boot leaves the live set empty so a live Actor has no chrome.

# Independent Standards review — live Actor chrome

Tip `65bdbc404879e8b39ac23eb9ec64690898370b3f`. Range `git diff origin/staging...HEAD`. Source, tests, [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md), and [project.md](plan/core-creation/project.md) only. Report files are out of scope. Scan lines that name a rule path are documented-standard findings. Tests are exempt from the file-size rule in [fsharp-source.md](.agents/rules/fsharp-source.md).

## Documented-standard violations (hard)

### File size — [fsharp-source.md](.agents/rules/fsharp-source.md)

Rule: 800 lines or less per file; if a file is already longer, split when the change increases it; split in a later standalone commit. Scan:

- [RowView.fs](src/Client/RowView.fs) FILE 415→417
- [Update.fs](src/Client/Update.fs) FILE 405→411
- [SyncLogic.fs](src/Shared/SyncLogic.fs) FILE 402→413

This tip has no split commit. [SyncLogicTests.fs](tests/Shared.Tests/SyncLogicTests.fs) 666→707 and [ViewModelRowStateTests.fs](tests/Shared.Tests/ViewModelRowStateTests.fs) 791→820 are exempt.

### Group related parameters — [fsharp-source.md](.agents/rules/fsharp-source.md)

Rule: reuse a named type; do not lengthen a one-off tuple. [ViewModel.fs](src/Shared/ViewModel.fs) `BootGraphApplied` adds `actorLiveFocusIds` as a fifth tuple field. [ClientSyncState](src/Shared/SyncLogic.fs) already holds `graph`, `eventId`, `history`, and `actorLiveFocusIds`. [Program.fs](src/Client/Program.fs) and [Update.fs](src/Client/Update.fs) pass the extra slot.

### Labeled links — [markdown-writing.md](.agents/rules/markdown-writing.md) and [refer-by-name.md](.agents/rules/refer-by-name.md)

Rule: labeled links are `[label](path)` with a project-root path; the label includes number and name. New [project.md](plan/core-creation/project.md) note uses `issues/21-client-shows-lock-present.md` and `issues/22-client-cancels-a-job.md`, not `plan/core-creation/issues/...`. Touched checklist on [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) keeps `[[22-client-cancels-a-job.md|22]]` (Obsidian labeled wikilink; label is the id only).

## Checked, not a finding

`applyActorLive` is `private` (8 lines). Nested `state` in [UpdateHelpers.fs](src/Client/UpdateHelpers.fs) is 2 lines. Both are under 40 lines. No new module or source file name contains a ticket number. Public records expose `actorLiveFocusIds`; the new helper stays private. [core-api.md](.agents/rules/core-api.md): Client does not mint stored EventId; projection stays off Core `State`.

## Baseline smells (judgement, not hard)

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

## Counts

Hard: 3 file-size, 1 parameter-group, 1 markdown/refer-by-name. Smells: Shotgun Surgery, Duplicated Code, CSS prefix. Most serious hard issue: [SyncLogic.fs](src/Shared/SyncLogic.fs) grew 402→413 with no split.

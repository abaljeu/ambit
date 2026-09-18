# Code review — 10 Boot IndexedDB

Independent review. Not approval. Ticket [10 — Boot IndexedDB](plan/single-event-source/issues/10-boot-indexeddb.md) stays **Status:** `coded`.

Range: `origin/staging...origin/cursor/boot-indexeddb-6df3` (three-dot). Tip `ebe9ae7b`. Base `76cb4930`. Non-empty. 9 files. One commit: `ebe9ae7b Boot IndexedDB holds Ev list with EventJson codec`. [History.fs](src/Shared/History.fs) is not in the range (no incidental edit; no [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md) EventId type work).

Mechanical scan (`python3 .agents/skills/code-review-fsharp/scripts/measure-fs-size.py --diff origin/staging`): [eventObj](src/Client/BootCacheStore.fs) 10 lines; [appendChanges](src/Client/BootCacheStore.fs) 12; [bootLog](src/Client/Program.fs) 1; [finishPaint](src/Client/Program.fs) 13; [encodeEvent](src/Shared/BootCache.fs) 2; [decodeEvent](src/Shared/BootCache.fs) 2; [changesAfter](src/Shared/BootCache.fs) 5; [clientRevision](src/Shared/BootCache.fs) 8. None over 40. No added LONG lines.

Alan locks applied: Ev locals `event`/`events`; labeled-link path form waived (flag only `[[path|label]]`); History.fs is ticket 11; no GitHub PR for this report.

## Standards

Range `origin/staging...origin/cursor/boot-indexeddb-6df3` (`ebe9ae7b`). Size scan: no OVER/LONG. [History.fs](src/Shared/History.fs) is not in the diff.

**Hard documented violations:** none.

Locals whose type is now `Ev` are `event`/`events` in [BootCache.fs](src/Shared/BootCache.fs), [BootCacheStore.fs](src/Client/BootCacheStore.fs), [Program.fs](src/Client/Program.fs), and the Shared tests — matches [fsharp-source.md](.agents/rules/fsharp-source.md) (Change→Ev locals). Plan links are `[label](path)` ([markdown-writing.md](.agents/rules/markdown-writing.md)). [project.md](plan/single-event-source/project.md) stays `Stage: build` and names [10 — Boot IndexedDB](plan/single-event-source/issues/10-boot-indexeddb.md) ([project-stage.md](.agents/rules/project-stage.md), [refer-by-name.md](.agents/rules/refer-by-name.md)). Arch/ticket checkboxes record the ticket, not a branch ([planning-docs.md](.agents/rules/planning-docs.md)). No `EventId.next` / `fromJson` in the hunks ([core-api.md](.agents/rules/core-api.md)). Edits stay on the boot-cache type change ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md) surgical).

**Judgement (smells):**

**Mysterious Name** — public APIs still say Change while they take `Ev list`:

```
let changesAfter (snapshotRevision: int) (log: Ev list) : Ev list =
```

```
let appendChanges (file: string) (events: Ev list) : unit =
```

[fsharp-source.md](.agents/rules/fsharp-source.md) only requires renaming *locals*; surgical + the IndexedDB store literal `changeStore = "changes"` also argue leaving these. Not a hard breach.

**Duplicated Code** — same fixture in both test files:

```
let private mkEvent id = Ev.ofChange "fixture" (mkChange id)
```

**Middle Man** (`encodeEvent`/`decodeEvent` → `EventJson`) is overridden: arch BootCache interface is encode/decode Ev via EventJson.

## Spec

Range `origin/staging...origin/cursor/boot-indexeddb-6df3` is non-empty (one commit `ebe9ae7b`, 9 files). [History.fs](src/Shared/History.fs) is not in the diff. CI was not run.

**(a) Missing or partial** — none.

[BootCache](src/Shared/BootCache.fs) and [BootCacheStore](src/Client/BootCacheStore.fs) hold `Ev list` (`changesAfter`, `acceptedForLog`, `foldLog`, `novelEvents`, `decideBootPoll`, `appendChanges`, `decodeCachePayload`, `readSnapshotAndLog`; [Program.fs](src/Client/Program.fs) `bootLog`). Codec is Ev via `EventJson.encode` / `EventJson.decode` (`encodeEvent` / `decodeEvent`); IndexedDB field is `eventJson`, not leftover Change JSON. Leftover `type Change` / `module Change` and `Serialization.encodeChange` remain. No ticket 12 deletes. No ticket 11 EventId-private / History.fs / pending-queue work.

**(b) Scope creep** — none. Ticket/arch/project status, Time, and test/call-site Ev wiring match this ticket. `Ev.ofChange` in tests keeps leftover Change compiling.

**(c) Implemented but wrong** — none. [App.fs](src/Client/App.fs) now passes `confirmed` (`Ev list`) straight into `acceptedForLog`. Store name `changes` / `appendChanges` is leftover naming, not leftover Change JSON.

## Summary

Standards: 0 hard findings, 2 judgement smells (Mysterious Name, Duplicated Code); worst is Mysterious Name — `changesAfter` / `appendChanges` take `Ev list`.

Spec: 0 findings; [History.fs](src/Shared/History.fs) untouched; worst none.

## Alan follow-up

Alan ordered the Mysterious Name renames and the Duplicated Code fixture delete: `changesAfter` → `eventsAfter` (filter is `id > snapshotRevision`, not a Change-only subset); `appendChanges` → `appendEvents`; one shared `mkEvent` in [BootCacheTestHelpers.fs](tests/Shared.Tests/BootCacheTestHelpers.fs).

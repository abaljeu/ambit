# Code review — 08 Core doors

Independent review. Not approval. Ticket [08 — Core doors](plan/single-event-source/issues/08-core-doors.md) stays **Status:** `coded`.

Range: `origin/staging...origin/cursor/08-core-doors-eaa1` (three-dot). Tip `38e4bd01`. Base `49acd736`. Non-empty. 47 files. 7 commits. [History.fs](src/Shared/History.fs) is not in the range (no incidental edit; no [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md) EventId type work).

Mechanical scan (`python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`): [CoreMailbox.fs](src/Server/Core/CoreMailbox.fs) `postGraphOnly` 8 lines; [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) `dispatchPostGraphOnly` 22 lines. Neither over 40. No added LONG lines.

Axis files: [standards-08-core-doors.md](standards-08-core-doors.md), [spec-08-core-doors.md](spec-08-core-doors.md).

## Standards

Range: `git diff origin/staging...HEAD` (`38e4bd01`). 47 files. No [History.fs](src/Shared/History.fs). Mechanical scan: [CoreMailbox.fs](src/Server/Core/CoreMailbox.fs) `postGraphOnly` 8 lines; [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) `dispatchPostGraphOnly` 22 lines. Neither over 40 ([fsharp-source.md](.agents/rules/fsharp-source.md)). No added LONG lines.

### (a) Documented standards

#### Hard

[markdown-writing.md](.agents/rules/markdown-writing.md): labeled links are `[label](path)`; same-directory files use the file name; other local files use a path relative to the project root.

Same-directory tickets linked through `../issues/`:

- [09 — Command mint](plan/single-event-source/issues/09-command-mint.md) See also: `[02 — Files, Query, and Command as Event work](../issues/02-files-query-and-command-as-event-work.md)` — must be `02-files-query-and-command-as-event-work.md`.
- [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md) See also: `[03 — Cleanup seam order](../issues/03-cleanup-seam-order.md)` — must be `03-cleanup-seam-order.md`.

Other-local labeled links use `../`, not a project-root path: `[Single event source architecture](../arch.md)` on [07 — Persist apply](plan/single-event-source/issues/07-persist-apply.md), [08 — Core doors](plan/single-event-source/issues/08-core-doors.md), [09 — Command mint](plan/single-event-source/issues/09-command-mint.md), [10 — Boot IndexedDB](plan/single-event-source/issues/10-boot-indexeddb.md), [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md), [12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md). [arch.md](plan/single-event-source/arch.md) uses `issues/08-core-doors.md` rather than `plan/single-event-source/issues/08-core-doors.md`. Edited files have no `[[path|label]]`.

#### Pass (locked)

- Ev locals are `event` / `events`. Leftover `change` still types Change ([GraphOnlyChangePost](src/Server/GraphOnlyChangePost.fs) chunks).
- No `EventId.next` in the F# range. EventId private-id / EventLog-only next is docs on [core-api.md](.agents/rules/core-api.md), [CONTEXT.md](CONTEXT.md), arch, [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md).
- Status `defined` (not `blocked`) on 09–12 and [04 — Write core-creation arch.md last](plan/single-event-source/issues/04-write-core-creation-arch-md-last.md) with linked Blocked-by ([triage-labels.md](doc/agents/triage-labels.md)).
- Leftover `Ev.ofChange` / `asChange` not scored (ticket 12).
- Surgical: [CoreMailbox.fs](src/Server/Core/CoreMailbox.fs) shrinks; test edits follow the door type ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md)). Files already over 400 lines did not grow ([fsharp-source.md](.agents/rules/fsharp-source.md)).

### (b) Baseline smells (judgement)

Possible Duplicated Code — [TestBackend.fs](tests/Server.Tests/TestBackend.fs) adds `toEvent` / `toEvents` beside `eventFromChange`:

```
let toEvent (change: Change) = Ev.ofChange "" change
```

The same `fun change -> let event = Ev.ofChange "" change; …postGraphOnly event` appears in [LazyLoadReconciliationServer.fs](src/Server/LazyLoadReconciliationServer.fs) and [CoreRuntimeTests.fs](tests/Server.Tests/CoreRuntimeTests.fs).

Possible Mysterious Name — edited tests still say `postChange` (`requireOk "postChange"` in [TestActorHelloTests.fs](tests/Server.Tests/TestActorHelloTests.fs); `getEventsSince returns Ev after postChange` in [Issue42PersistHandlersTests.fs](tests/Server.Tests/Issue42PersistHandlersTests.fs) and [PersistHandlersRestoreTests.fs](tests/Server.Tests/PersistHandlersRestoreTests.fs)).

Shotgun Surgery across many tests: expected for a Core door type change; suppressed.

## Spec

Range: `git diff origin/staging...HEAD` on `cursor/08-core-doors-eaa1` (`38e4bd01`). Spec: [08 — Core doors](plan/single-event-source/issues/08-core-doors.md) What to build. Arch migrate [Core doors](plan/single-event-source/arch.md) is checked. [History.fs](src/Shared/History.fs) is not in the range (no incidental edit; no [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md) EventId type work).

### (a) Missing or partial

None for the Core doors.

Spec: "`postEvents` and `postGraphOnly` both take Ev. `postChange` (Change list) and `PostGraphOnlyChange` of leftover Change are gone." [CoreChanges](src/Server/Core/CoreChanges.fs) and [CoreMailbox](src/Server/Core/CoreMailbox.fs) expose `postEvents: Ev list` and `postGraphOnly: Ev`. Those modules no longer have `postChange` or `postGraphOnlyChange`. The mailbox case is `PostGraphOnly` of Ev.

Remaining `postChange` / `postGraphOnlyChange` names sit on `PersistHandlers` in [CoreMsg.fs](src/Server/Core/CoreMsg.fs), [FileAgent.fs](src/Server/Core/FileAgent.fs), [DbAgent.fs](src/Server/Core/DbAgent.fs), and failed persist in [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs). Those are leftover persist/adapter handles, not the CoreChanges/CoreMailbox doors this ticket deletes. FileAgent.fs and DbAgent.fs are not in the range. Dispatch uses `applyEvent`, not those fields.

Spec: "Leftover Change still compiles at command mint." [GraphOnlyChangePost.fs](src/Server/GraphOnlyChangePost.fs) still mints leftover Change ([09 — Command mint](plan/single-event-source/issues/09-command-mint.md), not an 08 miss). Door call sites convert with `Ev.ofChange`. Do not treat leftover `asChange`/`ofChange` as 08 failures ([12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md)).

### (b) Scope creep

Labeled-link, Status-`defined`, and Ev-local-naming commits do not change 08 What to build. Status `coded` is correct.

Commit `38e4bd01` records EventId private-id / EventLog-only next on [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md), [core-api.md](.agents/rules/core-api.md), [CONTEXT.md](CONTEXT.md), and arch serial text. 11 What-to-build items and arch One serial stay unchecked. That is docs for 11, not History.fs type edits, and not 08 implementing 11 types.

### (c) Looks implemented but wrong

None.

Spec: "Graph-only still skips file persist, not EventLog." `dispatchPostGraphOnly` calls `CoreEventDispatch.postEvent` with `graphOnly = true`. FileAgent `persistPostChange` skips document write when `graphOnly`. `commit` still `appendEvent` and EventLog.append. Test `postGraphOnly updates Graph and EventLog without file write` asserts EventLog and Graph.

## Summary

Standards: 2 hard findings (labeled-link path form), 2 judgement smells (Duplicated Code, Mysterious Name); worst is [markdown-writing.md](.agents/rules/markdown-writing.md) labeled links using `../` instead of same-directory file name or project-root path.

Spec: 0 findings; [History.fs](src/Shared/History.fs) untouched; worst none.

This report is not approval.

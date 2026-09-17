# Standards — 08 Core doors

Range: `git diff origin/staging...HEAD` (`38e4bd01`). 47 files. No [History.fs](src/Shared/History.fs). Mechanical scan: [CoreMailbox.fs](src/Server/Core/CoreMailbox.fs) `postGraphOnly` 8 lines; [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) `dispatchPostGraphOnly` 22 lines. Neither over 40 ([fsharp-source.md](.agents/rules/fsharp-source.md)). No added LONG lines.

## (a) Documented standards

### Hard

[markdown-writing.md](.agents/rules/markdown-writing.md): labeled links are `[label](path)`; same-directory files use the file name; other local files use a path relative to the project root.

Same-directory tickets linked through `../issues/`:

- [09 — Command mint](plan/single-event-source/issues/09-command-mint.md) See also: `[02 — Files, Query, and Command as Event work](../issues/02-files-query-and-command-as-event-work.md)` — must be `02-files-query-and-command-as-event-work.md`.
- [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md) See also: `[03 — Cleanup seam order](../issues/03-cleanup-seam-order.md)` — must be `03-cleanup-seam-order.md`.

Other-local labeled links use `../`, not a project-root path: `[Single event source architecture](../arch.md)` on [07 — Persist apply](plan/single-event-source/issues/07-persist-apply.md), [08 — Core doors](plan/single-event-source/issues/08-core-doors.md), 09, [10 — Boot IndexedDB](plan/single-event-source/issues/10-boot-indexeddb.md), 11, [12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md). [arch.md](plan/single-event-source/arch.md) uses `issues/08-core-doors.md` rather than `plan/single-event-source/issues/08-core-doors.md`. Edited files have no `[[path|label]]`.

### Pass (locked)

- Ev locals are `event` / `events`. Leftover `change` still types Change ([GraphOnlyChangePost](src/Server/GraphOnlyChangePost.fs) chunks).
- No `EventId.next` in the F# range. EventId private-id / EventLog-only next is docs on [core-api.md](.agents/rules/core-api.md), [CONTEXT.md](CONTEXT.md), arch, [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md).
- Status `defined` (not `blocked`) on 09–12 and [04 — Write core-creation arch.md last](plan/single-event-source/issues/04-write-core-creation-arch-md-last.md) with linked Blocked-by ([triage-labels.md](doc/agents/triage-labels.md)).
- Leftover `Ev.ofChange` / `asChange` not scored (ticket 12).
- Surgical: [CoreMailbox.fs](src/Server/Core/CoreMailbox.fs) shrinks; test edits follow the door type ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md)). Files already over 400 lines did not grow ([fsharp-source.md](.agents/rules/fsharp-source.md)).

## (b) Baseline smells (judgement)

Possible Duplicated Code — [TestBackend.fs](tests/Server.Tests/TestBackend.fs) adds `toEvent` / `toEvents` beside `eventFromChange`:

```
let toEvent (change: Change) = Ev.ofChange "" change
```

The same `fun change -> let event = Ev.ofChange "" change; …postGraphOnly event` appears in [LazyLoadReconciliationServer.fs](src/Server/LazyLoadReconciliationServer.fs) and [CoreRuntimeTests.fs](tests/Server.Tests/CoreRuntimeTests.fs).

Possible Mysterious Name — edited tests still say `postChange` (`requireOk "postChange"` in [TestActorHelloTests.fs](tests/Server.Tests/TestActorHelloTests.fs); ``getEventsSince returns Ev after postChange`` in [Issue42PersistHandlersTests.fs](tests/Server.Tests/Issue42PersistHandlersTests.fs) and [PersistHandlersRestoreTests.fs](tests/Server.Tests/PersistHandlersRestoreTests.fs)).

Shotgun Surgery across many tests: expected for a Core door type change; suppressed.

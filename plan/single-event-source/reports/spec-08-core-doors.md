# Spec: 08 — Core doors

Range: `git diff origin/staging...HEAD` on `cursor/08-core-doors-eaa1` (`38e4bd01`). Spec: [08 — Core doors](../issues/08-core-doors.md) What to build. Arch migrate [Core doors](../arch.md) is checked. [src/Shared/History.fs](src/Shared/History.fs) is not in the range (no incidental edit; no [11 — One serial event id](../issues/11-one-serial-event-id.md) EventId type work).

## (a) Missing or partial

None for the Core doors.

Spec: "`postEvents` and `postGraphOnly` both take Ev. `postChange` (Change list) and `PostGraphOnlyChange` of leftover Change are gone." [CoreChanges](src/Server/Core/CoreChanges.fs) and [CoreMailbox](src/Server/Core/CoreMailbox.fs) expose `postEvents: Ev list` and `postGraphOnly: Ev`. Those modules no longer have `postChange` or `postGraphOnlyChange`. The mailbox case is `PostGraphOnly` of Ev.

Remaining `postChange` / `postGraphOnlyChange` names sit on `PersistHandlers` in [CoreMsg.fs](src/Server/Core/CoreMsg.fs), [FileAgent.fs](src/Server/Core/FileAgent.fs), [DbAgent.fs](src/Server/Core/DbAgent.fs), and failed persist in [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs). Those are leftover persist/adapter handles, not the CoreChanges/CoreMailbox doors this ticket deletes. FileAgent.fs and DbAgent.fs are not in the range. Dispatch uses `applyEvent`, not those fields.

Spec: "Leftover Change still compiles at command mint." [GraphOnlyChangePost.fs](src/Server/GraphOnlyChangePost.fs) still mints leftover Change ([09 — Command mint](../issues/09-command-mint.md), not an 08 miss). Door call sites convert with `Ev.ofChange`. Do not treat leftover `asChange`/`ofChange` as 08 failures ([12 — Contract leftover Change and Revision](../issues/12-contract-leftover-change-and-revision.md)).

## (b) Scope creep

Labeled-link, Status-`defined`, and Ev-local-naming commits do not change 08 What to build. Status `coded` is correct.

Commit `38e4bd01` records EventId private-id / EventLog-only next on [11 — One serial event id](../issues/11-one-serial-event-id.md), [core-api.md](.agents/rules/core-api.md), [CONTEXT.md](CONTEXT.md), and arch serial text. 11 What-to-build items and arch One serial stay unchecked. That is docs for 11, not History.fs type edits, and not 08 implementing 11 types.

## (c) Looks implemented but wrong

None.

Spec: "Graph-only still skips file persist, not EventLog." `dispatchPostGraphOnly` calls `CoreEventDispatch.postEvent` with `graphOnly = true`. FileAgent `persistPostChange` skips document write when `graphOnly`. `commit` still `appendEvent` and EventLog.append. Test `postGraphOnly updates Graph and EventLog without file write` asserts EventLog and Graph.

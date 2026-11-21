# Code review — Log Notices on the event log

- Range: `git diff staging...HEAD`
- Spec: [76 — Log Notices on the event log](../issues/76-log-notices-on-the-event-log.md)
- Arch: [core-refinement architecture](../arch.md), [parse-thread architecture](../../parse-thread/arch.md)

## 1. Standards

1. **No findings.**
2. **Scan note** — `logNotice` in [Core event dispatch](../../../src/Server/Core/CoreEventDispatch.fs) is 9 lines. The 40-line function limit and the 800-line file limit in [F# source](../../../.agents/rules/fsharp-source.md) both hold.

## 2. Spec

1. **No findings.**

## 3. Arch

1. **Blocker — Apply logs** — [core-refinement architecture](../arch.md) §1 Special-node Parse and Persist axes, §5 decision 12 Notice, §7 vocabulary item 8 Say Notice, §9 Graph change Apply, §10 Core loop Node messages. `CoreMailboxLoad.applyNotice` in [Core mailbox load](../../../src/Server/Core/CoreMailboxLoad.fs) only applies the message. `dispatchItem` logs through `CoreEventDispatch.logNotice` in [Core event dispatch](../../../src/Server/Core/CoreEventDispatch.fs). The arch sentence is that `applyNotice` applies each message and logs it.
2. **Blocker — Append failure** — [core-refinement architecture](../arch.md) §1 Special-node Parse and Persist axes and §7 vocabulary item 8 Say Notice. `CoreMailboxLoad.dispatchItem` in [Core mailbox load](../../../src/Server/Core/CoreMailboxLoad.fs) reports an `Error` from `logNotice` and still calls `applyNotice`. The arch sentence is that Notice and CoreMsg are appended to the same event log.
3. **Ticket note** — [76 — Log Notices on the event log](../issues/76-log-notices-on-the-event-log.md) names `dispatchItem` as the place that appends, then applies. That ticket sentence and the arch sentence that `applyNotice` logs it name different functions.

## 4. Totals

- Standards: 0 findings. Worst: none.
- Spec: 0 findings. Worst: none.
- Arch: 2 findings. Worst: the log call sits in `dispatchItem`, and the arch says `applyNotice` logs the message.
- Suite: Server 727 passed. Shared 1921 passed, 1 skipped. Client build succeeded. Tests that counted only Changes now also expect `Notice.SnapshotDone`.

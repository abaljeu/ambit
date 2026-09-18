# Code review — 16 Approve / merge stamp + beforeAll

Range: uncommitted vs `HEAD`. Spec: [16 — Approve / merge stamp + beforeAll](../issues/16-approve-merge-stamp-beforeall.md). Scan: none.

Production approve/stamp, `EventId.beforeAll`, and BootCache SnapshotRecord `eventId` are already on `HEAD` from the historical [11 — One serial event id](../issues/11-one-serial-event-id.md) repair. This peel adds merge-ahead and Redo-target tests at [ClientHistory.approve](src/Shared/ClientHistory.fs).

## Standards

Mechanical scan: none. **No hard documented-standard hits.**

F# hunks in [ClientHistoryRuntimeTests.fs](tests/Shared.Tests/ClientHistoryRuntimeTests.fs) stay under 100-char lines, 40-line functions, and 400-line file ([fsharp-source.md](.agents/rules/fsharp-source.md)). New Ev locals are `event` / specific names, not leftover `change`. `failwith` in match arms matches this file; [Surgical Changes](.agents/rules/core-agent-behavior.md) says match existing style.

Markdown hunks: one blank line between blocks, no mid-paragraph wraps ([markdown-writing.md](.agents/rules/markdown-writing.md)). New issue refs use titled links, e.g. [11 — One serial event id](../issues/11-one-serial-event-id.md) ([refer-by-name.md](.agents/rules/refer-by-name.md)).

**Judgement — Duplicated Code.** Both new facts share the same record-from-clear shape:

```
let source = textChange 0 (NodeId.New()) "old" "new"
let recorded =
    ClientHistory.clear ()
    |> ClientHistory.record { source with commandName = "Edit node" }
```

Same setup already repeats earlier in this file; extracting would fight Surgical Changes.

**Judgement — Mysterious Name.** `source` is an `Ev` (pending local). Nearby names (`foreign`, `confirmed`, `event`) are clearer.

## Spec

Range: `git diff HEAD`. Spec: [16 — Approve / merge stamp + beforeAll](../issues/16-approve-merge-stamp-beforeall.md).

**(a) Missing or partial**

None. “On approve, replace zero with server id. On revised stream (server ops inserted ahead), rewind and stamp zeros from matching `submissionId`. Stamp nested Undo/Redo targets. `EventId.beforeAll` instead of `fromJson -1`. BootCache SnapshotRecord `eventId` if still open after [14 — EventId serial on Shared + Server](../issues/14-eventid-serial-shared-server.md).” Production for those lines is already on HEAD (approve/stamp, `EventId.beforeAll`, SnapshotRecord `eventId`; SyncLogic catch-up rewind). “**Green bar:** merge/revise and Undo/Redo target tests from the repair review.” This diff adds merge-ahead (foreign event first) and Redo-target tests at [ClientHistory.approve](src/Shared/ClientHistory.fs). Envelope stamp-by-`submissionId` and Undo-target tests already sit on HEAD in [ClientHistoryTests.fs](tests/Shared.Tests/ClientHistoryTests.fs).

**(b) Scope creep**

None versus this ticket’s product behaviour. Status/`Actual`/comment/Time on [16 — Approve / merge stamp + beforeAll](../issues/16-approve-merge-stamp-beforeall.md) and [project.md](../project.md) are land bookkeeping. [15 — Client pending = zero + submissionId](../issues/15-client-pending-zero-submissionid.md) Status `done` is Alan marking the blocker, not 16 scope. Out of scope contract deletes and [core-creation arch.md](../../core-creation/arch.md) are untouched.

**(c) Looks implemented, looks wrong**

None. Merge-ahead: `approve [foreign; confirmed]` then `undoEvent` targets `EventId` 9, not the leading foreign 8 — matching “stamp zeros from matching `submissionId`.” Redo-target: record → undo → redo → `approve [confirmedOriginal; confirmedUndo]` then `tryPeekUndoEvent` is `EventBody.Redo` targeting confirmed Undo id 10 — matching “Stamp nested Undo/Redo targets.”

## Summary

Standards: 0 hard; 2 judgement smells (duplicated record setup; Mysterious Name `source`). Spec: 0 missing, 0 creep, 0 wrong; worst: none.

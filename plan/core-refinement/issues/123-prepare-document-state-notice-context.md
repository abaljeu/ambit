# 123 — Every Notice states its document-state write

**Status:** `coded`

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] none

## 3. Look at

### Owner

[ ] [History.fs](../../../src/Shared/History.fs) `Notice`

### Source

[ ] [CoreMailboxLoop.fs](../../../src/Server/Core/CoreMailboxLoop.fs) `applyNotice`
[ ] [CoreMailboxFilesAxis.fs](../../../src/Server/Core/CoreMailboxFilesAxis.fs) `writePullingFileUnpersisted`, `writeReceived`, `applyNodeParsed`, `writeUnpersisted`, `writeParseState`, `writePersisted`

### Tests

[ ] [CoreLoopNoticeTests.fs](../../../tests/Server.Tests/CoreLoopNoticeTests.fs)

## 4. Context

[x] `Notice` has eight cases: `NodeUploading`, `NodeReceived`, `NodeParsing`, `NodeParsed`, `NodePersisting`, `NodePersisted`, `PersistFailed`, and `MarkUnparsed`.
[x] `CoreMailboxLoop.applyNotice` matches each case.
[x] `DocumentState` is `parseState`, `persistState`, `pullingFile`, `persistedVersion`, and `changedVersion`.
[x] [122 — Document state moves by Notice](122-document-state-moves-by-notice.md) names the Load, Parse, and Persist senders.

## 5. Changes

[x] Each `Notice` case has one owner ticket and one test that state the `DocumentState` write.

### NodeUploading

[x] `writePullingFileUnpersisted` sets `pullingFile`, `PersistState.Unpersisted`, and `ParseState.Unparsed`.
[x] [50 — Upload emits NodeUploading](50-upload-emits-nodeuploading.md) states PullingFile and Unpersisted.
[x] ``NodeUploading applies PullingFile`` checks that apply.

### NodeReceived

[x] `writeReceived` sets `ParseState.Unparsed` and `PersistState.Persisted` and clears `pullingFile`.
[x] [53 — Arrival and reconcile emit NodeReceived](53-arrival-and-reconcile-emit-nodereceived.md) states that write.
[x] ``NodeReceived applies Unparsed`` checks that apply.

### NodeParsing

[x] The `Notice.NodeParsing` arm leaves `parseState` and `persistState` as they are.
[x] [73 — Add the NodeParsing and NodePersisting Notices](73-add-the-nodeparsing-and-nodepersisting-notices.md) states that write.
[x] ``NodeParsing changes nothing`` checks that apply.

### NodeParsed

[x] `applyNodeParsed` sets `ParseState.Parsed` and clears `pullingFile` when `pullingFile` is clear.
[x] When `pullingFile` is set, `GraphMutate.setParseState` leaves the node `ParseState.Unparsed`.
[x] Persist state stays as it is.
[x] [54 — Parse and import stop the Parsed Set](54-parse-and-import-stop-the-parsed-set.md) states that write.
[x] ``ParseFinished sets Parsed and leaves PersistState`` and ``ParseFinished leaves a pullingFile File Node Unparsed`` check that apply.

### NodePersisting

[x] `writeUnpersisted` sets `PersistState.Unpersisted` and leaves `parseState` as it is.
[x] 73 — Add the NodeParsing and NodePersisting Notices states that write.
[x] ``NodePersisting matches the Unpersisted Set`` checks that apply.

### NodePersisted

[x] `writePersisted` sets `persistedVersion` to the written version and stamps `updateTime`.
[x] `persistState` becomes `Persisted` when that version has reached `changedVersion`. An older write stays `Unpersisted`.
[x] [100 — Core loop gates Persisted on the written version](100-core-loop-gates-persisted-on-the-written-version.md) states that write.
[x] ``NodePersisted sets Persisted and calls the diff hook`` and ``NodePersisted for an older version leaves the node Unpersisted`` check that apply.

### PersistFailed

[x] The `Notice.PersistFailed` arm leaves `persistState` as it is.
[x] [75 — A failed write does not fail the edit](75-a-failed-write-does-not-fail-the-edit.md) states that write.
[x] ``PersistFailed does not change persist state`` checks that apply.

### MarkUnparsed

[x] `writeParseState` sets `ParseState.Unparsed` and leaves `persistState` as it is.
[x] [121 — MarkUnparsed leaves Notice](121-markunparsed-leaves-notice.md) removes this case.
[x] ``MarkUnparsed sets Unparsed and leaves PersistState`` checks the write while the case is present.

## 6. Tests

[x] No new test is required.
[x] The tests named in section 5 stay the check for each `Notice` write.

## 7. Done

[x] Run `scripts/agent-test.sh` with no arguments.
[x] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[x] All executed tests pass.

See [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md): §7 note 1: `Ev.apply` and the mailbox's `applyNotice` write document state differently.

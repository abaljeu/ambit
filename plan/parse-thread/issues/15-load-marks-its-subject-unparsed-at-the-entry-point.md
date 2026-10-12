# 15 — Load marks its subject Unparsed at the entry point

**Status:** `coded`
Actual: 1h
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## What to build

The Unparsed mark belongs on the user-invoked Load entry point. [Core mailbox load](../../../src/Server/Core/CoreMailboxLoad.fs) `dispatchLoad` calls `parsePush`. That push is `markUnparsedThenPush`, so Load sets its subject Unparsed and then pushes that subject. [Directory reconcile](../../../src/Shared/dotnet/DirectoryReconcile.fs) returns an empty push list. Reconcile does not call Load. Per [core-refinement architecture](../../core-refinement/arch.md) §5 **Load on Directory**, reconcile never marks an existing member Directory Node or File Node that is still present Unparsed.

1. [x] **Mark at Load** — The user-invoked Load sets its subject File Node or Directory Node to Unparsed, then pushes it onto the parse stack.
2. [x] **Push does not mark** — Reconcile does not call the marking push. Load is the caller that sets the subject Unparsed and then pushes that subject.
3. [x] **Reconcile never pushes** — Directory reconcile pushes nothing onto the parse stack. It compares disk members with the member nodes that are already present. It adds a new Directory Node or File Node, and that member starts Unparsed. It removes a member node whose file is gone. It leaves an existing member untouched.

### Acceptance Criteria

1. [x] **Directory subject marked** — A user Load on a Parsed Directory Node marks that Directory Node Unparsed, then pushes it, so the parse thread reconciles it.
2. [x] **Members untouched** — Directory reconcile does not mark an existing member Directory Node or File Node that is still present Unparsed. The parse state of an existing still-present member stays as it was. New members start Unparsed.
3. [x] **File still parses** — File Load marks its File Node Unparsed and parses that file, regardless of date or Parsed state.
4. [x] **No reconcile push** — After a Directory reconcile, the parse stack holds nothing the reconcile added.

## Implementation

1. **No production edit** — `dispatchLoad` is the Load entry point the client reaches through mailbox Load. `parsePush` marks the subject Unparsed and then pushes that subject. `planFromEntries` sets `push` to `[]`. [Lazy load reconciliation server](../../../src/Server/LazyLoadReconciliationServer.fs) posts graph-only changes. It does not call Load and it does not push the parse stack.
2. **Proof** — [Parse thread load tests](../../../tests/Server.Tests/ParseThreadLoadTests.fs) `user Load on a Parsed Directory marks it Unparsed then reconciles`, `Directory reconcile leaves existing member parse state unchanged`, `File Load parses its file regardless of Parsed state or disk date`, and `Directory reconcile pushes nothing onto the parse stack`.

## Side effects

1. **New members stay Unparsed** — A new member node starts Unparsed. Reconcile does not push that member, so the parse thread does not parse it. A later user Load of that member is what parses it. This ticket does not add a workaround.
2. **Requeue push still marks** — The push returned with the mailbox still calls `markUnparsedThenPush`. The parse thread holds that push for requeue. Requeue is off, and the reconcile push list is empty, so that push does not run for members. This ticket does not split the mark out of that function.

## Time

- 2026-10-12 1h — ticket and Server tests for the Load mark and the reconcile push (from chat)

## See also

- [14 — Directory reconcile matches disk members](14-directory-reconcile-matches-disk-members.md)

# 143 — Tests post Notices through the rig

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] None.

## 3. Look at

### Owner

[ ] The test rigs that build a `MailboxHost`

### Test files that call `CoreMailboxApi.postNotice`

[ ] [CoreLoopNoticeTests.fs](tests/Server.Tests/CoreLoopNoticeTests.fs)
[ ] [DbAgentNoticeTests.fs](tests/Server.Tests/DbAgentNoticeTests.fs)
[ ] [FileAgentFailureTests.fs](tests/Server.Tests/FileAgentFailureTests.fs)
[ ] [GithubTransportActorTests.fs](tests/Server.Tests/GithubTransportActorTests.fs)
[ ] [GithubTransportLoadPullTests.fs](tests/Server.Tests/GithubTransportLoadPullTests.fs)
[ ] [LazyLoadReconciliationServerTests.fs](tests/Server.Tests/LazyLoadReconciliationServerTests.fs)
[ ] [LoadSaveCommandTests.fs](tests/Server.Tests/LoadSaveCommandTests.fs)
[ ] [MailboxExpandTests.fs](tests/Server.Tests/MailboxExpandTests.fs)
[ ] [NodeEventExpandTests.fs](tests/Server.Tests/NodeEventExpandTests.fs)
[ ] [ParseThreadLoadTests.fs](tests/Server.Tests/ParseThreadLoadTests.fs)
[ ] [PersistApplyTests.fs](tests/Server.Tests/PersistApplyTests.fs)

### Test files that call `CoreMailboxApi.logout`

[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs)
[ ] [SessionTests.fs](tests/Server.Tests/SessionTests.fs)

## 4. Context

### Today

[ ] Each test posts a Notice with `CoreMailboxApi.postNotice host`, or drops authority with `CoreMailboxApi.logout host`, on the `MailboxHost` it built

### Decisions

[ ] Alan, 2026-10-29: the `CoreQueue` post is private and granted to approved holders. A test gets the post from its rig, not from the `MailboxHost`.
[ ] This ticket blocks [115 — The CoreQueue post is private](115-the-corequeue-post-is-private.md)

## 5. Changes

### Change

[ ] One change: these tests post Notices and drop authority through the post their rig hands them

### Size

[ ] 13 test files, over the 4-test-file limit; needs Alan's approval or a split

## 6. Tests

### Kept

[ ] Every test in the files above keeps its name and its assertions

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.

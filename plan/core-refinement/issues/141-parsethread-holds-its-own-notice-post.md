# 141 — ParseThread holds its own Notice post

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] None.

## 3. Look at

### Owner

[ ] [ParseThread.fs](src/Server/ParseThread.fs) owns `postParseOps` and `bootCore`

### Source

[ ] [ParseThread.fs](src/Server/ParseThread.fs) `postParseOps`, `bootCore`
[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `postNotice`, read only
[ ] [CoreChangesDoor.fs](src/Server/Core/CoreChangesDoor.fs) `CoreChangesDoor`, read only

### Notice posts

[ ] `postParseOps` posts `Notice.NodeParsing` and `Notice.ParseFailed` through `CoreMailboxApi.postNotice handle.host`
[ ] `handle` is a `CoreChangesDoor`; every door holder can reach `host` the same way
[ ] `bootCore` fills `finishParse` with `CoreMailboxApi.postNotice core.host (Notice.NodeParsed nodeId)`

## 4. Context

### Decisions

[ ] Alan, 2026-10-29: the `CoreQueue` post is private and granted to approved holders. ParseThread is an approved holder of the Notice post.
[ ] This ticket blocks [115 — The CoreQueue post is private](115-the-corequeue-post-is-private.md)

## 5. Changes

### Change

[ ] One change: ParseThread posts its Notices through a Notice post it receives at boot, not through `handle.host`

### Rules

[ ] `postParseOps` reads no `host` from its `CoreChangesDoor` to post a Notice
[ ] The Notices posted and their order are unchanged: `NodeParsing`, the Change, then `NodeParsed`, or `ParseFailed` on error

## 6. Tests

### Kept

[ ] Keep the tests in [ParseThreadLoadTests.fs](tests/Server.Tests/ParseThreadLoadTests.fs)
[ ] Filter: `ParseThreadLoadTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.

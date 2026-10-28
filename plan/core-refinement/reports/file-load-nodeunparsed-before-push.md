# File Load sends NodeUnparsed before the parse push

## 1. Branch

[ ] The worktree is `dev`.

## 2. Load

[ ] `replyLoad` in [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) appends the Load event, then calls `dispatchLoad`.
[ ] `dispatchLoad` logs `Notice.NodeUnparsed` for the loaded node, writes that axis, and then calls `parsePush`.
[ ] `applyLoggedNotice` in the same file is the log-then-axis step.

## 3. Facts

[ ] `File Load logs Load, NodeUnparsed, NodeParsing, then NodeParsed` passed: 1 passed, 0 failed.
[ ] `NewSpecialNode starts Parsed and Persisted` passed: 1 passed, 0 failed.
[ ] Facts skipped with `pending documentState/Load spec` stay skipped.
[ ] [SpecialNodeStateAxesTests.fs](tests/Shared.Tests/SpecialNodeStateAxesTests.fs) was left unchanged.

## 4. Other files

[ ] The lasting source edit is [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs).
[ ] [GithubTransportLoadPullTests.fs](tests/Server.Tests/GithubTransportLoadPullTests.fs) line 72 is the committed text `->n`. That token blocks `Gambol.Server.Tests` from compiling. The token was `->` only while the File Load fact ran, then the committed text was restored.

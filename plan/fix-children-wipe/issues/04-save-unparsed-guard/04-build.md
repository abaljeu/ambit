# 04 — Guard Save on an Unparsed Node build

Ticket: [04 — Guard Save on an Unparsed Node](../04-save-unparsed-guard.md)

## Guard

`GithubTransportActor.persistFocus` in `src/Server/GithubTransportActor.fs` calls `GithubTransportActor.parseUnparsedFocusFromDisk` before `GithubTransportActor.focusCanBeParsed`. That call reads the focus's real disk content first, so the later `Notice.NodeParsed` and persist write never run against stale in-memory content.

`GithubTransportActor.planUnparsedFocusFromDisk` picks the read by the focus's kind and parse state.

- A focus that is already Parsed, or that is pulling, plans no ops. `GithubTransportActor.persistFocus` then runs exactly as before this ticket: unaffected are a Parsed File Node, Directory Node, or Workspace Node, and a pulling File Node (`focusCanBeParsed`'s own "stays unparsed while a file is pulling" refusal).
- An Unparsed File Node reads through `DocumentPersistWrite.planParseFile`, the same read `ParseThread.parseFile` uses for an ordinary Load. A missing File, a directory at that path, or a read failure, is the `Error` that call already returns; that `Error` reaches the Save caller unchanged.
- An Unparsed Directory Node or Workspace Node reads through `GithubTransportActor.planDirectoryFromDisk`, locked the way `ParseThread.reconcileDirectory` locks a Directory reconcile. `GithubTransportActor.directoryGoneFromDisk` checks the Directory's own folder with `System.IO.Directory.Exists` before any reconcile: `DocumentPersistPath.discoverImmediateMembers` reads a missing folder as an empty listing, which `DirectoryReconcile.planDirectoryReconcile` would otherwise read as a Directory with no members.

`GithubTransportActor.parseUnparsedFocusFromDisk` posts the read's ops as a `"Parse"` authority change through `changes.postGraphOnly`, then rereads the graph, before `focusCanBeParsed` runs. A read `Error` is the `Error` `GithubTransportActor.persistFocus` returns: the Save actor fails before `Notice.NodeUnpersisted`, `Notice.NodeParsed`, or any persist write, and `GithubTransportActor.runSaveBody` never reaches commit, pull, or push.

## Tests

`tests/Server.Tests/GithubTransportActorTests.fs`:

- ``git Save on an unparsed file with real disk content persists that content, not empty`` writes real text to the File Node's disk path, marks it Unparsed, Saves it, and asserts the committed file holds that text (not empty) and the Node ends Parsed.
- ``git Save on an unparsed file gone from disk refuses the Save`` marks a File Node Unparsed with no disk path ever written, Saves it, and asserts the actor fails with `"file not found"`, no commit step runs, and the Node stays Unparsed.

`tests/Server.Tests/LoadSaveCommandTests.fs`: ``Load of a ROOT file skips git and git Save uses the data directory`` writes the ROOT File Node's disk content before its Save phase. The Desk Load earlier in that test marks the File Node Unparsed, and that test's host runs no Parse thread to read it back, so the Save phase needs real disk content to pass the guard.

## Check

`scripts/agent-test.sh --filter` for `GithubTransportActorTests`, `GithubTransportLoadPullTests`, `LoadSaveCommandTests`, `LoadSaveSaveCommandTests`, `NodeEventExpandTests`, and `CoreLoopNoticeTests` passed 97, failed 0.

`scripts/agent-test.sh --full` with `AMBIT_FULL_SUITE=1`: Server passed 779, skipped 1, total 780, failed 0. Shared passed 1948, skipped 3, total 1951, failed 0.

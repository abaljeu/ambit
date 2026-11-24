# 04 — Guard Save on an Unparsed Node review

Check: pass

Ticket: [04 — Guard Save on an Unparsed Node](../04-save-unparsed-guard.md)
Build: [[04-build.md]]
Project: [[../../project.md]]

## 1. Correctness

`GithubTransportActor.persistFocus` calls `GithubTransportActor.parseUnparsedFocusFromDisk` before `GithubTransportActor.focusCanBeParsed`, and returns that call's `Error` before posting `Notice.NodeUnpersisted` or `Notice.NodeParsed`, so no persist write runs on a disk-read failure.

`GithubTransportActor.planUnparsedFocusFromDisk` dispatches on the focus's own kind and state:

- A focus that is already Parsed, or that is a pulling File, plans `[]`. This matches `focusCanBeParsed`'s own pre-existing "stays unparsed while a file is pulling" refusal, and keeps an already-Parsed Save on the path it ran before this ticket.
- An Unparsed File reads through `DocumentPersistWrite.planParseFile`, the same call `ParseThread.parseFile` uses for an ordinary Load. A missing file, a directory at that path, or a read failure, is the `Error` that call already returns (`DocumentPersistPath.readFileTextAtRelative`'s `"file not found"`, `"path is a directory"`, or a `"read failed: ..."` message); that `Error` reaches the Save caller unchanged.
- An Unparsed Directory or Workspace reads through `GithubTransportActor.planDirectoryFromDisk`, which takes the same `NodeLockPool.withLockResult` lock `ParseThread.reconcileDirectory` takes, then calls `GithubTransportActor.directoryGoneFromDisk` before `DirectoryReconcile.planDirectoryReconcile`. `directoryGoneFromDisk` checks `System.IO.Directory.Exists` on the focus's own folder, so a folder gone from disk is an `Error` instead of the empty member listing `DocumentPersistPath.discoverImmediateMembers` would otherwise read — the same missing-folder shape as the Rank 1 cause, Load reconciling against a missing or wrong-case folder, now refused for this path too.

`parseUnparsedFocusFromDisk` posts the read's ops as a `"Parse"` graph-only change (`changes.postGraphOnly`), then rereads the graph, before `focusCanBeParsed` runs its own dry-run flip to `ParseState.Parsed` on that reread graph. The dry-run and the later `Notice.NodeParsed` are unchanged from before this ticket; only the graph they run against now carries real disk content first.

`Special Workspaces` (the Workspaces-list root, distinct from a named `Special Workspace`) falls through `planUnparsedFocusFromDisk`'s catch-all to `Ok []`. This is not a gap: `GraphMutate.setParseState` already refuses a `Special Workspaces` focus with `"workspaces is not a graph document"`, so `focusCanBeParsed` fails that focus the same way on staging and on this branch.

## 2. Unaffected paths

An already-Parsed focus and a pulling File are unaffected: `planUnparsedFocusFromDisk`'s first match arm returns `Ok []` for both, so `persistFocus` reaches `focusCanBeParsed` exactly as it did before this ticket. ``git Save persists focus then commits pulls and pushes`` creates its focus through `FileNodeOps.planCreateOwnedFile`, which starts a node `Current` (Parsed), and that test still passes unchanged, so this is covered, not only reasoned.

## 3. Error paths refuse before any persist write

Confirmed by reading: `parseUnparsedFocusFromDisk`'s `Error` path returns before `persistFocus` reaches `focusCanBeParsed` or posts any notice. Confirmed by test: ``git Save on an unparsed file gone from disk refuses the Save`` asserts `ActorFailed "file not found"`, an empty `wroteFile`, an empty `steps` (no commit, pull, or push ran), and the Node stays `ParseState.Unparsed`.

## 4. The `LoadSaveCommandTests.fs` fixture line

`Load of a ROOT file skips git and git Save uses the data directory`` now writes `loose.txt`'s real text to disk before its Save phase. That test's own assertions (`LoadSavePath.Git`, a stop count, and the recorded pull/push roots) never checked file content, so the added line changes no assertion; it only supplies the disk content this ticket's guard now requires, since the test's Desk Load marks the Node Unparsed and the test host runs no Parse thread to read it back. This is justified, not a masked regression.

## 5. Test coverage of the Done items

The first two Done items name "Save on an Unparsed File or Directory focus" and "a focus ... gone from disk". `GithubTransportActorTests.fs`'s two new tests cover both for a File focus only (real content persisted; gone-from-disk refusal). No test in the suite starts a git Save on an Unparsed Directory or Workspace focus, so `GithubTransportActor.planDirectoryFromDisk` and `GithubTransportActor.directoryGoneFromDisk` run only under the reasoning in section 1 above, not under a test. See the Directory/Workspace guard path untested finding below.

## 6. Unintended behavior change

None found. The one new observable effect is the extra `"Parse"` graph-only change `parseUnparsedFocusFromDisk` posts ahead of the existing `Notice.NodeUnpersisted`/`Notice.NodeParsed` pair, on the Unparsed path only; `waitUntilPersisted`'s `eventsSince` scan already tolerates intervening events since it filters for `Notice.PersistFailed` by focus id, so this is not a race.

## 7. Tests

`scripts/agent-test.sh --full`: `tests/Server.Tests` passed 779, skipped 1, total 780, failed 0. `tests/Shared.Tests` passed 1948, skipped 3, total 1951, failed 0.

## 8. Findings

1. **Medium — Directory/Workspace guard path untested.** `GithubTransportActor.planDirectoryFromDisk` and `GithubTransportActor.directoryGoneFromDisk` are new code with no direct test: no test in the suite Saves an Unparsed Directory or Workspace focus, either with real disk members or with its folder gone. The File-focus path is tested; the Directory/Workspace path, which also carries the `NodeLockPool` lock and the `DirectoryReconcile.planDirectoryReconcile` call, is not. A test for each case closes this gap: an Unparsed Directory Save with real disk members persisting those members, and an Unparsed Directory Save whose folder is gone refusing the Save.

## 9. Verdict

Pass. The guard is correct for the tested File path and reasons correctly for the Directory/Workspace path from the code alone; the Directory/Workspace guard path untested finding above names the one gap between the ticket's Done items and its test coverage.

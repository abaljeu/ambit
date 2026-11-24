# 04 — Guard Save on an Unparsed Node build

Ticket: [04 — Guard Save on an Unparsed Node](../04-save-unparsed-guard.md)

## Guard

`GithubTransportActor.focusCanBeParsed` is the only Save guard. Before this ticket, it called `GraphMutate.setParseState focusId ParseState.Parsed graph` unconditionally: on an Unparsed, non-pulling focus that call fell to `setParseState`'s last case and flipped the focus straight to Parsed without ever reading its real content, so the persist write that followed wrote empty content over it (`3a1a0aa8` emptied issue 07 this way).

`focusCanBeParsed` now checks the focus's own `parseState` first:

- An Unparsed focus that is not a pulling File — a File, Directory, or Workspace — refuses outright with `"focused node is unparsed"`, before `GraphMutate.setParseState` ever runs. This is the one new case: it refuses exactly the case that used to fall through to the flip.
- Every other focus still runs through `GraphMutate.setParseState focusId ParseState.Parsed graph` exactly as before this ticket: a pulling File still refuses with `"focused node stays unparsed while a file is pulling"` from `setParseState`'s own result; a Parsed File, Directory, or Workspace still succeeds with `Ok ()`; and any other `setParseState` refusal (for example a Workspaces-list focus) still reaches the Save caller unchanged.

`GithubTransportActor.persistFocus` calls `focusCanBeParsed` first and returns its `Error` before posting `Notice.NodeUnpersisted` or `Notice.NodeParsed`, so a refused Save reaches no persist write, commit, pull, or push.

A file or folder missing from disk is not an Unparsed case. `focusCanBeParsed` never reads the filesystem, so a Parsed focus whose file or folder is missing from disk is not refused; Save proceeds exactly as it did before this ticket, the same as on `staging`.

## Tests

`tests/Server.Tests/GithubTransportActorTests.fs`:

- ``git Save on an unparsed file is refused and the file on disk is unchanged`` writes real content to the File Node's disk path, marks it Unparsed, Saves it, and asserts the actor fails with `"focused node is unparsed"`, no commit/pull/push step runs, the disk content is untouched, and the Node stays Unparsed.
- ``git Save on an unparsed folder is refused and the folder on disk is unchanged`` does the same for an owned Directory Node with a real file inside its folder.
- ``git Save on a Parsed folder missing from disk is not refused`` Saves a freshly created (Parsed) Directory Node whose folder was never created on disk, and asserts the Save succeeds and commits, pulls, and pushes as usual.

`tests/Server.Tests/LoadSaveCommandTests.fs`: ``Load of a ROOT file skips git and git Save uses the data directory`` found that its Desk Load leaves the ROOT File Node Unparsed (this test's host runs no Parse thread to read it back), so its Save phase now posts `Notice.NodeParsed` directly on that Node before Saving, instead of writing disk content for a since-removed read step.

## Check

`scripts/agent-test.sh --filter` for `GithubTransportActorTests`, `GithubTransportLoadPullTests`, `LoadSaveCommandTests`, `LoadSaveSaveCommandTests`, `NodeEventExpandTests`, and `CoreLoopNoticeTests` passed 81, failed 0.

`scripts/agent-test.sh --full` with `AMBIT_FULL_SUITE=1`: Server passed 780, skipped 1, total 781, failed 0. Shared passed 1948, skipped 3, total 1951, failed 0.

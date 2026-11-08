# 31 — Force-save the focused node
**Status**: coded

One job: Save on an artifact focus marks that node Parsed and pushes it on the persistence stack before commit, including when the node is already Persisted. A Normal Node skips that persist. Save then commits, pulls, and pushes. The stage is Solution 11 — git Save sequence in [github-transport spec](../spec.md).

Type:

[ ] coding

Blockers:

[ ] none

## Inputs

### Working

[ ] `saveActorFunction` in `src/Server/GithubTransportActor.fs` runs Save.
[ ] `runSaveBody` in that file is the Save body. Load stays `runLoadBody`.
[ ] `CoreMailboxFilesAxis.applyNodeParsed` in `src/Server/Core/CoreMailboxFilesAxis.fs` posts `Notice.NodeParsed`, then pushes when the node is Parsed and Unpersisted.
[ ] `PersistThread.runRequest` in `src/Server/Core/PersistThread.fs` skips a node that is still Persisted.
[ ] `GraphMutate.setParseState` in `src/Shared/GraphMutate.fs` refuses a normal node and a directory file, and leaves a pulling file Unparsed.
[ ] `ActorChanges` in `src/Shared/History.fs` is the actor door. `CoreDoor.actorChanges` in `src/Server/Core/CoreDoor.fs` fills it.
[ ] Scope report: [31 — Force-save the focused node — scope](../reports/31-force-save-focused-node-scope.md).

### Reference

[ ] Solution 11 — git Save sequence.
[ ] [30 — GitHub save path: missing workspace upward scan and workspace upload](30-github-save-path-missing-workspace-upward-scan-and-workspace-upload.md).

## Do not load

[ ] Load's pull resolution and the git transport.
[ ] Desk Save.
[ ] The parse stack.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change Save's persist step so an artifact focus is the node marked Parsed and pushed. Save posts `Notice.NodeUnpersisted`, then `Notice.NodeParsed`, on that focus. The second notice is the existing push.
3. [x] Wait until that node is Persisted, or until `Notice.PersistFailed` for that node, before commit.
4. [x] A Normal Node skips persist and its state stays unchanged. Save then commits, pulls, and pushes. When `GraphMutate.setParseState` cannot mark any other focus Parsed, fail Save with that error. Save does not walk to an enclosing document.

## Outputs

[ ] `src/Server/GithubTransportActor.fs`
[ ] `src/Shared/History.fs`
[ ] `src/Server/Core/CoreDoor.fs`
[ ] `tests/Server.Tests/GithubTransportActorTests.fs`
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs`
[ ] `plan/github-transport/spec.md`
[ ] `plan/github-transport/project.md`
[ ] `plan/github-transport/map.md`

## Stop when

[ ] Record the stop at `plan/github-transport/reports/31-force-save-the-focused-node-stop.md`, then ask.
[ ] Stop when the persist step names an enclosing Workspace or document instead of the focus.
[ ] Stop when Save gains a second persistence writer beside the persistence stack.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``git Save persists focus then commits pulls and pushes``
  - `tests/Server.Tests`: ``git Save marks an unparsed file parsed and writes it before commit``
  - `tests/Server.Tests`: ``git Save on a Normal Node skips persist then commits pulls and pushes``
  - `tests/Server.Tests`: ``git Save gates commit then releases before push``
  - `tests/Server.Tests`: ``git Save stops when the pull fails and does not push``
  - `tests/Server.Tests`: ``Load and Save return the same conflict reject shape``
  - `tests/Server.Tests`: ``routed Git Save commits then pushes through Peer Actor``
[x] All executed tests pass.
[x] Write the verdict to `plan/github-transport/reports/31-force-save-the-focused-node-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.

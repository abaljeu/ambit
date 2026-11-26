Check: pass

# 04 — Guard Save on an Unparsed Node review

Ticket: [04 — Guard Save on an Unparsed Node](../04-save-unparsed-guard.md)
Build: [[04-build.md]]
Project: [[../../project.md]]

## 1. The decided rule, in code

The decided rule in the project.md Decisions entry is: the only Save guard is that Save on an Unparsed File, Directory, or Workspace focus is refused with an Error before any persist write, commit, pull, or push; a file or folder missing from disk is not an Unparsed case.

`GithubTransportActor.focusCanBeParsed` carries that whole rule in one check. It reads the focus's own `parseState` first: when the focus is `ParseState.Unparsed` and `documentState.pullingFile` is clear, it returns `Error "focused node is unparsed"` immediately, before `GraphMutate.setParseState` ever runs. Every other focus — Parsed, or an Unparsed File mid-pull — falls through to the same `GraphMutate.setParseState focusId ParseState.Parsed graph` call that ran on `staging`, unchanged. The check reads only the focus's own node; it is the same one line regardless of whether that node is a File, a Directory, or a Workspace, so the rule covers all three kinds by construction, not by a kind-specific branch that could miss one.

## 2. Every Save path reaches the guard before any write

`GithubTransportActor.persistFocus` calls `focusCanBeParsed` and returns its `Error` before posting `Notice.NodeUnpersisted` or `Notice.NodeParsed`. `GithubTransportActor.persistFocusWhenNotOrdinary` calls `persistFocus` for every focus whose `kind` is not `Normal` — the only focuses `GraphMutate.setParseState` ever accepts. `GithubTransportActor.runSaveBody` calls `persistFocusWhenNotOrdinary` first, and only on `Ok ()` does it call `readSaveWorkspace` to decide between the git-remote Save (`GithubTransportActor.saveFetched`, which commits, fetches, pulls, and pushes) and the Neither Save (`GithubTransportActor.commitSaveRepository`, which commits a local repository with no remote). Both arms sit downstream of the same `persistFocusWhenNotOrdinary` call, so an Unparsed focus is refused before either arm's commit, pull, or push runs. `GithubTransportActor.runSave` is the same `runSaveBody` body under a direct name for a non-actor caller (used by `tests/Server.Tests/GithubTransportLoadPullTests.fs`); it is not a second code path.

No other route in `src/Server` writes a focus's content from the in-memory graph under a Save command. `GithubTransportActor.saveActorFunction`, reached from `LoadSaveStart.startGit`, is the only `Save`-operation actor the Peer Actor system starts; `LoadSaveStart.startDeskCommand` (the Desk path) starts no actor for a Save request, since `DeskLoadPush.subjectToLoad` dispatches a Load only, never a Save. `DailyGitSave.walk` and `GitSave.commitAll` run `git add -A` and `git commit` against whatever is already on disk; neither reads the graph, neither calls `GraphMutate.setParseState`, and neither can flip a node to Parsed or write its content, so they cannot reproduce the issue-07 loss. `SavePrep.syncDataDir` only computes an event id for the database sync status; it writes nothing.

## 3. Save never writes a descendant

The write this ticket guards against is the single call `DocumentPersistWrite.writeDocument`, reached through `DocumentPersistChange.persistNode`, reached when the persist thread pops the one `nodeId` that `CoreMailboxFilesAxis.pushParsedUnpersisted` pushed for the `Notice.NodeParsed focusId` that `persistFocus` posts. That push names only the focus id; `GraphMutate.setParseState` likewise mutates only the one node named by its `nodeId` argument. A Save never pushes, parses, or writes any node other than its own focus, so a Parsed Directory or Workspace focus with an Unparsed descendant is not at risk here: Save does not touch that descendant's content at all, in either direction.

## 4. Unaffected paths

The diff against `origin/staging` inserts one new match arm ahead of the pre-existing body of `focusCanBeParsed` and otherwise leaves that function, `persistFocus`, and `persistFocusWhenNotOrdinary` byte-for-byte as they are on `staging`. A pulling File still reaches `"focused node stays unparsed while a file is pulling"` from `setParseState`'s own result, since the new check's guard clause excludes `documentState.pullingFile`. A `Special Workspaces` focus (the Workspaces-list root) still reaches `"workspaces is not a graph document"` from `setParseState`, since that node's `parseState` defaults to `Parsed` through `Node.Create` and nothing in the graph pipeline sets it to `Unparsed` — only `GraphMutate.setParseState`, `GraphMutate.markUnparsedThenPush`, and similar calls can do that, and each first calls `artifactNode`, which already refuses a `Special Workspaces` or `Normal` focus. `GithubTransportActor.runLoadBody` never calls `persistFocus` or `focusCanBeParsed`, so Load is unaffected; `` git Load tells the client the actor completed ``, `` git Load gates pull from Workspace focus ``, and the other Load tests in `tests/Server.Tests/GithubTransportActorTests.fs` and `tests/Server.Tests/GithubTransportLoadPullTests.fs` pass unchanged. A Parsed focus whose folder is missing from disk is not refused: `focusCanBeParsed` never reads the filesystem, so that case reaches `GraphMutate.setParseState`'s `Ok ()` arm exactly as before.

## 5. Tests

`tests/Server.Tests/GithubTransportActorTests.fs` carries the three cases the ticket's Done items name:

- `` git Save on an unparsed file is refused and the file on disk is unchanged `` writes real text to a File focus's disk path, marks that focus Unparsed, Saves it, and asserts `ActorFailed "focused node is unparsed"`, an empty `steps` list (no commit, pull, or push ran), the disk text unchanged, and the Node still `ParseState.Unparsed`.
- `` git Save on an unparsed folder is refused and the folder on disk is unchanged `` does the same for an owned Directory focus with a real file inside its folder.
- `` git Save on a Parsed folder missing from disk is not refused `` Saves a freshly created (Parsed) Directory focus whose folder was never created on disk, and asserts the Save succeeds and commits, pulls, and pushes as usual.

These three tests exercise exactly the Unparsed File refusal, the Unparsed Directory refusal, and the Parsed missing-folder case going ahead — the three Done items the ticket names, no more and no fewer. No test Saves an Unparsed Workspace focus directly, but `focusCanBeParsed`'s check does not branch on node kind, so the File and Directory tests already exercise the one line a Workspace focus would also reach; this is a coverage-completeness note, not a logic gap (see Findings).

`tests/Server.Tests/LoadSaveCommandTests.fs`'s `` Load of a ROOT file skips git and git Save uses the data directory `` adds one line, `CoreMailboxApi.postNotice host (Notice.NodeParsed fileId)`, with a comment explaining why: that test's Desk Load leaves the File focus Unparsed, and the test harness runs no Parse thread to mark it Parsed the way a production Load would. Before this ticket, the test's Save phase ran on a focus that `setParseState` would flip from Unparsed to Parsed directly; after this ticket, that same Save would be refused unless something marks the focus Parsed first, so the fixture now does that marking explicitly. The test's own assertions — `LoadSavePath.Git`, the stop count, and the recorded pull and push roots — never read file content, so the added line changes no assertion; it supplies the one precondition the guard now requires, and this is the only fixture change in the suite. Every other Save test builds its focus through `FileNodeOps.planCreateOwnedFile` or `FileNodeOps.planCreateOwnedDirectory`, both of which start a node Parsed (`Current`), so no other fixture needed a change.

## 6. Documentation

The ticket, `04-build.md`, and the project.md Decisions entry name `GithubTransportActor.focusCanBeParsed`, `GithubTransportActor.persistFocus`, `GraphMutate.setParseState`, `Notice.NodeUnpersisted`, and `Notice.NodeParsed`; each symbol exists in `src/Server/GithubTransportActor.fs` and `src/Shared/GraphMutate.fs` with the behavior described. No doc names a line number, and none mentions a bot.

`04-build.md`'s Check section records the full-suite result as Server passed 780, skipped 1, total 781, failed 0, and Shared passed 1948, skipped 3, total 1951, failed 0; a fresh `scripts/agent-test.sh --full` run today reproduces those exact counts (section 7, below). The same section's targeted-filter count — `` `scripts/agent-test.sh --filter` for `GithubTransportActorTests`, `GithubTransportLoadPullTests`, `LoadSaveCommandTests`, `LoadSaveSaveCommandTests`, `NodeEventExpandTests`, and `CoreLoopNoticeTests` passed 81, failed 0 `` — does not match a fresh run of that same filter today, which passes 98 with 0 failed (see Findings).

## 7. Test run

`scripts/agent-test.sh --full` with `AMBIT_FULL_SUITE=1`: `tests/Server.Tests` passed 780, skipped 1, total 781, failed 0. `tests/Shared.Tests` passed 1948, skipped 3, total 1951, failed 0.

A separate `dotnet test tests/Server.Tests --filter` run for `GithubTransportActorTests`, `GithubTransportLoadPullTests`, `LoadSaveCommandTests`, `LoadSaveSaveCommandTests`, `NodeEventExpandTests`, and `CoreLoopNoticeTests` passed 98, skipped 0, total 98, failed 0.

## 8. Findings

1. **Low — No test Saves an Unparsed Workspace focus directly.** `GithubTransportActorTests.fs` tests the Unparsed refusal for a File focus and a Directory focus, but not a Workspace focus. `focusCanBeParsed`'s new check matches on `parseState` alone, with no branch by node kind, so the File and Directory tests already exercise the identical line a Workspace focus would reach; closing this gap would add confidence, not change behavior.
2. **Low — `04-build.md`'s targeted-filter test count is stale.** Its Check section records that filter as "passed 81, failed 0"; a fresh run of the same filter today passes 98 with 0 failed, consistent with the two extra Facts the current `GithubTransportActorTests.fs` carries (the Directory-focus refusal and the Parsed-missing-folder tests) over the superseded read-from-disk approach's two Facts. The full-suite counts in the same section are independently confirmed accurate (section 7).

## 9. Verdict

Pass. `GithubTransportActor.focusCanBeParsed` carries the one decided rule — Save refuses an Unparsed File, Directory, or Workspace focus before any persist write, commit, pull, or push — and both Save routes (git Save and Neither Save) run it before either can write. Save writes only its own focus, never a descendant, so a Parsed Directory or Workspace focus with an Unparsed child is not at risk from this path. No route outside `GithubTransportActor.fs` writes a focus's content under Save. Parsed focuses, a pulling File, other `setParseState` refusals, Load, and the missing-folder case all behave as they do on `staging`. The two Low findings above are a coverage-completeness note and a stale test count in `04-build.md`; neither points at a behavior gap.

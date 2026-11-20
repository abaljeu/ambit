# 33 — File Load leaves the File Unparsed: review

Verdict: back to build, then document.

- The code does not implement the Load algorithm for every subject and pre-pick. Finding B1, finding B2, finding B3, finding B4, and finding B5 name each gap.
- The plain File Load and plain Directory Load fix through the web route is correct, and its outcome tests are real. Finding T1 names the evidence.
- Save routing is unchanged. Finding S1 names the evidence.
- The spec, arch, map, user doc, and ticket 33 still differ from the algorithm or from the code in places. Finding D1 to finding D8 name each one.

Ticket: [33 — File Load leaves the File Unparsed](33-file-load-leaves-file-unparsed.md)

Pull request: 632 — Restore the Load rule: a plain File or Directory Load parses from disk, with git only for a Workspace. Branch `cursor/revert-load-spec-nonworkspace-404e`. Reviewed head `399311c2`. The last source and test commit is `d18369f1`. `399311c2` changes only five doc files. Base `staging` at `caea653b`.

## Standard

The owner's Load algorithm is the standard for this review. Ticket 33 states it at `plan/github-transport/issues/33-file-load-leaves-file-unparsed.md:60`.

- Workspace subject: the transport step, then reconcile. The transport step is git Load when a remote exists, desk Load when only a local mapping exists, and nothing otherwise.
- Any other subject (File, Directory, non-subject): reconcile only.
- A failed or skipped desk Load does not matter, because reconcile still runs.

## Trace

Each cell is what the code does. The trace follows `UpdateWorkspaceLoad.prePickForLoad` and `loadOpFor`, `PathPick.plainLoadPath`, `LoadSaveRouting.resolvePath`, `PathPick.resolveCommand`, `LoadSaveStart.startCommand`, `DeskLoadPush.runLoadSave`, `LoadSaveCommandClient.applyResponse`, and `GithubTransportActor.loadActorFunction`, `runLoadBody`, `runLoadChoice`, and `runGitOrNeither`.

| Subject | Plain | Explicit git | Explicit desk |
|---|---|---|---|
| Workspace, including ROOT | Load actor. Remote: pull, mark, `loadNode` Workspace. No remote: Neither, `loadNode` Workspace. Local mapping only: Neither, and no desktop push. | Same as plain with a remote. No remote: Neither. | Desktop and mapping: desktop push, then `stackLoad`. Otherwise: parse push. |
| File or Directory | Web: parse push (reconcile). Desktop and mapping: desktop push first, then `stackLoad`. | Load actor on the enclosing Workspace. Pull when a remote exists. `loadNode` Workspace only. The File stays Unparsed. | Same as plain. |
| Non-subject | Server: nothing. | Inside a Workspace: Load actor pulls the Workspace. Outside: Neither with label `SYSTEM`. | Server: nothing. |

The Workspace row matches the standard when the transport step succeeds. The other two rows match only in the Plain column on the web.

## Findings

### Build

- **B1 — Explicit git Load of a File or a Directory pulls the Workspace and leaves the File Unparsed.** Severity: High. `src/Shared/PathPick.fs:103-104` sends explicit Git to the Load actor for every subject. `src/Server/GithubTransportActor.fs:306-310` `runLoadChoice` ignores the subject (`Some _`). `src/Server/GithubTransportActor.fs:160-171` `startParse` and `finishLoadOn` load only `target.workspaceId`. A scratch test on `d18369f1` (explicit Git Load of a File through `Api.postLoadSaveCommand` on a host with a parse thread, no remote) ends `ActorSucceeded` and fails `assertParsed` with `plain Load left the File Unparsed with no children`. That is the ticket 33 bug on the explicit git pre-pick. The standard says reconcile only.
- **B2 — Explicit git Load of a non-subject inside a Workspace pulls the whole Workspace.** Severity: High. `src/Server/GithubTransportActor.fs:507-509` resolves the focus to `LoadSubject.enclosingSubject`, which includes the Workspace, so `runGitOrNeither` pulls. The standard says reconcile only.
- **B3 — Plain Load and desk Load of a File or a Directory on the desktop run desk Load first.** Severity: High. `src/Client/UpdateWorkspaceLoad.fs:138-147` and `:177-179` plan an upload for every non-Git pre-pick. `src/Shared/WorkspaceUpload.fs:82-92` returns `DesktopPush` for a File, Directory, or Workspace target when the desktop can push and a local mapping exists. `holdFor` sets `holdStack`, so `src/Server/Core/DeskLoadPush.fs:13-14` skips the parse push. Reconcile runs only from `loadAfterDesktopPush` after a successful push (`src/Client/UpdateWorkspaceSync.fs:233-266`). The standard says reconcile only for these subjects.
- **B4 — Reconcile is skipped when the transport step fails.** Severity: Medium. A failed desktop push ends in `failWorkspacePush` with no `stackLoad` (`src/Client/UpdateWorkspaceSync.fs:252`, `:261`, `:265-266`). A `remoteExists` error other than `not a git repository` returns the error with no reconcile (`src/Server/GithubTransportActor.fs:299-304`). A failed commit, fetch, or merge returns before `startParse` (`src/Server/GithubTransportActor.fs:245-248`). The standard says reconcile still runs.
- **B5 — Plain Load and desk Load of a non-subject reconcile nothing.** Severity: Medium. `src/Server/Core/DeskLoadPush.fs:16-21` returns `None` when the focus is not a Load subject, so no parse push happens. `plan/github-transport/spec.md:22` and `plan/github-transport/issues/33-file-load-leaves-file-unparsed.md:58` say a non-subject only reconciles.
- **B6 — Plain Workspace Load with only a local mapping runs no desk Load.** Severity: Low. The client plans `DesktopPush` and sets `holdStack` (`src/Client/UpdateWorkspaceLoad.fs:138-147`). `PathPick.resolveCommand` returns Git for a plain Workspace Load (`src/Shared/PathPick.fs:103-104`), so the Load actor runs Neither and the response path is Git. `LoadSaveCommandClient.applyResponse` calls `commandDone`, not `continueDesk` (`src/Client/LoadSaveCommandClient.fs:31-35`), so the desktop push never starts. Reconcile runs. The standard accepts a desk Load that does nothing. The docs say desk Load runs (finding D1).

### Tests

- **T1 — The two new outcome tests check the outcome through the production route and fail on the regressing code.** Severity: Info. ``plain Load of a File parses that file from disk`` (`tests/Server.Tests/LoadSaveCommandTests.fs:892`) and ``plain Load of a File or Directory with a remote parses and does not pull`` (`:912`) post through `Api.postLoadSaveCommand` with `LoadSaveRouting.resolvePath` and `LoadSaveStart.startCommand` on `createParsingHost` (`:153`), and assert the File is Current with its child text. With `src/Shared/PathPick.fs` from `caea653b` (the routing that `d57b5be6` introduced), all six restored or new route tests fail. Each fails first at the route assertion `Expected: Desk, Actual: Git` (`:876`, `:541`, `:1103`, and `tests/Server.Tests/ParseThreadLoadTests.fs:217`). With that route assertion removed, both outcome tests still fail at `assertParsed` (`:888`) with `plain Load left the File Unparsed with no children`.
- **T2 — Some restored tests check routing or the parse push, not the parse result.** Severity: Low. ``plain Load of a non-Workspace returns Desk when a remote exists`` (`tests/Server.Tests/LoadSaveCommandTests.fs:522`) checks the path and that no actor started. ``plain Load of a File or Directory pushes that node onto the parse stack`` (`tests/Server.Tests/ParseThreadLoadTests.fs:362`) checks the push list. ``plain Load of a Directory does not leave a cancelable actor`` (`tests/Server.Tests/ParseThreadLoadTests.fs:231`) checks the live set. The remote test at `tests/Server.Tests/LoadSaveCommandTests.fs:912` checks no outcome for the Directory.
- **T3 — No test checks the outcome of explicit git Load or explicit desk Load of a File or a Directory, or the client upload plan for a File.** Severity: Medium. That gap leaves finding B1 and finding B3 unseen. `UpdateWorkspaceLoad.loadOpFor` has no test for a File or a Workspace with a desktop mapping.
- **T4 — Existing tests pin behavior that differs from the standard.** Severity: Medium. ``explicit Git Load of a non-repo Directory is Neither`` (`tests/Server.Tests/LoadSaveCommandTests.fs:943`) requires the Load actor for a Directory. ``git Load pulls a staging checkout and does not reconcile`` (`:753`) and ``git Load does not reconcile a disk file into the graph`` (`:1149`) post an explicit git Load from `seed.focusId`, a Normal node, and require the pull. ``Load actor fails when git remote fails besides no repository`` (`:1030`) requires `ActorFailed` with no reconcile.
- **T5 — The new parse wait uses time as the mechanism.** Severity: Low (Judgement). `waitForParsedFile` (`tests/Server.Tests/LoadSaveCommandTests.fs:855-864`) polls with `Task.Delay 10` to a 5 second deadline (`:887`). The tdd skill rule is "No time as a mechanism". The loop also repeats the shape of `waitForStopCount` (`:332-339`), which is Duplicated Code.

### Save

- **S1 — Save routing is unchanged.** Severity: Info. `plainLoadStaysDesk` requires `LoadSaveOperation.Load` (`src/Shared/PathPick.fs:89-92`). `plainSavePath` (`src/Shared/PathPick.fs:124-135`) and `src/Client/UpdateSave.fs` are not in the diff. In ``Load of a ROOT file skips git and git Save uses the data directory`` (`tests/Server.Tests/LoadSaveCommandTests.fs:1070`), the Save half is the same except that the stop count starts at 0 (`:1133`, `:1135`), because the Load no longer posts a stop.

### Docs

- **D1 — The docs say a plain Workspace Load with only a local mapping runs desk Load.** Severity: Low. `plan/github-transport/spec.md:22` (Solution 5 — Load and Save), `plan/github-transport/spec.md:43` (story 3 — Server-git when a remote exists), `plan/github-transport/spec.md:54` (story 14 — Plain Load prefers git), `plan/github-transport/map.md:45` (decision 9 — When pull and push fire), `plan/github-transport/arch.md:108` (path 14 — Plain Load prefers git, step 3), and `doc/user/reference/commands/files.md:21`. The code skips that desk Load (finding B6). `plan/github-transport/map.md:44` (decision 8 — Actor command surface) still says "git when a remote exists, else desk", which also differs from "nothing otherwise".
- **D2 — The docs say explicit git Load from a File, a Directory, or a subnode pulls the Workspace.** Severity: Medium. `plan/github-transport/spec.md:27` (Solution 10 — Git Load/Save are Workspace-scoped), `plan/github-transport/spec.md:54` ("Explicit git Load still pulls the Workspace"), `plan/github-transport/map.md:44` ("The git actor starts for a Workspace Node, or for explicit git Load"), `plan/github-transport/map.md:47` (decision 11 — Git Load/Save are Workspace-scoped), `plan/github-transport/arch.md:63` (module 8 — Load, step 4), and `doc/user/reference/commands/files.md:21` ("git Load pulls files when a git upstream is defined"). The standard says reconcile only for a subject that is not a Workspace.
- **D3 — Solution 10 cites a function that does not exist.** Severity: Medium. `plan/github-transport/spec.md:27` ends with `src/Shared/PathPick.fs` `PathPick.effectiveLoadPath`. No source file defines it. Ticket 33 states that `d57b5be6` deleted it (`plan/github-transport/issues/33-file-load-leaves-file-unparsed.md:35`).
- **D4 — The arch PathPick interface omits `plainLoadPath`.** Severity: Low. `plan/github-transport/arch.md:206` says plain Load asks `PathPick.resolveCommand`. The client still calls `PathPick.plainLoadPath` (`src/Client/UpdateWorkspaceLoad.fs:155`).
- **D5 — Ticket 33 calls ROOT a non-subject.** Severity: Low. `plan/github-transport/issues/33-file-load-leaves-file-unparsed.md:58` says "no subject (ROOT with nothing selected)". `GraphBuild` creates ROOT as `Special Workspace` (`src/Shared/GraphBuild.fs:52`), so `LoadSubject.ofId` returns Workspace, and a plain ROOT Load enters the Load actor. The pull request body says the same thing about ROOT.
- **D6 — Ticket 33 describes explicit git Load as unchanged and omits that it keeps the bug.** Severity: Medium. `plan/github-transport/issues/33-file-load-leaves-file-unparsed.md:41` says explicit git Load is unchanged. The Bug section (`:9-11`) is "A person runs Load on a File Node". Finding B1 shows the explicit git Load of a File still leaves it Unparsed.
- **D7 — Ticket 33 is `done` before review and is not in the ticket template.** Severity: Low. `plan/github-transport/issues/33-file-load-leaves-file-unparsed.md:3` and `plan/github-transport/project.md:43` say `done`. The file has no One job, Inputs, Process, Outputs, Stop when, or Check section from `plan/context/ticket-work.md`. Recent tickets use a ticket folder, for example `plan/github-transport/issues/32-route-plain-save/`.
- **D8 — Repeated text.** Severity: Low. `.agents/rules/writing-markdown.md` Repetition.
  - `plan/github-transport/arch.md:108` (path 14, step 3) restates the desk Load message, parse push, and subject list from `plan/github-transport/arch.md:64` (module 8, step 5).
  - `plan/github-transport/issues/33-file-load-leaves-file-unparsed.md:60` restates the algorithm from `plan/github-transport/spec.md:22`.
  - The user doc says a plain File Load or Directory Load only reconciles (`doc/user/reference/commands/files.md:21`). On the desktop with a local mapping, it uploads first (finding B3).

### Unrequested edits

- **U1 — `Updated:` dates bumped.** Severity: Low. `plan/github-transport/spec.md:3`, `plan/github-transport/arch.md:4`, and `plan/github-transport/project.md:5` say `2026-11-20`.
- **U2 — Two dated project entries.** Severity: Low. `plan/github-transport/project.md:42-43`. The first entry restates the spec rule from `plan/github-transport/spec.md:22`.
- **U3 — A Comments section on a ticket in another Project.** Severity: Low (Judgement). `plan/actor-as-client/issues/23-run-the-load-actor-for-git-and-neither/23-run-the-load-actor-for-git-and-neither-ticket.md:89-92`, on 23 — Run the Load actor for Git and Neither.

### Code

- **C1 — `plainLoadStaysDesk` tests the pre-pick twice.** Severity: Low (Judgement, Overcomplication). `src/Shared/PathPick.fs:91` checks `prePick = Plain` inside the `LoadSavePrePick.Plain when` guard at `:101`.
- **C2 — Stale comments.** Severity: Low.
  - `src/Server/LoadSaveRouting.fs:16-17` says Desk is an explicit pre-pick. A plain Load of a File or a Directory now resolves to Desk.
  - `src/Server/Core/DeskLoadPush.fs:23` says a plain Load is not moved onto Desk. `PathPick.resolveCommand` now does that.
  - `src/Client/UpdateWorkspaceLoad.fs:149` says plain Load sends Git when the workspace has both mappings. `plainLoadPath` now does that only for a Workspace subject.
  - `src/Server/GithubTransportActor.fs:306` says Workspace, Directory, and File share the remote check. A plain Directory Load or plain File Load no longer reaches it.
- **C3 — `plainLoadPath` repeats a server decision.** Severity: Low (Judgement, Duplicated Code). `src/Shared/PathPick.fs:139-151` and `src/Client/UpdateWorkspaceLoad.fs:150-162` upgrade a plain Workspace Load to Git. `PathPick.resolveCommand` already sends a plain Workspace Load to the Load actor, and `runGitOrNeither` checks the remote.
- **C4 — A test file over the size limit grows.** Severity: Low (Hard). `tests/Server.Tests/LoadSaveCommandTests.fs` goes from 1593 to 1603 lines. `.agents/rules/fsharp-source.md` sets 800 lines per file and asks for a split when a change increases a longer file.
- **C5 — A test name is over the line limit.** Severity: Low (Hard). `tests/Shared.Tests/PathPickTests.fs:166` is 120 characters. `.agents/rules/fsharp-source.md` sets 100.

## Test runs

Both suites ran on `d18369f1` with `DOTNET_ThreadPool_ForceMinWorkerThreads=64` and `COMPlus_ThreadPool_ForceMinWorkerThreads=64` through `scripts/agent-test.sh --full`. `399311c2` changes no source or test file.

| Suite | Passed | Failed | Skipped | Total |
|---|---|---|---|---|
| `tests/Server.Tests` | 777 | 0 | 1 | 778 |
| `tests/Shared.Tests` | 1947 | 0 | 3 | 1950 |

- The Server skip is ``Change.upload structure persistence preserves existing file mtime``.
- The Shared skips are ``reuses existing owned path without duplicating``, ``planServerFilePresentOps transitions only matching absent files``, and ``listForPush without git still returns walk``.
- `LoadSaveCommandTests` and `ParseThreadLoadTests` with `src/Shared/PathPick.fs` from `caea653b`: 42 passed, 6 failed. The six are the restored or new plain Load route tests (finding T1).
- `scripts/standards-scan.py --diff origin/staging` on `399311c2` prints `tests/Shared.Tests/PathPickTests.fs:166 LONG (120)` and `plainLoadStaysDesk` at 5 lines.

## Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- **Problem.** `PathPick.choose` and `PathPick.resolve` (`src/Shared/PathPick.fs:65-80`) have no caller outside `tests/Shared.Tests/PathPickTests.fs`. **Fix.** Delete them and their tests, or give them a production caller.
- **Problem.** `CoreActorPool.startLoadSaveCommand` (`src/Server/Core/CoreActorPool.fs:242-251`) ignores its arguments and returns `Ok None` for both paths. **Fix.** Remove it from `DeskLoadPush.runLoadSave`, or give it a job.
- **Problem.** `.cursor/rules` points at `.agents/rules/no-retrofit.md`, `.agents/rules/gambol.md`, `.agents/rules/project-values.md`, `.agents/rules/environment.md`, `.agents/rules/project-stage.md`, and `.agents/rules/core-agent-behavior.md`. None of these files exists. The cloud-agent-git skill path `.agents/skills/cloud-agent-git/SKILL.md` does not exist. **Fix.** Restore those files, or remove the pointers.

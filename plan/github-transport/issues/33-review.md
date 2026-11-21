# 33 — File Load leaves the File Unparsed: review

Verdict: back to document.

- The code at `d23b0a11` implements the owner's Load rules across `PathPick.resolveCommand`, `PathPick.plainLoadPath`, `UpdateWorkspaceLoad` (`prePickForLoad`, `plannedUpload`), `LoadSaveRouting.resolvePath`, `DeskLoadPush.runLoadSave`, and `GithubTransportActor`, and leaves Save routing unchanged.
- Full Server and Shared suites pass with `DOTNET_ThreadPool_ForceMinWorkerThreads=64` and `COMPlus_ThreadPool_ForceMinWorkerThreads=64`. The outcome tests exercise the production route, assert post-Load graph and disk state, would fail on `d57b5be6` routing, and every removed test was obsolete under the owner's rules. All modified source files and split test files are under 800 lines, and no modified or added line exceeds 100 characters.
- Two low-severity code cleanups remain: `LoadSubject.enclosingSubject` (`src/Shared/PathPick.fs:33-47`) is now dead code, and `UpdateWorkspaceLoad.subjectsForLoad` (`src/Client/UpdateWorkspaceLoad.fs:125-136`) returns `[]` for explicit git Load of a non-Workspace, skipping owning-file resolution and multi-selection.
- The document step is required: spec, arch, map, project, user reference, and [33 — File Load leaves the File Unparsed](33-file-load-leaves-file-unparsed.md) still contradict the owner's rules in multiple places.

Pull request: [632 — Restore the Load rule: a plain File or Directory Load parses from disk, with git only for a Workspace](https://github.com/abaljeu/ambit/pull/632). Branch `cursor/revert-load-spec-nonworkspace-404e`, reviewed head `d23b0a11`, base `caea653b`.

## Findings

| ID | Category | Location | Severity | Finding |
|---|---|---|---|---|
| C1 | Code | `src/Shared/PathPick.fs:33-47` | Low | `LoadSubject.enclosingSubject` and its private helper `isSubjectNode` have no callers left after `d23b0a11` removed the call in `GithubTransportActor.fs`. |
| C2 | Code | `src/Client/UpdateWorkspaceLoad.fs:125-136` | Low | `subjectsForLoad` matches `LoadSavePrePick.Git, _` and returns `[]` even for a non-Workspace focus, so explicit git Load on a Normal node inside a File or on multiple selected Files sends one raw focus node instead of resolving `WorkspaceUpload.loadSubjects` (unlike `plannedUpload`, which already distinguishes `Git` on Workspace vs non-Workspace). |
| C3 | Code (pre-existing) | `src/Shared/PathPick.fs:66-80` | Info | `PathPick.choose` and `PathPick.resolve` have no production callers outside `tests/Shared.Tests/PathPickTests.fs`. |
| C4 | Code (pre-existing) | `src/Server/Core/CoreActorPool.fs:242-251` | Info | `CoreActorPool.startLoadSaveCommand` ignores its arguments and returns `Ok None` on both paths. |
| T1 | Tests | `tests/Server.Tests/LoadSaveCommandTests.fs:271-489` | Info | Outcome tests run through `Api.postLoadSaveCommand`, `LoadSaveRouting.resolvePath`, and `LoadSaveStart.startCommand` on `createParsingHost`, asserting File and Directory end `Current` with children, no pull of a newer remote commit, Workspace pull plus reconcile, and failed pull aborting before reconcile; all fail on `d57b5be6` routing. All 14 removed tests asserted pre-rule non-Workspace actor/pull behavior and were genuinely obsolete. |
| T2 | Tests | `tests/Server.Tests/LoadSaveCommandTests.fs:291-319` | Low | `plain Load of a File or Directory with a remote parses and does not pull` asserts `assertParsed` for `fileId` and no remote pull, but does not assert `assertDirectoryParsed` for `dirId` (Directory reconcile is covered by `explicit git Load of a Directory reconciles disk and does not pull` at `:356`). |
| T3 | Tests | `tests/Server.Tests/LoadSaveCommandFixtures.fs:481-489` | Low | `waitUntilPredicate` polls with `Task.Delay 10` to a 5-second deadline, repeating the polling pattern in `waitForStopCount`. |
| S1 | Save | `src/Shared/PathPick.fs:89-103`, `src/Shared/PathPick.fs:122-133`, `src/Client/UpdateSave.fs:60-90` | Info | Save routing is unchanged: `nonWorkspaceLoad` gates on `LoadSaveOperation.Load`, `plainSavePath` and `UpdateSave.fs` are untouched, and all Save tests in `tests/Server.Tests/LoadSaveSaveCommandTests.fs` pass. |
| K1 | Known / out of scope | `src/Client/UpdateWorkspaceLoad.fs:148`, `src/Shared/WorkspaceUpload.fs:82-92` | Info | Plain and desk Load of a File or Directory on Desktop with a local mapping still plan `DesktopPush` before reconcile. Out of scope per owner's rules. |
| K2 | Known / out of scope | `src/Server/Core/DeskLoadPush.fs:18-20` | Info | Desk Load of a non-subject returns `None` from `subjectToLoad` and reconciles nothing. Out of scope per owner's rules. |
| K3 | Known / out of scope | `src/Shared/PathPick.fs:95-103`, `src/Client/LoadSaveCommandClient.fs:31-35` | Info | Plain Load of a local-only Workspace (no remote, local mapping only) enters the Load actor Neither arm and does not run desktop upload. Out of scope per owner's rules. |
| D1 | Doc (spec) | `plan/github-transport/spec.md:22` | Medium | Solution 5 — Load and Save restricts reconcile-only to Plain Load of a File, Directory, or non-subject, and states that git Load starts the Server Actor without restricting git pull to a Workspace subject. |
| D2 | Doc (spec) | `plan/github-transport/spec.md:23-24` | Medium | Solution 6 — Person-started only and Solution 7 — Thin handoff to Core restrict the no-git parse-stack rule to Plain Load of a File or Directory, omitting explicit git Load of non-Workspace subjects. |
| D3 | Doc (spec) | `plan/github-transport/spec.md:27` | Medium | Solution 10 — Git Load/Save are Workspace-scoped states that explicit git Load from a subnode pulls the whole Workspace, contradicting the Workspace-only pull rule, and cites deleted `PathPick.effectiveLoadPath`. |
| D4 | Doc (spec) | `plan/github-transport/spec.md:42` | Low | User Story 2 — Every Workspace may connect cites `PathPick.choose`, which is unused in production. |
| D5 | Doc (spec) | `plan/github-transport/spec.md:43` | Medium | User Story 3 — Server-git when a remote exists restricts reconcile-only to Plain Load of a File, Directory, or non-subject, omitting explicit git Load. |
| D6 | Doc (spec) | `plan/github-transport/spec.md:50` | Medium | User Story 10 — git Load states that explicit git Load merges the tracked remote branch even from a subnode, contradicting the Workspace-only pull rule. |
| D7 | Doc (spec) | `plan/github-transport/spec.md:54` | Medium | User Story 14 — Plain Load prefers git states "Explicit git Load still pulls the Workspace" without restricting to a Workspace subject, and states "Reconcile runs whether or not the transport step did anything" without stating that a failed step aborts before reconcile. |
| D8 | Doc (spec) | `plan/github-transport/spec.md:64` | Medium | User Story 24 — Workspace-scoped git states that git Load from a subnode pulls the whole Workspace work tree, contradicting the Workspace-only pull rule. |
| D9 | Doc (arch) | `plan/github-transport/arch.md:63` | Medium | Story path 8 — Load, step 4 states that PathPick git invokes the Server Actor whenever the pre-pick is explicit git Load, contradicting the Workspace-only pull rule. |
| D10 | Doc (arch) | `plan/github-transport/arch.md:72-75` | Medium | Story path 10 — git Load, steps 1–3 state that PathPick is skipped and the Server Actor runs for explicit git Load without restricting to a Workspace subject. |
| D11 | Doc (arch) | `plan/github-transport/arch.md:107-108` | Medium | Story path 14 — Plain Load prefers git, steps 2–3 restrict reconcile-only to Plain Load of a File, Directory, or non-subject, and omit that a failed transport step aborts before reconcile. |
| D12 | Doc (arch) | `plan/github-transport/arch.md:160` | Medium | Story path 24 — Workspace-scoped git, step 1 states that git Load from a subnode uses whole-tree pull, contradicting the Workspace-only pull rule. |
| D13 | Doc (arch) | `plan/github-transport/arch.md:191`, `plan/github-transport/arch.md:310` | Low | Narrowest shared test seam (PathPick entry) and Seams 1 — PathPick choose restrict no-git to Plain Load of a File, Directory, or non-subject, omitting explicit git Load. |
| D14 | Doc (arch) | `plan/github-transport/arch.md:206-207` | Low | Module map 1 — Command Load/Save omits `PathPick.plainLoadPath` and states that explicit Git skips the plain choice without noting the non-Workspace Desk routing. |
| D15 | Doc (arch) | `plan/github-transport/arch.md:225` | Medium | Module map 2 — PathPick states "`resolveCommand` returns Git for Plain and for Git, and Desk for Desk", contradicting both the rule and the code (non-Workspace Load returns `Desk` for Plain and Git). |
| D16 | Doc (map) | `plan/github-transport/map.md:44` | Medium | Decision 8 — Actor command surface states "Plain Load of a Workspace Node is git when a remote exists, else desk" (missing the default nothing arm), restricts no-git to Plain Load, and states "The git actor starts for a Workspace Node, or for explicit git Load." |
| D17 | Doc (map) | `plan/github-transport/map.md:45` | Medium | Decision 9 — When pull and push fire restricts reconcile-only to Plain Load of a File, Directory, or non-subject, omitting explicit git Load and abort-on-failure. |
| D18 | Doc (map) | `plan/github-transport/map.md:47` | Medium | Decision 11 — Git Load/Save are Workspace-scoped states that explicit git Load from a subnode pulls the whole Workspace work tree, contradicting the Workspace-only pull rule. |
| D19 | Doc (project) | `plan/github-transport/project.md:18`, `plan/github-transport/project.md:42-43` | Low | The 2026-09-26 note still says git vs desk with no default nothing arm; the 2026-11-20 notes restrict no-git to plain Load and mark 33 — File Load leaves the File Unparsed `done` before the document step finishes. |
| D20 | Doc (user) | `doc/user/reference/commands/files.md:21` | Medium | Load reference says "git Load pulls files when a git upstream is defined" without restricting git pull to a Workspace subject, and restricts reconcile-only to Plain Load of a File, Directory, or no selection. |
| D21 | Doc (ticket) | `plan/github-transport/issues/33-file-load-leaves-file-unparsed.md:3`, `:67`, `:69` | Medium | Marks status `done` before the document step; line 67 calls ROOT a non-subject (`GraphBuild` creates ROOT as `Special Workspace` at `src/Shared/GraphBuild.fs:52`, so `LoadSubject.ofId` returns `Workspace` and ROOT enters the Load actor Neither arm); line 69 says "Reconcile runs whether or not the transport step did anything" without stating that a failed step aborts before reconcile. |
| D22 | Doc (other ticket) | `plan/actor-as-client/issues/23-run-the-load-actor-for-git-and-neither/23-run-the-load-actor-for-git-and-neither-ticket.md:91` | Low | Comment 1 restricts the no-git parse-stack rule to plain Load of a File or Directory, omitting explicit git Load of non-Workspace subjects. |

## Test counts

Both full suites ran on `d23b0a11` with `DOTNET_ThreadPool_ForceMinWorkerThreads=64`, `COMPlus_ThreadPool_ForceMinWorkerThreads=64`, and `AMBIT_FULL_SUITE=1` (`dotnet test -nodeReuse:false -p:AMBIT_FULL_SUITE=1`).

| Suite | Passed | Failed | Skipped | Total |
|---|---|---|---|---|
| `tests/Server.Tests` | 778 | 0 | 1 | 779 |
| `tests/Shared.Tests` | 1948 | 0 | 3 | 1951 |

- The Server skip is `DocumentPersistenceTests.Change.upload structure persistence preserves existing file mtime`.
- The Shared skips are `WorkspaceLocalInventoryTests.listForPush without git still returns walk`, `WorkspaceUploadStructureTests.reuses existing owned path without duplicating`, and `WorkspaceUploadStructureTests.planServerFilePresentOps transitions only matching absent files`.
- `python3 scripts/standards-scan.py --diff caea653b` reports zero F# line-length or binding-size violations across all modified and added files.

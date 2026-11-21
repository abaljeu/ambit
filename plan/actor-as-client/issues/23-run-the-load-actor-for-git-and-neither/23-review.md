Check: pass

# 23 — Run the Load actor for Git and Neither

Ticket: [[23-run-the-load-actor-for-git-and-neither-ticket.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

## 1. Standards

1. **Scan.** `python3 scripts/standards-scan.py --diff origin/staging` printed no line and exited 0. HEAD is `staging` at `0914ec25`, so that range is empty.
2. **Ticket diff.** The same scanner on `d57b5be6^..d57b5be6` (pull request 591, already on staging) printed no `LONG`, `TAB`, `MUTABLE`, `BARE_ID`, `PHRASE`, or `FILE` line. Changed bindings stay under 40 lines. `runLoadBody` is 19. `runLoadOn` is 22. `start` is 21.

## 2. Spec

1. **Spec file.** `plan/actor-as-client/spec.md` is absent. The ticket is the spec.
2. **Shape.** `PathPick.resolveCommand` sends a plain Load to `LoadSavePath.Git`. `RouteRegistration.registerStateRoutes` calls `GithubTransportActor.start` with `loadActorFunction`. `finishActor` calls `changes.actorStop`. The posted name is `github-load`.
3. **Git arm.** `runGitOrNeither` calls `runLoadOn` when `remoteExists` returns `Ok true`. `mergeLocked` pulls through `pullTracked`. The actor then calls `reconcileFromChanges` and `loadNode`.
4. **Neither arm.** `Ok false`, a missing directory, and an error whose text contains `not a git repository` call `runNeither`. `runNeither` calls `finishLoadOn` with an empty path list. `finishLoadOn` calls `reconcileFromChanges`, then `loadNode`.
5. **Directory with a remote.** A plain Directory Load uses that remote check on the workspace work tree. ``plain Directory Load with a remote pulls through the Load actor`` goes through `LoadSaveRouting.resolvePath`, asserts `LoadSavePath.Git`, and reads the pulled file `from-remote.txt`. Ticket 23 requires this case. The one-job shape pulls when the work tree is Git, and the ticket says a plain Load enters the Load actor and a work tree with a Git remote pulls, then reconciles. A Directory is one of the three subjects.
6. **Other cases.** No remote, a missing work tree, an explicit Git Load of a Directory that is not a repository, and an object outside a workspace take Neither. Any other `remoteExists` error fails the actor. Explicit Desk stays on `CoreMailboxApi.startLoadSaveCommand`. `DeskLoadPush.runLoadSave` performs no path coercion. Desktop_mapped WebDAV is unused. `saveActorFunction` still commits, pulls, and pushes when the focus has a workspace work tree. An outside-workspace Save returns `Ok ()` and does not push.

## 3. Arch

1. No findings.

## 4. Tests

1. **Suite.** `tests/Server.Tests` passed 776, failed 2, skipped 1. The skip is ``Change.upload structure persistence preserves existing file mtime``. The failures are ``DbAgent serves reads while sweep buffers FIFO mutations then trims`` (`Expected first queued mutation to complete first.`) and ``changes POST rejects non-zero EventId and admits zero`` (`System.TimeoutException`). Run alone, the EventId test passed and the DbAgent test failed again. `tests/Shared.Tests` passed 1936, failed 0, skipped 3. The skips are ``reuses existing owned path without duplicating``, ``planServerFilePresentOps transitions only matching absent files``, and ``listForPush without git still returns walk``. `scripts/check-test.sh` has no execute bit.
2. **Named modules.** The Server skip and the two failures are outside `GithubTransportActorTests`, `LoadSaveCommandTests`, and `ParseThreadLoadTests`. ``plain Directory Load with a remote pulls through the Load actor`` is a `SkippableFact` and was not skipped. Git is on `PATH`.
3. **Outputs.** `githubLoadStarted` requires `Order.StartActor` with `actorName = "github-load"`. The Neither, failure, outside-workspace, and remote-pull tests assert that start, the stop, and whether `pullTracked` ran.

## 5. Miss

1. No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

1. **Problem.** `plan/github-transport/spec.md` still says a plain Load of a File or a Directory stays the parse-stack path when a remote exists. Stories 5, 6, 10, and 14 say that. Staging follows 23 — Run the Load actor for Git and Neither.
   **Fix.** Change those sentences so a plain Load enters the Load actor, and a work tree with a Git remote pulls, then reconciles.
2. **Problem.** ``DbAgent serves reads while sweep buffers FIFO mutations then trims`` fails on staging. After the second queued mutation completes, the first is still incomplete.
   **Fix.** Repair the DbAgent sweep queue, or the test's completion check. This sits outside 23 — Run the Load actor for Git and Neither.

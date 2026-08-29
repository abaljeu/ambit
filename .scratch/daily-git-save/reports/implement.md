# Daily git save — implement

Stage: active. Branch: `w/daily-git-save`, cut from `selective-client-sync`. Tree left dirty (parent did not ask for a commit).

## Behaviour

After listen (`ApplicationStarted`) and after DbAgent `ready.Task` when a DbAgent exists, a background walk runs at most once per UTC day:

1. Cheap stamp read of `SYSTEM/gambol.git-save-day`. If it already holds today's `YYYY-MM-DD`, skip (do not wait for ready, do not run git).
2. Await `whenReady`.
3. Sequential `commitAll` of DataDir when it has `.git` (`GitSave.commitAll`), then each immediate child that already has `.git` (`WorkspaceGit.commitAll`). Message: `gambol: daily autosave`. No `git init`. No F# skip of SYSTEM/TRASH.
4. Write the stamp only after the whole walk succeeds. Failure leaves the stamp unset. Clean repos (`nothing to commit`) are success.

Git never runs on the listen path or the DbAgent startup sweep. `Task.Run` keeps git off the `ApplicationStarted` thread. Git failure is logged and does not flip or delay ready. Ctrl+S `/ambit/save` is unchanged.

## Git lock

Pull, push, and Upload do not serialize git on a repo. `GitGateway` runs pack subprocesses with no mutex; `WorkspaceGit.commitAll` has no lock. No new locking framework. Sequential background after ready is the seam.

## Files

- [[src/Server/DailyGitSave.fs]] — pure plan (`shouldRunToday`, `repoRoots`) plus stamp I/O, discover, walk, `start`, `register`.
- [[src/Server/DbAgent.fs]] — `whenReady: Task` from the existing `TaskCompletionSource`.
- [[src/Server/RouteRegistration.fs]] — after persistence, wait for DbAgent `whenReady` when `DbStatus.Ok` (including file+DB mirror, so git does not compete with the sweep on Azure `/home`); otherwise `Task.CompletedTask`.
- [[tests/Server.Tests/DailyGitSaveTests.fs]] — stamp/skip, discover (SYSTEM/TRASH included, nested skipped), failed walk leaves stamp unset, `start` waits for ready, skip does not wait for ready, git `commitAll` DataDir then child.
- [[tests/Server.Tests/TestBackend.fs]] — writes today's stamp before host tests so existing WebApplicationFactory cases do not race daily `commitAll`.

## Tests

Focused `dotnet test tests/Server.Tests -c Debug --filter FullyQualifiedName~DailyGitSaveTests` did not run. `src/Server/bin/Debug` is locked by `netcoredbg.exe` and a running `.NET Host` (fullstack debug). Per environment rules, OutputPath was not redirected. F# CoreCompile of DailyGitSave succeeded (`obj/Debug/net10.0/Gambol.Server.dll` is newer and larger than `bin`).

Stop the debug session, then run the filter above.

## Board mutations

- `add` Active: [[.scratch/daily-git-save/project.md]] — daily background git commit-all after listen and ready.
- `block` that item: focused tests not executed while Server Debug bin is locked. Unblock after the filter is green.

## Suggested commit

```
Add once-per-UTC-day background git commit-all after listen and DbAgent ready.

```

# Daily git save — implement

Stage: active. Branch: `w/daily-git-save`. Tree left dirty (no commit).

## Failure (do not bury)

`walk commitAll DataDir then immediate child` returned `Error`, not `Ok true`. `$"{other}"` on `Result<bool,string>` printed the type name, not the case.

Reproduced at the shell (DataDir `git init`, child `home` `git init`, unborn HEAD, then `git add -A` in DataDir):

```
error: 'home/' does not have a commit checked out
fatal: adding files failed
```

That is the realistic production shape: DataDir `.git` plus immediate workspace `.git` directories. Parent `git add -A` cannot add an unborn nested repo as a gitlink. Assertions now print `Error "..."` / `Ok false` via `formatBoolResult`.

Fix: [[src/Server/GitSave.fs]] `commitAll` runs `git add -A -- . ":!child"` for each immediate child that is itself a repo. Nested repos are committed by their own `commitAll`, not as gitlinks. Not a SYSTEM/TRASH skip list.

## Isolation (user spec)

Git is a background subprocess after listen (`ApplicationStarted` + `Task.Run`). It does not wait on DbAgent, FileAgent, SavePrep, or `/state`. Same-disk `git add -A` can still contend with the startup sweep; that is physical I/O, not an API coupling.

An earlier wiring waited on `DbAgent.whenReady`. That wait is removed. `whenReady` / `tryGetCachedDbAgent` are gone.

## Can daily git explain Starting up + Loading?

Only via Azure `/home` I/O if `git add -A` runs during the sweep (first UTC day, no stamp). It cannot hold `isReady` through a mailbox wait. A hang with today's stamp already set is not this job.

## Tests

`dotnet test tests/Server.Tests -c Debug --filter FullyQualifiedName~DailyGitSaveTests` — Passed: 15, Failed: 0.

## Suggested commit

```
Add once-per-UTC-day background git commit-all after listen; exclude nested repos from parent add.

```

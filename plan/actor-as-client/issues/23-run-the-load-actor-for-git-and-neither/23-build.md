# 23 — Run the Load actor for Git and Neither build

Ticket: [23 — Run the Load actor for Git and Neither](23-run-the-load-actor-for-git-and-neither-ticket.md)

## Stage

[x] Stage is build in [23 — Run the Load actor for Git and Neither](CONTEXT.md).
[x] [Sequence](../../sequence.md) status cell is `build`. The Done box stays clear.

## Edits

[x] `runLoadChoice` sends a Workspace, a Directory, and a File through `runGitOrNeither`.
[x] A work tree with a Git remote pulls through `runLoadOn`.
[x] No work tree, no remote, and a directory that is not a repository take `runNeither`.
[x] Any other `WorkspaceGit.remoteExists` error fails the Load actor.
[x] `runNeither` calls `finishLoadOn` with an empty received-path list.
[x] `readFocusWorkspace` is the shared `graphAtEvent` then `workspaceFromFocus` step for `runLoadBody` and `runSaveBody`.
[x] `githubLoadStarted` is the shared `Order.StartActor` scan for `actorName = "github-load"`.
[x] `GithubTransportActor.start` still posts `Order.StartActor` through `CoreMailboxApi.coreMailboxStartActorFromFunction`.
[x] `finishActor` still calls `changes.actorStop`.
[x] `saveActorFunction` is unchanged. The Desk command path is unchanged. Desktop_mapped stays unused.

## Check

[x] Verdict is in [23 — Run the Load actor for Git and Neither check](../../reports/23-run-the-load-actor-for-git-and-neither-check.md).

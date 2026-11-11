# 23 — Run the Load actor for Git and Neither build

Ticket: [23 — Run the Load actor for Git and Neither](23-run-the-load-actor-for-git-and-neither-ticket.md)

## Stage

[x] Stage is build in [23 — Run the Load actor for Git and Neither](CONTEXT.md).
[x] [Sequence](../../sequence.md) status cell is `build`. The Done box stays clear.

## Edits

[x] `PathPick.resolveCommand` sends a plain Directory Load with no Git remote to `LoadSavePath.Git`.
[x] A plain Directory Load with a remote stays `LoadSavePath.Desk`.
[x] A plain File Load and a non-subject stay `LoadSavePath.Desk`.
[x] `GithubTransportActor.runDirectory` pulls through `runLoadOn` when `WorkspaceGit.remoteExists` is true.
[x] The same function runs `runNeither` when the remote is absent or the work tree is not a repository.
[x] `runNeither` calls `finishLoadOn` with an empty received-path list.
[x] A Workspace Load still pulls through `runLoad`. A File Load still calls `loadFocus`.
[x] `GithubTransportActor.start` still posts `Order.StartActor` through `CoreMailboxApi.coreMailboxStartActorFromFunction`.
[x] `finishActor` still calls `changes.actorStop`.
[x] `saveActorFunction` is unchanged. The Desk command path is unchanged.

## Check

[x] Verdict is in [23 — Run the Load actor for Git and Neither check](../../reports/23-run-the-load-actor-for-git-and-neither-check.md).

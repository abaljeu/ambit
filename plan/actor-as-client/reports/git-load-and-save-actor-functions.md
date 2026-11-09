# Git Load and Save actor functions

## Signatures

[ ] `GithubTransportActor.loadActorFunction` is `string -> GithubTransportActorDependencies -> NodeId -> Gambol.Shared.ActorFunction`.
[ ] `GithubTransportActor.saveActorFunction` is `string -> GithubTransportActorDependencies -> NodeId -> Gambol.Shared.ActorFunction`.
[ ] `Gambol.Shared.ActorFunction` is `FunctionRun -> ActorChanges -> Async<unit>`.
[ ] The `NodeId` argument is `focusId`. Each function closes over it before `CoreMailboxApi.coreMailboxStartActorFromFunction`.

## Call sites

[ ] `GithubTransportActor.start` posts both operations through `CoreMailboxApi.coreMailboxStartActorFromFunction`.
[ ] Load uses `loadActorFunction` and actor name `github-load`.
[ ] Save uses `saveActorFunction` and actor name `github-save`.
[ ] `actorNodeIdOf` is `fun item -> item.focusId` for both.
[ ] Load reads that same `focusId`. The workspace id is derived from it for the pull and for `loadNode`.
[ ] [RouteRegistration.fs](../../../src/Server/RouteRegistration.fs) calls `GithubTransportActor.start` for a Git `/ambit/load-save-command`.
[ ] Desk load and save stay on `CoreMailboxApi.startLoadSaveCommand`.
[ ] The pool does not register `github-load` or `github-save`.

## Check

[ ] `scripts/agent-test.sh --filter FullyQualifiedName~GithubTransportActorTests|FullyQualifiedName~LoadSaveCommandTests server`
[ ] Passed 25, failed 0, skipped 0.

namespace Gambol.Server

open Gambol.Shared

type internal CoreMsg =
    | GetState of AsyncReplyChannel<Result<State, string>>
    | GetRevision of AsyncReplyChannel<Result<Revision, string>>
    | GetChangesSince of
        after: Revision * AsyncReplyChannel<Result<Change list, string>>
    | GetEventHistory of AsyncReplyChannel<HistoryEvent list>
    | PostChange of
        caller: Caller *
        changes: Change list *
        AsyncReplyChannel<Result<CoreChangesAccepted, string>>
    | PostGraphOnlyChange of
        caller: Caller *
        changes: Change list *
        AsyncReplyChannel<Result<CoreChangesAccepted, string>>
    | Logout of
        Caller *
        AsyncReplyChannel<Result<unit, string>>
    | SnapshotDone of graph: Graph option
    | StartActor of
        caller: Caller *
        request: StartActorRequest *
        AsyncReplyChannel<Result<unit, string>>
    | ActorStop of
        caller: Caller *
        result: ActorResult *
        AsyncReplyChannel<Result<unit, string>>
    | Login of
        Caller *
        AsyncReplyChannel<Result<unit, string>>
    | AdmitCaller of Caller * AsyncReplyChannel<bool>

type PersistHandlers = {
    getState: unit -> Result<State, string>
    getRevision: unit -> Result<Revision, string>
    getChangesSince: Revision -> Result<Change list, string>
    postChange:
        Change list -> Result<CoreChangesAccepted, string>
    postGraphOnlyChange:
        Change list -> Result<CoreChangesAccepted, string>
    snapshotDone: Graph option -> unit
}

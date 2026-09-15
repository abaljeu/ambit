namespace Gambol.Server

open Gambol.Shared

type internal CoreMsg =
    | GetState of AsyncReplyChannel<Result<State, string>>
    | GetRevision of AsyncReplyChannel<Result<Revision, string>>
    | GetChangesSince of
        after: Revision * AsyncReplyChannel<Result<Change list, string>>
    | GetEventHistory of
        AsyncReplyChannel<Gambol.Shared.Events.EventLog>
    | PostGraphOnlyChange of
        caller: Caller *
        change: Change *
        AsyncReplyChannel<Result<CoreChangesAccepted, string>>
    | Logout of
        Caller *
        AsyncReplyChannel<Result<unit, string>>
    | SnapshotDone of graph: Graph option
    | StartActor of
        caller: Caller *
        request: Gambol.Shared.Events.ActorStart *
        AsyncReplyChannel<Result<unit, string>>
    | ActorStop of
        caller: Caller *
        result: ActorResult *
        AsyncReplyChannel<Result<unit, string>>
    | Login of
        Caller *
        AsyncReplyChannel<Result<unit, string>>
    | AdmitCaller of Caller * AsyncReplyChannel<bool>
    | PostEvent of
        caller: Caller *
        event: Gambol.Shared.Events.Event *
        AsyncReplyChannel<
            Result<
                Gambol.Shared.Events.Event *
                CoreChangesAccepted option,
                string>>
    | EventsSince of
        after: Gambol.Shared.Events.EventId *
        AsyncReplyChannel<Gambol.Shared.Events.EventLog>

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

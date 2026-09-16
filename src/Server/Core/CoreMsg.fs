namespace Gambol.Server

open Gambol.Shared

type internal CoreMsg =
    | GetState of AsyncReplyChannel<Result<State, string>>
    | GetRevision of AsyncReplyChannel<Result<Revision, string>>
    | GetEventsSince of
        after: Gambol.Shared.EventId *
        AsyncReplyChannel<
            Result<Gambol.Shared.Ev list, string>>
    | GetEventHistory of
        AsyncReplyChannel<Gambol.Shared.EventLog>
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
        request: Gambol.Shared.ActorStart *
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
        event: Gambol.Shared.Ev *
        AsyncReplyChannel<
            Result<
                Gambol.Shared.Ev *
                CoreChangesAccepted option,
                string>>
    | EventsSince of
        after: Gambol.Shared.EventId *
        AsyncReplyChannel<Gambol.Shared.EventLog>

type PersistHandlers = {
    getState: unit -> Result<State, string>
    getRevision: unit -> Result<Revision, string>
    getEventsSince:
        Gambol.Shared.EventId
            -> Result<Gambol.Shared.Ev list, string>
    appendEvent:
        Gambol.Shared.Ev -> Result<unit, string>
    postChange:
        Change list -> Result<CoreChangesAccepted, string>
    postGraphOnlyChange:
        Change list -> Result<CoreChangesAccepted, string>
    snapshotDone: Graph option -> unit
}

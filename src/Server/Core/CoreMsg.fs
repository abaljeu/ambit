namespace Gambol.Server

open Gambol.Shared

type internal CoreMsg =
    | GetState of AsyncReplyChannel<Result<State, string>>
    | GetEventId of AsyncReplyChannel<Result<EventId, string>>
    | GetEventsSince of
        after: Gambol.Shared.EventId *
        AsyncReplyChannel<
            Result<Ev list, string>>
    | GetEventHistory of
        AsyncReplyChannel<Gambol.Shared.EventLog>
    | PostGraphOnly of
        caller: Caller *
        event: Ev *
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
        event: Ev *
        AsyncReplyChannel<
            Result<
                Ev *
                CoreChangesAccepted option,
                string>>
    | EventsSince of
        after: Gambol.Shared.EventId *
        AsyncReplyChannel<Gambol.Shared.EventLog>

type PersistHandlers = {
    getState: unit -> Result<State, string>
    getEventId: unit -> Result<EventId, string>
    getEventsSince:
        Gambol.Shared.EventId
            -> Result<Ev list, string>
    getEventLog: unit -> Result<EventLog, string>
    appendEvent:
        Ev -> Result<unit, string>
    applyEvent:
        Ev -> bool -> Result<CoreChangesAccepted, string>
    snapshotDone: Graph option -> unit
}

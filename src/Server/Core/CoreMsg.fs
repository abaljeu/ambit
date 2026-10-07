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
    | StartActor of
        caller: Caller *
        request: Gambol.Shared.ActorStart *
        AsyncReplyChannel<Result<unit, string>>
    | StartPeerActor of
        caller: Caller *
        peerName: PeerActorName *
        request: Gambol.Shared.ActorStart *
        AsyncReplyChannel<Result<unit, string>>
    | StartLoadSaveCommand of
        caller: Caller *
        path: LoadSavePath *
        peerName: PeerActorName *
        request: LoadSaveCommandRequest *
        AsyncReplyChannel<Result<unit, string>>
    /// Search Actor bookkeeping. Records ActorStart. Does not run the walk.
    | RecordSearchStart of
        caller: Caller *
        request: Gambol.Shared.ActorStart *
        AsyncReplyChannel<Result<unit, string>>
    /// Search Actor bookkeeping. Puts ActorStop on the event source.
    /// The id is the root.
    | RecordSearchStop of
        caller: Caller *
        rootId: NodeId *
        AsyncReplyChannel<Result<unit, string>>
    /// Parse-stack Load: subject must be a File node. Fast push only.
    | Load of
        caller: Caller *
        subject: NodeId *
        AsyncReplyChannel<Result<unit, string>>
    | ActorStop of
        caller: Caller *
        result: ActorResult *
        AsyncReplyChannel<Result<unit, string>>
    | CancelActor of
        caller: Caller *
        focusId: NodeId *
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

/// Internal message. Not an Op. No public enqueue door.
type internal InMsg =
    | ParseFinished of nodeId: NodeId
    | SnapshotDone of nodeId: NodeId * graph: Graph option
    | MarkUnparsed of nodeId: NodeId

[<RequireQualifiedAccess>]
type internal QueueSum =
    | Core of CoreMsg
    | In of InMsg

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
    replaceGraph: Graph -> unit
    snapshotDone: Graph option -> unit
}

/// Persist + lifecycle the mailbox hosts. File and Db build this; CoreMailbox
/// does not read agent fields.
type internal PersistFilling = {
    handlers: PersistHandlers
    onError: string -> string -> exn -> unit
    formatError: string -> string
    isReady: unit -> bool
    flushSnapshot: unit -> Async<Result<unit, string>>
    dispose: unit -> unit
    until: Async<Result<unit, string>> option
    bindSnapshot: (InMsg -> unit) -> unit
}

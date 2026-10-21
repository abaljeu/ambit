namespace Gambol.Server

open Gambol.Shared

type internal CoreMsg =
    | PostGraphOnly of
        caller: Caller *
        event: Ev *
        AsyncReplyChannel<Result<CoreChangesAccepted, string>>
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
    /// Parse-stack Load: subject is a Workspace, Directory, or File. Fast push only.
    | LoadNode of
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
    | PostEvent of
        caller: Caller *
        event: Ev *
        AsyncReplyChannel<
            Result<
                Ev *
                CoreChangesAccepted option,
                string>>
[<RequireQualifiedAccess>]
type internal Query =
    | State of AsyncReplyChannel<Result<State, string>>
    | LastEventId of AsyncReplyChannel<Result<EventId, string>>
    | EventsSince of
        after: EventId *
        AsyncReplyChannel<Result<EventLog, string>>

[<RequireQualifiedAccess>]
type internal Mailbox =
    | CoreMsg of CoreMsg
    | PlaceOrder of
        caller: Caller *
        orderId: System.Guid *
        commandName: string option *
        order: Order *
        reply: AsyncReplyChannel<Result<EventId, string>>
    | Ask of Query

[<RequireQualifiedAccess>]
type internal CoreQueue =
    | Public of Mailbox
    | Notice of Notice
    | DropAuthority of stopped: StoppedAdmission
    /// Reset asks the loop to exit after this item. The reply means it exited.
    | Shutdown of AsyncReplyChannel<unit>

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
    noteDiffDone: NodeId -> unit
    noteParsed: NodeId -> unit
}

/// Persist + lifecycle the mailbox hosts. File and Db build this; CoreMailbox
/// does not read agent fields.
type internal PersistFilling = {
    handlers: PersistHandlers
    onError: string -> string -> exn -> unit
    formatError: string -> string
    isReady: unit -> bool
    dispose: unit -> unit
    until: Async<Result<unit, string>> option
    bindPersistFinished: (Notice -> unit) -> unit
}

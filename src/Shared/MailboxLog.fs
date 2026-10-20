namespace Gambol.Shared

type PeerActorName = PeerActorName of string

[<RequireQualifiedAccess>]
type ChangeOrigin =
    | Edit
    | Parse

[<RequireQualifiedAccess>]
type Order =
    | Change of ops: Op list * origin: ChangeOrigin
    | Undo of target: EventId
    | Redo of target: EventId
    | StartActor of ActorStart
    | StartPeerActor of
        peerName: PeerActorName *
        request: ActorStart
    | LoadSave of
        path: LoadSavePath *
        peerName: PeerActorName *
        request: LoadSaveCommandRequest
    | Load of nodeId: NodeId
    | Cancel of focusId: NodeId

[<RequireQualifiedAccess>]
type Notice =
    | NodeParsed of nodeId: NodeId
    | SnapshotDone of nodeId: NodeId
    | MarkUnparsed of nodeId: NodeId
    | NodeUploading of nodeId: NodeId
    | NodeReceived of nodeId: NodeId
    | NodeUnpersisted of nodeId: NodeId

type PlacedOrder = {
    orderId: System.Guid
    authority: Authority
    commandName: string option
    order: Order }

[<RequireQualifiedAccess>]
module MailboxLog =

    type private HistoryEv = Gambol.Shared.Ev
    type private HistoryBody = Gambol.Shared.EventBody

    [<RequireQualifiedAccess>]
    type EventBody =
        | Order of PlacedOrder
        | Notice of Notice

    type Ev = {
        id: EventId
        body: EventBody }

    let private commandFromText (name: string) =
        if name = "" then None else Some name

    let private commandToText (name: string option) =
        match name with
        | None -> ""
        | Some text -> text

    let private placedOrder (event: HistoryEv) (order: Order) = {
        orderId = event.submissionId
        authority = event.authority
        commandName = commandFromText event.commandName
        order = order }

    let private orderOfBody (body: HistoryBody) =
        match body with
        | HistoryBody.Change _ -> Error "origin is not logged"
        | HistoryBody.Undo (target, _) -> Ok (Order.Undo target)
        | HistoryBody.Redo (target, _) -> Ok (Order.Redo target)
        | HistoryBody.ActorStart start ->
            Ok (Order.StartActor start)
        | HistoryBody.Cancel focusId -> Ok (Order.Cancel focusId)
        | HistoryBody.ActorStop _
        | HistoryBody.NodeUnpersisted _ -> Error "unsupported"

    let private historyBody (order: Order) =
        match order with
        | Order.Change(ops, _) -> Ok (HistoryBody.Change ops)
        | Order.Undo target -> Ok (HistoryBody.Undo(target, []))
        | Order.Redo target -> Ok (HistoryBody.Redo(target, []))
        | Order.StartActor start -> Ok (HistoryBody.ActorStart start)
        | Order.Cancel focusId -> Ok (HistoryBody.Cancel focusId)
        | Order.StartPeerActor _
        | Order.LoadSave _
        | Order.Load _ -> Error "unsupported"

    let ofEv (event: HistoryEv) : Result<Ev, string> =
        match orderOfBody event.body with
        | Error err -> Error err
        | Ok order ->
            Ok {
                id = event.id
                body = EventBody.Order (placedOrder event order) }

    let toEv (event: Ev) : Result<HistoryEv, string> =
        match event.body with
        | EventBody.Notice _ -> Error "unsupported"
        | EventBody.Order placed ->
            match historyBody placed.order with
            | Error err -> Error err
            | Ok body ->
                Ok {
                    id = event.id
                    submissionId = placed.orderId
                    authority = placed.authority
                    commandName = commandToText placed.commandName
                    body = body }

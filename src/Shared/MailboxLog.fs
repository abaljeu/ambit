namespace Gambol.Shared

type PlacedOrder = {
    orderId: OrderId
    authority: Authority
    commandName: string option
    order: Order }

[<RequireQualifiedAccess>]
module MailboxLog =

    type private HistoryEv = Gambol.Shared.Ev
    type private HistoryBody = Gambol.Shared.EventBody

    [<RequireQualifiedAccess>]
    type MailboxEventBody =
        | Order of PlacedOrder
        | Notice of Notice

    type Ev = {
        id: EventId
        body: MailboxEventBody }

    let private commandFromText (name: string) =
        if name = "" then None else Some name

    let private commandToText (name: string option) =
        match name with
        | None -> ""
        | Some text -> text

    let private placedOrder (event: HistoryEv) (order: Order) = {
        orderId = OrderId event.submissionId
        authority = event.authority
        commandName = commandFromText event.commandName
        order = order }

    let private orderOfBody (body: HistoryBody) =
        match body with
        | HistoryBody.Change ops ->
            Ok (Order.Change(ops, ChangeOrigin.Edit))
        | HistoryBody.Undo (target, _) -> Ok (Order.Undo target)
        | HistoryBody.Redo (target, _) -> Ok (Order.Redo target)
        | HistoryBody.ActorStart start ->
            Ok (Order.StartActor start)
        | HistoryBody.Load nodeId -> Ok (Order.Load nodeId)
        | HistoryBody.Cancel focusId -> Ok (Order.Cancel focusId)
        | HistoryBody.ActorStop _
        | HistoryBody.Notice _ -> Error "unsupported"

    let private historyBody (order: Order) =
        match order with
        | Order.Change (ops, _) -> Ok (HistoryBody.Change ops)
        | Order.Undo target -> Ok (HistoryBody.Undo(target, []))
        | Order.Redo target -> Ok (HistoryBody.Redo(target, []))
        | Order.StartActor start -> Ok (HistoryBody.ActorStart start)
        | Order.Load nodeId -> Ok (HistoryBody.Load nodeId)
        | Order.Cancel focusId -> Ok (HistoryBody.Cancel focusId)
        | Order.StartPeerActor _
        | Order.LoadSave _ -> Error "unsupported"

    let ofEv (event: HistoryEv) : Result<Ev, string> =
        match orderOfBody event.body with
        | Error err -> Error err
        | Ok order ->
            Ok {
                id = event.id
                body = MailboxEventBody.Order (placedOrder event order) }

    let toEv (event: Ev) : Result<HistoryEv, string> =
        match event.body with
        | MailboxEventBody.Notice _ -> Error "unsupported"
        | MailboxEventBody.Order placed ->
            match historyBody placed.order with
            | Error err -> Error err
            | Ok body ->
                Ok {
                    id = event.id
                    submissionId = placed.orderId.Value
                    authority = placed.authority
                    commandName = commandToText placed.commandName
                    body = body }

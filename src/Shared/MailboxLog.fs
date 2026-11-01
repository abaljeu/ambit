namespace Gambol.Shared

type PlacedOrder = {
    orderId: OrderId
    authority: Authority
    commandName: string option
    order: Order }

[<RequireQualifiedAccess>]
module MailboxLog =

    let private commandFromText (name: string) =
        if name = "" then None else Some name

    let private commandToText (name: string option) =
        match name with
        | None -> ""
        | Some text -> text

    let private orderOfBody (body: EventBody) =
        match body with
        | EventBody.ActorStop _
        | EventBody.Notice _ -> Error "unsupported"
        | EventBody.Order order -> Ok order

    let private historyBody (order: Order) =
        match order with
        | Order.Change _
        | Order.Undo _
        | Order.Redo _
        | Order.Load _
        | Order.StartActor _
        | Order.Cancel _ ->
            Ok (EventBody.Order order)
        | Order.StartPeerActor _
        | Order.LoadSave _ -> Error "unsupported"

    let placedOrderFromEvent (event: Ev) : Result<PlacedOrder, string> =
        match orderOfBody event.body with
        | Error err -> Error err
        | Ok order ->
            Ok {
                orderId = OrderId event.submissionId
                authority = event.authority
                commandName = commandFromText event.commandName
                order = order }

    let eventFromPlacedOrder
        (id: EventId)
        (placed: PlacedOrder)
        : Result<Ev, string> =
        match historyBody placed.order with
        | Error err -> Error err
        | Ok body ->
            Ok {
                id = id
                submissionId = placed.orderId.Value
                authority = placed.authority
                commandName = commandToText placed.commandName
                body = body }

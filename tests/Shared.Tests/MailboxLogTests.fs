module Gambol.Shared.Tests.MailboxLogTests

open System
open Xunit
open Gambol.Shared

let private childOp () =
    Op.NewNode(NodeId.New(), "hello")

let private source commandName body : Ev = {
    id = EventId.zero
    submissionId = Guid.Parse "11111111-1111-1111-1111-111111111111"
    authority = Authority "Browser"
    commandName = commandName
    body = body }

let private readOrder (posted: Ev) =
    match MailboxLog.placedOrderFromEvent posted with
    | Ok value -> value
    | Error err ->
        Assert.Fail(err)
        Unchecked.defaultof<_>

let private restored (posted: Ev) =
    let order = readOrder posted
    match MailboxLog.eventFromPlacedOrder (posted.id) order with
    | Ok value -> value
    | Error err ->
        Assert.Fail(err)
        Unchecked.defaultof<_>

[<Fact>]
let ``placed order then logged event keeps a Change`` () =
    let ops = [ childOp () ]
    let event = source "Set text" (EventBody.Change ops)
    let order = readOrder event
    Assert.Equal(OrderId event.submissionId, order.orderId)
    Assert.Equal(event.authority, order.authority)
    Assert.Equal(Some "Set text", order.commandName)
    Assert.Equal(Order.Change(ops, ChangeOrigin.Edit), order.order)
    let back = restored event
    Assert.Equal(event.id, back.id)
    Assert.Equal(event, back)

[<Fact>]
let ``empty command name maps to None and returns as empty`` () =
    let ops = [ childOp () ]
    let event = source "" (EventBody.Change ops)
    let order = readOrder event
    Assert.Equal(None, order.commandName)
    let back = restored event
    Assert.Equal("", back.commandName)
    Assert.Equal(event, back)

[<Fact>]
let ``Undo and Redo keep the target and the ops`` () =
    let target = EventId.fromJson 4
    let ops = [ childOp () ]
    let undo = source "Undo" (EventBody.Undo(target, ops))
    let redo = source "Redo" (EventBody.Redo(target, ops))
    Assert.Equal(Order.Undo(target, ops), (readOrder undo).order)
    Assert.Equal(Order.Redo(target, ops), (readOrder redo).order)
    Assert.Equal(
        EventBody.Order(Order.Undo(target, ops)),
        (restored undo).body)
    Assert.Equal(
        EventBody.Order(Order.Redo(target, ops)),
        (restored redo).body)

[<Fact>]
let ``Load keeps its node through EventJson and MailboxLog`` () =
    let nodeId = NodeId.New()
    let event =
        { source "" (EventBody.Load nodeId) with
            id = EventId.fromJson 3 }
    let json =
        Thoth.Json.Newtonsoft.Encode.toString 0 (EventJson.encode event)
    match Thoth.Json.Newtonsoft.Decode.fromString EventJson.decode json with
    | Error err -> Assert.Fail(err)
    | Ok decoded -> Assert.Equal(event, decoded)
    Assert.Equal(Order.Load nodeId, (readOrder event).order)
    Assert.Equal(EventBody.Load nodeId, (restored event).body)
    let change =
        { source "Edit" (EventBody.Change [ childOp () ]) with
            id = EventId.fromJson 2 }
    let recorded =
        ClientHistory.clear ()
        |> ClientHistory.recordEvent "Edit" change
        |> ClientHistory.recordEvent "" event
    match ClientHistory.undoEvent recorded with
    | None -> Assert.Fail("expected Undo of the edit")
    | Some (undo, _) ->
        match undo.body with
        | EventBody.Undo(target, _) -> Assert.Equal(change.id, target)
        | _ -> Assert.Fail("expected Undo")

[<Fact>]
let ``StartPeerActor carries Shared PeerActorName`` () =
    let name = PeerActorName "github-load"
    Assert.Equal("Gambol.Shared", typeof<PeerActorName>.Namespace)
    let start = {
        zoomId = Graph.rootId
        focusId = Graph.rootId
        commandId = Graph.rootId
        graphIds = [ Graph.rootId ]
        eventId = EventId.zero }
    let order = Order.StartPeerActor(name, start)
    match order with
    | Order.StartPeerActor (peerName = foundName; request = foundStart) ->
        Assert.Equal(name, foundName)
        Assert.Equal(start, foundStart)
    | _ -> Assert.Fail("expected StartPeerActor")

[<Fact>]
let ``placed order reads EventBody.Order`` () =
    let ops = [ childOp () ]
    let order = Order.Change(ops, ChangeOrigin.Parse)
    let event = source "Parse" (EventBody.Order order)
    Assert.Equal(order, (readOrder event).order)

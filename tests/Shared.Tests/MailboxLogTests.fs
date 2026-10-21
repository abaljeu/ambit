module Gambol.Shared.Tests.MailboxLogTests

open System
open Xunit
open Gambol.Shared

module Enc = Thoth.Json.Newtonsoft.Encode
module Dec = Thoth.Json.Newtonsoft.Decode

let private childOp () =
    Op.NewNode(NodeId.New(), "hello")

let private source commandName body : Ev = {
    id = EventId.zero
    submissionId = Guid.Parse "11111111-1111-1111-1111-111111111111"
    authority = Authority "Browser"
    commandName = commandName
    body = body }

let private logged event =
    match MailboxLog.ofEv event with
    | Ok value -> value
    | Error err ->
        Assert.Fail(err)
        Unchecked.defaultof<_>

let private restored event =
    match MailboxLog.toEv event with
    | Ok value -> value
    | Error err ->
        Assert.Fail(err)
        Unchecked.defaultof<_>

let private placed event =
    match (logged event).body with
    | MailboxLog.EventBody.Order value -> value
    | MailboxLog.EventBody.Notice _ ->
        Assert.Fail("expected Order")
        Unchecked.defaultof<_>

[<Fact>]
let ``ofEv then toEv keeps a Change`` () =
    let ops = [ childOp () ]
    let event = source "Set text" (EventBody.Change ops)
    let order = placed event
    Assert.Equal(event.id, (logged event).id)
    Assert.Equal(event.submissionId, order.orderId)
    Assert.Equal(event.authority, order.authority)
    Assert.Equal(Some "Set text", order.commandName)
    Assert.Equal(Order.Change ops, order.order)
    Assert.Equal(event, restored (logged event))

[<Fact>]
let ``empty command name maps to None and returns as empty`` () =
    let ops = [ childOp () ]
    let event = source "" (EventBody.Change ops)
    let order = placed event
    Assert.Equal(None, order.commandName)
    let back = restored (logged event)
    Assert.Equal("", back.commandName)
    Assert.Equal(event, back)

[<Fact>]
let ``Undo and Redo keep the target and drop the ops`` () =
    let target = EventId.fromJson 4
    let ops = [ childOp () ]
    let undo = source "Undo" (EventBody.Undo(target, ops))
    let redo = source "Redo" (EventBody.Redo(target, ops))
    Assert.Equal(Order.Undo target, (placed undo).order)
    Assert.Equal(Order.Redo target, (placed redo).order)
    Assert.Equal(EventBody.Undo(target, []), (restored (logged undo)).body)
    Assert.Equal(EventBody.Redo(target, []), (restored (logged redo)).body)

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
let ``RecordSearchStart stores ActorStart`` () =
    let start = {
        zoomId = Graph.rootId
        focusId = Graph.rootId
        commandId = Graph.rootId
        graphIds = [ Graph.rootId ]
        eventId = EventId.zero }
    let search = Order.RecordSearchStart start
    let loggedSearch: MailboxLog.Ev = {
        id = EventId.zero
        body =
            MailboxLog.EventBody.Order {
                orderId = Guid.Empty
                authority = Authority "Browser"
                commandName = None
                order = search } }
    match MailboxLog.toEv loggedSearch with
    | Ok restoredSearch ->
        Assert.Equal(EventBody.ActorStart start, restoredSearch.body)
        Assert.Equal(Order.StartActor start, (placed restoredSearch).order)
    | Error err -> Assert.Fail(err)
    let started = source "" (EventBody.ActorStart start)
    Assert.Equal(Order.StartActor start, (placed started).order)
    Assert.Equal(started, restored (logged started))
    let stopped =
        source "Stop" (EventBody.ActorStop(start.focusId, ActorSucceeded))
    match MailboxLog.ofEv stopped with
    | Error err -> Assert.Fail(err)
    | Ok loggedStop ->
        match loggedStop.body with
        | MailboxLog.EventBody.Order placed ->
            let (NodeId guid) = start.focusId
            Assert.Equal(
                Order.ActorStop(guid.ToString(), ActorSucceeded),
                placed.order)
        | MailboxLog.EventBody.Notice _ ->
            Assert.Fail("expected ActorStop")
        match MailboxLog.toEv loggedStop with
        | Ok back ->
            Assert.Equal(stopped.body, back.body)
        | Error err -> Assert.Fail(err)

[<Fact>]
let ``Save keeps its node through EventJson and MailboxLog`` () =
    let nodeId = NodeId.New()
    let event = source "" (EventBody.Save nodeId)
    Assert.Equal(Order.Save nodeId, (placed event).order)
    Assert.Equal(EventBody.Save nodeId, (restored (logged event)).body)
    Assert.Equal(event, restored (logged event))
    let json = Enc.toString 0 (EventJson.encode event)
    match Dec.fromString EventJson.decode json with
    | Ok decoded -> Assert.Equal(event, decoded)
    | Error err -> Assert.Fail(err)

[<Fact>]
let ``Load keeps its node through EventJson and MailboxLog`` () =
    let nodeId = NodeId.New()
    let event = source "" (EventBody.Load nodeId)
    Assert.Equal(Order.Load nodeId, (placed event).order)
    Assert.Equal(EventBody.Load nodeId, (restored (logged event)).body)
    Assert.Equal(event, restored (logged event))
    let json = Enc.toString 0 (EventJson.encode event)
    match Dec.fromString EventJson.decode json with
    | Ok decoded -> Assert.Equal(event, decoded)
    | Error err -> Assert.Fail(err)

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

let private placedChange origin commandName : MailboxLog.Ev =
    let ops = [ childOp () ]
    {
        id = EventId.zero
        body =
            MailboxLog.EventBody.Order {
                orderId = Guid.Parse "11111111-1111-1111-1111-111111111111"
                authority = Authority "Browser"
                commandName = commandName
                order = Order.Change(ops, origin)
            }
    }

let private changeOps (event: MailboxLog.Ev) =
    match event.body with
    | MailboxLog.EventBody.Order placed ->
        match placed.order with
        | Order.Change(ops, _) -> ops
        | _ -> []
    | MailboxLog.EventBody.Notice _ -> []

[<Fact>]
let ``toEv keeps Change ops and drops origin`` () =
    let edit = placedChange ChangeOrigin.Edit (Some "Set text")
    let parse = placedChange ChangeOrigin.Parse None
    let editBack = restored edit
    let parseBack = restored parse
    Assert.Equal(EventBody.Change(changeOps edit), editBack.body)
    Assert.Equal(EventBody.Change(changeOps parse), parseBack.body)
    Assert.Equal("Set text", editBack.commandName)
    Assert.Equal("", parseBack.commandName)
    match MailboxLog.ofEv editBack with
    | Error _ -> ()
    | Ok _ -> Assert.Fail("origin is not on the logged Change")

[<Fact>]
let ``empty command name maps to None and returns as empty`` () =
    let event = placedChange ChangeOrigin.Edit None
    match event.body with
    | MailboxLog.EventBody.Order order ->
        Assert.Equal(None, order.commandName)
    | MailboxLog.EventBody.Notice _ ->
        Assert.Fail("expected Order")
    let back = restored event
    Assert.Equal("", back.commandName)

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

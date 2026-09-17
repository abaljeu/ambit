module Gambol.Server.Tests.Issue41CoreMailboxTests

open System
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error error ->
        Assert.Fail($"{label}: {error}")
        Unchecked.defaultof<_>

let private withHost actors body =
    task {
        let pool = CoreActorPool.create ()
        actors
        |> List.iter (fun (name, actor) -> pool.register name actor)
        let host =
            CoreMailbox.host
                pool
                (FileAgent.persist (FileAgent.create (newTempDir ())))
                admittedCredentials
        try
            do! body host pool
        finally
            CoreMailbox.dispose host
    }

let private addRootChild text = addRootChildEvent text

let private wireEvent submissionId body : Ev =
    { id = EventId.zero
      submissionId = submissionId
      authority = Gambol.Shared.Authority "Wire"
      commandName = "test"
      body = body }

[<Fact>]
let ``CoreMailbox.postEvents appends Change Events to EventLog`` () =
    withHost [] (fun host _ -> task {
        let childId, event = addRootChild "door-change"
        let! accepted =
            CoreMailbox.postEvents host testCaller [ event ]
            |> Async.StartAsTask
        let accepted = requireOk "postEvents" accepted
        let! history =
            CoreMailbox.eventHistory host
            |> Async.StartAsTask
        let stored = Assert.Single(history.events)
        let! state =
            CoreMailbox.getState host
            |> Async.StartAsTask
        let state = requireOk "get state" state
        Assert.Equal(EventId.fromJson 1, accepted.eventId)
        Assert.Equal(event.submissionId, stored.submissionId)
        Assert.Equal(
            Gambol.Shared.Authority "Test",
            stored.authority)
        Assert.Equal("door-change", state.graph.nodes.[childId].text)
        match stored.body with
        | Gambol.Shared.EventBody.Change _ -> ()
        | _ -> Assert.Fail("expected Change EventBody")
    })

[<Fact>]
let ``postGraphOnly updates Graph and EventLog without file write`` () =
    withHost [] (fun host _ -> task {
        let childId, event = addRootChild "graph-only"
        let! accepted =
            CoreMailbox.postGraphOnly host testCaller event
            |> Async.StartAsTask
        let accepted = requireOk "graph-only" accepted
        let! history =
            CoreMailbox.eventHistory host
            |> Async.StartAsTask
        let! state =
            CoreMailbox.getState host
            |> Async.StartAsTask
        let state = requireOk "get state" state
        Assert.Equal(EventId.fromJson 1, accepted.eventId)
        let stored = Assert.Single(history.events)
        Assert.Equal(event.submissionId, stored.submissionId)
        Assert.Equal("graph-only", state.graph.nodes.[childId].text)
    })

[<Fact>]
let ``CoreChanges builds Ev at postEvent and persists its Graph Ops`` () =
    withHost [] (fun host _ -> task {
        let childId, event = addRootChild "through-event"
        let! accepted =
            (CoreMailbox.coreChanges host testCaller).postEvents [ event ]
            |> Async.StartAsTask
        let accepted = requireOk "post change" accepted
        let! state =
            CoreMailbox.getState host
            |> Async.StartAsTask
        let state = requireOk "get state" state
        let! history =
            CoreMailbox.eventHistory host
            |> Async.StartAsTask
        let stored = Assert.Single(history.events)
        Assert.Equal("through-event", state.graph.nodes.[childId].text)
        Assert.Equal(EventId.fromJson 1, accepted.eventId)
        Assert.Equal(event.submissionId, stored.submissionId)
        Assert.Equal(
            Gambol.Shared.Authority "Test",
            stored.authority)
        let postedOps = Ev.ops event |> Option.defaultValue []
        let storedOps = Ev.ops stored |> Option.defaultValue []
        Assert.Equal<Op list>(
            postedOps,
            storedOps |> List.take postedOps.Length)
    })

[<Fact>]
let ``postEvent stamps admitted authority instead of wire authority`` () =
    withHost [] (fun host _ -> task {
        let _, event = addRootChild "authority"
        let! result =
            CoreMailbox.postEvent host testCaller event
            |> Async.StartAsTask
        let stored = requireOk "post event" result
        Assert.Equal(
            Gambol.Shared.Authority "Test",
            stored.authority)
        Assert.NotEqual(event.authority, stored.authority)
    })

[<Fact>]
let ``name-only Undo and Redo store completed Events with submission ids`` () =
    withHost [] (fun host _ -> task {
        let childId, event = addRootChild "changed"
        let! changedResult =
            CoreMailbox.postEvent host testCaller event
            |> Async.StartAsTask
        let storedEvent = requireOk "change" changedResult
        let undoId = Guid.NewGuid()
        let undo =
            wireEvent
                undoId
                (Gambol.Shared.EventBody.Undo(
                    storedEvent.id,
                    []))
        let! undoResult =
            CoreMailbox.postEvent host testCaller undo
            |> Async.StartAsTask
        let storedUndo = requireOk "undo" undoResult
        let redoId = Guid.NewGuid()
        let redo =
            wireEvent
                redoId
                (Gambol.Shared.EventBody.Redo(
                    storedUndo.id,
                    []))
        let! redoResult =
            CoreMailbox.postEvent host testCaller redo
            |> Async.StartAsTask
        let storedRedo = requireOk "redo" redoResult
        let! state =
            CoreMailbox.getState host
            |> Async.StartAsTask
        let state = requireOk "state after redo" state
        Assert.Equal(undoId, storedUndo.submissionId)
        Assert.Equal(redoId, storedRedo.submissionId)
        Assert.NotEmpty(
            Ev.ops storedUndo
            |> Option.defaultValue [])
        Assert.NotEmpty(
            Ev.ops storedRedo
            |> Option.defaultValue [])
        Assert.Contains(
            state.graph.nodes.[Graph.rootId].children,
            fun child -> child.id = childId)
    })

[<Fact>]
let ``eventHistory returns full EventLog and eventsSince returns its tail`` () =
    withHost [] (fun host _ -> task {
        let firstChildId, firstEvent = addRootChild "first"
        let secondChildId = NodeId.New()
        let secondEvent =
            { id = EventId.fromJson 0
              submissionId = Guid.NewGuid()
              authority = Authority "Browser"
              commandName = ""
              body = EventBody.Change
                    [ Op.NewNode(secondChildId, "second")
                      Op.Replace(
                          Graph.rootId,
                          [ ChildNode.owner firstChildId ],
                          [ ChildNode.owner firstChildId
                            ChildNode.owner secondChildId ]) ] }
        let! firstResult =
            CoreMailbox.postEvent host testCaller firstEvent
            |> Async.StartAsTask
        let storedFirst = requireOk "first" firstResult
        let! secondResult =
            CoreMailbox.postEvent host testCaller secondEvent
            |> Async.StartAsTask
        let storedSecond = requireOk "second" secondResult
        let! full =
            CoreMailbox.eventHistory host
            |> Async.StartAsTask
        let! tail =
            CoreMailbox.eventsSince host storedFirst.id
            |> Async.StartAsTask
        Assert.Equal(2, full.events.Length)
        Assert.Equal<Ev list>(
            [ storedSecond ],
            tail.events)
        Assert.Equal(full.nextId, tail.nextId)
    })

[<Fact>]
let ``mailbox appends ActorStart and ActorStop in lifecycle order`` () =
    let actorSecret = Credential "issue-41-actor"
    let pool: CoreActorPool =
        { register = fun _ _ -> ()
          startActor = fun _ _ -> Ok actorSecret
          schedule = fun _ _ -> ()
          isLive = fun secret -> secret = actorSecret
          admit = fun _ -> Ok ()
          drop = fun _ -> ()
          finish = fun _ _ -> Ok ()
          liveFocusIds = fun () -> Set.empty
          getFocusId = fun _ -> Some Graph.rootId }
    task {
        let host =
            CoreMailbox.host
                pool
                (FileAgent.persist (FileAgent.create (newTempDir ())))
                admittedCredentials
        try
            let request: Gambol.Shared.ActorStart =
                { zoomId = Graph.rootId
                  focusId = Graph.rootId
                  commandId = Graph.rootId
                  graphIds = [ Graph.rootId ]
                  eventId = EventId.fromJson 0 }
            let! started =
                CoreMailbox.startActor host testCaller request
                |> Async.StartAsTask
            requireOk "start actor" started
            let! afterStart =
                CoreMailbox.eventHistory host
                |> Async.StartAsTask
            let startEvent = Assert.Single(afterStart.events)
            let actorCaller =
                { authority = Authority "Actor"
                  name = ""
                  secret = actorSecret }
            let! stopped =
                CoreMailbox.actorStop host actorCaller ActorSucceeded
                |> Async.StartAsTask
            requireOk "stop actor" stopped
            let! history =
                CoreMailbox.eventHistory host
                |> Async.StartAsTask
            let ordered = history.events |> List.sortBy (_.id)
            Assert.Equal(2, ordered.Length)
            let stopEvent = ordered.[1]
            Assert.Equal(
                Gambol.Shared.Authority "Test",
                startEvent.authority)
            Assert.Equal(
                Gambol.Shared.Authority "Actor",
                stopEvent.authority)
            Assert.Equal(
                Gambol.Shared.EventBody.ActorStart request,
                startEvent.body)
            Assert.Equal(
                Gambol.Shared.EventBody.ActorStop(
                    request.focusId,
                    Gambol.Shared.ActorResult.ActorSucceeded),
                stopEvent.body)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``postEvent rejects a non-zero EventId on a new client Event`` () =
    withHost [] (fun host _ -> task {
        let _, event = addRootChild "nonzero"
        let dirty = { event with id = EventId.fromJson 99 }
        let! result =
            CoreMailbox.postEvent host testCaller dirty
            |> Async.StartAsTask
        match result with
        | Error msg -> Assert.Equal("posted EventId must be zero", msg)
        | Ok _ -> Assert.Fail("expected posted EventId must be zero")
    })

[<Fact>]
let ``two live client edits with EventId.zero are admitted`` () =
    withHost [] (fun host _ -> task {
        let childId, createEvent = addRootChild "before"
        let! created =
            CoreMailbox.postEvent host testCaller createEvent
            |> Async.StartAsTask
        let storedCreate = requireOk "create" created
        Assert.Equal(EventId.fromJson 1, storedCreate.id)
        let edit =
            ClientHistory.mintChange
                "Edit node"
                [ Op.SetText(childId, "before", "after") ]
        Assert.Equal(EventId.zero, edit.id)
        let! edited =
            CoreMailbox.postEvent host testCaller edit
            |> Async.StartAsTask
        let storedEdit = requireOk "edit" edited
        Assert.Equal(EventId.fromJson 2, storedEdit.id)
        let! state =
            CoreMailbox.getState host
            |> Async.StartAsTask
        let state = requireOk "state after edit" state
        Assert.Equal("after", state.graph.nodes.[childId].text)
    })

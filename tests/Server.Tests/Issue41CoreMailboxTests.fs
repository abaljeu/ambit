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

let private addRootChild text =
    let childId = NodeId.New()
    childId,
    { id = 0
      changeId = Guid.NewGuid()
      ops =
        [ Op.NewNode(childId, text)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }

let private wireEvent submissionId body : Gambol.Shared.Events.Event =
    { id = Gambol.Shared.Events.EventId 99
      submissionId = submissionId
      authority = Gambol.Shared.Events.Authority "Wire"
      commandName = "test"
      body = body }

[<Fact>]
let ``CoreMailbox.postChange appends Change Events to EventLog`` () =
    withHost [] (fun host _ -> task {
        let childId, change = addRootChild "door-change"
        let! accepted =
            CoreMailbox.postChange host testCaller [ change ]
            |> Async.StartAsTask
        let accepted = requireOk "postChange" accepted
        let! history =
            CoreMailbox.eventHistory host
            |> Async.StartAsTask
        let stored = Assert.Single(history.events)
        let! state =
            CoreMailbox.getState host
            |> Async.StartAsTask
        let state = requireOk "get state" state
        Assert.Equal(Revision 1, accepted.revision)
        Assert.Equal(change.changeId, stored.submissionId)
        Assert.Equal(
            Gambol.Shared.Events.Authority "Test",
            stored.authority)
        Assert.Equal("door-change", state.graph.nodes.[childId].text)
        match stored.body with
        | Gambol.Shared.Events.EventBody.Change _ -> ()
        | _ -> Assert.Fail("expected Change EventBody")
    })

[<Fact>]
let ``postGraphOnlyChange updates Graph without EventLog append`` () =
    withHost [] (fun host _ -> task {
        let childId, change = addRootChild "graph-only"
        let! accepted =
            CoreMailbox.postGraphOnlyChange host testCaller change
            |> Async.StartAsTask
        let accepted = requireOk "graph-only" accepted
        let! history =
            CoreMailbox.eventHistory host
            |> Async.StartAsTask
        let! state =
            CoreMailbox.getState host
            |> Async.StartAsTask
        let state = requireOk "get state" state
        Assert.Equal(Revision 1, accepted.revision)
        Assert.Empty(history.events)
        Assert.Equal("graph-only", state.graph.nodes.[childId].text)
    })

[<Fact>]
let ``CoreChanges builds Event at postEvent and persists its Graph Ops`` () =
    withHost [] (fun host _ -> task {
        let childId, change = addRootChild "through-event"
        let! accepted =
            (CoreMailbox.coreChanges host testCaller).postChange [ change ]
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
        Assert.Equal(Revision 1, accepted.revision)
        Assert.Equal(change.changeId, stored.submissionId)
        Assert.Equal(
            Gambol.Shared.Events.Authority "Test",
            stored.authority)
        let storedOps =
            Gambol.Shared.Events.Event.ops stored
            |> Option.defaultValue []
        Assert.Equal<Op list>(
            change.ops,
            storedOps |> List.take change.ops.Length)
    })

[<Fact>]
let ``postEvent stamps admitted authority instead of wire authority`` () =
    withHost [] (fun host _ -> task {
        let _, change = addRootChild "authority"
        let event =
            wireEvent
                change.changeId
                (Gambol.Shared.Events.EventBody.Change change.ops)
        let! result =
            CoreMailbox.postEvent host testCaller event
            |> Async.StartAsTask
        let stored = requireOk "post event" result
        Assert.Equal(
            Gambol.Shared.Events.Authority "Test",
            stored.authority)
        Assert.NotEqual(event.authority, stored.authority)
    })

[<Fact>]
let ``name-only Undo and Redo store completed Events with submission ids`` () =
    withHost [] (fun host _ -> task {
        let childId, change = addRootChild "changed"
        let changed =
            wireEvent
                (Guid.NewGuid())
                (Gambol.Shared.Events.EventBody.Change change.ops)
        let! changedResult =
            CoreMailbox.postEvent host testCaller changed
            |> Async.StartAsTask
        let storedChange = requireOk "change" changedResult
        let undoId = Guid.NewGuid()
        let undo =
            wireEvent
                undoId
                (Gambol.Shared.Events.EventBody.Undo(
                    storedChange.id,
                    []))
        let! undoResult =
            CoreMailbox.postEvent host testCaller undo
            |> Async.StartAsTask
        let storedUndo = requireOk "undo" undoResult
        let redoId = Guid.NewGuid()
        let redo =
            wireEvent
                redoId
                (Gambol.Shared.Events.EventBody.Redo(
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
            Gambol.Shared.Events.Event.ops storedUndo
            |> Option.defaultValue [])
        Assert.NotEmpty(
            Gambol.Shared.Events.Event.ops storedRedo
            |> Option.defaultValue [])
        Assert.Contains(
            state.graph.nodes.[Graph.rootId].children,
            fun child -> child.id = childId)
    })

[<Fact>]
let ``eventHistory returns full EventLog and eventsSince returns its tail`` () =
    withHost [] (fun host _ -> task {
        let firstChildId, firstChange = addRootChild "first"
        let secondChildId = NodeId.New()
        let secondChange =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(secondChildId, "second")
                  Op.Replace(
                      Graph.rootId,
                      [ ChildNode.owner firstChildId ],
                      [ ChildNode.owner firstChildId
                        ChildNode.owner secondChildId ]) ] }
        let first =
            wireEvent
                firstChange.changeId
                (Gambol.Shared.Events.EventBody.Change firstChange.ops)
        let second =
            wireEvent
                secondChange.changeId
                (Gambol.Shared.Events.EventBody.Change secondChange.ops)
        let! firstResult =
            CoreMailbox.postEvent host testCaller first
            |> Async.StartAsTask
        let storedFirst = requireOk "first" firstResult
        let! secondResult =
            CoreMailbox.postEvent host testCaller second
            |> Async.StartAsTask
        let storedSecond = requireOk "second" secondResult
        let! full =
            CoreMailbox.eventHistory host
            |> Async.StartAsTask
        let! tail =
            CoreMailbox.eventsSince host storedFirst.id
            |> Async.StartAsTask
        Assert.Equal(2, full.events.Length)
        Assert.Equal<Gambol.Shared.Events.Event list>(
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
            let request: StartActorRequest =
                { zoomId = Graph.rootId
                  focusId = Graph.rootId
                  commandId = Graph.rootId
                  graphIds = [ Graph.rootId ]
                  revision = Gambol.Shared.Events.EventId 0 }
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
                Gambol.Shared.Events.Authority "Test",
                startEvent.authority)
            Assert.Equal(
                Gambol.Shared.Events.Authority "Actor",
                stopEvent.authority)
            Assert.Equal(
                Gambol.Shared.Events.EventBody.ActorStart request,
                startEvent.body)
            Assert.Equal(
                Gambol.Shared.Events.EventBody.ActorStop(
                    request.focusId,
                    Gambol.Shared.Events.ActorResult.ActorSucceeded),
                stopEvent.body)
        finally
            CoreMailbox.dispose host
    }

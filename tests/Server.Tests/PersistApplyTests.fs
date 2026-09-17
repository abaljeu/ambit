module Gambol.Server.Tests.PersistApplyTests

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

let private addRootChild text =
    let childId = NodeId.New()
    childId,
    [ Op.NewNode(childId, text)
      Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ]

let private wireEvent submissionId ops : Ev =
    { id = EventId.fromJson 99
      submissionId = submissionId
      authority = Authority "Wire"
      commandName = "persist-apply"
      body = EventBody.Change ops }

let private eventsSince (handlers: PersistHandlers) after =
    requireOk "getEventsSince" (handlers.getEventsSince after)

let private applyThenAppend (handlers: PersistHandlers) event =
    let accepted = requireOk "applyEvent" (handlers.applyEvent event false)
    Assert.Empty(eventsSince handlers (EventId.fromJson -1))
    requireOk "appendEvent" (handlers.appendEvent event)
    let stored = Assert.Single(eventsSince handlers (EventId.fromJson -1))
    Assert.Equal(event.submissionId, stored.submissionId)
    accepted

[<Fact>]
let ``FileAgent applyEvent applies Ev Ops without leftover Change`` () =
    let childId, ops = addRootChild "persist-apply-file"
    let event = wireEvent (Guid.NewGuid()) ops
    let agent = FileAgent.create (newTempDir ())
    let handlers = (FileAgent.persist agent).handlers
    let accepted = applyThenAppend handlers event
    let state = requireOk "getState" (handlers.getState ())
    Assert.Equal("persist-apply-file", state.graph.nodes.[childId].text)
    Assert.Equal(event.submissionId, Assert.Single(accepted.events).submissionId)

[<Fact>]
let ``DbAgent applyEvent applies Ev Ops without leftover Change`` () =
    let childId, ops = addRootChild "persist-apply-db"
    let event = wireEvent (Guid.NewGuid()) ops
    let initial =
        { graph = Graph.create (); eventId = EventId.zero }
    let agent = DbAgent.createForTest initial (fun _ -> Ok [])
    let handlers = (DbAgent.persist agent).handlers
    let accepted = applyThenAppend handlers event
    let state = requireOk "getState" (handlers.getState ())
    Assert.Equal("persist-apply-db", state.graph.nodes.[childId].text)
    Assert.Equal(event.submissionId, Assert.Single(accepted.events).submissionId)

[<Fact>]
let ``CoreEventDispatch persist apply does not copy Ev to leftover Change`` () =
    let childId, ops = addRootChild "no-change-copy"
    let event = wireEvent (Guid.NewGuid()) ops
    let persist = FileAgent.persist (FileAgent.create (newTempDir ()))
    let filling =
        { persist with
            handlers =
                { persist.handlers with
                    postChange =
                        fun _ -> Error "leftover Change apply must not run"
                    postGraphOnlyChange =
                        fun _ -> Error "leftover Change apply must not run" } }
    let host =
        CoreMailbox.host
            (CoreActorPool.create ())
            filling
            admittedCredentials
    try
        let stored =
            CoreMailbox.postEvent host testCaller event
            |> Async.RunSynchronously
            |> requireOk "postEvent"
        let state =
            CoreMailbox.getState host
            |> Async.RunSynchronously
            |> requireOk "getState"
        Assert.Equal("no-change-copy", state.graph.nodes.[childId].text)
        Assert.Equal(event.submissionId, stored.submissionId)
        match Ev.ops stored with
        | Some storedOps ->
            Assert.Equal<Op list>(ops, List.take ops.Length storedOps)
        | None -> Assert.Fail("expected Change EventBody Ops")
        let logged =
            CoreMailbox.getEventsSince host (EventId.fromJson -1)
            |> Async.RunSynchronously
        Assert.Equal(event.submissionId, Assert.Single(logged).submissionId)
    finally
        CoreMailbox.dispose host

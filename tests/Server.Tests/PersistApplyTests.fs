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
    { id = EventId 99
      submissionId = submissionId
      authority = Authority "Wire"
      commandName = "persist-apply"
      body = EventBody.Change ops }

[<Fact>]
let ``FileAgent applyEvent applies Ev Ops without leftover Change`` () =
    let childId, ops = addRootChild "persist-apply-file"
    let event = wireEvent (Guid.NewGuid()) ops
    let agent = FileAgent.create (newTempDir ())
    let handlers = (FileAgent.persist agent).handlers
    let accepted = requireOk "applyEvent" (handlers.applyEvent event false)
    let state = requireOk "getState" (handlers.getState ())
    Assert.Equal("persist-apply-file", state.graph.nodes.[childId].text)
    Assert.Equal(event.submissionId, Assert.Single(accepted.events).submissionId)

[<Fact>]
let ``DbAgent applyEvent applies Ev Ops without leftover Change`` () =
    let childId, ops = addRootChild "persist-apply-db"
    let event = wireEvent (Guid.NewGuid()) ops
    let initial =
        { graph = Graph.create (); revision = Revision 0 }
    let agent = DbAgent.createForTest initial (fun _ -> Ok [])
    let handlers = (DbAgent.persist agent).handlers
    let accepted = requireOk "applyEvent" (handlers.applyEvent event false)
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
    finally
        CoreMailbox.dispose host

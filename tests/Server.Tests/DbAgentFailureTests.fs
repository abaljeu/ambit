module Gambol.Server.Tests.DbAgentFailureTests

open System
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private host agent = admittedHostDb agent

let private getState agent = async {
    match! CoreMailboxApi.getState (host agent) with
    | Ok state -> return state
    | Error error ->
        Assert.Fail($"get state: {error}")
        return Unchecked.defaultof<_>
}

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error error -> failwith $"{label}: {error}"

let private owned = ChildNode.owners

let private special id kind name owner =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private fileEditGraph () =
    let wsId = NodeId.New()
    let fileId = NodeId.New()
    let bodyId = NodeId.New()
    let graph =
        Graph.create ()
        |> Graph.addDetachedNode
            (special wsId Workspace "home" Graph.workspacesId)
        |> Graph.addDetachedNode (special fileId File "a.txt" wsId)
        |> Graph.addDetachedNode
            (Node.Create(bodyId, text = "alpha", owner = fileId))
        |> Graph.replace Graph.workspacesId 0 [] (owned [ wsId ])
        |> requireOk "workspaces"
        |> Graph.replace wsId 0 [] (owned [ fileId ])
        |> requireOk "file"
        |> Graph.replace fileId 0 [] (owned [ bodyId ])
        |> requireOk "body"
    graph, fileId, bodyId

let private editBody bodyId =
    [ {
        id = EventId.zero
        submissionId = Guid.NewGuid()
        authority = Authority "Browser"
        commandName = ""
        body = EventBody.Change [ Op.SetText(bodyId, "alpha", "ALPHA") ]
    } ]

let private throwingPersist
    : string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string> =
    fun _ _ _ _ ->
        raise (InvalidOperationException("injected persistence failure"))

let private persistFailed events =
    events
    |> List.choose (fun event ->
        match event.body with
        | EventBody.Notice (Notice.PersistFailed (nodeId, message)) ->
            Some (nodeId, message)
        | _ -> None)

let private postEdit agent bodyId = async {
    return!
        CoreDoor.postEvents
            (admittedChanges (host agent))
            (editBody bodyId)
}

/// A failed document write keeps the edit. The order succeeds. The file
/// stays Unpersisted. The mailbox still serves a later read.
[<Fact>]
let ``Failed write keeps the edit`` () = task {
    let dataDir = newTempDir ()
    let graph, fileId, bodyId = fileEditGraph ()
    DocumentPersistWrite.writeAllDocuments dataDir graph
    |> requireOk "seed"
    |> ignore
    let agent =
        DbAgent.createForTestWithDependencies
            { graph = graph; eventId = EventId.zero }
            (Some dataDir)
            throwingPersist
            (fun _ -> Ok [])
    let! postResult =
        postEdit agent bodyId
        |> Async.StartAsTask
        |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
    match postResult with
    | Error error -> Assert.Fail($"expected success, got {error}")
    | Ok accepted ->
        Assert.Equal(
            Some "injected persistence failure",
            accepted.message)
    let! state =
        getState agent
        |> Async.StartAsTask
        |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
    Assert.Equal("ALPHA", state.graph.nodes.[bodyId].text)
    Assert.Equal(
        PersistState.Unpersisted,
        state.graph.nodes.[fileId].documentState.persistState)
    Assert.True(EventId.isAccepted state.eventId)
}

[<Fact>]
let ``Failed write posts PersistFailed`` () = task {
    let dataDir = newTempDir ()
    let graph, fileId, bodyId = fileEditGraph ()
    DocumentPersistWrite.writeAllDocuments dataDir graph
    |> requireOk "seed"
    |> ignore
    let agent =
        DbAgent.createForTestWithDependencies
            { graph = graph; eventId = EventId.zero }
            (Some dataDir)
            throwingPersist
            (fun _ -> Ok [])
    let! postResult =
        postEdit agent bodyId
        |> Async.StartAsTask
        |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
    match postResult with
    | Error error -> Assert.Fail($"expected success, got {error}")
    | Ok _ -> ()
    let! events =
        CoreMailboxApi.getEventsSince (host agent) EventId.zero
        |> Async.StartAsTask
        |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
    let nodeId, message = Assert.Single(persistFailed events)
    Assert.Equal(fileId, nodeId)
    Assert.Contains("injected persistence failure", message)
}

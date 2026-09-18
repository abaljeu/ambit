module ClientHistoryRuntimeTests

open System
open Gambol.Shared
open Gambol.Shared
open Xunit

let private textChange n nodeId oldText newText : Ev =
    { id = EventId.fromJson n
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = ""
      body = EventBody.Change [ Op.SetText(nodeId, oldText, newText) ] }

let private clientState graph revision history : ClientSyncState =
    ClientSyncState.create graph revision history

let private unloadedWorkspace () : Graph * NodeId * Node =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let ws =
        Node.Create(
            wsId,
            text = "ws",
            name = Filename.Ok "ws",
            kind = Special Workspace,
            childrenStatus = Unloaded,
            owner = Graph.workspacesId)
    let workspaces = graph0.nodes.[Graph.workspacesId]
    let nodes =
        graph0.nodes
        |> Map.add wsId ws
        |> Map.add
            Graph.workspacesId
            { workspaces with
                children =
                    workspaces.children @ [ ChildNode.owner wsId ] }
    Graph.fromNodes graph0.root nodes, wsId, ws

[<Fact>]
let ``applyLocalEvent records the submitted Event and Normal transition`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "before" graph0
    let change = textChange 3 nodeId "before" "after"
    let state = clientState graph1 (EventId.fromJson 3) (ClientHistory.clear ())
    match SyncLogic.applyLocalEvent { change with commandName = "Edit node" } state with
    | Error msg -> failwith msg
    | Ok (next, pending) ->
        Assert.Equal("after", next.graph.nodes.[nodeId].text)
        Assert.Equal(EventId.zero, pending.id)
        Assert.Equal("Edit node", pending.commandName)
        Assert.Equal(change.submissionId, pending.submissionId)
        Assert.Equal<Op list>(
            Ev.ops change |> Option.defaultValue [],
            Ev.ops pending |> Option.defaultValue [])
        match ClientHistory.undo (Guid.NewGuid()) next.history with
        | None -> failwith "Expected recorded History"
        | Some (inverse, _) ->
            Assert.Equal("Edit node", inverse.commandName)
            Assert.Equal(EventId.zero, inverse.id)
            Assert.Equal<Op list>(
                [ Op.SetText(nodeId, "after", "before") ],
                Ev.ops inverse |> Option.defaultValue [])

[<Fact>]
let ``applyLocalUndo projects the inverse through ResidentProjection`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "before" graph0
    let change = textChange 3 nodeId "before" "after"
    match
        SyncLogic.applyLocalEvent
            { change with commandName = "Edit node" }
            (clientState graph1 (EventId.fromJson 3) (ClientHistory.clear ()))
    with
    | Error msg -> failwith msg
    | Ok (afterEdit, _) ->
        let undoId = Guid.NewGuid()
        match SyncLogic.applyLocalUndo undoId afterEdit with
        | None -> failwith "Expected Undo"
        | Some (Error msg) -> failwith msg
        | Some (Ok (afterUndo, pending)) ->
            Assert.Equal("before", afterUndo.graph.nodes.[nodeId].text)
            Assert.Equal(EventId.zero, pending.id)
            Assert.Equal("Edit node", pending.commandName)
            Assert.Equal(undoId, pending.submissionId)
            Assert.Equal<Op list>(
                [ Op.SetText(nodeId, "after", "before") ],
                Ev.ops pending |> Option.defaultValue [])

[<Fact>]
let ``applyLocalRedo projects the inverse through ResidentProjection`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "before" graph0
    let change = textChange 3 nodeId "before" "after"
    match
        SyncLogic.applyLocalEvent
            { change with commandName = "Edit node" }
            (clientState graph1 (EventId.fromJson 3) (ClientHistory.clear ()))
    with
    | Error msg -> failwith msg
    | Ok (afterEdit, _) ->
        match SyncLogic.applyLocalUndo (Guid.NewGuid()) afterEdit with
        | Some (Ok (afterUndo, _)) ->
            let redoId = Guid.NewGuid()
            match SyncLogic.applyLocalRedo redoId afterUndo with
            | None -> failwith "Expected Redo"
            | Some (Error msg) -> failwith msg
            | Some (Ok (afterRedo, pending)) ->
                Assert.Equal("after", afterRedo.graph.nodes.[nodeId].text)
                Assert.Equal(EventId.zero, pending.id)
                Assert.Equal("Edit node", pending.commandName)
                Assert.Equal(redoId, pending.submissionId)
        | _ -> failwith "Expected Undo before Redo"

[<Fact>]
let ``empty Poll tail preserves ClientHistory`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "before" graph0
    let change = textChange 0 nodeId "before" "after"
    let history =
        ClientHistory.clear ()
        |> ClientHistory.record { change with commandName = "Edit node" }
    let state = clientState graph1 (EventId.fromJson 3) history
    match SyncLogic.applyServerTail [] state with
    | Error msg -> failwith msg
    | Ok result ->
        Assert.Equal(state.eventId, result.eventId)
        Assert.Equal(state.history, result.history)

[<Fact>]
let ``non-empty Poll tail preserves ClientHistory before projection`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "before" graph0
    let change = textChange 0 nodeId "before" "after"
    let history =
        ClientHistory.clear ()
        |> ClientHistory.record { change with commandName = "Edit node" }
    let state = clientState graph1 (EventId.fromJson 3) history
    let upstream =
        { id = EventId.fromJson 3
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(nodeId, "before", "remote") ] }
    match SyncLogic.applyServerTail [ upstream ] state with
    | Error msg -> failwith msg
    | Ok result ->
        Assert.Equal(state.history, result.history)
        Assert.Equal("remote", result.graph.nodes.[nodeId].text)
        Assert.Equal(EventId.fromJson 3, result.eventId)

[<Fact>]
let ``package-only Load preserves ClientHistory at the same settled Revision`` () =
    let graph, wsId, ws = unloadedWorkspace ()
    let change = textChange 2 (NodeId.New()) "x" "y"
    let history =
        ClientHistory.clear ()
        |> ClientHistory.record { change with commandName = "Edit node" }
    let state: ClientSyncState =
        ClientSyncState.create graph (EventId.fromJson 4) history
    let loadedEmpty = { ws with children = []; childrenStatus = Loaded }
    match
        SyncLogic.applyLoadResponse
            (EventId.fromJson 4)
            false
            { events = []; packages = [ loadedEmpty ] }
            state
    with
    | Error msg -> failwith msg
    | Ok result ->
        Assert.Equal(history, result.history)
        Assert.Equal(EventId.fromJson 4, result.eventId)
        Assert.Equal(Loaded, result.graph.nodes.[wsId].childrenStatus)

[<Fact>]
let ``package-only Load refuses a raced pending local transition`` () =
    let graph, _, ws = unloadedWorkspace ()
    let state: ClientSyncState =
        ClientSyncState.create graph (EventId.fromJson 4) (ClientHistory.clear ())
    let loadedEmpty = { ws with children = []; childrenStatus = Loaded }
    match
        SyncLogic.applyLoadResponse
            (EventId.fromJson 4)
            true
            { events = []; packages = [ loadedEmpty ] }
            state
    with
    | Ok _ -> failwith "Expected raced package refusal"
    | Error msg -> Assert.Contains("raced", msg)

[<Fact>]
let ``package-only Load refuses a revision mismatch`` () =
    let graph, _, ws = unloadedWorkspace ()
    let state: ClientSyncState =
        ClientSyncState.create graph (EventId.fromJson 4) (ClientHistory.clear ())
    let loadedEmpty = { ws with children = []; childrenStatus = Loaded }
    match
        SyncLogic.applyLoadResponse
            (EventId.fromJson 5)
            false
            { events = []; packages = [ loadedEmpty ] }
            state
    with
    | Ok _ -> failwith "Expected raced package refusal"
    | Error msg -> Assert.Contains("raced", msg)

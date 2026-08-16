module ClientHistoryRuntimeTests

open System
open Gambol.Shared
open Xunit

let private textChange id nodeId oldText newText : Change =
    { id = id
      changeId = Guid.NewGuid()
      ops = [ Op.SetText(nodeId, oldText, newText) ] }

let private clientState graph revision history : ClientSyncState =
    { graph = graph
      revision = revision
      history = history }

let private pendingKind (item: PendingChange) : PendingKind =
    match item.transition with
    | Some transition -> transition.kind
    | None -> failwith "Expected PendingTransition"

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
let ``applyLocalChange records the submitted Change and Normal transition`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "before" graph0
    let change = textChange 3 nodeId "before" "after"
    let state = clientState graph1 (Revision 3) (ClientHistory.clear ())
    match SyncLogic.applyLocalChange "Edit node" change state with
    | Error msg -> failwith msg
    | Ok (next, pending) ->
        Assert.Equal("after", next.graph.nodes.[nodeId].text)
        Assert.Equal(change.changeId, pending.change.changeId)
        Assert.Equal<Op list>(change.ops, pending.change.ops)
        Assert.Equal(PendingKind.Normal, pendingKind pending)
        Assert.Equal(change.changeId, pending.transition.Value.submittedChangeId)
        Assert.Equal(0, pending.transition.Value.recordId)
        match ClientHistory.undo (Revision 4) (Guid.NewGuid()) next.history with
        | None -> failwith "Expected recorded History"
        | Some (inverse, commandName, _, recordId) ->
            Assert.Equal("Edit node", commandName)
            Assert.Equal(0, recordId)
            Assert.Equal<Op list>(
                [ Op.SetText(nodeId, "after", "before") ],
                inverse.ops)

[<Fact>]
let ``applyLocalUndo projects the inverse through ResidentProjection`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "before" graph0
    let change = textChange 3 nodeId "before" "after"
    match
        SyncLogic.applyLocalChange
            "Edit node"
            change
            (clientState graph1 (Revision 3) (ClientHistory.clear ()))
    with
    | Error msg -> failwith msg
    | Ok (afterEdit, _) ->
        let undoId = Guid.NewGuid()
        match SyncLogic.applyLocalUndo undoId afterEdit with
        | None -> failwith "Expected Undo"
        | Some (Error msg) -> failwith msg
        | Some (Ok (afterUndo, pending)) ->
            Assert.Equal("before", afterUndo.graph.nodes.[nodeId].text)
            Assert.Equal(undoId, pending.change.changeId)
            Assert.Equal(PendingKind.Undo, pendingKind pending)
            Assert.Equal<Op list>(
                [ Op.SetText(nodeId, "after", "before") ],
                pending.change.ops)

[<Fact>]
let ``applyLocalRedo projects the inverse through ResidentProjection`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "before" graph0
    let change = textChange 3 nodeId "before" "after"
    match
        SyncLogic.applyLocalChange
            "Edit node"
            change
            (clientState graph1 (Revision 3) (ClientHistory.clear ()))
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
                Assert.Equal(redoId, pending.change.changeId)
                Assert.Equal(PendingKind.Redo, pendingKind pending)
        | _ -> failwith "Expected Undo before Redo"

[<Fact>]
let ``empty Poll tail preserves ClientHistory`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "before" graph0
    let change = textChange 0 nodeId "before" "after"
    let history, _ =
        ClientHistory.clear () |> ClientHistory.record "Edit node" change
    let state = clientState graph1 (Revision 3) history
    match SyncLogic.applyServerTail [] state with
    | Error msg -> failwith msg
    | Ok result ->
        Assert.Equal(state.revision, result.revision)
        Assert.Equal(state.history, result.history)

[<Fact>]
let ``non-empty Poll tail clears ClientHistory before projection`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "before" graph0
    let change = textChange 0 nodeId "before" "after"
    let history, _ =
        ClientHistory.clear () |> ClientHistory.record "Edit node" change
    let state = clientState graph1 (Revision 3) history
    let upstream =
        { id = 3
          changeId = Guid.NewGuid()
          ops = [ Op.SetText(nodeId, "before", "remote") ] }
    match SyncLogic.applyServerTail [ upstream ] state with
    | Error msg -> failwith msg
    | Ok result ->
        Assert.Equal(ClientHistory.clear (), result.history)
        Assert.Equal("remote", result.graph.nodes.[nodeId].text)
        Assert.Equal(Revision 4, result.revision)

[<Fact>]
let ``package-only Load preserves ClientHistory at the same settled Revision`` () =
    let graph, wsId, ws = unloadedWorkspace ()
    let change = textChange 2 (NodeId.New()) "x" "y"
    let history, _ =
        ClientHistory.clear () |> ClientHistory.record "Edit node" change
    let state: ClientSyncState =
        { graph = graph
          history = history
          revision = Revision 4 }
    let loadedEmpty = { ws with children = []; childrenStatus = Loaded }
    match
        SyncLogic.applyLoadResponse
            4
            false
            { changes = []; packages = [ loadedEmpty ] }
            state
    with
    | Error msg -> failwith msg
    | Ok result ->
        Assert.Equal(history, result.history)
        Assert.Equal(Revision 4, result.revision)
        Assert.Equal(Loaded, result.graph.nodes.[wsId].childrenStatus)

[<Fact>]
let ``package-only Load refuses a raced pending local transition`` () =
    let graph, _, ws = unloadedWorkspace ()
    let state: ClientSyncState =
        { graph = graph
          history = ClientHistory.clear ()
          revision = Revision 4 }
    let loadedEmpty = { ws with children = []; childrenStatus = Loaded }
    match
        SyncLogic.applyLoadResponse
            4
            true
            { changes = []; packages = [ loadedEmpty ] }
            state
    with
    | Ok _ -> failwith "Expected raced package refusal"
    | Error msg -> Assert.Contains("raced", msg)

[<Fact>]
let ``package-only Load refuses a revision mismatch`` () =
    let graph, _, ws = unloadedWorkspace ()
    let state: ClientSyncState =
        { graph = graph
          history = ClientHistory.clear ()
          revision = Revision 4 }
    let loadedEmpty = { ws with children = []; childrenStatus = Loaded }
    match
        SyncLogic.applyLoadResponse
            5
            false
            { changes = []; packages = [ loadedEmpty ] }
            state
    with
    | Ok _ -> failwith "Expected raced package refusal"
    | Error msg -> Assert.Contains("raced", msg)

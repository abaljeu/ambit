module ClientHistoryTests

open System
open Gambol.Shared
open Xunit

let private applied (change: Change) (state: State) : State =
    match Change.apply change state with
    | ApplyResult.Changed next -> next
    | ApplyResult.Unchanged _ -> failwith "expected Change to alter the Graph"
    | ApplyResult.Invalid(_, message) -> failwith message

let private reachableIds (graph: Graph) : Set<NodeId> =
    let rec walk visited nodeId =
        if Set.contains nodeId visited then
            visited
        else
            graph.nodes.[nodeId].children
            |> List.fold
                (fun state child -> walk state child.id)
                (Set.add nodeId visited)

    walk Set.empty graph.root

let private textChange id nodeId oldText newText : Change =
    { id = id
      changeId = Guid.NewGuid()
      ops = [ Op.SetText(nodeId, oldText, newText) ] }

[<Fact>]
let ``ordinary inverse reverses Set ops and uses supplied identity`` () =
    let nodeId = NodeId.New()
    let oldClasses = CssClass.ofList [ "old" ]
    let newClasses = CssClass.ofList [ "new" ]
    let oldTime = DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc)
    let newTime = oldTime.AddMinutes(1)
    let source =
        { id = 17
          changeId = Guid.NewGuid()
          ops =
            [ Op.SetText(nodeId, "before", "after")
              Op.SetClasses(nodeId, oldClasses, newClasses)
              Op.SetName(nodeId, "old.txt", "new.txt")
              Op.SetDocumentState(nodeId, Current, Unparsed)
              Op.SetUpdateTime(nodeId, oldTime, newTime) ] }
    let inverseId = Guid.NewGuid()
    let inverse = Change.inverse (Revision 41) inverseId source
    Assert.Equal(41, inverse.id)
    Assert.Equal(inverseId, inverse.changeId)
    Assert.Equal<Op list>(
        [ Op.SetUpdateTime(nodeId, newTime, oldTime)
          Op.SetDocumentState(nodeId, Unparsed, Current)
          Op.SetName(nodeId, "new.txt", "old.txt")
          Op.SetClasses(nodeId, newClasses, oldClasses)
          Op.SetText(nodeId, "after", "before") ],
        inverse.ops)

[<Fact>]
let ``ordinary inverse reverses nested Replace order`` () =
    let outerId = NodeId.New()
    let innerId = NodeId.New()
    let outerChild = ChildNode.owner innerId
    let leaf = ChildNode.owner (NodeId.New())
    let source =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            [ Op.Replace(innerId, 0, [], [ leaf ])
              Op.Replace(outerId, 0, [], [ outerChild ]) ] }
    let inverse = Change.inverse Revision.Zero (Guid.NewGuid()) source
    Assert.Equal<Op list>(
        [ Op.Replace(outerId, 0, [ outerChild ], [])
          Op.Replace(innerId, 0, [ leaf ], []) ],
        inverse.ops)

let private createPasteScenario () : State * Change * NodeId list =
    let initial =
        { graph = Graph.create ()
          history = History.empty
          revision = Revision.Zero }
    let topIds, pasteOps =
        Paste.buildPasteOps [ "parent", 0; "child", 1 ]
    let workspaceId = NodeId.New()
    let rootIndex = initial.graph.nodes.[initial.graph.root].children.Length
    let source =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            pasteOps
            @ [ Op.Replace(
                    initial.graph.root,
                    rootIndex,
                    [],
                    ChildNode.owners topIds)
                Op.NewSpecialNode(workspaceId, Workspace, "retained")
                Op.Replace(
                    Graph.workspacesId,
                    0,
                    [],
                    [ ChildNode.owner workspaceId ]) ] }
    let createdIds =
        source.ops
        |> List.choose (function
            | Op.NewNode(nodeId, _)
            | Op.NewSpecialNode(nodeId, _, _) -> Some nodeId
            | _ -> None)
    initial, source, createdIds

[<Fact>]
let ``create inverse retains detached nodes and Redo reconnects their identities`` () =
    let initial, source, createdIds = createPasteScenario ()
    let changed = applied source initial
    let recorded, _recordId =
        ClientHistory.clear ()
        |> ClientHistory.record "Paste" source
    let undo, afterUndo =
        match ClientHistory.undo (Revision 1) (Guid.NewGuid()) recorded with
        | None -> failwith "expected create Undo"
        | Some (change, _, history, _recordId) -> change, history
    Assert.DoesNotContain(
        undo.ops,
        fun op ->
            match op with
            | Op.NewNode _ | Op.NewSpecialNode _ -> true
            | _ -> false)
    let undone = applied undo changed
    let reachableAfterUndo = reachableIds undone.graph
    createdIds
    |> List.iter (fun nodeId ->
        Assert.True(Map.containsKey nodeId undone.graph.nodes)
        Assert.Equal(nodeId, undone.graph.nodes.[nodeId].id)
        Assert.DoesNotContain(nodeId, reachableAfterUndo))
    let redo =
        match ClientHistory.redo (Revision 2) (Guid.NewGuid()) afterUndo with
        | None -> failwith "expected create Redo"
        | Some (change, _, _, _recordId) -> change
    let redone = applied redo undone
    let reachableAfterRedo = reachableIds redone.graph
    createdIds
    |> List.iter (fun nodeId ->
        Assert.True(Map.containsKey nodeId redone.graph.nodes)
        Assert.Contains(nodeId, reachableAfterRedo))

[<Fact>]
let ``record returns a stable client record identity`` () =
    let change =
        { id = 7
          changeId = Guid.NewGuid()
          ops = [] }
    let _, recordId =
        ClientHistory.clear ()
        |> ClientHistory.record "Edit node" change
    Assert.Equal(0, recordId)

[<Fact>]
let ``clear has explicit empty Undo and Redo behavior`` () =
    let empty = ClientHistory.clear ()
    Assert.True(
        ClientHistory.undo (Revision 1) (Guid.NewGuid()) empty
        |> Option.isNone)
    Assert.True(
        ClientHistory.redo (Revision 1) (Guid.NewGuid()) empty
        |> Option.isNone)

[<Fact>]
let ``Undo moves the same named record and returns an ordinary inverse`` () =
    let nodeId = NodeId.New()
    let source = textChange 0 nodeId "old" "new"
    let history, recordId =
        ClientHistory.clear ()
        |> ClientHistory.record "Exact command name" source
    let undoId = Guid.NewGuid()
    match ClientHistory.undo (Revision 9) undoId history with
    | None -> failwith "expected an Undo transition"
    | Some (inverse, commandName, _, undoRecordId) ->
        Assert.Equal("Exact command name", commandName)
        Assert.Equal(recordId, undoRecordId)
        Assert.Equal(9, inverse.id)
        Assert.Equal(undoId, inverse.changeId)
        Assert.Equal<Op list>(
            [ Op.SetText(nodeId, "new", "old") ],
            inverse.ops)

[<Fact>]
let ``Redo moves the same logical record and keeps its exact command name`` () =
    let nodeId = NodeId.New()
    let source = textChange 0 nodeId "old" "new"
    let recorded, recordId =
        ClientHistory.clear ()
        |> ClientHistory.record "Name kept verbatim" source
    let undone =
        ClientHistory.undo (Revision 1) (Guid.NewGuid()) recorded
        |> Option.map (fun (_, _, history, _recordId) -> history)
        |> Option.defaultWith (fun () -> failwith "expected Undo")
    let redoId = Guid.NewGuid()
    match ClientHistory.redo (Revision 2) redoId undone with
    | None -> failwith "expected a Redo transition"
    | Some (redo, commandName, _, redoRecordId) ->
        Assert.Equal("Name kept verbatim", commandName)
        Assert.Equal(recordId, redoRecordId)
        Assert.Equal(2, redo.id)
        Assert.Equal(redoId, redo.changeId)
        Assert.Equal<Op list>(source.ops, redo.ops)

[<Fact>]
let ``normal record folds future without duplicating logical records`` () =
    let first = textChange 0 (NodeId.New()) "first-old" "first-new"
    let second = textChange 1 (NodeId.New()) "second-old" "second-new"
    let recordedFirst, firstRecordId =
        ClientHistory.clear ()
        |> ClientHistory.record "First" first
    let afterFirstUndo =
        ClientHistory.undo (Revision 1) (Guid.NewGuid()) recordedFirst
        |> Option.map (fun (_, _, history, _recordId) -> history)
        |> Option.defaultWith (fun () -> failwith "expected first Undo")
    let withSecond, _ =
        ClientHistory.record "Second" second afterFirstUndo
    let afterSecondUndo =
        match ClientHistory.undo (Revision 2) (Guid.NewGuid()) withSecond with
        | None -> failwith "expected Second Undo"
        | Some (_, name, history, _recordId) ->
            Assert.Equal("Second", name)
            history
    let afterFoldedUndo =
        match ClientHistory.undo (Revision 3) (Guid.NewGuid()) afterSecondUndo with
        | None -> failwith "expected folded First Undo"
        | Some (_, name, history, recordId) ->
            Assert.Equal("First", name)
            Assert.Equal(firstRecordId, recordId)
            history
    Assert.True(
        ClientHistory.undo (Revision 4) (Guid.NewGuid()) afterFoldedUndo
        |> Option.isNone)

[<Fact>]
let ``Undo and Redo retain only their submitted local Changes`` () =
    let nodeId = NodeId.New()
    let source = textChange 0 nodeId "old" "new"
    let recorded, recordId =
        ClientHistory.clear ()
        |> ClientHistory.record "Edit node" source
    let undoId = Guid.NewGuid()
    let undo, undone =
        match ClientHistory.undo (Revision 1) undoId recorded with
        | None -> failwith "expected Undo"
        | Some (change, _, history, undoRecordId) ->
            Assert.Equal(recordId, undoRecordId)
            change, history
    let redoId = Guid.NewGuid()
    let redo, redone =
        match ClientHistory.redo (Revision 2) redoId undone with
        | None -> failwith "expected Redo"
        | Some (change, _, history, redoRecordId) ->
            Assert.Equal(recordId, redoRecordId)
            change, history
    Assert.Equal(undoId, undo.changeId)
    Assert.Equal<Op list>([ Op.SetText(nodeId, "new", "old") ], undo.ops)
    Assert.Equal(redoId, redo.changeId)
    Assert.Equal<Op list>(source.ops, redo.ops)
    match ClientHistory.undo (Revision 3) (Guid.NewGuid()) redone with
    | None -> failwith "expected Undo"
    | Some (nextUndo, _, _, undoRecordId) ->
        Assert.Equal(recordId, undoRecordId)
        Assert.Equal<Op list>(undo.ops, nextUndo.ops)

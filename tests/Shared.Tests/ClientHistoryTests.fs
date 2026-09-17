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
      submissionId = Guid.NewGuid()
      ops = [ Op.SetText(nodeId, oldText, newText) ] }

let private recordNamed name change history =
    ClientHistory.record (Ev.ofChange name change) history

let private eventOps (event: Ev) =
    Ev.ops event |> Option.defaultValue []

[<Fact>]
let ``mintChange uses EventId.zero and commandName`` () =
    let ops = [ Op.SetText(NodeId.New(), "old", "new") ]
    let event = ClientHistory.mintChange "Run" ops
    Assert.Equal(EventId.zero, event.id)
    Assert.Equal("Run", event.commandName)
    match event.body with
    | EventBody.Change bodyOps -> Assert.Equal<Op list>(ops, bodyOps)
    | _ -> failwith "expected Change Event"

[<Fact>]
let ``ordinary inverse reverses Set ops and uses supplied identity`` () =
    let nodeId = NodeId.New()
    let oldClasses = CssClass.ofList [ "old" ]
    let newClasses = CssClass.ofList [ "new" ]
    let oldTime = DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc)
    let newTime = oldTime.AddMinutes(1)
    let source =
        { id = EventId.fromJson 17
          submissionId = Guid.NewGuid()
          ops =
            [ Op.SetText(nodeId, "before", "after")
              Op.SetClasses(nodeId, oldClasses, newClasses)
              Op.SetName(nodeId, "old.txt", "new.txt")
              Op.SetDocumentState(nodeId, Current, Unparsed)
              Op.SetUpdateTime(nodeId, oldTime, newTime) ] }
    let inverseId = Guid.NewGuid()
    let inverse = Change.inverse (EventId.fromJson 41) inverseId source
    Assert.Equal(EventId.fromJson 41, inverse.id)
    Assert.Equal(inverseId, inverse.submissionId)
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
        { id = EventId.fromJson 0
          submissionId = Guid.NewGuid()
          ops =
            [ Op.Replace(innerId, [], [ leaf ])
              Op.Replace(outerId, [], [ outerChild ]) ] }
    let inverse = Change.inverse EventId.zero (Guid.NewGuid()) source
    Assert.Equal<Op list>(
        [ Op.Replace(outerId, [ outerChild ], [])
          Op.Replace(innerId, [ leaf ], []) ],
        inverse.ops)

let private createPasteScenario () : State * Change * NodeId list =
    let initial =
        { graph = Graph.create ()
          eventId = EventId.zero }
    let topIds, pasteOps =
        Paste.buildPasteOps [ "parent", 0; "child", 1 ]
    let workspaceId = NodeId.New()
    let rootIndex = initial.graph.nodes.[initial.graph.root].children.Length
    let source =
        { id = EventId.fromJson 0
          submissionId = Guid.NewGuid()
          ops =
            pasteOps
            @ [ ChildListWire.append initial.graph.root initial.graph.nodes.[initial.graph.root].children (ChildNode.owners topIds)
                Op.NewSpecialNode(workspaceId, Workspace, "retained")
                Op.Replace(
                    Graph.workspacesId,
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
    let recorded =
        ClientHistory.clear ()
        |> recordNamed "Paste" source
    let undo, afterUndo =
        match ClientHistory.undo (Guid.NewGuid()) recorded with
        | None -> failwith "expected create Undo"
        | Some (event, history) -> event, history
    Assert.DoesNotContain(
        eventOps undo,
        fun op ->
            match op with
            | Op.NewNode _ | Op.NewSpecialNode _ -> true
            | _ -> false)
    let undone = applied (Ev.asChange undo) changed
    let reachableAfterUndo = reachableIds undone.graph
    createdIds
    |> List.iter (fun nodeId ->
        Assert.True(Map.containsKey nodeId undone.graph.nodes)
        Assert.Equal(nodeId, undone.graph.nodes.[nodeId].id)
        Assert.DoesNotContain(nodeId, reachableAfterUndo))
    let redo =
        match ClientHistory.redo (Guid.NewGuid()) afterUndo with
        | None -> failwith "expected create Redo"
        | Some (event, _) -> event
    let redone = applied (Ev.asChange redo) undone
    let reachableAfterRedo = reachableIds redone.graph
    createdIds
    |> List.iter (fun nodeId ->
        Assert.True(Map.containsKey nodeId redone.graph.nodes)
        Assert.Contains(nodeId, reachableAfterRedo))

[<Fact>]
let ``approve stamps EventId.zero by submissionId`` () =
    let source = textChange EventId.zero (NodeId.New()) "old" "new"
    let recorded =
        ClientHistory.clear ()
        |> recordNamed "Edit node" source
    let confirmed =
        { Ev.ofChange "Edit node" source with
            id = EventId.fromJson 9 }
    let approved = ClientHistory.approve [ confirmed ] recorded
    match ClientHistory.undoEvent approved with
    | None -> failwith "expected Undo"
    | Some (event, _) ->
        match event.body with
        | EventBody.Undo(target, _) ->
            Assert.Equal(EventId.fromJson 9, target)
        | _ -> failwith "expected Undo body"

[<Fact>]
let ``approve stamps Undo target written while original id was zero`` () =
    let source = textChange EventId.zero (NodeId.New()) "old" "new"
    let recorded =
        ClientHistory.clear ()
        |> recordNamed "Edit node" source
    match ClientHistory.undo (Guid.NewGuid()) recorded with
    | None -> failwith "expected Undo"
    | Some (undo, undone) ->
        match undo.body with
        | EventBody.Undo(target, _) -> Assert.Equal(EventId.zero, target)
        | _ -> failwith "expected Undo body"
        let confirmed =
            { Ev.ofChange "Edit node" source with
                id = EventId.fromJson 9 }
        let approved = ClientHistory.approve [ confirmed ] undone
        match ClientHistory.tryPeekRedoEvent approved with
        | None -> failwith "expected Redo stack Undo"
        | Some event ->
            match event.body with
            | EventBody.Undo(target, _) ->
                Assert.Equal(EventId.fromJson 9, target)
            | _ -> failwith "expected Undo body"

[<Fact>]
let ``record keeps EventId.zero and does not mint a local id`` () =
    let change =
        { id = EventId.fromJson 7
          submissionId = Guid.NewGuid()
          ops = [] }
    let recorded =
        ClientHistory.clear ()
        |> recordNamed "Edit node" change
    match ClientHistory.undo (Guid.NewGuid()) recorded with
    | None -> failwith "expected Undo"
    | Some (event, _) -> Assert.Equal(EventId.zero, event.id)

[<Fact>]
let ``clear has explicit empty Undo and Redo behavior`` () =
    let empty = ClientHistory.clear ()
    Assert.True(
        ClientHistory.undo (Guid.NewGuid()) empty
        |> Option.isNone)
    Assert.True(
        ClientHistory.redo (Guid.NewGuid()) empty
        |> Option.isNone)

[<Fact>]
let ``Undo moves the same named record and returns an ordinary inverse`` () =
    let nodeId = NodeId.New()
    let source = textChange EventId.zero nodeId "old" "new"
    let history =
        ClientHistory.clear ()
        |> recordNamed "Exact command name" source
    let undoId = Guid.NewGuid()
    match ClientHistory.undo undoId history with
    | None -> failwith "expected an Undo transition"
    | Some (inverse, _) ->
        Assert.Equal("Exact command name", inverse.commandName)
        Assert.Equal(EventId.zero, inverse.id)
        Assert.Equal(undoId, inverse.submissionId)
        Assert.Equal<Op list>(
            [ Op.SetText(nodeId, "new", "old") ],
            eventOps inverse)

[<Fact>]
let ``Redo moves the same logical record and keeps its exact command name`` () =
    let nodeId = NodeId.New()
    let source = textChange EventId.zero nodeId "old" "new"
    let recorded =
        ClientHistory.clear ()
        |> recordNamed "Name kept verbatim" source
    let undone =
        ClientHistory.undo (Guid.NewGuid()) recorded
        |> Option.map (fun (_, history) -> history)
        |> Option.defaultWith (fun () -> failwith "expected Undo")
    let redoId = Guid.NewGuid()
    match ClientHistory.redo redoId undone with
    | None -> failwith "expected a Redo transition"
    | Some (redo, _) ->
        Assert.Equal("Name kept verbatim", redo.commandName)
        Assert.Equal(EventId.zero, redo.id)
        Assert.Equal(redoId, redo.submissionId)
        Assert.Equal<Op list>(source.ops, eventOps redo)

[<Theory>]
[<InlineData("Edit node")>]
[<InlineData("Paste")>]
[<InlineData("Cut")>]
[<InlineData("Load")>]
[<InlineData("Download")>]
let ``record stores required command names verbatim`` (name: string) =
    let source = textChange EventId.zero (NodeId.New()) "old" "new"
    let history =
        ClientHistory.clear ()
        |> recordNamed name source
    match ClientHistory.undo (Guid.NewGuid()) history with
    | None -> failwith "expected an Undo transition"
    | Some (undoEvent, undone) ->
        Assert.Equal(name, undoEvent.commandName)
        match ClientHistory.redo (Guid.NewGuid()) undone with
        | None -> failwith "expected a Redo transition"
        | Some (redoEvent, _) -> Assert.Equal(name, redoEvent.commandName)

[<Fact>]
let ``tryPeekUndoName and tryPeekRedoName follow the stacks`` () =
    let source = textChange EventId.zero (NodeId.New()) "old" "new"
    let empty = ClientHistory.clear ()
    Assert.Equal(None, ClientHistory.tryPeekUndoName empty)
    Assert.Equal(None, ClientHistory.tryPeekRedoName empty)
    let recorded = recordNamed "Cut" source empty
    Assert.Equal(Some "Cut", ClientHistory.tryPeekUndoName recorded)
    Assert.Equal(None, ClientHistory.tryPeekRedoName recorded)
    match ClientHistory.undo (Guid.NewGuid()) recorded with
    | None -> failwith "expected Undo"
    | Some (_, undone) ->
        Assert.Equal(None, ClientHistory.tryPeekUndoName undone)
        Assert.Equal(Some "Cut", ClientHistory.tryPeekRedoName undone)

[<Fact>]
let ``normal record folds future without duplicating logical records`` () =
    let first = textChange EventId.zero (NodeId.New()) "first-old" "first-new"
    let second = textChange (EventId.fromJson 1) (NodeId.New()) "second-old" "second-new"
    let recordedFirst =
        ClientHistory.clear ()
        |> recordNamed "First" first
    let afterFirstUndo =
        ClientHistory.undo (Guid.NewGuid()) recordedFirst
        |> Option.map (fun (_, history) -> history)
        |> Option.defaultWith (fun () -> failwith "expected first Undo")
    let withSecond =
        recordNamed "Second" second afterFirstUndo
    let afterSecondUndo =
        match ClientHistory.undo (Guid.NewGuid()) withSecond with
        | None -> failwith "expected Second Undo"
        | Some (event, history) ->
            Assert.Equal("Second", event.commandName)
            history
    let afterFoldedUndo =
        match ClientHistory.undo (Guid.NewGuid()) afterSecondUndo with
        | None -> failwith "expected folded First Undo"
        | Some (event, history) ->
            Assert.Equal("First", event.commandName)
            history
    Assert.True(
        ClientHistory.undo (Guid.NewGuid()) afterFoldedUndo
        |> Option.isNone)

[<Fact>]
let ``Undo and Redo retain only their submitted local Changes`` () =
    let nodeId = NodeId.New()
    let source = textChange EventId.zero nodeId "old" "new"
    let recorded =
        ClientHistory.clear ()
        |> recordNamed "Edit node" source
    let undoId = Guid.NewGuid()
    let undo, undone =
        match ClientHistory.undo undoId recorded with
        | None -> failwith "expected Undo"
        | Some (event, history) ->
            event, history
    let redoId = Guid.NewGuid()
    let redo, redone =
        match ClientHistory.redo redoId undone with
        | None -> failwith "expected Redo"
        | Some (event, history) ->
            event, history
    Assert.Equal(undoId, undo.submissionId)
    Assert.Equal<Op list>(
        [ Op.SetText(nodeId, "new", "old") ],
        eventOps undo)
    Assert.Equal(redoId, redo.submissionId)
    Assert.Equal<Op list>(source.ops, eventOps redo)
    match ClientHistory.undo (Guid.NewGuid()) redone with
    | None -> failwith "expected Undo"
    | Some (nextUndo, _) ->
        Assert.Equal<Op list>(eventOps undo, eventOps nextUndo)

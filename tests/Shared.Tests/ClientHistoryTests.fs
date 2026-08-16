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

let private expectErrorContaining
    (fragment: string)
    (result: Result<'a, string>)
    : unit =
    match result with
    | Ok _ -> failwith $"expected failure containing '{fragment}'"
    | Error error -> Assert.Contains(fragment, error)

let private textChange id nodeId oldText newText : Change =
    { id = id
      changeId = Guid.NewGuid()
      ops = [ Op.SetText(nodeId, oldText, newText) ] }

let private confirmedHistory transition confirmed history =
    ClientHistory.confirm transition confirmed history
    |> Result.map fst
    |> Result.defaultWith failwith

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
    let recorded, _ =
        ClientHistory.clear ()
        |> ClientHistory.record "Paste" source
    let undo, afterUndo =
        match ClientHistory.undo (Revision 1) (Guid.NewGuid()) recorded with
        | None -> failwith "expected create Undo"
        | Some (change, _, history, _) -> change, history
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
        | Some (change, _, _, _) -> change
    let redone = applied redo undone
    let reachableAfterRedo = reachableIds redone.graph
    createdIds
    |> List.iter (fun nodeId ->
        Assert.True(Map.containsKey nodeId redone.graph.nodes)
        Assert.Contains(nodeId, reachableAfterRedo))

[<Fact>]
let ``record returns a Normal transition with stable client identity`` () =
    let change =
        { id = 7
          changeId = Guid.NewGuid()
          ops = [] }
    let _, transition =
        ClientHistory.clear ()
        |> ClientHistory.record "Edit node" change
    Assert.Equal(PendingTransitionKind.Normal, transition.kind)
    Assert.Equal(change.changeId, transition.submittedChangeId)
    Assert.Equal(0, transition.recordId)

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
    let history, normal =
        ClientHistory.clear ()
        |> ClientHistory.record "Exact command name" source
    let undoId = Guid.NewGuid()
    match ClientHistory.undo (Revision 9) undoId history with
    | None -> failwith "expected an Undo transition"
    | Some (inverse, commandName, _, transition) ->
        Assert.Equal("Exact command name", commandName)
        Assert.Equal(normal.recordId, transition.recordId)
        Assert.Equal(PendingTransitionKind.Undo, transition.kind)
        Assert.Equal(undoId, transition.submittedChangeId)
        Assert.Equal(9, inverse.id)
        Assert.Equal(undoId, inverse.changeId)
        Assert.Equal<Op list>(
            [ Op.SetText(nodeId, "new", "old") ],
            inverse.ops)

[<Fact>]
let ``Redo moves the same logical record and keeps its exact command name`` () =
    let nodeId = NodeId.New()
    let source = textChange 0 nodeId "old" "new"
    let recorded, normal =
        ClientHistory.clear ()
        |> ClientHistory.record "Name kept verbatim" source
    let undone =
        ClientHistory.undo (Revision 1) (Guid.NewGuid()) recorded
        |> Option.map (fun (_, _, history, _) -> history)
        |> Option.defaultWith (fun () -> failwith "expected Undo")
    let redoId = Guid.NewGuid()
    match ClientHistory.redo (Revision 2) redoId undone with
    | None -> failwith "expected a Redo transition"
    | Some (redo, commandName, _, transition) ->
        Assert.Equal("Name kept verbatim", commandName)
        Assert.Equal(normal.recordId, transition.recordId)
        Assert.Equal(PendingTransitionKind.Redo, transition.kind)
        Assert.Equal(redoId, transition.submittedChangeId)
        Assert.Equal(2, redo.id)
        Assert.Equal(redoId, redo.changeId)
        Assert.Equal<Op list>(source.ops, redo.ops)

[<Fact>]
let ``normal record folds future without duplicating logical records`` () =
    let first = textChange 0 (NodeId.New()) "first-old" "first-new"
    let second = textChange 1 (NodeId.New()) "second-old" "second-new"
    let recordedFirst, firstTransition =
        ClientHistory.clear ()
        |> ClientHistory.record "First" first
    let afterFirstUndo =
        ClientHistory.undo (Revision 1) (Guid.NewGuid()) recordedFirst
        |> Option.map (fun (_, _, history, _) -> history)
        |> Option.defaultWith (fun () -> failwith "expected first Undo")
    let withSecond, _ =
        ClientHistory.record "Second" second afterFirstUndo
    let afterSecondUndo =
        match ClientHistory.undo (Revision 2) (Guid.NewGuid()) withSecond with
        | None -> failwith "expected Second Undo"
        | Some (_, name, history, _) ->
            Assert.Equal("Second", name)
            history
    let afterFoldedUndo =
        match ClientHistory.undo (Revision 3) (Guid.NewGuid()) afterSecondUndo with
        | None -> failwith "expected folded First Undo"
        | Some (_, name, history, transition) ->
            Assert.Equal("First", name)
            Assert.Equal(firstTransition.recordId, transition.recordId)
            history
    Assert.True(
        ClientHistory.undo (Revision 4) (Guid.NewGuid()) afterFoldedUndo
        |> Option.isNone)

[<Fact>]
let ``Normal confirmation amends its logical record without adding one`` () =
    let nodeId = NodeId.New()
    let submitted = textChange 0 nodeId "old" "new"
    let recorded, transition =
        ClientHistory.clear ()
        |> ClientHistory.record "Edit node" submitted
    let classes = CssClass.ofList [ "confirmed" ]
    let confirmed =
        { submitted with
            ops =
                submitted.ops
                @ [ Op.SetClasses(nodeId, CssClass.empty, classes) ] }
    let amended =
        match ClientHistory.confirm transition confirmed recorded with
        | Error error -> failwith error
        | Ok (history, Some _) -> failwith "unexpected dependent Change"
        | Ok (history, None) -> history
    let undoId = Guid.NewGuid()
    match ClientHistory.undo (Revision 1) undoId amended with
    | None -> failwith "expected amended record to remain undoable"
    | Some (inverse, _, afterUndo, undoTransition) ->
        Assert.Equal(transition.recordId, undoTransition.recordId)
        Assert.Equal<Op list>(
            [ Op.SetClasses(nodeId, classes, CssClass.empty)
              Op.SetText(nodeId, "new", "old") ],
            inverse.ops)
        Assert.True(
            ClientHistory.undo (Revision 2) (Guid.NewGuid()) afterUndo
            |> Option.isNone)

[<Fact>]
let ``confirmation re-derives its direct dependent without changing identity`` () =
    let nodeId = NodeId.New()
    let submitted = textChange 0 nodeId "old" "new"
    let recorded, normalTransition =
        ClientHistory.clear ()
        |> ClientHistory.record "Edit node" submitted
    let undoId = Guid.NewGuid()
    let originalUndo, undone =
        match ClientHistory.undo (Revision 1) undoId recorded with
        | None -> failwith "expected rapid Undo"
        | Some (change, _, history, _) -> change, history
    let classes = CssClass.ofList [ "server" ]
    let confirmed =
        { submitted with
            ops =
                submitted.ops
                @ [ Op.SetClasses(nodeId, CssClass.empty, classes) ] }
    match ClientHistory.confirm normalTransition confirmed undone with
    | Error error -> failwith error
    | Ok (_, None) -> failwith "expected a re-derived dependent"
    | Ok (amended, Some revisedUndo) ->
        Assert.Equal(originalUndo.id, revisedUndo.id)
        Assert.Equal(originalUndo.changeId, revisedUndo.changeId)
        Assert.Equal<Op list>(
            [ Op.SetClasses(nodeId, classes, CssClass.empty)
              Op.SetText(nodeId, "new", "old") ],
            revisedUndo.ops)
        match ClientHistory.redo (Revision 2) (Guid.NewGuid()) amended with
        | None -> failwith "expected Redo after rapid Undo"
        | Some (redo, _, _, _) ->
            Assert.Equal<Op list>(confirmed.ops, redo.ops)

[<Fact>]
let ``confirmation rejects submitted identity and Ops prefix mismatches`` () =
    let nodeId = NodeId.New()
    let submitted = textChange 0 nodeId "old" "new"
    let history, transition =
        ClientHistory.clear ()
        |> ClientHistory.record "Edit node" submitted
    ClientHistory.confirm
        transition
        { submitted with changeId = Guid.NewGuid() }
        history
    |> expectErrorContaining "identity"
    ClientHistory.confirm
        transition
        { submitted with
            ops = [ Op.SetText(nodeId, "different", "new") ] }
        history
    |> expectErrorContaining "prefix"

[<Fact>]
let ``confirmation rejects record and direction lineage mismatches`` () =
    let submitted = textChange 0 (NodeId.New()) "old" "new"
    let recorded, normalTransition =
        ClientHistory.clear ()
        |> ClientHistory.record "Edit node" submitted
    let afterUndo, undoTransition =
        match ClientHistory.undo (Revision 1) (Guid.NewGuid()) recorded with
        | None -> failwith "expected Undo"
        | Some (_, _, history, transition) -> history, transition
    ClientHistory.confirm undoTransition submitted afterUndo
    |> expectErrorContaining "lineage"
    ClientHistory.confirm
        { normalTransition with recordId = 99 }
        submitted
        afterUndo
    |> expectErrorContaining "record identity"
    ClientHistory.confirm
        { normalTransition with kind = PendingTransitionKind.Undo }
        submitted
        afterUndo
    |> expectErrorContaining "lineage"

[<Fact>]
let ``Undo and Redo confirmations amend the same logical record`` () =
    let nodeId = NodeId.New()
    let source = textChange 0 nodeId "old" "new"
    let recorded, normalTransition =
        ClientHistory.clear ()
        |> ClientHistory.record "Edit node" source
    let confirmedNormal =
        confirmedHistory normalTransition source recorded
    let undo, undone, undoTransition =
        match ClientHistory.undo (Revision 1) (Guid.NewGuid()) confirmedNormal with
        | None -> failwith "expected Undo"
        | Some (change, _, history, transition) ->
            change, history, transition
    let classes = CssClass.ofList [ "server" ]
    let confirmedUndo =
        { undo with
            ops =
                undo.ops
                @ [ Op.SetClasses(nodeId, CssClass.empty, classes) ] }
    let afterConfirmedUndo =
        confirmedHistory undoTransition confirmedUndo undone
    let redo, redone, redoTransition =
        match ClientHistory.redo (Revision 2) (Guid.NewGuid()) afterConfirmedUndo with
        | None -> failwith "expected Redo"
        | Some (change, _, history, transition) ->
            change, history, transition
    let confirmedRedo =
        { redo with
            ops = redo.ops @ [ Op.SetName(nodeId, "old", "server") ] }
    let amended =
        confirmedHistory redoTransition confirmedRedo redone
    match ClientHistory.undo (Revision 3) (Guid.NewGuid()) amended with
    | None -> failwith "expected Undo"
    | Some (nextUndo, _, _, _) ->
        Assert.Equal<Op list>(
            [ Op.SetName(nodeId, "server", "old")
              Op.SetText(nodeId, "new", "old")
              Op.SetClasses(nodeId, CssClass.empty, classes) ],
            nextUndo.ops)

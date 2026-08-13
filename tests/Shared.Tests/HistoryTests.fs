module HistoryTests

open System
open Gambol.Shared
open Xunit

let private expectChanged (result: ApplyResult) : State =
    match result with
    | ApplyResult.Changed state -> state
    | ApplyResult.Unchanged _ -> failwith "expected Changed, got Unchanged"
    | ApplyResult.Invalid(_, msg) -> failwithf "expected Changed, got Invalid: %s" msg

let private expectUnchanged (result: ApplyResult) : State =
    match result with
    | ApplyResult.Unchanged state -> state
    | ApplyResult.Changed _ -> failwith "expected Unchanged, got Changed"
    | ApplyResult.Invalid(_, msg) -> failwithf "expected Unchanged, got Invalid: %s" msg

let private expectInvalid (result: ApplyResult) : State * string =
    match result with
    | ApplyResult.Invalid(state, msg) -> state, msg
    | ApplyResult.Changed _ -> failwith "expected Invalid, got Changed"
    | ApplyResult.Unchanged _ -> failwith "expected Invalid, got Unchanged"

let private findNodeByText (text: string) (state: State) : Node =
    state.graph.nodes
    |> Map.toSeq
    |> Seq.map snd
    |> Seq.find (fun n -> n.text = text)

let private stateWithNodes (nodes: Node list) =
    let graph0 = Graph.create ()
    let allNodes =
        nodes
        |> List.fold (fun acc node -> Map.add node.id node acc) graph0.nodes
    { graph = Graph.fromNodes graph0.root allNodes
      history = History.empty
      revision = Revision.Zero }

let private specialNode kind name =
    let id = NodeId.New()
    id,
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        kind = Special kind)

let private actionState () =
    let state = ModelBuilder.createState12 ()
    let root = state.graph.nodes.[state.graph.root]
    let node = state.graph.nodes.[root.children.Head.id]
    state, node

[<Fact>]
let ``ChangeRequest Change materializes unchanged change`` () =
    let state, node = actionState ()
    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops = [ Op.SetText(node.id, node.text, "changed") ] }
    match History.applyAction (ChangeRequest.Change change) state with
    | Error error -> failwith error
    | Ok (next, materialized) ->
        Assert.Equal(change, materialized)
        Assert.Equal("changed", next.graph.nodes.[node.id].text)
        Assert.Equal<Change list>([ change ], next.history.past)

[<Fact>]
let ``ChangeRequest Undo and Redo materialize canonical operations with action identity`` () =
    let state, node = actionState ()
    let original =
        { id = 0
          changeId = Guid.NewGuid()
          ops = [ Op.SetText(node.id, node.text, "changed") ] }
    let changed =
        History.applyAction (ChangeRequest.Change original) state
        |> Result.map fst
        |> Result.defaultWith failwith
    let undoId = Guid.NewGuid()
    let undone, undoChange =
        History.applyAction (ChangeRequest.Undo(1, undoId)) changed
        |> Result.defaultWith failwith
    Assert.Equal(1, undoChange.id)
    Assert.Equal(undoId, undoChange.changeId)
    Assert.Equal<Op list>(
        [ Op.SetText(node.id, "changed", node.text) ],
        undoChange.ops)
    Assert.Equal(node.text, undone.graph.nodes.[node.id].text)
    let redoId = Guid.NewGuid()
    let redone, redoChange =
        History.applyAction (ChangeRequest.Redo(2, redoId)) undone
        |> Result.defaultWith failwith
    Assert.Equal(2, redoChange.id)
    Assert.Equal(redoId, redoChange.changeId)
    Assert.Equal<Op list>(original.ops, redoChange.ops)
    Assert.Equal("changed", redone.graph.nodes.[node.id].text)

[<Fact>]
let ``ChangeRequest Undo and Redo reject empty history`` () =
    let state, _ = actionState ()
    match History.applyAction (ChangeRequest.Undo(0, Guid.NewGuid())) state with
    | Ok _ -> failwith "expected empty Undo to fail"
    | Error error -> Assert.Contains("Undo", error)
    match History.applyAction (ChangeRequest.Redo(0, Guid.NewGuid())) state with
    | Ok _ -> failwith "expected empty Redo to fail"
    | Error error -> Assert.Contains("Redo", error)

[<Fact>]
let ``NewSpecialNode rejects reserved system names case-insensitively`` () =
    let state = ModelBuilder.createState12 ()
    [ Workspace, "gambol.workspace"
      Directory, "GAMBOL.Directory"
      File, "GaMbOl.file" ]
    |> List.iter (fun (kind, name) ->
        let _, message =
            Op.apply (Op.NewSpecialNode(NodeId.New(), kind, name)) state
            |> expectInvalid
        Assert.Contains("reserved", message))
    Op.apply (Op.NewSpecialNode(NodeId.New(), File, ".gitignore")) state
    |> expectChanged
    |> ignore

[<Fact>]
let ``SetName rejects a reserved system name case-insensitively`` () =
    let fileId, file = specialNode File "notes.txt"
    let state = stateWithNodes [ file ]
    let _, message =
        Op.apply (Op.SetName(fileId, "notes.txt", "GAMBOL.Metadata")) state
        |> expectInvalid
    Assert.Contains("reserved", message)

[<Fact>]
let ``NewSpecialNode rejects exact Directory File basename case-insensitively`` () =
    let state = ModelBuilder.createState12 ()
    [ File, ".amb"; Directory, ".AMB"; Workspace, ".Amb" ]
    |> List.iter (fun (kind, name) ->
        Op.apply (Op.NewSpecialNode(NodeId.New(), kind, name)) state
        |> expectInvalid
        |> ignore)

[<Fact>]
let ``SetName rejects rename to exact Directory File basename`` () =
    let fileId, file = specialNode File "notes.txt"
    let state = stateWithNodes [ file ]
    Op.apply (Op.SetName(fileId, "notes.txt", ".amb")) state
    |> expectInvalid
    |> ignore

[<Fact>]
let ``Replace rejects Special path under reserved ancestor but allows Normal child`` () =
    let directoryId, directory = specialNode Directory "Gambol.cache"
    let fileId, file = specialNode File "notes.txt"
    let normalId = NodeId.New()
    let normal = Node.Create(normalId, text = "ordinary")
    let state = stateWithNodes [ directory; file; normal ]
    let owner = ChildNode.owner
    let _, message =
        Op.apply (Op.Replace(directoryId, 0, [], [ owner fileId ])) state
        |> expectInvalid
    Assert.Contains("reserved", message)
    Op.apply (Op.Replace(directoryId, 0, [], [ owner normalId ])) state
    |> expectChanged
    |> ignore

[<Fact>]
let ``CreateState12 has empty history`` () =
    let state = ModelBuilder.createState12 ()
    Assert.Empty(state.history.past)
    Assert.Empty(state.history.future)

[<Fact>]
let ``NewChange uses next id and has no ops`` () =
    let history = History.empty
    let change: Change = History.newChange history
    Assert.Equal(0, change.id)
    Assert.Empty(change.ops)

[<Fact>]
let ``AddOp appends to change`` () =
    let history = History.empty
    let change0: Change = History.newChange history
    let op1 = Op.SetText(NodeId.New(), "", "x")
    let op2 = Op.SetText(NodeId.New(), "", "y")
    let change1 = Change.addOp op1 change0
    let change2 = Change.addOp op2 change1
    Assert.Equal<Op>([ op1; op2 ], change2.ops)

[<Fact>]
let ``AddChange pushes to past and clears future`` () =
    let history0 =
        { History.empty with
            future = [ { id = 99; changeId = System.Guid.NewGuid(); ops = [] } ] }

    let change: Change = History.newChange history0
    let history1 = History.addChange change history0
    // Emacs model: the existing future entry is folded back into past as an inverse,
    // so past = [newChange; invert(futureEntry)] — length 2.
    Assert.Equal(2, history1.past.Length)
    Assert.Equal(change, history1.past.[0])
    Assert.Empty(history1.future)

[<Fact>]
let ``Undo does nothing when past is empty`` () =
    let state = ModelBuilder.createState12 ()
    let state1 = History.undo state |> expectUnchanged
    Assert.Same(state, state1)

[<Fact>]
let ``Redo does nothing when future is empty`` () =
    let state = ModelBuilder.createState12 ()
    let state1 = History.redo state |> expectUnchanged
    Assert.Same(state, state1)

[<Fact>]
let ``Apply change that updates f g h text`` () =
    let state0 = ModelBuilder.createState12 ()
    let nodeF = findNodeByText "f" state0
    let nodeG = findNodeByText "g" state0
    let nodeH = findNodeByText "h" state0

    let change =
        History.newChange History.empty
        |> Change.addOp (Op.SetText(nodeF.id, nodeF.text, "newf"))
        |> Change.addOp (Op.SetText(nodeG.id, nodeG.text, "newg"))
        |> Change.addOp (Op.SetText(nodeH.id, nodeH.text, "newh"))

    let state1 = History.applyChange change state0 |> expectChanged

    let nodeF' = state1.graph.nodes |> Map.find nodeF.id
    let nodeG' = state1.graph.nodes |> Map.find nodeG.id
    let nodeH' = state1.graph.nodes |> Map.find nodeH.id

    Assert.Equal("newf", nodeF'.text)
    Assert.Equal("newg", nodeG'.text)
    Assert.Equal("newh", nodeH'.text)

    let state2 = History.undo state1 |> expectChanged

    let nodeF'' = state2.graph.nodes |> Map.find nodeF.id
    let nodeG'' = state2.graph.nodes |> Map.find nodeG.id
    let nodeH'' = state2.graph.nodes |> Map.find nodeH.id

    Assert.Equal(nodeF.text, nodeF''.text)
    Assert.Equal(nodeG.text, nodeG''.text)
    Assert.Equal(nodeH.text, nodeH''.text)

[<Fact>]
let ``Apply NewNode adds node to graph`` () =
    let state = ModelBuilder.createState12 ()
    let nodeId = NodeId.New()
    let op = Op.NewNode(nodeId, "hello")
    let state2 = Op.apply op state |> expectChanged
    Assert.True(Graph.contains nodeId state2.graph)
    let node = state2.graph.nodes |> Map.find nodeId
    Assert.Equal("hello", node.text)

[<Fact>]
let ``Apply NewNode with canonical root id is invalid`` () =
    let state = ModelBuilder.createState12 ()
    let op = Op.NewNode(Graph.rootId, "evil")
    let _, msg = Op.apply op state |> expectInvalid
    Assert.Contains("root", msg)

[<Fact>]
let ``Apply SetText updates node text`` () =
    let state = ModelBuilder.createState12 ()
    let rootNode = state.graph.nodes.[state.graph.root]
    let nodeId = rootNode.children.[0].id
    let oldText = state.graph.nodes.[nodeId].text
    let op = Op.SetText(nodeId, oldText, oldText + "!")
    let state2 = Op.apply op state |> expectChanged
    let node = state2.graph.nodes |> Map.find nodeId
    Assert.Equal(oldText + "!", node.text)

[<Fact>]
let ``Apply SetUpdateTime stamps without requiring old match`` () =
    let state = ModelBuilder.createState12 ()
    let rootNode = state.graph.nodes.[state.graph.root]
    let nodeId = rootNode.children.[0].id
    let stamp = DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc)
    let op =
        Op.SetUpdateTime(nodeId, NodeUpdateTime.missing, stamp)
    let state2 = Op.apply op state |> expectChanged
    Assert.Equal(
        NodeUpdateTime.toDbPrecision stamp,
        state2.graph.nodes.[nodeId].updateTime)
    let undone = Op.undo op state2 |> expectChanged
    Assert.Equal(
        NodeUpdateTime.missing,
        undone.graph.nodes.[nodeId].updateTime)

[<Fact>]
let ``PersistStamp opsBetween emits SetUpdateTime for changed stamps`` () =
    let state = ModelBuilder.createState12 ()
    let rootNode = state.graph.nodes.[state.graph.root]
    let nodeId = rootNode.children.[0].id
    let stamp = DateTime(2026, 7, 22, 15, 30, 0, DateTimeKind.Utc)
    let before = state.graph
    let oldTime = NodeUpdateTime.toDbPrecision before.nodes.[nodeId].updateTime
    let after =
        { before with
            nodes =
                Map.add
                    nodeId
                    (NodeUpdateTime.withStamp stamp before.nodes.[nodeId])
                    before.nodes }
    match PersistStamp.opsBetween before after with
    | [ Op.SetUpdateTime(id, oldT, newT) ] ->
        Assert.Equal(nodeId, id)
        Assert.Equal(oldTime, oldT)
        Assert.Equal(NodeUpdateTime.toDbPrecision stamp, newT)
    | other ->
        failwith $"expected one SetUpdateTime, got {other}"

[<Fact>]
let ``Apply SetText on canonical root is invalid`` () =
    let state = ModelBuilder.createState12 ()
    let op = Op.SetText(Graph.rootId, "ROOT", "x")
    let _, msg = Op.apply op state |> expectInvalid
    Assert.Contains("root", msg)

[<Fact>]
let ``Apply Replace updates parent children`` () =
    let state = ModelBuilder.createState12 ()
    let parentId = state.graph.root
    let parent = state.graph.nodes |> Map.find parentId
    let originalChildren = parent.children
    let oldChild0 = originalChildren |> List.head
    let newNode = ChildNode.New()
    let state1 = Op.apply (Op.NewNode(newNode.id, "new")) state |> expectChanged
    let op =
        Op.Replace(
            parentId,
            0,
            [ oldChild0 ],
            [ newNode ]
        )
    let state2 = Op.apply op state1 |> expectChanged
    let updatedParent = state2.graph.nodes |> Map.find parentId
    Assert.Equal(newNode.id, updatedParent.children.[0].id)

[<Fact>]
let ``Invalid move change does not modify graph`` () =
    let state0 = ModelBuilder.createState12 ()
    let parentId = state0.graph.root
    let parent0 = state0.graph.nodes |> Map.find parentId
    let originalChildren = parent0.children
    let first = originalChildren.[0]
    let second = originalChildren.[1]

    // Simulate a move as remove+insert, but make the remove invalid
    // by supplying a mismatched old span at index 0.
    let invalidRemove = Op.Replace(parentId, 0, [ second ], [])
    let insertAtEnd = Op.Replace(parentId, originalChildren.Length, [], [ first ])
    let moveChange =
        History.newChange state0.history
        |> Change.addOp invalidRemove
        |> Change.addOp insertAtEnd

    let stateAfter, _ = History.applyChange moveChange state0 |> expectInvalid
    let parentAfter = stateAfter.graph.nodes |> Map.find parentId
    Assert.Equal<ChildNode>(originalChildren, parentAfter.children)

[<Fact>]
let ``Move with correct old span is rejected when target is owned-descendant`` () =
    let state0 = ModelBuilder.createState12 ()
    let rootId = state0.graph.root
    let root = state0.graph.nodes |> Map.find rootId
    let childA = root.children.[0]
    let nodeA = state0.graph.nodes |> Map.find childA.id
    let childB = nodeA.children.[0]
    let originalRootChildren = root.children
    let originalBChildren = (state0.graph.nodes |> Map.find childB.id).children

    // Valid move-shape (remove+insert) using the correct old span.
    // Illegal by ownership semantics: moving A under its owned descendant B.
    let removeAFromRoot = Op.Replace(rootId, 0, [ childA ], [])
    let insertAUnderB = Op.Replace(childB.id, originalBChildren.Length, [], [ childA ])
    let moveChange =
        History.newChange state0.history
        |> Change.addOp removeAFromRoot
        |> Change.addOp insertAUnderB

    let stateAfter, _ = History.applyChange moveChange state0 |> expectInvalid
    let rootAfter = stateAfter.graph.nodes |> Map.find rootId
    let bAfter = stateAfter.graph.nodes |> Map.find childB.id

    Assert.Equal<ChildNode>(originalRootChildren, rootAfter.children)
    Assert.Equal<ChildNode>(originalBChildren, bAfter.children)

let private unparsedFileState () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let childId = NodeId.New()
    let otherId = NodeId.New()
    let holderId = NodeId.New()
    let file =
        Node.Create(
            fileId,
            text = "file.txt",
            name = Filename.create "file.txt",
            children = [ ChildNode.owner childId ],
            kind = Special File,
            documentState = Unparsed)
    let child = Node.Create(childId, text = "body", owner = fileId)
    let other =
        Node.Create(
            otherId,
            text = "other.txt",
            name = Filename.create "other.txt",
            kind = Special File)
    let holder =
        Node.Create(
            holderId,
            text = "holder",
            children = [ ChildNode.reference fileId ])
    let root = graph0.nodes.[Graph.rootId]
    let additions =
        [ ChildNode.owner fileId
          ChildNode.owner otherId
          ChildNode.owner holderId ]
    let nodes =
        graph0.nodes
        |> Map.add Graph.rootId { root with children = root.children @ additions }
        |> Map.add fileId file
        |> Map.add childId child
        |> Map.add otherId other
        |> Map.add holderId holder
    let graph = Graph.fromNodes graph0.root nodes
    { graph = graph; history = History.empty; revision = Revision.Zero },
    fileId,
    childId,
    otherId,
    holderId

let private unparsedError = "operation cannot modify an unparsed document; parse it first"

let private assertUnparsedInvalid state op =
    let unchanged, error = Op.apply op state |> expectInvalid
    Assert.Equal(unparsedError, error)
    Assert.Equal(state.graph, unchanged.graph)

[<Fact>]
let ``edit or rename of unparsed file root is rejected`` () =
    let state, fileId, _, _, _ = unparsedFileState ()
    assertUnparsedInvalid state (Op.SetText(fileId, "file.txt", "changed"))
    assertUnparsedInvalid state (Op.SetName(fileId, "file.txt", "renamed.txt"))

[<Fact>]
let ``edit or rename of unparsed file descendant is rejected`` () =
    let state, _, childId, _, _ = unparsedFileState ()
    assertUnparsedInvalid state (Op.SetText(childId, "body", "changed"))
    assertUnparsedInvalid state (Op.SetName(childId, "", "renamed"))

[<Fact>]
let ``edit of no-server-file document is rejected`` () =
    let state, fileId, _, _, _ = unparsedFileState ()
    let absent =
        Op.apply
            (Op.SetDocumentState(fileId, Unparsed, NoServerFile))
            state
        |> expectChanged
    assertUnparsedInvalid
        absent
        (Op.SetText(fileId, "file.txt", "changed"))

[<Fact>]
let ``structural relocate of unparsed file to start among siblings succeeds`` () =
    let state, fileId, _, _, _ = unparsedFileState ()
    let oldChildren = state.graph.nodes.[Graph.rootId].children
    let index = oldChildren |> List.findIndex (fun child -> child.id = fileId)
    let occurrence = oldChildren.[index]
    let without =
        oldChildren
        |> List.indexed
        |> List.filter (fun (i, _) -> i <> index)
        |> List.map snd
    let newChildren = occurrence :: without
    let changed =
        Op.apply (Op.Replace(Graph.rootId, 0, oldChildren, newChildren)) state
        |> expectChanged
    Assert.Equal(fileId, changed.graph.nodes.[Graph.rootId].children.Head.id)
    Assert.Equal(Unparsed, changed.graph.nodes.[fileId].documentState)

[<Fact>]
let ``operation in unrelated current document remains valid`` () =
    let state, _, _, otherId, _ = unparsedFileState ()
    let changed =
        Op.apply (Op.SetText(otherId, "other.txt", "changed")) state
        |> expectChanged
    Assert.Equal("changed", changed.graph.nodes.[otherId].text)

[<Fact>]
let ``ref occurrence is governed by occurrence document not target document`` () =
    let state, fileId, _, _, holderId = unparsedFileState ()
    let oldRef = state.graph.nodes.[holderId].children.Head
    let replacementId = NodeId.New()
    let withReplacement =
        Op.apply (Op.NewNode(replacementId, "replacement")) state
        |> expectChanged
    let replacement = ChildNode.owner replacementId
    let changed =
        Op.apply (Op.Replace(holderId, 0, [ oldRef ], [ replacement ])) withReplacement
        |> expectChanged
    Assert.Equal(replacementId, changed.graph.nodes.[holderId].children.Head.id)
    assertUnparsedInvalid state (Op.SetText(fileId, "file.txt", "changed"))

[<Fact>]
let ``parse state transition before tree mutation succeeds and reverse order fails`` () =
    let state, fileId, _, _, _ = unparsedFileState ()
    let parsedId = NodeId.New()
    let attach = ChildNode.owner parsedId
    let parseChange =
        { id = 0
          changeId = System.Guid.NewGuid()
          ops =
            [ Op.SetDocumentState(fileId, Unparsed, Current)
              Op.NewNode(parsedId, "parsed")
              Op.Replace(fileId, 1, [], [ attach ]) ] }
    let parsed = History.applyChange parseChange state |> expectChanged
    Assert.Equal(Current, parsed.graph.nodes.[fileId].documentState)
    Assert.Equal(parsedId, parsed.graph.nodes.[fileId].children.[1].id)

    let reverse =
        { parseChange with
            changeId = System.Guid.NewGuid()
            ops =
                [ Op.NewNode(parsedId, "parsed")
                  Op.Replace(fileId, 1, [], [ attach ])
                  Op.SetDocumentState(fileId, Unparsed, Current) ] }
    let rejected, error = History.applyChange reverse state |> expectInvalid
    Assert.Equal(unparsedError, error)
    Assert.Equal(state.graph, rejected.graph)

[<Fact>]
let ``marking document unparsed remains legal`` () =
    let state, _, _, otherId, _ = unparsedFileState ()
    let changed =
        Op.apply (Op.SetDocumentState(otherId, Current, Unparsed)) state
        |> expectChanged
    Assert.Equal(Unparsed, changed.graph.nodes.[otherId].documentState)

[<Fact>]
let ``valid parse batch can replay undo and redo`` () =
    let state, fileId, childId, _, _ = unparsedFileState ()
    let change =
        { id = 0
          changeId = System.Guid.NewGuid()
          ops =
            [ Op.SetDocumentState(fileId, Unparsed, Current)
              Op.SetText(childId, "body", "parsed") ] }
    let applied = History.applyChange change state |> expectChanged
    let undone = History.undo applied |> expectChanged
    Assert.Equal(Unparsed, undone.graph.nodes.[fileId].documentState)
    Assert.Equal("body", undone.graph.nodes.[childId].text)
    let redone = History.redo undone |> expectChanged
    Assert.Equal(Current, redone.graph.nodes.[fileId].documentState)
    Assert.Equal("parsed", redone.graph.nodes.[childId].text)

[<Fact>]
let ``nested file parse under current directory replaces file tree`` () =
    let graph0 = Graph.create ()
    let workspaceId, workspaceOps =
        FileNodeOps.planCreateWorkspace graph0 "home"
    let state0 =
        workspaceOps
        |> List.fold (fun state op -> Op.apply op state |> expectChanged)
            { graph = graph0; history = History.empty; revision = Revision.Zero }
    let directoryId, directoryOps =
        FileNodeOps.planCreateOwnedDirectory state0.graph workspaceId "docs"
    let state1 =
        directoryOps
        |> List.fold (fun state op -> Op.apply op state |> expectChanged) state0
    let fileId, fileOps =
        FileNodeOps.planCreateOwnedFile state1.graph directoryId "note.txt"
    let state2 =
        fileOps
        |> List.fold (fun state op -> Op.apply op state |> expectChanged) state1
    let fileUnparsed =
        Op.apply (Op.SetDocumentState(fileId, Current, Unparsed)) state2
        |> expectChanged
    Assert.Equal(Current, fileUnparsed.graph.nodes.[directoryId].documentState)
    let parsedId = NodeId.New()
    let attach = ChildNode.owner parsedId
    let parseChange =
        { id = 0
          changeId = System.Guid.NewGuid()
          ops =
            [ Op.SetDocumentState(fileId, Unparsed, Current)
              Op.NewNode(parsedId, "parsed")
              Op.Replace(fileId, 0, [], [ attach ]) ] }
    let parsed = History.applyChange parseChange fileUnparsed |> expectChanged
    Assert.Equal(Current, parsed.graph.nodes.[fileId].documentState)
    Assert.Equal(Current, parsed.graph.nodes.[directoryId].documentState)
    Assert.Equal(parsedId, parsed.graph.nodes.[fileId].children.Head.id)

[<Fact>]
let ``nested file parse still allowed when enclosing directory is unparsed`` () =
    let graph0 = Graph.create ()
    let workspaceId, workspaceOps =
        FileNodeOps.planCreateWorkspace graph0 "home"
    let state0 =
        workspaceOps
        |> List.fold (fun state op -> Op.apply op state |> expectChanged)
            { graph = graph0; history = History.empty; revision = Revision.Zero }
    let directoryId, directoryOps =
        FileNodeOps.planCreateOwnedDirectory state0.graph workspaceId "docs"
    let state1 =
        directoryOps
        |> List.fold (fun state op -> Op.apply op state |> expectChanged) state0
    let fileId, fileOps =
        FileNodeOps.planCreateOwnedFile state1.graph directoryId "note.txt"
    let state2 =
        fileOps
        |> List.fold (fun state op -> Op.apply op state |> expectChanged) state1
    // Legitimate Directory Unparsed (e.g. `.amb` modified), not upload stubs.
    let bothUnparsed =
        [ Op.SetDocumentState(directoryId, Current, Unparsed)
          Op.SetDocumentState(fileId, Current, Unparsed) ]
        |> List.fold (fun state op -> Op.apply op state |> expectChanged) state2
    let parsedId = NodeId.New()
    let attach = ChildNode.owner parsedId
    let parseChange =
        { id = 0
          changeId = System.Guid.NewGuid()
          ops =
            [ Op.SetDocumentState(fileId, Unparsed, Current)
              Op.NewNode(parsedId, "parsed")
              Op.Replace(fileId, 0, [], [ attach ]) ] }
    let parsed = History.applyChange parseChange bothUnparsed |> expectChanged
    Assert.Equal(Current, parsed.graph.nodes.[fileId].documentState)
    Assert.Equal(Unparsed, parsed.graph.nodes.[directoryId].documentState)
    Assert.Equal(parsedId, parsed.graph.nodes.[fileId].children.Head.id)

[<Fact>]
let ``unparsed invariant also applies to directory and workspace documents`` () =
    let graph0 = Graph.create ()
    let workspaceId, workspaceOps =
        FileNodeOps.planCreateWorkspace graph0 "home"
    let state0 =
        workspaceOps
        |> List.fold (fun state op -> Op.apply op state |> expectChanged)
            { graph = graph0; history = History.empty; revision = Revision.Zero }
    let directoryId, directoryOps =
        FileNodeOps.planCreateOwnedDirectory state0.graph workspaceId "docs"
    let state1 =
        directoryOps
        |> List.fold (fun state op -> Op.apply op state |> expectChanged) state0
    let directoryUnparsed =
        Op.apply
            (Op.SetDocumentState(directoryId, Current, Unparsed))
            state1
        |> expectChanged
    assertUnparsedInvalid
        directoryUnparsed
        (Op.SetName(directoryId, "docs", "renamed"))
    let workspaceUnparsed =
        Op.apply
            (Op.SetDocumentState(workspaceId, Current, Unparsed))
            state1
        |> expectChanged
    assertUnparsedInvalid
        workspaceUnparsed
        (Op.SetName(directoryId, "docs", "renamed"))

let private graphWithDistantFileUnderFileViolation () =
    let graph0 = Graph.create ()
    let fileAId, fileA = specialNode File "outer.txt"
    let fileBId, fileB = specialNode File "inner.txt"
    let owner = ChildNode.owner
    let root = graph0.nodes.[Graph.rootId]
    let nodes =
        graph0.nodes
        |> Map.add Graph.rootId { root with children = root.children @ [ owner fileAId ] }
        |> Map.add fileAId { fileA with children = [ owner fileBId ] }
        |> Map.add fileBId fileB
    let graph = Graph.fromNodes graph0.root nodes
    graph, fileAId, fileBId

[<Fact>]
let ``SetClasses via applyChange succeeds despite distant ownership violation`` () =
    let graph, fileAId, fileBId = graphWithDistantFileUnderFileViolation ()
    match History.validateOwnership graph with
    | Ok () -> failwith "expected global ownership validation to fail"
    | Error _ -> ()
    match History.validateOwnershipLocated graph with
    | Ok () -> failwith "expected located ownership validation to fail"
    | Error (msg, nodeId) ->
        Assert.Contains("File and Directory", msg)
        Assert.Equal(fileBId, nodeId)
    let state =
        { graph = graph; history = History.empty; revision = Revision.Zero }
    let fileB = state.graph.nodes.[fileBId]
    let change =
        History.newChange History.empty
        |> Change.addOp (Op.SetClasses(fileBId, fileB.cssClasses, CssClass.ofList [ "edited" ]))
    let result = History.applyChange change state |> expectChanged
    Assert.Equal(CssClass.ofList [ "edited" ], result.graph.nodes.[fileBId].cssClasses)
    Assert.Equal(fileAId, result.graph.nodes.[fileAId].id)

[<Fact>]
let ``validateOwnershipLocated Ok when Ref owner defaults to ROOT`` () =
    // Selective-load shape: Ref under Loaded parent, real Owner Unloaded, but
    // Node.owner was defaulted/rewritten to ROOT (Guid.Empty) — incomplete, not proven.
    let graph0 = Graph.create ()
    let childId = NodeId.New()
    let child = Node.Create(childId, text = "orphan-ref")
    let root = graph0.nodes.[Graph.rootId]
    let nodes =
        graph0.nodes
        |> Map.add Graph.rootId
            { root with
                children =
                    root.children @ [ ChildNode.reference childId ] }
        |> Map.add childId child
    let graph = Graph.fromNodes graph0.root nodes
    match History.validateOwnershipLocated graph with
    | Ok () -> ()
    | Error (msg, _) -> Assert.True(false, $"expected Ok, got Error: {msg}")

[<Fact>]
let ``validateOwnershipLocated Ok when Ref owner parent is Unloaded`` () =
    let graph0 = Graph.create ()
    let owner = ChildNode.owner
    let refOf = ChildNode.reference
    let ownerParentId = NodeId.New()
    let loadedParentId = NodeId.New()
    let headerId = NodeId.New()
    let ownerParent =
        Node.Create(
            ownerParentId,
            text = "owner-unloaded",
            childrenStatus = Unloaded,
            owner = Graph.rootId)
    let loadedParent =
        Node.Create(
            loadedParentId,
            text = "loaded-parent",
            children = [ refOf headerId ],
            owner = Graph.rootId)
    let header =
        Node.Create(
            headerId,
            text = "ref-header",
            childrenStatus = Unloaded,
            owner = ownerParentId)
    let root = graph0.nodes.[Graph.rootId]
    let nodes =
        graph0.nodes
        |> Map.add Graph.rootId
            { root with
                children =
                    root.children
                    @ [ owner ownerParentId; owner loadedParentId ] }
        |> Map.add ownerParentId ownerParent
        |> Map.add loadedParentId loadedParent
        |> Map.add headerId header
    let graph = Graph.fromNodes graph0.root nodes
    match History.validateOwnershipLocated graph with
    | Ok () -> ()
    | Error (msg, _) -> Assert.True(false, $"expected Ok, got Error: {msg}")

[<Fact>]
let ``validateOwnershipLocated Ok when Ref owner defaulted to ROOT with Unloaded real owner`` () =
    let graph0 = Graph.create ()
    let owner = ChildNode.owner
    let refOf = ChildNode.reference
    let ownerParentId = NodeId.New()
    let loadedParentId = NodeId.New()
    let headerId = NodeId.New()
    let ownerParent =
        Node.Create(
            ownerParentId,
            text = "owner-unloaded",
            childrenStatus = Unloaded,
            owner = Graph.rootId)
    let loadedParent =
        Node.Create(
            loadedParentId,
            text = "loaded-parent",
            children = [ refOf headerId ],
            owner = Graph.rootId)
    // Claim defaulted to ROOT despite Unloaded real owner existing (appendChildren path).
    let header =
        Node.Create(
            headerId,
            text = "LAPS-log",
            childrenStatus = Unloaded,
            owner = Graph.rootId)
    let root = graph0.nodes.[Graph.rootId]
    let nodes =
        graph0.nodes
        |> Map.add Graph.rootId
            { root with
                children =
                    root.children
                    @ [ owner ownerParentId; owner loadedParentId ] }
        |> Map.add ownerParentId ownerParent
        |> Map.add loadedParentId loadedParent
        |> Map.add headerId header
    let graph = Graph.fromNodes graph0.root nodes
    Assert.Equal(Graph.rootId, graph.nodes.[headerId].owner)
    match History.validateOwnershipLocated graph with
    | Ok () -> ()
    | Error (msg, _) -> Assert.True(false, $"expected Ok, got Error: {msg}")

[<Fact>]
let ``validateOwnershipLocated Error when Ref owner parent is Loaded without Owner`` () =
    let graph0 = Graph.create ()
    let owner = ChildNode.owner
    let refOf = ChildNode.reference
    let claimedOwnerId = NodeId.New()
    let loadedParentId = NodeId.New()
    let headerId = NodeId.New()
    let claimedOwner =
        Node.Create(
            claimedOwnerId,
            text = "claimed-owner",
            children = [],
            owner = Graph.rootId)
    let loadedParent =
        Node.Create(
            loadedParentId,
            text = "loaded-parent",
            children = [ refOf headerId ],
            owner = Graph.rootId)
    let header =
        Node.Create(
            headerId,
            text = "orphan-ref-header",
            owner = claimedOwnerId)
    let root = graph0.nodes.[Graph.rootId]
    let nodes =
        graph0.nodes
        |> Map.add Graph.rootId
            { root with
                children =
                    root.children
                    @ [ owner claimedOwnerId; owner loadedParentId ] }
        |> Map.add claimedOwnerId claimedOwner
        |> Map.add loadedParentId loadedParent
        |> Map.add headerId header
    let graph = Graph.fromNodes graph0.root nodes
    match History.validateOwnershipLocated graph with
    | Ok () -> Assert.True(false, "expected Error")
    | Error (msg, nodeId) ->
        Assert.Contains("missing owner", msg)
        Assert.Equal(headerId, nodeId)

[<Fact>]
let ``validateOwnershipLocated reports multiple owner occurrences with ids`` () =
    let graph0 = Graph.create ()
    let childId = NodeId.New()
    let parentAId = NodeId.New()
    let parentBId = NodeId.New()
    let owner = ChildNode.owner
    let child = Node.Create(childId, text = "child")
    let parentA = { Node.Create(parentAId, text = "a") with children = [ owner childId ] }
    let parentB = { Node.Create(parentBId, text = "b") with children = [ owner childId ] }
    let root = graph0.nodes.[Graph.rootId]
    let nodes =
        graph0.nodes
        |> Map.add Graph.rootId
            { root with children = root.children @ [ owner parentAId; owner parentBId ] }
        |> Map.add parentAId parentA
        |> Map.add parentBId parentB
        |> Map.add childId child
    let graph = Graph.fromNodes graph0.root nodes
    match History.validateOwnershipLocated graph with
    | Ok () -> Assert.True(false, "expected Error")
    | Error (msg, nodeId) ->
        Assert.Contains("expected exactly one owner occurrence", msg)
        Assert.Contains("text='child'", msg)
        Assert.Contains($"id={NodeId.GuidTail8 childId.Value}", msg)
        Assert.Contains(NodeId.GuidTail8 parentAId.Value, msg)
        Assert.Contains(NodeId.GuidTail8 parentBId.Value, msg)
        Assert.Equal(childId, nodeId)

[<Fact>]
let ``validateOwnershipLocated reports owner chain that does not reach root`` () =
    let graph0 = Graph.create ()
    let aId = NodeId.New()
    let bId = NodeId.New()
    let owner = ChildNode.owner
    let a = { Node.Create(aId, text = "a") with children = [ owner bId ] }
    let b = { Node.Create(bId, text = "b") with children = [ owner aId ] }
    let nodes =
        graph0.nodes
        |> Map.add aId a
        |> Map.add bId b
    let graph = Graph.fromNodes graph0.root nodes
    match History.validateOwnershipLocated graph with
    | Ok () -> Assert.True(false, "expected Error")
    | Error (msg, nodeId) ->
        Assert.Contains("owner chain does not reach root", msg)
        Assert.True(
            msg.Contains("text='a'") || msg.Contains("text='b'"),
            $"expected located node text in msg: {msg}")
        Assert.Contains($"id={NodeId.GuidTail8 nodeId.Value}", msg)
        Assert.Contains(NodeId.GuidTail8 aId.Value, msg)
        Assert.Contains(NodeId.GuidTail8 bId.Value, msg)
        Assert.True(nodeId = aId || nodeId = bId)

[<Fact>]
let ``validateOwnershipLocated reports duplicate artifact name`` () =
    let graph0 = Graph.create ()
    let d1Id, d1 = specialNode Directory "dup"
    let d2Id, d2 = specialNode Directory "dup"
    let owner = ChildNode.owner
    let root = graph0.nodes.[Graph.rootId]
    let nodes =
        graph0.nodes
        |> Map.add Graph.rootId
            { root with children = root.children @ [ owner d1Id; owner d2Id ] }
        |> Map.add d1Id d1
        |> Map.add d2Id d2
    let graph = Graph.fromNodes graph0.root nodes
    match History.validateOwnershipLocated graph with
    | Ok () -> Assert.True(false, "expected Error")
    | Error (msg, nodeId) ->
        Assert.Contains("duplicate name", msg)
        Assert.Contains("dup", msg)
        Assert.True(nodeId = d1Id || nodeId = d2Id)

[<Fact>]
let ``local shape op succeeds despite distant ownership violation`` () =
    let graph, _, _ = graphWithDistantFileUnderFileViolation ()
    let state =
        { graph = graph; history = History.empty; revision = Revision.Zero }
    let newId = NodeId.New()
    let change =
        History.newChange History.empty
        |> Change.addOp (Op.NewNode(newId, "sibling"))
        |> Change.addOp (
            Op.Replace(
                Graph.rootId,
                state.graph.nodes.[Graph.rootId].children.Length,
                [],
                [ ChildNode.owner newId ]))
    History.applyChange change state |> expectChanged |> ignore

[<Fact>]
let ``childOwnership follows edge.ref even when Node.owner matches parent`` () =
    let state0 = ModelBuilder.createState12 ()
    let parentId = state0.graph.root
    let owned = state0.graph.nodes.[parentId].children.Head
    Assert.Equal(Ownership.Owner, owned.ref)
    Assert.Equal(parentId, state0.graph.nodes.[owned.id].owner)
    let asRef = ChildNode.reference owned.id
    Assert.Equal(Ownership.Ref, Node.childOwnership state0.graph parentId asRef)

[<Fact>]
let ``applyChange accepts same-parent Owner then Ref (Duplicate link)`` () =
    let state0 = ModelBuilder.createState12 ()
    let parent = state0.graph.nodes.[state0.graph.root]
    let ownedChild = parent.children.Head
    let insertAt = parent.children.Length
    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            [ Op.Replace(
                  state0.graph.root,
                  insertAt,
                  [],
                  [ ChildNode.reference ownedChild.id ]) ] }
    let state1 = History.applyChange change state0 |> expectChanged
    let kids = state1.graph.nodes.[state0.graph.root].children
    Assert.Equal(insertAt + 1, kids.Length)
    Assert.Equal(ChildNode.reference ownedChild.id, kids.[insertAt])
    Assert.Equal(Ownership.Ref, Node.childOwnership state1.graph state0.graph.root kids.[insertAt])

/// Live Duplicate inserts at selection endd (often mid-list) → fromNodes, not appendChildren.
[<Fact>]
let ``applyChange accepts mid-list same-parent Ref (Duplicate link)`` () =
    let state0 = ModelBuilder.createState12 ()
    let parentId = state0.graph.root
    let kids0 = state0.graph.nodes.[parentId].children
    Assert.True(kids0.Length >= 2, "need a mid-list insert slot")
    let ownedChild = kids0.Head
    let insertAt = 1
    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            [ Op.Replace(parentId, insertAt, [], [ ChildNode.reference ownedChild.id ]) ] }
    let state1 = History.applyChange change state0 |> expectChanged
    let kids = state1.graph.nodes.[parentId].children
    Assert.Equal(kids0.Length + 1, kids.Length)
    Assert.Equal(ChildNode.reference ownedChild.id, kids.[insertAt])
    Assert.Equal(Ownership.Ref, Node.childOwnership state1.graph parentId kids.[insertAt])
    match
        SyncPlanner.applyAndEnqueueLocalAction
            (ChangeRequest.Change change)
            state0
            SyncInfo.initial
    with
    | Error msg -> failwithf "Duplicate mid-list applyAndPost path failed: %s" msg
    | Ok (st, _, _) ->
        let kids2 = st.graph.nodes.[parentId].children
        Assert.Equal(ChildNode.reference ownedChild.id, kids2.[insertAt])

/// Replace parent is always in Op.involvedNodeIds historically; ownership scope must not
/// re-validate the parent's own dual-Owner merely because a Ref was inserted under it.
let private graphWithDualOwnedParentAndChild () =
    let graph0 = Graph.create ()
    let parentId = NodeId.New()
    let altParentId = NodeId.New()
    let childId = NodeId.New()
    let owner = ChildNode.owner
    let parent = { Node.Create(parentId, text = "parent") with children = [ owner childId ] }
    let altParent = Node.Create(altParentId, text = "alt")
    let child = Node.Create(childId, text = "child")
    let root = graph0.nodes.[Graph.rootId]
    let nodes =
        graph0.nodes
        |> Map.add Graph.rootId
            { root with children = root.children @ [ owner parentId; owner altParentId ] }
        |> Map.add parentId parent
        // Second Owner edge to parent (pre-existing graph dirt).
        |> Map.add altParentId { altParent with children = [ owner parentId ] }
        |> Map.add childId child
    let graph = Graph.fromNodes graph0.root nodes
    graph, parentId, childId

[<Fact>]
let ``Duplicate Ref succeeds despite dual-Owned Replace parent`` () =
    let graph, parentId, childId = graphWithDualOwnedParentAndChild ()
    match History.validateOwnershipLocated graph with
    | Ok () -> failwith "expected dual-Owner parent seed"
    | Error (msg, _) -> Assert.Contains("expected exactly one owner occurrence", msg)
    let state =
        { graph = graph; history = History.empty; revision = Revision.Zero }
    let insertAt = state.graph.nodes.[parentId].children.Length
    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            [ Op.Replace(parentId, insertAt, [], [ ChildNode.reference childId ]) ] }
    let state1 = History.applyChange change state |> expectChanged
    let kids = state1.graph.nodes.[parentId].children
    Assert.Equal(ChildNode.reference childId, kids.[insertAt])

[<Fact>]
let ``Duplicate Ref succeeds despite distant dual-Owner`` () =
    let graph0 = Graph.create ()
    let parentId = NodeId.New()
    let childId = NodeId.New()
    let u1 = NodeId.New()
    let u2 = NodeId.New()
    let victim = NodeId.New()
    let owner = ChildNode.owner
    let parent = { Node.Create(parentId, text = "parent") with children = [ owner childId ] }
    let child = Node.Create(childId, text = "child")
    let root = graph0.nodes.[Graph.rootId]
    let nodes =
        graph0.nodes
        |> Map.add Graph.rootId
            { root with
                children =
                    root.children
                    @ [ owner parentId; owner u1; owner u2 ] }
        |> Map.add parentId parent
        |> Map.add childId child
        |> Map.add u1 { Node.Create(u1, text = "u1") with children = [ owner victim ] }
        |> Map.add u2 { Node.Create(u2, text = "u2") with children = [ owner victim ] }
        |> Map.add victim (Node.Create(victim, text = "victim"))
    let graph = Graph.fromNodes graph0.root nodes
    match History.validateOwnershipLocated graph with
    | Ok () -> failwith "expected distant dual-Owner seed"
    | Error _ -> ()
    let state =
        { graph = graph; history = History.empty; revision = Revision.Zero }
    let insertAt = state.graph.nodes.[parentId].children.Length
    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            [ Op.Replace(parentId, insertAt, [], [ ChildNode.reference childId ]) ] }
    History.applyChange change state |> expectChanged |> ignore

[<Fact>]
let ``applyChange rejects Replace that introduces a second Owner edge`` () =
    let state0 = ModelBuilder.createState12 ()
    let rootKids = state0.graph.nodes.[state0.graph.root].children
    let parentA = rootKids.[0].id
    let parentB = rootKids.[1].id
    let ownedUnderA = state0.graph.nodes.[parentA].children.Head.id
    let insertAt = state0.graph.nodes.[parentB].children.Length
    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            [ Op.Replace(parentB, insertAt, [], [ ChildNode.owner ownedUnderA ]) ] }
    let _, msg = History.applyChange change state0 |> expectInvalid
    Assert.Contains("expected exactly one owner occurrence", msg)

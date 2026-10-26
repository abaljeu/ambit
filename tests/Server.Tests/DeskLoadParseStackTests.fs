module Gambol.Server.Tests.DeskLoadParseStackTests

open Xunit
open Gambol.Client
open Gambol.Shared
open Gambol.Shared.ViewModel

let private applyOps (graph: Graph) (ops: Op list) =
    ops
    |> List.fold
        (fun current op ->
            match Op.apply op current with
            | ApplyResult.Changed state -> state
            | ApplyResult.Unchanged state -> state
            | ApplyResult.Invalid(_, message) -> failwith message)
        { graph = graph; eventId = EventId.zero }
    |> fun state -> state.graph

let private addWorkspace label graph =
    let id, ops = FileNodeOps.planCreateWorkspace graph label
    id, applyOps graph ops

let private addDirectory graph parentId name =
    let id, ops = FileNodeOps.planCreateOwnedDirectory graph parentId name
    id, applyOps graph ops

let private addFile graph parentId name =
    let id, ops = FileNodeOps.planCreateOwnedFile graph parentId name
    id, applyOps graph ops

let private markCurrent (graph: Graph) (nodeId: NodeId) =
    match Map.tryFind nodeId graph.nodes with
    | None -> graph
    | Some node ->
        { graph with
            nodes =
                Map.add nodeId (Node.withDocumentState Current node) graph.nodes }

let private selectRange (model: VM) (startIndex: int) (endIndex: int) =
    let parent = model.siteMap.entries.[model.siteMap.rootId]
    { model with
        selectedNodes =
            Some
                { range =
                    { parent = parent
                      start = startIndex
                      endd = endIndex }
                  focus = startIndex } }

let private loadFocuses (effects: Effect list) =
    effects
    |> List.choose (function
        | Effect.SubmitLoadSaveCommand (request, _)
            when request.operation = LoadSaveOperation.Load ->
            Some request.start.focusId
        | _ -> None)

let private postsReconcileOrParse (effects: Effect list) =
    effects
    |> List.exists (function
        | Effect.ContinueDirectoryReconcile _ -> true
        | _ -> false)

let private withDesktopMap (model: VM) =
    { model with
        desktopCapabilities =
            Some (DesktopCapabilities.desktopEnabled true)
        workspaceMappedLabels = Set.singleton "home" }

let private stackLoads (effects: Effect list) =
    effects
    |> List.choose (function
        | Effect.SubmitLoadSaveCommand (request, plan)
            when request.operation = LoadSaveOperation.Load ->
            Some (request, plan)
        | _ -> None)

[<Fact>]
let ``desktop-mapped Load and git Load after-step use the Parse stack`` () =
    let wsId, graph1 = Graph.create () |> addWorkspace "home"
    let fileId, graph = addFile graph1 wsId "note.txt"
    let fileModel =
        VmTestHelpers.emptyModelAt graph wsId
        |> withDesktopMap
        |> fun ready -> selectRange ready 0 1
    let _, fileEffects = UpdateWorkspaceLoad.loadOp fileModel
    match stackLoads fileEffects with
    | [ request, Some (WorkspaceUploadAction.DesktopPush (Some id)) ] ->
        Assert.Equal(fileId, id)
        Assert.Equal(fileId, request.start.focusId)
        Assert.True(request.holdStack)
    | other ->
        Assert.Fail($"expected a held file push, got {other}")
    Assert.False(postsReconcileOrParse fileEffects)
    let _, afterFile =
        UpdateWorkspaceSync.loadAfterDesktopPush (Some fileId) fileModel
    Assert.False(postsReconcileOrParse afterFile)
    match stackLoads afterFile with
    | [ request, Some (WorkspaceUploadAction.ParseServerDisk id) ] ->
        Assert.Equal(fileId, id)
        Assert.Equal(fileId, request.start.focusId)
        Assert.False(request.holdStack)
        Assert.Equal(LoadSavePrePick.Desk, request.prePick)
    | other ->
        Assert.Fail($"expected file Load on the stack, got {other}")

    let dirId, dirGraph = addDirectory graph1 wsId "docs"
    let dirModel =
        VmTestHelpers.emptyModelAt dirGraph wsId
        |> withDesktopMap
        |> fun ready -> selectRange ready 0 1
    let _, dirEffects = UpdateWorkspaceLoad.loadOp dirModel
    match stackLoads dirEffects with
    | [ request, Some (WorkspaceUploadAction.DesktopPush None) ] ->
        Assert.Equal(dirId, request.start.focusId)
        Assert.True(request.holdStack)
    | other ->
        Assert.Fail($"expected a held directory push, got {other}")
    Assert.False(postsReconcileOrParse dirEffects)
    let _, afterDir =
        UpdateWorkspaceSync.loadAfterDesktopPush (Some dirId) dirModel
    Assert.False(postsReconcileOrParse afterDir)
    match stackLoads afterDir with
    | [ request, Some WorkspaceUploadAction.ReconcileServerDisk ] ->
        Assert.Equal(dirId, request.start.focusId)
        Assert.False(request.holdStack)
    | other ->
        Assert.Fail($"expected directory Load on the stack, got {other}")

    let _, gitEffects =
        UpdateWorkspaceLoad.loadOpFor LoadSavePrePick.Git fileModel
    Assert.False(postsReconcileOrParse gitEffects)
    match stackLoads gitEffects with
    | [ request, None ] ->
        Assert.Equal(LoadSavePrePick.Git, request.prePick)
        Assert.False(request.holdStack)
        Assert.Equal(fileId, request.start.focusId)
    | other ->
        Assert.Fail($"expected git Load without a client hop, got {other}")

[<Fact>]
let ``Directory Load posts that directory on the Load command`` () =
    let wsId, graph1 = Graph.create () |> addWorkspace "home"
    let dirId, graph = addDirectory graph1 wsId "docs"
    let model =
        VmTestHelpers.emptyModelAt graph wsId
        |> fun ready -> selectRange ready 0 1
    let _, effects = UpdateWorkspaceLoad.loadOp model
    Assert.Equal<NodeId list>([ dirId ], loadFocuses effects)
    Assert.False(postsReconcileOrParse effects)
    let _, after = UpdateWorkspaceLoad.deskLoadOp model
    Assert.False(postsReconcileOrParse after)
    Assert.Empty(loadFocuses after)

[<Fact>]
let ``File Load posts that file on the Load command`` () =
    let wsId, graph1 = Graph.create () |> addWorkspace "home"
    let fileId, graph = addFile graph1 wsId "note.txt"
    let model =
        VmTestHelpers.emptyModelAt graph wsId
        |> fun ready -> selectRange ready 0 1
    let _, effects = UpdateWorkspaceLoad.loadOp model
    Assert.Equal<NodeId list>([ fileId ], loadFocuses effects)
    Assert.False(postsReconcileOrParse effects)

[<Fact>]
let ``two selected files post one Load command each`` () =
    let wsId, graph1 = Graph.create () |> addWorkspace "home"
    let firstId, graph2 = addFile graph1 wsId "a.txt"
    let secondId, graph = addFile graph2 wsId "b.txt"
    let model =
        VmTestHelpers.emptyModelAt graph wsId
        |> fun ready -> selectRange ready 0 2
    let _, effects = UpdateWorkspaceLoad.loadOp model
    let focuses = loadFocuses effects
    Assert.Equal<NodeId list>([ firstId; secondId ], focuses)
    Assert.DoesNotContain(wsId, focuses)
    Assert.False(postsReconcileOrParse effects)

[<Fact>]
let ``a normal sibling does not post its parent workspace`` () =
    let wsId, graph1 = Graph.create () |> addWorkspace "home"
    let fileId, graph2 = addFile graph1 wsId "note.txt"
    let noteId = NodeId.New()
    let graph =
        applyOps
            (markCurrent graph2 wsId)
            [ Op.NewNode(noteId, "loose")
              ChildListWire.append
                  wsId
                  (Graph.children graph2 wsId)
                  [ ChildNode.owner noteId ] ]
    let model =
        VmTestHelpers.emptyModelAt graph wsId
        |> fun ready -> selectRange ready 0 2
    let _, effects = UpdateWorkspaceLoad.loadOp model
    let focuses = loadFocuses effects
    Assert.Equal<NodeId list>([ fileId ], focuses)
    Assert.DoesNotContain(wsId, focuses)

[<Fact>]
let ``Workspace Load names that Workspace`` () =
    let wsId, graph = Graph.create () |> addWorkspace "home"
    let model =
        VmTestHelpers.emptyModelAt graph Graph.workspacesId
        |> fun ready -> selectRange ready 0 1
    let _, effects = UpdateWorkspaceLoad.loadOp model
    Assert.Equal<NodeId list>([ wsId ], loadFocuses effects)
    Assert.False(postsReconcileOrParse effects)

[<Fact>]
let ``two nodes inside one File yield that File once`` () =
    let wsId, graph1 = Graph.create () |> addWorkspace "home"
    let fileId, graph2 = addFile graph1 wsId "note.txt"
    let firstId = NodeId.New()
    let secondId = NodeId.New()
    let graph =
        applyOps
            (markCurrent graph2 fileId)
            [ Op.NewNode(firstId, "one")
              Op.NewNode(secondId, "two")
              ChildListWire.append
                  fileId
                  (Graph.children graph2 fileId)
                  [ ChildNode.owner firstId
                    ChildNode.owner secondId ] ]
    let model =
        VmTestHelpers.emptyModelAt graph fileId
        |> fun ready -> selectRange ready 0 2
    let _, effects = UpdateWorkspaceLoad.loadOp model
    Assert.Equal<NodeId list>([ fileId ], loadFocuses effects)

[<Fact>]
let ``a node inside a file posts that file`` () =
    let wsId, graph1 = Graph.create () |> addWorkspace "home"
    let fileId, graph2 = addFile graph1 wsId "note.txt"
    let lineId = NodeId.New()
    let graph =
        applyOps
            (markCurrent graph2 fileId)
            [ Op.NewNode(lineId, "line")
              ChildListWire.append
                  fileId
                  (Graph.children graph2 fileId)
                  [ ChildNode.owner lineId ] ]
    let model =
        VmTestHelpers.emptyModelAt graph fileId
        |> fun ready -> selectRange ready 0 1
    let _, effects = UpdateWorkspaceLoad.loadOp model
    let focuses = loadFocuses effects
    Assert.Equal<NodeId list>([ fileId ], focuses)
    Assert.DoesNotContain(wsId, focuses)
    Assert.False(postsReconcileOrParse effects)

[<Fact>]
let ``a non-focus File and Directory each get a Load message`` () =
    let wsId, graph1 = Graph.create () |> addWorkspace "home"
    let noteId = NodeId.New()
    let graph2 =
        applyOps
            (markCurrent graph1 wsId)
            [ Op.NewNode(noteId, "loose")
              ChildListWire.append
                  wsId
                  (Graph.children graph1 wsId)
                  [ ChildNode.owner noteId ] ]
    let fileId, graph3 = addFile graph2 wsId "note.txt"
    let dirId, graph = addDirectory graph3 wsId "docs"
    let model =
        let ready = VmTestHelpers.emptyModelAt graph wsId
        let parentEntry = ready.siteMap.entries.[ready.siteMap.rootId]
        { ready with
            selectedNodes =
                Some
                    { range =
                        { parent = parentEntry
                          start = 0
                          endd = 3 }
                      focus = 0 } }
    let _, effects = UpdateWorkspaceLoad.loadOp model
    let focuses = loadFocuses effects
    Assert.Equal<NodeId list>([ fileId; dirId ], focuses)
    Assert.DoesNotContain(noteId, focuses)
    Assert.DoesNotContain(wsId, focuses)
    Assert.False(postsReconcileOrParse effects)

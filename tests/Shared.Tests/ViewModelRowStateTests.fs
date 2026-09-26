module ViewModelRowStateTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open GraphChildMapHelpers
open VmTestHelpers
open Xunit

let private owned = ChildNode.owner

let private reference = ChildNode.reference

let private requireOk label result =
    result |> ModelBuilder.requireOk label

let private addChild parent child graph =
    let index = (Graph.children graph parent).Length
    Graph.replace parent index [] [ owned child ] graph
    |> requireOk "add owned child"

let private entryUnderParentNode parentNodeId nodeId (model: VM) =
    model.siteMap.entries
    |> Map.toSeq
    |> Seq.map snd
    |> Seq.find (fun entry ->
        entry.nodeId = nodeId
        && entry.parentInstanceId
           |> Option.bind (fun id -> Map.tryFind id model.siteMap.entries)
           |> Option.exists (fun parent -> parent.nodeId = parentNodeId))

let private expandNode nodeId (model: VM) =
    let entry =
        model.siteMap.entries
        |> Map.toSeq
        |> Seq.map snd
        |> Seq.find (fun entry -> entry.nodeId = nodeId)
    let siteMap, nextId =
        expandEntry entry.instanceId model.graph model.siteMap model.nextSiteId
    { model with siteMap = siteMap; nextSiteId = nextId }

let private modelFromGraph graph =
    emptyModelAt graph Graph.rootId

let private modelWithUnparsedFile () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let childId = NodeId.New()
    let grandchildId = NodeId.New()
    let refParentId = NodeId.New()
    let siblingFileId = NodeId.New()
    let file =
        Node.Create(
            fileId,
            text = "file",
            name = Filename.create "file.md",
            kind = Special File,
            documentState = Unparsed)
    let sibling =
        Node.Create(
            siblingFileId,
            text = "sibling",
            name = Filename.create "sibling.md",
            kind = Special File)
    let graph1 =
        graph0
        |> addDetachedMany
            [ file
              Node.Create(childId, text = "child")
              Node.Create(grandchildId, text = "grandchild")
              Node.Create(refParentId, text = "refs")
              sibling ]
    let graph2 = addChild Graph.rootId fileId graph1
    let graph3 = addChild fileId childId graph2
    let graph4 = addChild childId grandchildId graph3
    let graph5 = addChild Graph.rootId refParentId graph4
    let graph6 =
        Graph.replace refParentId 0 [] [ reference childId ] graph5
        |> requireOk "add reference"
    let graph7 = addChild Graph.rootId siblingFileId graph6
    let model =
        emptyModelAt graph7 Graph.rootId
        |> expandNode fileId
        |> expandNode childId
        |> expandNode refParentId
    model, fileId, childId, grandchildId, refParentId, siblingFileId

[<Fact>]
let ``unparsed File class eligibility follows owned document membership`` () =
    let model, fileId, childId, grandchildId, refParentId, siblingFileId =
        modelWithUnparsedFile ()
    let rootEntry = entryUnderParentNode Graph.rootId fileId model
    let childEntry = entryUnderParentNode fileId childId model
    let grandchildEntry = entryUnderParentNode childId grandchildId model
    let referenceEntry = entryUnderParentNode refParentId childId model
    let siblingEntry = entryUnderParentNode Graph.rootId siblingFileId model

    Assert.True(rowFileUnparsedClassEligible model rootEntry)
    Assert.True(rowFileUnparsedClassEligible model childEntry)
    Assert.True(rowFileUnparsedClassEligible model grandchildEntry)
    Assert.False(rowFileUnparsedClassEligible model referenceEntry)
    Assert.False(rowFileUnparsedClassEligible model siblingEntry)

[<Fact>]
let ``document state change patches all visible owned File member rows`` () =
    let newModel, fileId, childId, grandchildId, refParentId, siblingFileId =
        modelWithUnparsedFile ()
    let file = newModel.graph.nodes.[fileId]
    let oldGraph =
        fromExisting
            newModel.graph
            (newModel.graph.nodes
             |> Map.add fileId { file with documentState = Current })
    let oldModel = { newModel with graph = oldGraph }
    let cached = getVisibleInstanceIds oldModel.siteMap |> Set.ofList
    let patchedClasses =
        planPatchDOM oldModel newModel cached
        |> List.choose (function
            | PatchRow (instanceId, patches) ->
                patches
                |> List.tryPick (function
                    | SetClassName className -> Some (instanceId, className)
                    | _ -> None)
            | _ -> None)
        |> Map.ofList
    let classFor parentId nodeId =
        let entry = entryUnderParentNode parentId nodeId newModel
        Map.tryFind entry.instanceId patchedClasses

    Assert.Contains("amb-row-sync-unparsed", classFor Graph.rootId fileId |> Option.get)
    Assert.Contains("amb-row-sync-unparsed", classFor fileId childId |> Option.get)
    Assert.Contains("amb-row-sync-unparsed", classFor childId grandchildId |> Option.get)
    Assert.Equal(None, classFor refParentId childId)
    Assert.Equal(None, classFor Graph.rootId siblingFileId)

[<Fact>]
let ``desktop file indicator states map to row text`` () =
    Assert.Equal(
        "missing",
        DesktopFileIndicator.textByState.[AbsentArtifactIndicator])
    Assert.Equal(
        "invalid",
        DesktopFileIndicator.textByState.[InvalidFileReferenceIndicator])
    Assert.Equal("...", DesktopFileIndicator.toText (CheckingFileStatus (NodeId.New(), "x")))

[<Fact>]
let ``special row with absent artifact uses missing indicator and absent class`` () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let workspace =
        Node.Create(
            wsId,
            text = "bad workspace",
            name = Filename.Invalid "",
            owner = Graph.workspacesId,
            kind = Special Workspace)
    let graph =
        graph0
        |> Graph.addDetachedNode workspace
        |> setChildren Graph.workspacesId [ owned wsId ]
    let model = modelFromGraph graph |> expandNode Graph.workspacesId
    let entry = entryUnderParentNode Graph.workspacesId wsId model
    let node = model.graph.nodes.[wsId]

    Assert.Equal(Some AbsentArtifactIndicator, rowArtifactIndicatorState model entry node)
    Assert.Equal("", rowFileIndicatorText model entry node)
    Assert.True(rowArtifactAbsentClassEligible model entry node)

[<Fact>]
let ``missing workspace path reference uses missing text indicator`` () =
    let graph0 = Graph.create ()
    let refId = NodeId.New()
    let refNode = Node.Create(refId, text = "see [[//missing/file.md]]")
    let graph =
        graph0
        |> Graph.addDetachedNode refNode
        |> appendKids Graph.rootId [ owned refId ]
    let model = modelFromGraph graph
    let entry = entryUnderParentNode Graph.rootId refId model
    let node = model.graph.nodes.[refId]

    Assert.Equal(
        Some AbsentArtifactIndicator,
        rowArtifactIndicatorState model entry node)
    Assert.Equal("", rowFileIndicatorText model entry node)
    Assert.False(rowArtifactAbsentClassEligible model entry node)

let private desktopCaps : DesktopCapabilities =
    { file =
        { canOpen = false
          canImport = true
          canExport = true
          canStatus = true
          canWorkspacePaths = true }
      git = { canGit = true } }

let private modelWithWorkspaceFile
    (documentState: DocumentState)
    =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let fileId = NodeId.New()
    let workspace =
        Node.Create(
            wsId,
            text = "home",
            name = Filename.Ok "home",
            owner = Graph.workspacesId,
            kind = Special Workspace)
    let file =
        Node.Create(
            fileId,
            text = "note",
            name = Filename.Ok "note.md",
            owner = wsId,
            kind = Special File,
            documentState = documentState)
    let graph1 =
        graph0
        |> addDetachedMany [ workspace; file ]
    let graph2 =
        Graph.replace Graph.workspacesId 0 [] [ owned wsId ] graph1
        |> requireOk "workspaces->ws"
    let graph3 =
        Graph.replace wsId 0 [] [ owned fileId ] graph2
        |> requireOk "ws->file"
    let model =
        modelFromGraph graph3
        |> expandNode Graph.workspacesId
        |> expandNode wsId
    model, wsId, fileId

[<Fact>]
let ``web or unmapped desktop only surfaces Unparsed`` () =
    let model, wsId, fileId = modelWithWorkspaceFile Unparsed
    let entry = entryUnderParentNode wsId fileId model
    let node = model.graph.nodes.[fileId]
    Assert.Equal(
        Some WorkspacePathSyncStatus.Unparsed,
        rowWorkspacePathSyncStatus model entry node)
    Assert.Equal("\u2026", rowFileIndicatorText model entry node)
    Assert.Equal(
        Some "unparsed",
        rowFileIndicator model entry node |> snd)
    Assert.Equal(
        Some "amb-row-sync-unparsed",
        rowWorkspacePathSyncClass model entry node)

[<Fact>]
let ``web and desktop surface no-server-file before path comparison`` () =
    let baseModel, wsId, fileId = modelWithWorkspaceFile NoServerFile
    let entry = entryUnderParentNode wsId fileId baseModel
    let node = baseModel.graph.nodes.[fileId]
    Assert.Equal(
        Some WorkspacePathSyncStatus.NoServerFile,
        rowWorkspacePathSyncStatus baseModel entry node)
    Assert.Equal("\u2205", rowFileIndicatorText baseModel entry node)
    Assert.Equal(
        Some "no file on server",
        rowFileIndicator baseModel entry node |> snd)

[<Fact>]
let ``mapped desktop uses ledger comparison and Unparsed overlay`` () =
    let baseModel, wsId, fileId = modelWithWorkspaceFile Unparsed
    let entry = entryUnderParentNode wsId fileId baseModel
    let node = baseModel.graph.nodes.[fileId]
    let t = System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
    let fact =
        { relative = "note.md"
          isDirectory = false
          presence = WorkspacePathPresence.Both
          localMtimeUtc = Some t
          serverMtimeUtc = Some t }
    let model =
        { baseModel with
            desktopCapabilities = Some desktopCaps
            workspaceMappedLabels = Set.singleton "home"
            workspaceSyncFacts =
                Map.ofList [ "home", Map.ofList [ "note.md", fact ] ] }
    Assert.True(canCompareWorkspacePathSync model "home")
    Assert.Equal(
        Some WorkspacePathSyncStatus.Unparsed,
        rowWorkspacePathSyncStatus model entry node)
    let parsedNode = { node with documentState = Current }
    let parsedModel =
        { model with
            graph =
                fromExisting
                    model.graph
                    (Map.add fileId parsedNode model.graph.nodes) }
    Assert.Equal(
        Some WorkspacePathSyncStatus.Synced,
        rowWorkspacePathSyncStatus
            parsedModel
            entry
            parsedModel.graph.nodes.[fileId])

[<Fact>]
let ``applyWorkspacePathSyncSnapshot matches mapping labels case-insensitively`` () =
    let baseModel, wsId, fileId = modelWithWorkspaceFile Current
    let entry = entryUnderParentNode wsId fileId baseModel
    let t = System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
    let fact =
        { relative = "note.md"
          isDirectory = false
          presence = WorkspacePathPresence.Both
          localMtimeUtc = Some t
          serverMtimeUtc = Some t }
    let model =
        applyWorkspacePathSyncSnapshot
            (Set.singleton "Home")
            (Map.ofList [ "Home", Map.ofList [ "note.md", fact ] ])
            { baseModel with desktopCapabilities = Some desktopCaps }
    Assert.True(canCompareWorkspacePathSync model "home")
    Assert.Equal(
        Some WorkspacePathSyncStatus.Synced,
        rowWorkspacePathSyncStatus
            model
            entry
            model.graph.nodes.[fileId])

[<Fact>]
let ``desktop without mapping ignores ledger comparison`` () =
    let baseModel, wsId, fileId = modelWithWorkspaceFile Current
    let entry = entryUnderParentNode wsId fileId baseModel
    let node = baseModel.graph.nodes.[fileId]
    let fact =
        { relative = "note.md"
          isDirectory = false
          presence = WorkspacePathPresence.LocalOnly
          localMtimeUtc = None
          serverMtimeUtc = None }
    let model =
        { baseModel with
            desktopCapabilities = Some desktopCaps
            workspaceMappedLabels = Set.empty
            workspaceSyncFacts =
                Map.ofList [ "home", Map.ofList [ "note.md", fact ] ] }
    Assert.False(canCompareWorkspacePathSync model "home")
    Assert.Equal(
        None,
        rowWorkspacePathSyncStatus model entry node)

[<Fact>]
let ``mapped without live fact defaults to OnlyOnServer`` () =
    let baseModel, wsId, fileId = modelWithWorkspaceFile Current
    let fileEntry = entryUnderParentNode wsId fileId baseModel
    let wsEntry = entryUnderParentNode Graph.workspacesId wsId baseModel
    let model =
        applyWorkspacePathSyncSnapshot
            (Set.singleton "home")
            Map.empty
            { baseModel with desktopCapabilities = Some desktopCaps }
    Assert.True(canCompareWorkspacePathSync model "home")
    Assert.Equal(
        Some WorkspacePathSyncStatus.OnlyOnServer,
        rowWorkspacePathSyncStatus
            model
            fileEntry
            model.graph.nodes.[fileId])
    Assert.Equal(
        Some WorkspacePathSyncStatus.OnlyOnServer,
        rowWorkspacePathSyncStatus
            model
            wsEntry
            model.graph.nodes.[wsId])

[<Fact>]
let ``mapped live local fact compares against node server stamp`` () =
    let baseModel, wsId, fileId = modelWithWorkspaceFile Current
    let entry = entryUnderParentNode wsId fileId baseModel
    let local = System.DateTime(2026, 1, 3, 0, 0, 0, System.DateTimeKind.Utc)
    let server = System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
    let fact =
        { relative = "note.md"
          isDirectory = false
          presence = WorkspacePathPresence.Both
          localMtimeUtc = Some local
          serverMtimeUtc = None }
    let stamped =
        { baseModel.graph.nodes.[fileId] with updateTime = server }
    let model =
        applyWorkspacePathSyncSnapshot
            (Set.singleton "home")
            (Map.ofList [ "home", Map.ofList [ "note.md", fact ] ])
            { baseModel with
                desktopCapabilities = Some desktopCaps
                graph =
                    fromExisting
                        baseModel.graph
                        (Map.add fileId stamped baseModel.graph.nodes) }
    Assert.Equal(
        Some WorkspacePathSyncStatus.NewerOnDesktop,
        rowWorkspacePathSyncStatus
            model
            entry
            model.graph.nodes.[fileId])

[<Fact>]
let ``mapped desktop NewerOnServer when ledger server ahead of local`` () =
    let baseModel, wsId, fileId = modelWithWorkspaceFile Current
    let entry = entryUnderParentNode wsId fileId baseModel
    let local = System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
    let server =
        System.DateTime(2026, 1, 2, 0, 0, 0, System.DateTimeKind.Utc)
    let fact =
        { relative = "note.md"
          isDirectory = false
          presence = WorkspacePathPresence.Both
          localMtimeUtc = Some local
          serverMtimeUtc = Some server }
    let model =
        { baseModel with
            desktopCapabilities = Some desktopCaps
            workspaceMappedLabels = Set.singleton "home"
            workspaceSyncFacts =
                Map.ofList [ "home", Map.ofList [ "note.md", fact ] ] }
    Assert.Equal(
        Some WorkspacePathSyncStatus.NewerOnServer,
        rowWorkspacePathSyncStatus
            model
            entry
            model.graph.nodes.[fileId])

[<Fact>]
let ``mapped desktop NewerOnDesktop when node stamp lags download-aligned ledger`` () =
    let baseModel, wsId, fileId = modelWithWorkspaceFile Current
    let entry = entryUnderParentNode wsId fileId baseModel
    let local =
        System.DateTime(2026, 1, 2, 0, 0, 0, System.DateTimeKind.Utc)
    let nodeOlder =
        System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
    let fact =
        { relative = "note.md"
          isDirectory = false
          presence = WorkspacePathPresence.Both
          localMtimeUtc = Some local
          serverMtimeUtc = Some local }
    let stamped =
        { baseModel.graph.nodes.[fileId] with updateTime = nodeOlder }
    let model =
        { baseModel with
            desktopCapabilities = Some desktopCaps
            workspaceMappedLabels = Set.singleton "home"
            workspaceSyncFacts =
                Map.ofList [ "home", Map.ofList [ "note.md", fact ] ]
            graph =
                fromExisting
                    baseModel.graph
                    (Map.add fileId stamped baseModel.graph.nodes) }
    Assert.Equal(
        Some WorkspacePathSyncStatus.NewerOnDesktop,
        rowWorkspacePathSyncStatus
            model
            entry
            model.graph.nodes.[fileId])

[<Fact>]
let ``mapped desktop Synced when node stamp equals aligned ledger`` () =
    let baseModel, wsId, fileId = modelWithWorkspaceFile Current
    let entry = entryUnderParentNode wsId fileId baseModel
    let t = System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
    let fact =
        { relative = "note.md"
          isDirectory = false
          presence = WorkspacePathPresence.Both
          localMtimeUtc = Some t
          serverMtimeUtc = Some t }
    let stamped =
        { baseModel.graph.nodes.[fileId] with updateTime = t }
    let model =
        { baseModel with
            desktopCapabilities = Some desktopCaps
            workspaceMappedLabels = Set.singleton "home"
            workspaceSyncFacts =
                Map.ofList [ "home", Map.ofList [ "note.md", fact ] ]
            graph =
                fromExisting
                    baseModel.graph
                    (Map.add fileId stamped baseModel.graph.nodes) }
    Assert.Equal(
        Some WorkspacePathSyncStatus.Synced,
        rowWorkspacePathSyncStatus
            model
            entry
            model.graph.nodes.[fileId])

[<Fact>]
let ``mapped desktop NewerOnServer when persist stamp newer than aligned local`` () =
    let baseModel, wsId, fileId = modelWithWorkspaceFile Current
    let entry = entryUnderParentNode wsId fileId baseModel
    let local = System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
    let stamp =
        System.DateTime(2026, 1, 3, 0, 0, 0, System.DateTimeKind.Utc)
    let fact =
        { relative = "note.md"
          isDirectory = false
          presence = WorkspacePathPresence.Both
          localMtimeUtc = Some local
          serverMtimeUtc = Some local }
    let stamped =
        { baseModel.graph.nodes.[fileId] with updateTime = stamp }
    let model =
        { baseModel with
            desktopCapabilities = Some desktopCaps
            workspaceMappedLabels = Set.singleton "home"
            workspaceSyncFacts =
                Map.ofList [ "home", Map.ofList [ "note.md", fact ] ]
            graph =
                fromExisting
                    baseModel.graph
                    (Map.add fileId stamped baseModel.graph.nodes) }
    Assert.Equal(
        Some WorkspacePathSyncStatus.NewerOnServer,
        rowWorkspacePathSyncStatus
            model
            entry
            model.graph.nodes.[fileId])

[<Fact>]
let ``mapped Unparsed file keeps ledger stamps despite newer node updateTime`` () =
    let baseModel, wsId, fileId = modelWithWorkspaceFile Unparsed
    let entry = entryUnderParentNode wsId fileId baseModel
    let local = System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
    let nodeServer =
        System.DateTime(2026, 1, 2, 0, 0, 0, System.DateTimeKind.Utc)
    let fact =
        { relative = "note.md"
          isDirectory = false
          presence = WorkspacePathPresence.Both
          localMtimeUtc = Some local
          serverMtimeUtc = Some local }
    let stamped =
        { baseModel.graph.nodes.[fileId] with updateTime = nodeServer }
    let model =
        { baseModel with
            desktopCapabilities = Some desktopCaps
            workspaceMappedLabels = Set.singleton "home"
            workspaceSyncFacts =
                Map.ofList [ "home", Map.ofList [ "note.md", fact ] ]
            graph =
                fromExisting
                    baseModel.graph
                    (Map.add fileId stamped baseModel.graph.nodes) }
    Assert.Equal(
        Some WorkspacePathSyncStatus.Unparsed,
        rowWorkspacePathSyncStatus
            model
            entry
            model.graph.nodes.[fileId])

// ---------------------------------------------------------------------------
// rowChildrenIndicator — hollow circle for Unloaded / Unparsed leaves
// ---------------------------------------------------------------------------

let private graphWithNode (node: Node) =
    Graph.addDetachedNode node (Graph.create ())

let private graphWithKids (parent: Node) (child: Node) =
    Graph.create ()
    |> addDetachedMany [ parent; child ]
    |> setChildren parent.id [ ChildNode.owner child.id ]

[<Fact>]
let ``rowChildrenIndicator is HollowCircle when children are Unloaded`` () =
    let node = Node.Create(NodeId.New(), text = "ws")
    let graph = graphWithNode node |> unload node.id
    Assert.Equal(RowChildrenIndicator.HollowCircle, rowChildrenIndicator graph node)

[<Fact>]
let ``rowChildrenIndicator is HollowCircle when document is Unparsed leaf`` () =
    let node =
        Node.Create(
            NodeId.New(),
            text = "file",
            kind = Special File,
            documentState = Unparsed)
    Assert.Equal(
        RowChildrenIndicator.HollowCircle,
        rowChildrenIndicator (graphWithNode node) node)

[<Fact>]
let ``rowChildrenIndicator is SolidCircle for Loaded Parsed empty children`` () =
    let node = Node.Create(NodeId.New(), text = "leaf")
    Assert.Equal(
        RowChildrenIndicator.SolidCircle,
        rowChildrenIndicator (graphWithNode node) node)

[<Fact>]
let ``rowChildrenIndicator is FoldChevron when Loaded with children`` () =
    let childId = NodeId.New()
    let node = Node.Create(NodeId.New(), text = "parent")
    let child = Node.Create(childId, text = "c")
    Assert.Equal(
        RowChildrenIndicator.FoldChevron,
        rowChildrenIndicator (graphWithKids node child) node)

[<Fact>]
let ``rowChildrenIndicator keeps FoldChevron for Unparsed with resident children`` () =
    let childId = NodeId.New()
    let node =
        Node.Create(
            NodeId.New(),
            text = "file",
            kind = Special File,
            documentState = Unparsed)
    let child = Node.Create(childId, text = "c")
    Assert.Equal(
        RowChildrenIndicator.FoldChevron,
        rowChildrenIndicator (graphWithKids node child) node)

[<Fact>]
let ``planPatchDOM recreates row when leaf circle becomes hollow`` () =
    let graph0 = Graph.create ()
    let leafId = NodeId.New()
    let leaf = Node.Create(leafId, text = "leaf")
    let graph1 = Graph.addDetachedNode leaf graph0
    let graph2 = addChild Graph.rootId leafId graph1
    let oldModel = modelFromGraph graph2
    let newModel = { oldModel with graph = unload leafId graph2 }
    let leafInst =
        entryUnderParentNode Graph.rootId leafId oldModel
    let cached = Set.ofList [ oldModel.siteMap.rootId; leafInst.instanceId ]
    let mutations = planPatchDOM oldModel newModel cached
    Assert.True(
        mutations
        |> List.exists (function
            | RecreateRow id -> id = leafInst.instanceId
            | _ -> false))

[<Fact>]
let ``planPatchDOM recreates row when Unloaded leaf becomes Loaded with children`` () =
    let graph0 = Graph.create ()
    let leafId = NodeId.New()
    let childId = NodeId.New()
    let unloaded = Node.Create(leafId, text = "leaf")
    let graph1 = Graph.addDetachedNode unloaded graph0 |> unload leafId
    let graph2 = addChild Graph.rootId leafId graph1
    let oldModel = modelFromGraph graph2
    let child = Node.Create(childId, text = "c", owner = leafId)
    let newModel =
        { oldModel with
            graph =
                graph2
                |> Graph.addDetachedNode child
                |> setChildren leafId [ ChildNode.owner childId ] }
    let leafInst =
        entryUnderParentNode Graph.rootId leafId oldModel
    let cached = Set.ofList [ oldModel.siteMap.rootId; leafInst.instanceId ]
    let mutations = planPatchDOM oldModel newModel cached
    Assert.True(
        mutations
        |> List.exists (function
            | RecreateRow id -> id = leafInst.instanceId
            | _ -> false))

// ---------------------------------------------------------------------------
// bulletTip — hover inspector facts, self-gated, fixed order
// ---------------------------------------------------------------------------

let private stubFormatLocal (d: System.DateTime) : string = $"LOCAL[{d.Ticks}]"

let private modelWithLooseNode (node: Node) =
    modelFromGraph (graphWithNode node)

[<Fact>]
let ``bulletTip minimal node lists guid residency and update time only`` () =
    let nid = NodeId.New()
    let t = System.DateTime(2026, 1, 2, 3, 4, 0, System.DateTimeKind.Utc)
    let node = Node.Create(nid, text = "leaf", updateTime = t)
    let model = modelWithLooseNode node
    let tip = bulletTip stubFormatLocal model node
    let lines = tip.Split('\n')
    Assert.Equal(3, lines.Length)
    Assert.Contains(NodeId.GuidTail8 nid.Value, lines.[0])
    Assert.Equal("Residency: Loaded, Current", lines.[1])
    Assert.Equal($"Updated: {stubFormatLocal t}", lines.[2])

[<Fact>]
let ``bulletTip disambiguates hollow by Unloaded versus Unparsed`` () =
    let unloaded = Node.Create(NodeId.New(), text = "ws")
    let unparsed =
        Node.Create(
            NodeId.New(),
            text = "file",
            kind = Special File,
            documentState = Unparsed)
    let unloadedModel =
        modelFromGraph (graphWithNode unloaded |> unload unloaded.id)
    let tipU = bulletTip stubFormatLocal unloadedModel unloaded
    let tipP = bulletTip stubFormatLocal (modelWithLooseNode unparsed) unparsed
    Assert.Contains("Residency: Unloaded, Current", tipU)
    Assert.Contains("Residency: Loaded, Unparsed", tipP)
    Assert.NotEqual<string>(tipU, tipP)

[<Fact>]
let ``bulletTip reads Loaded Current for a parsed leaf`` () =
    let node = Node.Create(NodeId.New(), text = "leaf")
    let tip = bulletTip stubFormatLocal (modelWithLooseNode node) node
    Assert.Contains("Residency: Loaded, Current", tip)

[<Fact>]
let ``bulletTip includes workspace path between residency and update time`` () =
    let model, _wsId, fileId = modelWithWorkspaceFile Current
    let node = model.graph.nodes.[fileId]
    let tip = bulletTip stubFormatLocal model node
    let lines = tip.Split('\n')
    let residencyIdx = System.Array.FindIndex(lines, fun l -> l.StartsWith "Residency:")
    let pathIdx = System.Array.FindIndex(lines, fun l -> l.StartsWith "Path:")
    let updatedIdx = System.Array.FindIndex(lines, fun l -> l.StartsWith "Updated:")
    Assert.Equal("Path: //home/note.md", lines.[pathIdx])
    Assert.True(residencyIdx < pathIdx && pathIdx < updatedIdx)

[<Fact>]
let ``bulletTip shows both workspace and local path when a desktop mapping exists`` () =
    let baseModel, _wsId, fileId = modelWithWorkspaceFile Current
    let model = { baseModel with workspaceRoots = Map.ofList [ "home", "d:\\life" ] }
    let node = model.graph.nodes.[fileId]
    let tip = bulletTip stubFormatLocal model node
    let lines = tip.Split('\n')
    let pathIdx = System.Array.FindIndex(lines, fun l -> l.StartsWith "Path:")
    let localIdx = System.Array.FindIndex(lines, fun l -> l.StartsWith "Local:")
    let updatedIdx = System.Array.FindIndex(lines, fun l -> l.StartsWith "Updated:")
    Assert.Equal("Path: //home/note.md", lines.[pathIdx])
    Assert.Equal("Local: d:\\life\\note.md", lines.[localIdx])
    Assert.True(pathIdx < localIdx && localIdx < updatedIdx)

[<Fact>]
let ``bulletTip omits local path when the workspace has no desktop mapping`` () =
    let model, _wsId, fileId = modelWithWorkspaceFile Current
    let node = model.graph.nodes.[fileId]
    let tip = bulletTip stubFormatLocal model node
    Assert.Contains("Path: //home/note.md", tip)
    Assert.DoesNotContain("Local:", tip)

[<Fact>]
let ``bulletTip omits workspace path for an unmapped node`` () =
    let node = Node.Create(NodeId.New(), text = "leaf")
    let tip = bulletTip stubFormatLocal (modelWithLooseNode node) node
    Assert.DoesNotContain("Path:", tip)

[<Fact>]
let ``bulletTip lists css classes last when present`` () =
    let node =
        Node.Create(
            NodeId.New(),
            text = "leaf",
            cssClasses = CssClass.ofList [ "h1"; "blue" ])
    let tip = bulletTip stubFormatLocal (modelWithLooseNode node) node
    let lines = tip.Split('\n')
    Assert.Equal("Classes: h1 blue", Array.last lines)

[<Fact>]
let ``bulletTip omits css line when node has no classes`` () =
    let node = Node.Create(NodeId.New(), text = "leaf")
    let tip = bulletTip stubFormatLocal (modelWithLooseNode node) node
    Assert.DoesNotContain("Classes:", tip)

[<Fact>]
let ``bulletTip keeps line order stable across chevron and leaf nodes`` () =
    let childId = NodeId.New()
    let chevron = Node.Create(NodeId.New(), text = "parent")
    let child = Node.Create(childId, text = "c")
    let leaf = Node.Create(NodeId.New(), text = "leaf")
    let orderOf graph node =
        (bulletTip stubFormatLocal (modelFromGraph graph) node).Split('\n')
        |> Array.map (fun l -> l.Substring(0, l.IndexOf ':' + 1))
    Assert.Equal<string[]>(
        orderOf (graphWithKids chevron child) chevron,
        orderOf (graphWithNode leaf) leaf)

[<Fact>]
let ``planPatchDOM live Actor adds actor-live and clears it`` () =
    let graph0, ids = ModelBuilder.createNodes [ "focus" ] (Graph.create ())
    let focusId = List.head ids
    let graph = addChild Graph.rootId focusId graph0
    let idle = emptyModel graph
    let live = { idle with actorLiveFocusIds = Set.singleton focusId }
    let cached = getVisibleInstanceIds idle.siteMap |> Set.ofList
    let classAfter start finish =
        planPatchDOM start finish cached
        |> List.choose (function
            | PatchRow (instanceId, patches) ->
                let entry = finish.siteMap.entries.[instanceId]
                if entry.nodeId <> focusId then
                    None
                else
                    patches
                    |> List.tryPick (function
                        | SetClassName className -> Some className
                        | _ -> None)
            | _ -> None)
        |> List.tryHead
    match classAfter idle live with
    | None -> failwith "expected class patch when Actor becomes live"
    | Some className -> Assert.Contains("amb-actor-live", className)
    match classAfter live idle with
    | None -> failwith "expected class patch when Actor leaves live set"
    | Some className -> Assert.DoesNotContain("amb-actor-live", className)

[<Fact>]
let ``bulletTip renders update time via the injected formatter verbatim`` () =
    let t = System.DateTime(2027, 5, 6, 7, 8, 0, System.DateTimeKind.Utc)
    let node = Node.Create(NodeId.New(), text = "leaf", updateTime = t)
    let mutable seen = []
    let spy (d: System.DateTime) =
        seen <- d :: seen
        "STAMP"
    let tip = bulletTip spy (modelWithLooseNode node) node
    Assert.Equal<System.DateTime list>([ t ], seen)
    Assert.Contains("Updated: STAMP", tip)

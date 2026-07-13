module ViewModelRowStateTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open VmTestHelpers
open Xunit

let private owned id =
    { ref = Ownership.Owner; id = id }

let private reference id =
    { ref = Ownership.Ref; id = id }

let private requireOk label result =
    result |> ModelBuilder.requireOk label

let private addChild parent child graph =
    let index = graph.nodes.[parent].children.Length
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
    let nodes =
        graph0.nodes
        |> Map.add fileId file
        |> Map.add childId (Node.Create(childId, text = "child"))
        |> Map.add grandchildId (Node.Create(grandchildId, text = "grandchild"))
        |> Map.add refParentId (Node.Create(refParentId, text = "refs"))
        |> Map.add siblingFileId sibling
    let graph1 = Graph.fromNodes graph0.root nodes
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
        Graph.fromNodes
            newModel.graph.root
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

    Assert.Contains("amb-row-file-unparsed", classFor Graph.rootId fileId |> Option.get)
    Assert.Contains("amb-row-file-unparsed", classFor fileId childId |> Option.get)
    Assert.Contains("amb-row-file-unparsed", classFor childId grandchildId |> Option.get)
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
    let workspaces =
        { graph0.nodes.[Graph.workspacesId] with
            children = [ owned wsId ] }
    let graph =
        graph0.nodes
        |> Map.add Graph.workspacesId workspaces
        |> Map.add wsId workspace
        |> Graph.fromNodes graph0.root
    let model = modelFromGraph graph |> expandNode Graph.workspacesId
    let entry = entryUnderParentNode Graph.workspacesId wsId model
    let node = model.graph.nodes.[wsId]

    Assert.Equal(Some AbsentArtifactIndicator, rowArtifactIndicatorState model entry node)
    Assert.Equal("missing", rowFileIndicatorText model entry node)
    Assert.True(rowArtifactAbsentClassEligible model entry node)

[<Fact>]
let ``missing workspace path reference uses missing text indicator`` () =
    let graph0 = Graph.create ()
    let refId = NodeId.New()
    let refNode = Node.Create(refId, text = "see [[//missing/file.md]]")
    let root =
        { graph0.nodes.[Graph.rootId] with
            children = graph0.nodes.[Graph.rootId].children @ [ owned refId ] }
    let graph =
        graph0.nodes
        |> Map.add Graph.rootId root
        |> Map.add refId refNode
        |> Graph.fromNodes graph0.root
    let model = modelFromGraph graph
    let entry = entryUnderParentNode Graph.rootId refId model
    let node = model.graph.nodes.[refId]

    Assert.Equal(
        Some AbsentArtifactIndicator,
        rowArtifactIndicatorState model entry node)
    Assert.Equal("missing", rowFileIndicatorText model entry node)
    Assert.False(rowArtifactAbsentClassEligible model entry node)

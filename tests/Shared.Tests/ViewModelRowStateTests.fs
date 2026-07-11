module ViewModelRowStateTests

open Gambol.Shared
open Gambol.Shared.ViewModel
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
    let siteMap, nextSiteId = buildSiteMapFrom graph7 Graph.rootId (Sid 0)
    let model =
        { graph = graph7
          revision = Revision.Zero
          history = History.empty
          selectedNodes = None
          mode = Selecting
          siteMap = siteMap
          nextSiteId = nextSiteId
          zoomRoot = Graph.rootId
          clipboard = None
          desktopCapabilities = None
          serverCapabilities = None
          desktopFileIndicator = BlankFileIndicator
          syncInfo = SyncInfo.initial
          lastCmdResult = None }
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

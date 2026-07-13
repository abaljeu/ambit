module ViewModelMoveOpsTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Shared.ViewModelMoveOps
open VmTestHelpers
open Xunit

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private modelWithSelection graph viewRoot parentInstId start endd focus : VM =
    let model = emptyModelAt graph viewRoot
    let parent = model.siteMap.entries.[parentInstId]

    { model with
        selectedNodes =
            Some
                { range =
                    { parent = parent
                      start = start
                      endd = endd }
                  focus = focus } }

let private sharedRefGraph () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "container"; "shared"; "new"; "child" ] graph0
    let cont = ids.[0]
    let sharedId = ids.[1]
    let newId = ids.[2]
    let childId = ids.[3]
    let sharedRef = { ref = Ownership.Ref; id = sharedId }
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ cont ]) graph1
        |> ModelBuilder.requireOk "root"

    let graph3 =
        let children = (owned [ sharedId ]) @ [ sharedRef ] @ (owned [ newId ])

        Graph.replace cont 0 [] children graph2
        |> ModelBuilder.requireOk "cont"

    let graph =
        Graph.replace sharedId 0 [] (owned [ childId ]) graph3
        |> ModelBuilder.requireOk "shared"

    graph, cont, sharedId, newId

[<Fact>]
let ``planIndentSelection remembers previous ref instance`` () =
    let graph, cont, sharedId, newId = sharedRefGraph ()
    let model = modelWithSelection graph cont (Sid 0) 2 3 2
    let refInstId = model.siteMap.entries.[model.siteMap.rootId].children.[1]
    let plan = planIndentSelection model |> Option.get

    Assert.Equal(sharedId, plan.target.pnode)
    Assert.Equal(0, plan.target.start)
    Assert.Equal(1, plan.target.endd)
    Assert.Equal(refInstId, plan.parentInstanceId)
    Assert.Equal(1, plan.insertIdx)
    Assert.Equal(1, plan.count)
    Assert.Equal(0, plan.focusOffset)
    Assert.Equal(newId, focusedNodeId graph plan.model.selectedNodes.Value)

[<Fact>]
let ``selectionAfterIndent focuses moved node under previous ref instance`` () =
    let graph, cont, sharedId, newId = sharedRefGraph ()
    let model = modelWithSelection graph cont (Sid 0) 2 3 2
    let ownerInstId = model.siteMap.entries.[model.siteMap.rootId].children.[0]
    let refInstId = model.siteMap.entries.[model.siteMap.rootId].children.[1]
    let plan = planIndentSelection model |> Option.get
    let newChild = graph.nodes.[cont].children.[2]
    let graph1 =
        Graph.replace cont 2 [ newChild ] [] graph
        |> ModelBuilder.requireOk "remove new"

    let graph2 =
        Graph.replace sharedId 1 [] [ newChild ] graph1
        |> ModelBuilder.requireOk "add under shared"

    let siteMap, nextId =
        reconcileSiteMapFrom graph2 cont plan.model.siteMap plan.model.nextSiteId

    let siteMap, _ =
        match Map.tryFind refInstId siteMap.entries with
        | Some entry when not entry.expanded ->
            expandEntry entry.instanceId graph2 siteMap nextId
        | _ -> siteMap, nextId

    let selection = selectionAfterIndent plan siteMap |> Option.get

    Assert.NotEqual(ownerInstId, selection.range.parent.instanceId)
    Assert.Equal(refInstId, selection.range.parent.instanceId)
    Assert.Equal(newId, focusedNodeId graph2 selection)

[<Fact>]
let ``planOutdentSelection uses visible ref parent instance`` () =
    let graph, cont, sharedId, _newId = sharedRefGraph ()
    let model0 = emptyModelAt graph cont
    let refInstId = model0.siteMap.entries.[model0.siteMap.rootId].children.[1]
    let siteMap, nextId = expandEntry refInstId graph model0.siteMap model0.nextSiteId
    let refEntry = siteMap.entries.[refInstId]
    let model =
        { model0 with
            siteMap = siteMap
            nextSiteId = nextId
            selectedNodes =
                Some
                    { range =
                        { parent = refEntry
                          start = 0
                          endd = 1 }
                      focus = 0 } }

    let plan = planOutdentSelection model |> Option.get

    Assert.Equal(cont, plan.target.pnode)
    Assert.Equal(1, plan.target.start)
    Assert.Equal(2, plan.target.endd)
    Assert.Equal(sharedId, plan.model.selectedNodes.Value.range.parent.nodeId)
    Assert.Equal(ReconcileCurrentZoom, plan.afterMove)

let private addSpecialNode id kind name (graph: Graph) =
    let node =
        Node.Create(
            id,
            text = name,
            name = Filename.create name,
            kind = Special kind)

    graph.nodes
    |> Map.add id node
    |> fun nodes -> Graph.fromNodes graph.root nodes

/// Normal sibling then owned Directory under ROOT — Tab would indent the
/// directory under the normal node (illegal Owner placement).
let private folderBesideNormalGraph () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "note" ] graph0
    let normalId = ids.[0]
    let dirId = NodeId.New()
    let graph2 = addSpecialNode dirId Directory "folder" graph1
    let idx = Graph.fileTreeInsertIndex graph2 Graph.rootId
    let graph =
        Graph.replace Graph.rootId idx [] (owned [ normalId; dirId ]) graph2
        |> ModelBuilder.requireOk "root children"
    graph, normalId, dirId

[<Fact>]
let ``completeIndent rejected keeps selection and sets invalid target message`` () =
    let graph, _normalId, dirId = folderBesideNormalGraph ()
    let model = emptyModelAt graph Graph.rootId
    let rootEntry = model.siteMap.entries.[model.siteMap.rootId]
    let dirIdx =
        rootEntry.children
        |> List.findIndex (fun sid -> model.siteMap.entries.[sid].nodeId = dirId)
    let original =
        { model with
            selectedNodes =
                Some
                    { range =
                        { parent = rootEntry
                          start = dirIdx
                          endd = dirIdx + 1 }
                      focus = dirIdx } }
    let plan = planIndentSelection original |> Option.get
    let result = completeIndent original plan None

    Assert.Equal(original.selectedNodes, result.selectedNodes)
    Assert.Equal(original.siteMap.rootId, result.siteMap.rootId)
    Assert.Equal(original.nextSiteId, result.nextSiteId)
    Assert.Equal(dirId, focusedNodeId result.graph result.selectedNodes.Value)
    Assert.Equal(
        Some(CmdLastResult.Error (None, invalidMoveTargetMessage)),
        result.lastCmdResult)
    Assert.Equal("target is not a valid location", invalidMoveTargetMessage)

[<Fact>]
let ``indent under normal sibling is rejected by History.applyChange`` () =
    let graph, normalId, dirId = folderBesideNormalGraph ()
    let model = emptyModelAt graph Graph.rootId
    let rootEntry = model.siteMap.entries.[model.siteMap.rootId]
    let dirIdx =
        rootEntry.children
        |> List.findIndex (fun sid -> model.siteMap.entries.[sid].nodeId = dirId)
    let selected =
        { model with
            selectedNodes =
                Some
                    { range =
                        { parent = rootEntry
                          start = dirIdx
                          endd = dirIdx + 1 }
                      focus = dirIdx } }
    let plan = planIndentSelection selected |> Option.get
    Assert.Equal(normalId, plan.target.pnode)
    let dirChild = graph.nodes.[Graph.rootId].children.[dirIdx]
    let ops =
        [ Op.Replace(Graph.rootId, dirIdx, [ dirChild ], [])
          Op.Replace(normalId, plan.target.endd, [], [ dirChild ]) ]
    let change =
        { id = selected.revision.Value
          changeId = System.Guid.NewGuid()
          ops = ops }
    let state =
        { graph = graph
          history = History.empty
          revision = selected.revision }
    match History.applyChange change state with
    | ApplyResult.Invalid(_, msg) ->
        Assert.Contains("File and Directory", msg)
    | _ -> Assert.True(false, "expected Invalid for indent under normal")

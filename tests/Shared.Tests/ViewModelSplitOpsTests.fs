module ViewModelSplitOpsTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Shared.ViewModelSplitOps
open GraphChildMapHelpers
open VmTestHelpers
open Xunit

let private owned = ChildNode.owners

let private expectChanged (result: ApplyResult) : State =
    match result with
    | ApplyResult.Changed state -> state
    | ApplyResult.Unchanged _ -> failwith "expected Changed, got Unchanged"
    | ApplyResult.Invalid(_, msg) ->
        failwithf "expected Changed, got Invalid: %s" msg

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private zoomHelloSib () : Graph * NodeId * NodeId * NodeId =
    let g0 = Graph.create ()
    let g1, zoomIds = ModelBuilder.createNodes [ "zoom" ] g0
    let zoomId = zoomIds.[0]
    let g2, childIds = ModelBuilder.createNodes [ "hello"; "sib" ] g1
    let helloId, sibId = childIds.[0], childIds.[1]
    let g3 =
        Graph.replace g2.root 0 [] (owned [ zoomId ]) g2
        |> requireOk "root"
    let graph =
        Graph.replace zoomId 0 [] (owned [ helloId; sibId ]) g3
        |> requireOk "zoom"
    graph, zoomId, helloId, sibId

let private editingHello (graph: Graph) (zoomId: NodeId) : VM =
    let m = emptyModelAt graph zoomId
    let helloInst = m.siteMap.entries.[m.siteMap.rootId].children.[0]
    startEditInstanceAtPos helloInst 3 m
    |> Option.defaultWith (fun () -> failwith "start edit")

let private applySplitOps (ops: Op list) (model: VM) : VM =
    let state0 = { graph = model.graph; eventId = model.eventId }
    let after = ChangeValidation.applyOps ops state0 |> expectChanged
    let siteMap, nextId =
        reconcileSiteMapFrom after.graph model.zoomRoot model.siteMap model.nextSiteId
    { model with
        graph = after.graph
        eventId = after.eventId
        siteMap = siteMap
        nextSiteId = nextId }

let private middleSplitOps helloId parentId (graph: Graph) suffixId =
    let before = Graph.children graph parentId
    [ Op.NewNode(suffixId, "lo")
      ChildListWire.insertAt parentId before 1 [ ChildNode.owner suffixId ]
      Op.SetText(helloId, "hello", "hel") ]

[<Fact>]
let ``editInputSeedText hydrates an empty draft from Graph text`` () =
    Assert.Equal("hello", editInputSeedText "" "hello")
    Assert.Equal("", editInputSeedText "" "")
    Assert.Equal("lo", editInputSeedText "lo" "hello")

[<Fact>]
let ``splitContinueEditText at offset 0 keeps the current node text`` () =
    Assert.Equal("keep me", splitContinueEditText 0 "keep me" "")

[<Fact>]
let ``splitContinueEditText in the middle uses the new node text`` () =
    Assert.Equal("lo", splitContinueEditText 3 "hello" "lo")

[<Fact>]
let ``splitContinueEditText at the end uses the new node text`` () =
    Assert.Equal("", splitContinueEditText 5 "hello" "")

[<Fact>]
let ``continueEditAfterSplit middle split keeps Editing on suffix text`` () =
    let graph, zoomId, helloId, _ = zoomHelloSib ()
    let start = editingHello graph zoomId
    let suffixId = NodeId.New()
    let split =
        applySplitOps (middleSplitOps helloId zoomId start.graph suffixId) start
    let parent = split.siteMap.entries.[split.siteMap.rootId]
    let suffixInst = parent.children.[1]
    let next =
        continueEditAfterSplit 3 "hello" "lo" suffixId (Some suffixInst) split
    match next.mode, tryVisibleEditingEntry next with
    | Editing (text, _), Some entry ->
        Assert.Equal(suffixId, entry.nodeId)
        Assert.Equal("lo", text)
        Assert.Equal("lo", next.graph.nodes.[suffixId].text)
    | _ -> Assert.True(false, "expected Editing and a visible edit row")

[<Fact>]
let ``planPatchDOM after middle split remounts edit on the suffix`` () =
    let graph, zoomId, helloId, _ = zoomHelloSib ()
    let start = editingHello graph zoomId
    let suffixId = NodeId.New()
    let split =
        applySplitOps (middleSplitOps helloId zoomId start.graph suffixId) start
    let parent = split.siteMap.entries.[split.siteMap.rootId]
    let suffixInst = parent.children.[1]
    let helloInst = parent.children.[0]
    let next =
        continueEditAfterSplit 3 "hello" "lo" suffixId (Some suffixInst) split
    let cached = getVisibleInstanceIds start.siteMap |> Set.ofList
    let mutations = planPatchDOM start next cached
    let recreatesHello =
        mutations
        |> List.exists (function
            | RecreateRow id -> id = helloInst
            | _ -> false)
    let createsSuffix =
        mutations
        |> List.exists (function
            | CreateRow id -> id = suffixInst
            | RecreateRow id -> id = suffixInst
            | _ -> false)
    Assert.True(recreatesHello)
    Assert.True(createsSuffix)
    Assert.True(isEditingEntry next next.siteMap.entries.[suffixInst])
    Assert.False(isEditingEntry next next.siteMap.entries.[helloInst])

[<Fact>]
let ``retargetEditingSelection recovers a stale parent instance`` () =
    let graph, zoomId, helloId, _ = zoomHelloSib ()
    let start = editingHello graph zoomId
    let helloInst = start.siteMap.entries.[start.siteMap.rootId].children.[0]
    let staleParent =
        { start.siteMap.entries.[start.siteMap.rootId] with
            instanceId = Sid 999 }
    let stale =
        { start with
            selectedNodes =
                Some
                    { range = { parent = staleParent; start = 0; endd = 1 }
                      focus = 0 } }
    Assert.True((tryVisibleEditingEntry stale).IsNone)
    let recovered = retargetEditingSelection stale
    match tryVisibleEditingEntry recovered with
    | None -> Assert.True(false, "expected a visible edit row")
    | Some entry ->
        Assert.Equal(helloId, entry.nodeId)
        Assert.Equal(helloInst, entry.instanceId)
        match recovered.mode with
        | Editing (text, _) -> Assert.Equal("hello", text)
        | _ -> Assert.True(false, "expected Editing")

[<Fact>]
let ``retargetEditingSelection rehydrates empty draft from focused Graph text`` () =
    let graph, zoomId, helloId, _ = zoomHelloSib ()
    let start = editingHello graph zoomId
    let staleDraft =
        { start with mode = Editing ("", EditCaret.Utf16Index 0) }
    match tryVisibleEditingEntry staleDraft with
    | Some entry -> Assert.Equal(helloId, entry.nodeId)
    | None -> Assert.True(false, "hello is already the edit row")
    let recovered = retargetEditingSelection staleDraft
    match recovered.mode, tryVisibleEditingEntry recovered with
    | Editing (text, _), Some entry ->
        Assert.Equal(helloId, entry.nodeId)
        Assert.Equal("hello", text)
        Assert.Equal("hello", recovered.graph.nodes.[helloId].text)
    | _ -> Assert.True(false, "expected Editing hydrated from Graph")

[<Fact>]
let ``retargetEditingSelection replaces a leftover suffix draft on hello`` () =
    let graph, zoomId, helloId, _ = zoomHelloSib ()
    let start = editingHello graph zoomId
    let leftover =
        { start with mode = Editing ("lo", EditCaret.Utf16Index 0) }
    let recovered = retargetEditingSelection leftover
    match recovered.mode with
    | Editing (text, _) -> Assert.Equal("hello", text)
    | _ -> Assert.True(false, "expected hello Graph text")

[<Fact>]
let ``retargetEditingSelection keeps a legitimately empty new-node draft`` () =
    let graph, zoomId, helloId, _ = zoomHelloSib ()
    let start = editingHello graph zoomId
    let emptyId = NodeId.New()
    let ops =
        let before = Graph.children start.graph zoomId
        [ Op.NewNode(emptyId, "")
          ChildListWire.insertAt zoomId before 1 [ ChildNode.owner emptyId ]
          Op.SetText(helloId, "hello", "hello") ]
    let split = applySplitOps ops start
    let emptyInst = split.siteMap.entries.[split.siteMap.rootId].children.[1]
    let next =
        continueEditAfterSplit 5 "hello" "" emptyId (Some emptyInst) split
    match next.mode, tryVisibleEditingEntry next with
    | Editing (text, _), Some entry ->
        Assert.Equal(emptyId, entry.nodeId)
        Assert.Equal("", text)
        Assert.Equal("", next.graph.nodes.[emptyId].text)
    | _ -> Assert.True(false, "expected empty suffix to stay empty")

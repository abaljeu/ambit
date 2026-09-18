module LargeChangeApplyTests

open System
open System.Diagnostics
open Gambol.Shared
open Gambol.Shared
open Xunit

module Enc = Thoth.Json.Newtonsoft.Encode

/// A parse of a large file arrives as one Change holding thousands of NewNode ops
/// followed by a Replace that attaches them, so per-op cost must not scale with
/// the size of the whole graph.
let private nodeCount = 2000

let private baseState () : State =
    let graph0 = Graph.create ()
    let filler =
        List.init nodeCount (fun i ->
            Node.Create(NodeId.New(), text = "filler " + string i))
    let nodes =
        filler |> List.fold (fun acc node -> Map.add node.id node acc) graph0.nodes
    { graph = Graph.fromNodes graph0.root nodes
      eventId = EventId.zero }

let private parseLikeChange (parentId: NodeId) : Ev =
    let children =
        List.init nodeCount (fun _ -> ChildNode.owner (NodeId.New()))
    { id = EventId.zero
      submissionId = System.Guid.NewGuid()
      authority = Authority "Browser"
      commandName = ""
      body = EventBody.Change
        [ for i, child in List.indexed children ->
            Op.NewNode(child.id, "line " + string i)
          yield Op.Replace(parentId, [], children) ] }

let private eventOps = SpecialNodeTestHelpers.eventOps

let private applied (state: State) (event: Ev) : State =
    match Ev.apply event state with
    | ApplyResult.Changed s -> s
    | ApplyResult.Unchanged s -> s
    | ApplyResult.Invalid(_, msg) -> failwithf "apply failed: %s" msg

let private undoEvent (event: Ev) (state: State) : ApplyResult =
    let ops = eventOps event
    let step (accState, hasChanged) op =
        match Op.undo op accState with
        | ApplyResult.Invalid _ as err -> Error err
        | ApplyResult.Unchanged s' -> Ok(s', hasChanged)
        | ApplyResult.Changed s' -> Ok(s', true)
    let result =
        ops
        |> List.rev
        |> List.fold
            (fun acc op ->
                match acc with
                | Error err -> Error err
                | Ok (s, changed) -> step (s, changed) op)
            (Ok(state, false))
    match result with
    | Error (ApplyResult.Invalid(_, message)) ->
        ApplyResult.Invalid(state, message)
    | Error err -> err
    | Ok (s, false) -> ApplyResult.Unchanged s
    | Ok (s, true) -> ApplyResult.Changed s

let private invertEvent eventId submissionId (event: Ev) : Ev =
    { event with
        id = eventId
        submissionId = submissionId
        body = EventBody.Change (Op.invertAll (eventOps event)) }

[<Fact>]
let ``bulk NewNode apply keeps the parent indexes consistent with a full rebuild`` () =
    let state = baseState ()
    let change = parseLikeChange Graph.workspacesId
    let result = applied state change
    let rebuilt = Graph.fromNodes result.graph.root result.graph.nodes
    Assert.Equal<Map<NodeId, NodeId * int>>(rebuilt.parentByChild, result.graph.parentByChild)
    Assert.Equal<Map<NodeId, NodeId>>(rebuilt.ownerParentByChild, result.graph.ownerParentByChild)
    Assert.Equal<Map<NodeId, Node>>(rebuilt.nodes, result.graph.nodes)

[<Fact>]
let ``bulk NewNode apply does not cost a full graph rebuild per op`` () =
    let state = baseState ()
    let change = parseLikeChange Graph.workspacesId
    let sw = Stopwatch.StartNew()
    applied state change |> ignore
    sw.Stop()
    Assert.True(
        sw.ElapsedMilliseconds < 300L,
        sprintf "applying %d NewNode ops took %dms" nodeCount sw.ElapsedMilliseconds)

[<Fact>]
let ``paste-shaped Undo records one rebuild opportunity per create Op`` () =
    let state = baseState ()
    let change = parseLikeChange Graph.workspacesId
    let createOpCount =
        eventOps change
        |> List.sumBy (function
            | Op.NewNode _ | Op.NewSpecialNode _ -> 1
            | _ -> 0)
    Assert.Equal(nodeCount, createOpCount)
    let changed = applied state change
    let sw = Stopwatch.StartNew()
    let undone =
        match undoEvent change changed with
        | ApplyResult.Changed result -> result
        | ApplyResult.Unchanged _ -> failwith "Undo did not change the graph"
        | ApplyResult.Invalid(_, message) -> failwithf "Undo failed: %s" message
    sw.Stop()
    Assert.Equal<ChildNode list>(
        state.graph.nodes.[Graph.workspacesId].children,
        undone.graph.nodes.[Graph.workspacesId].children)
    printfn "2,000-Node paste-shaped Undo: %d create Ops, %.3f ms"
        createOpCount sw.Elapsed.TotalMilliseconds

[<Fact>]
let ``ordinary inverse of large paste detaches but retains created nodes`` () =
    let state = baseState ()
    let change = parseLikeChange Graph.workspacesId
    let changed = applied state change
    let inverse =
        invertEvent (EventId.fromJson 1) (System.Guid.NewGuid()) change
    Assert.DoesNotContain(
        eventOps inverse,
        fun op ->
            match op with
            | Op.NewNode _ | Op.NewSpecialNode _ -> true
            | _ -> false)
    let undone = applied changed inverse
    Assert.Equal<ChildNode list>(
        state.graph.nodes.[Graph.workspacesId].children,
        undone.graph.nodes.[Graph.workspacesId].children)
    eventOps change
    |> List.iter (function
        | Op.NewNode(nodeId, _)
        | Op.NewSpecialNode(nodeId, _, _) ->
            Assert.True(Map.containsKey nodeId undone.graph.nodes)
        | _ -> ())

let private reachableStructure (graph: Graph) =
    let rec walk nodeId (visited, nodes) =
        if Set.contains nodeId visited then
            visited, nodes
        else
            let node = graph.nodes.[nodeId]
            let shape =
                node.text, node.name, node.cssClasses, node.owner,
                node.kind, node.documentState, node.childrenStatus, node.children
            node.children
            |> List.fold
                (fun state child -> walk child.id state)
                (Set.add nodeId visited, Map.add nodeId shape nodes)

    walk graph.root (Set.empty, Map.empty) |> snd

let private time f =
    let sw = Stopwatch.StartNew()
    let value = f ()
    sw.Stop()
    value, sw.Elapsed.TotalMilliseconds

let private undoInverse history =
    match ClientHistory.undo (Guid.NewGuid()) history with
    | Some (inverse, _) -> inverse
    | None -> failwith "Undo had no inverse Change"

let private projectInverse inverse state =
    match ResidentProjection.applyOps (eventOps inverse) state with
    | ApplyResult.Changed result -> result
    | ApplyResult.Unchanged _ -> failwith "projected apply did not change the graph"
    | ApplyResult.Invalid(_, message) -> failwithf "projected apply failed: %s" message

let private serverApplyInverse inverse state =
    match SpecialNodeTestHelpers.applyChange inverse state with
    | ApplyResult.Changed _ -> ()
    | ApplyResult.Unchanged _ -> failwith "server apply did not change the graph"
    | ApplyResult.Invalid(_, message) -> failwithf "server apply failed: %s" message

let private assertNoCreateOps (event: Ev) =
    Assert.Equal(1, (eventOps event).Length)
    Assert.DoesNotContain(
        eventOps event,
        fun op ->
            match op with
            | Op.NewNode _ | Op.NewSpecialNode _ -> true
            | _ -> false)

[<Fact>]
let ``delivered inverse of large paste measures phases without per-created-Node rebuild`` () =
    let state = baseState ()
    let change = parseLikeChange Graph.workspacesId
    let changed = applied state change
    let history0 =
        ClientHistory.record { change with commandName = "Paste" } (ClientHistory.clear ())
    let before = reachableStructure state.graph
    let after = reachableStructure changed.graph
    let inverse, planMs = time (fun () -> undoInverse history0)
    assertNoCreateOps inverse
    let projected, projectedMs = time (fun () -> projectInverse inverse changed)
    Assert.True((before = reachableStructure projected.graph))
    let redo = invertEvent inverse.id (Guid.NewGuid()) inverse
    Assert.True((after = reachableStructure (applied projected redo).graph))
    projected.graph.nodes
    |> Map.iter (fun nodeId _ ->
        Assert.True(Map.containsKey nodeId changed.graph.nodes))
    let _, serverMs = time (fun () -> serverApplyInverse inverse changed)
    Assert.True(
        projectedMs < 300.0,
        sprintf "projected inverse apply took %.3f ms" projectedMs)
    let siteMap0, nextId =
        ViewModel.buildSiteMapFrom changed.graph Graph.workspacesId (Sid 0)
    let _, siteMs =
        time (fun () ->
            ViewModel.reconcileSiteMapFrom
                projected.graph Graph.workspacesId siteMap0 nextId
            |> ignore)
    let inverseEvent: Ev =
        { id = EventId.zero
          submissionId = inverse.submissionId
          authority = Gambol.Shared.Authority ""
          commandName = ""
          body = inverse.body }
    let _, encodeMs =
        time (fun () ->
            Enc.toString 0 (EventJson.encodeEventBatch { events = [ inverseEvent ] })
            |> ignore)
    let ack: ChangeSuccessResponse =
        { eventId = EventId.fromJson 2
          buildEpochSec = 0
          pageBuildEpochSec = 0
          apiVersion = ApiVersion.current
          isReady = true
          externalChanges = false
          events = [ inverseEvent ]
          message = None
          bootstrapHash = None }
    let _, ackMs =
        time (fun () ->
            Enc.toString 0 (
                ApiResponseSerialization.encodeChangeSuccessResponse ack)
            |> ignore)
    printfn
        "2,000-Node paste inverse phases: planning=%.3f ms projected=%.3f ms"
        planMs projectedMs
    printfn
        "server-apply=%.3f ms sitemap=%.3f ms encode=%.3f ms ack-encode=%.3f ms ops=%d"
        serverMs siteMs encodeMs ackMs (eventOps inverse).Length

/// A nested document parses into one Replace per parent whose children changed.
let private nestedParseChange (documentRootId: NodeId) : Ev =
    let branches =
        List.init 200 (fun _ ->
            ChildNode.owner (NodeId.New()),
            List.init 10 (fun _ -> ChildNode.owner (NodeId.New())))
    { id = EventId.zero
      submissionId = System.Guid.NewGuid()
      authority = Authority "Browser"
      commandName = ""
      body = EventBody.Change
        [ for branch, leaves in branches do
            yield Op.NewNode(branch.id, "branch")
            for leaf in leaves -> Op.NewNode(leaf.id, "leaf")
          yield Op.Replace(documentRootId, [], branches |> List.map fst)
          for branch, leaves in branches ->
            Op.Replace(branch.id, [], leaves) ] }

[<Fact>]
let ``nested parse tail with many Replace ops stays responsive`` () =
    let state = baseState ()
    let change = nestedParseChange Graph.workspacesId
    let sw = Stopwatch.StartNew()
    let result = applied state change
    sw.Stop()
    let rebuilt = Graph.fromNodes result.graph.root result.graph.nodes
    Assert.Equal<Map<NodeId, NodeId * int>>(rebuilt.parentByChild, result.graph.parentByChild)
    Assert.Equal<Map<NodeId, NodeId>>(rebuilt.ownerParentByChild, result.graph.ownerParentByChild)
    Assert.True(
        sw.ElapsedMilliseconds < 300L,
        sprintf "applying a nested parse tail took %dms" sw.ElapsedMilliseconds)

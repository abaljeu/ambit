module LargeChangeApplyTests

open System.Diagnostics
open Gambol.Shared
open Xunit

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
      history = History.empty
      revision = Revision.Zero }

let private parseLikeChange (parentId: NodeId) : Change =
    let children =
        List.init nodeCount (fun _ -> { ref = Ownership.Owner; id = NodeId.New() })
    { id = 0
      changeId = System.Guid.NewGuid()
      ops =
        [ for i, child in List.indexed children ->
            Op.NewNode(child.id, "line " + string i)
          yield Op.Replace(parentId, 0, [], children) ] }

let private applied (state: State) (change: Change) : State =
    match Change.apply change state with
    | ApplyResult.Changed s -> s
    | ApplyResult.Unchanged s -> s
    | ApplyResult.Invalid(_, msg) -> failwithf "apply failed: %s" msg

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

/// A nested document parses into one Replace per parent whose children changed.
let private nestedParseChange (documentRootId: NodeId) : Change =
    let branches =
        List.init 200 (fun _ ->
            { ref = Ownership.Owner; id = NodeId.New() },
            List.init 10 (fun _ -> { ref = Ownership.Owner; id = NodeId.New() }))
    { id = 0
      changeId = System.Guid.NewGuid()
      ops =
        [ for branch, leaves in branches do
            yield Op.NewNode(branch.id, "branch")
            for leaf in leaves -> Op.NewNode(leaf.id, "leaf")
          yield Op.Replace(documentRootId, 0, [], branches |> List.map fst)
          for branch, leaves in branches ->
            Op.Replace(branch.id, 0, [], leaves) ] }

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

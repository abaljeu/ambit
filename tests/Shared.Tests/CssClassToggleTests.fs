module Gambol.Shared.Tests.CssClassToggleTests

open Xunit
open Gambol.Shared

let private apply (ops: Op list) (state: State) : State =
    match ChangeValidation.applyOps ops state with
    | ApplyResult.Changed st -> st
    | ApplyResult.Unchanged st -> st
    | ApplyResult.Invalid(_, msg) -> failwith msg

let private withClasses
    (nid: NodeId)
    (classes: CssClasses)
    (graph: Graph)
    : Graph =
    let node = graph.nodes.[nid]
    { graph with
        nodes =
            graph.nodes
            |> Map.add nid { node with cssClasses = classes } }

let private stateWithKids (count: int) : State * NodeId list =
    let texts = [ for i in 1 .. count -> $"n{i}" ]
    let graph, ids = ModelBuilder.createNodes texts (Graph.create ())
    { graph = graph; eventId = EventId.zero }, ids

[<Theory>]
[<InlineData("b")>]
[<InlineData("i")>]
[<InlineData("check")>]
let ``toggle plus SetClasses adds and removes class on one node``
    (className: string)
    =
    let state, ids = stateWithKids 1
    let nodeId = List.head ids
    let addOps = CssClassToggle.toggleClassOps className state.graph [ nodeId ]
    Assert.Equal<Op list>(
        [ Op.SetClasses(nodeId, CssClass.empty, CssClass.ofList [ className ]) ],
        addOps)
    let afterAdd = apply addOps state
    Assert.True(
        CssClass.contains className afterAdd.graph.nodes.[nodeId].cssClasses)
    let removeOps =
        CssClassToggle.toggleClassOps className afterAdd.graph [ nodeId ]
    Assert.Equal<Op list>(
        [ Op.SetClasses(
            nodeId,
            CssClass.ofList [ className ],
            CssClass.empty) ],
        removeOps)
    let afterRemove = apply removeOps afterAdd
    Assert.False(
        CssClass.contains className afterRemove.graph.nodes.[nodeId].cssClasses)

[<Fact>]
let ``toggle plus SetClasses applies to a multi-node Selection`` () =
    let state, ids = stateWithKids 3
    let a, b, c = ids.[0], ids.[1], ids.[2]
    let selected = [ a; b ]
    let ops = CssClassToggle.toggleClassOps "i" state.graph selected
    Assert.Equal(2, ops.Length)
    let after = apply ops state
    Assert.True(CssClass.contains "i" after.graph.nodes.[a].cssClasses)
    Assert.True(CssClass.contains "i" after.graph.nodes.[b].cssClasses)
    Assert.False(CssClass.contains "i" after.graph.nodes.[c].cssClasses)
    let again = CssClassToggle.toggleClassOps "check" after.graph selected
    let checkedAfter = apply again after
    Assert.True(
        CssClass.contains "check" checkedAfter.graph.nodes.[a].cssClasses)
    Assert.True(
        CssClass.contains "check" checkedAfter.graph.nodes.[b].cssClasses)
    Assert.False(
        CssClass.contains "check" checkedAfter.graph.nodes.[c].cssClasses)

[<Fact>]
let ``toggle plus SetClasses preserves reserved amb- classes`` () =
    let state, ids = stateWithKids 1
    let nodeId = List.head ids
    let prior = CssClass.ofList [ "amb-x"; "b" ]
    let graph = withClasses nodeId prior state.graph
    let state = { state with graph = graph }
    let offOps = CssClassToggle.toggleClassOps "b" graph [ nodeId ]
    let afterOff = apply offOps state
    Assert.Equal(
        CssClass.ofList [ "amb-x" ],
        afterOff.graph.nodes.[nodeId].cssClasses)
    let onOps = CssClassToggle.toggleClassOps "i" afterOff.graph [ nodeId ]
    let afterOn = apply onOps afterOff
    Assert.Equal(
        CssClass.ofList [ "amb-x"; "i" ],
        afterOn.graph.nodes.[nodeId].cssClasses)

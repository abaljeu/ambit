module Gambol.Shared.Tests.ChangeAmendmentTests

open Xunit
open Gambol.Shared

let private stateWithChild (text: string) =
    let initialState =
        { graph = Graph.create ()
          eventId = EventId.zero }

    let childId = NodeId.New()
    let ops =
        [ Op.NewNode(childId, text)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ]

    match ChangeValidation.applyOps ops initialState with
    | ApplyResult.Changed st -> st, childId
    | _ -> failwith "bootstrap failed"

[<Fact>]
let ``applyOps amends stale SetText collision`` () =
    let state, nodeId = stateWithChild "x0"
    let opsA = [ Op.SetText(nodeId, "x0", "xA") ]
    let state =
        match ChangeAmendment.applyOps opsA state with
        | ApplyResult.Changed st, false, _ -> st
        | other -> failwith $"changeA: {other}"

    let opsB = [ Op.SetText(nodeId, "x0", "xB") ]
    let result, amended, applied = ChangeAmendment.applyOps opsB state
    Assert.True(amended)
    Assert.NotEqual<Op list>(opsB, applied)
    match result with
    | ApplyResult.Changed st ->
        Assert.Equal("xA", st.graph.nodes.[nodeId].text)
        Assert.Equal(1, st.graph.nodes.[nodeId].children.Length)
    | _ -> failwith $"changeB result: {result}"

[<Fact>]
let ``applyOps amends stale SetClasses with set delta`` () =
    let state, nodeId = stateWithChild "tagged"
    let prior = CssClass.ofList [ "a"; "b" ]
    let setup = [ Op.SetClasses(nodeId, CssClass.empty, prior) ]
    let state =
        match ChangeAmendment.applyOps setup state with
        | ApplyResult.Changed st, false, _ -> st
        | other -> failwith $"setup: {other}"

    let opsA = [ Op.SetClasses(nodeId, prior, CssClass.ofList [ "b" ]) ]
    let state =
        match ChangeAmendment.applyOps opsA state with
        | ApplyResult.Changed st, false, _ -> st
        | other -> failwith $"changeA: {other}"

    let opsB =
        [ Op.SetClasses(nodeId, prior, CssClass.ofList [ "a"; "b"; "c" ]) ]
    let result, amended, applied = ChangeAmendment.applyOps opsB state
    Assert.True(amended)
    Assert.NotEqual<Op list>(opsB, applied)
    match result with
    | ApplyResult.Changed st ->
        let classes = st.graph.nodes.[nodeId].cssClasses |> CssClass.toList |> Set.ofList
        Assert.Equal<Set<string>>(Set.ofList [ "b"; "c" ], classes)
    | _ -> failwith $"changeB result: {result}"

let private stateWithParentChild (parentText: string) (childText: string) =
    let initialState =
        { graph = Graph.create ()
          eventId = EventId.zero }

    let parentId = NodeId.New()
    let childId = NodeId.New()
    let ops =
        [ Op.NewNode(parentId, parentText)
          Op.NewNode(childId, childText)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner parentId ])
          Op.Replace(parentId, [], [ ChildNode.owner childId ]) ]

    match ChangeValidation.applyOps ops initialState with
    | ApplyResult.Changed st -> st, parentId, childId
    | _ -> failwith "bootstrap failed"

[<Fact>]
let ``applyOps amends stale Replace collision`` () =
    let state, parentId, child0 = stateWithParentChild "p" "c0"
    let childA = NodeId.New()
    let opsA =
        [ Op.NewNode(childA, "a")
          Op.Replace(
              parentId,
              [ ChildNode.owner child0 ],
              [ ChildNode.owner childA ]) ]
    let state =
        match ChangeAmendment.applyOps opsA state with
        | ApplyResult.Changed st, false, _ -> st
        | other -> failwith $"changeA: {other}"

    let childB = NodeId.New()
    let opsB =
        [ Op.NewNode(childB, "b")
          Op.Replace(
              parentId,
              [ ChildNode.owner child0 ],
              [ ChildNode.owner childB ]) ]
    let result, amended, applied = ChangeAmendment.applyOps opsB state
    Assert.True(amended)
    Assert.NotEqual<Op list>(opsB, applied)
    match result with
    | ApplyResult.Changed st ->
        let children = st.graph.nodes.[parentId].children
        Assert.Equal(2, children.Length)
        Assert.Contains(ChildNode.owner childA, children)
        Assert.Contains(ChildNode.owner childB, children)
    | _ -> failwith $"changeB result: {result}"

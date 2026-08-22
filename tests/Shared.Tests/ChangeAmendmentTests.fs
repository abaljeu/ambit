module Gambol.Shared.Tests.ChangeAmendmentTests

open Xunit
open Gambol.Shared

let private stateWithChild (text: string) =
    let initialState =
        { graph = Graph.create ()
          history = History.empty
          revision = Revision 0 }

    let childId = NodeId.New()
    let change =
        { id = 0
          changeId = System.Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, text)
              Op.Replace(Graph.rootId, 0, [], [ ChildNode.owner childId ]) ] }

    match History.applyChange change initialState with
    | ApplyResult.Changed st -> st, childId
    | _ -> failwith "bootstrap failed"

[<Fact>]
let ``applyChange amends stale SetText collision`` () =
    let state, nodeId = stateWithChild "x0"
    let changeA =
        { id = 1
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(nodeId, "x0", "xA") ] }
    let state =
        match ChangeAmendment.applyChange changeA state with
        | ApplyResult.Changed st, false, _ -> st
        | other -> failwith $"changeA: {other}"

    let changeB =
        { id = 2
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(nodeId, "x0", "xB") ] }
    let result, amended, applied = ChangeAmendment.applyChange changeB state
    Assert.True(amended)
    Assert.NotEqual<Op list>(changeB.ops, applied.ops)
    match result with
    | ApplyResult.Changed st ->
        Assert.Equal("xA", st.graph.nodes.[nodeId].text)
        Assert.Equal(1, st.graph.nodes.[nodeId].children.Length)
    | _ -> failwith $"changeB result: {result}"

[<Fact>]
let ``applyChange amends stale SetClasses with set delta`` () =
    let state, nodeId = stateWithChild "tagged"
    let prior = CssClass.ofList [ "a"; "b" ]
    let setup =
        { id = 0
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetClasses(nodeId, CssClass.empty, prior) ] }
    let state =
        match ChangeAmendment.applyChange setup state with
        | ApplyResult.Changed st, false, _ -> st
        | other -> failwith $"setup: {other}"

    let changeA =
        { id = 1
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetClasses(nodeId, prior, CssClass.ofList [ "b" ]) ] }
    let state =
        match ChangeAmendment.applyChange changeA state with
        | ApplyResult.Changed st, false, _ -> st
        | other -> failwith $"changeA: {other}"

    let changeB =
        { id = 2
          changeId = System.Guid.NewGuid()
          ops =
            [ Op.SetClasses(nodeId, prior, CssClass.ofList [ "a"; "b"; "c" ]) ] }
    let result, amended, applied = ChangeAmendment.applyChange changeB state
    Assert.True(amended)
    Assert.NotEqual<Op list>(changeB.ops, applied.ops)
    match result with
    | ApplyResult.Changed st ->
        let classes = st.graph.nodes.[nodeId].cssClasses |> CssClass.toList |> Set.ofList
        Assert.Equal<Set<string>>(Set.ofList [ "b"; "c" ], classes)
    | _ -> failwith $"changeB result: {result}"

module Gambol.Shared.Tests.GraphProjectionTests

open Xunit
open Gambol.Shared

[<Fact>]
let ``graphEquals is true for same graph`` () =
    let g = Graph.create ()
    Assert.True(GraphProjection.graphEquals g g)

[<Fact>]
let ``graphRoundTrip preserves default graph`` () =
    let g = Graph.create ()

    match GraphProjection.graphRoundTrip g with
    | Error e -> Assert.Fail(e)
    | Ok g2 ->
        Assert.True(GraphProjection.graphEquals g g2)

        match g2.nodes.[Graph.trashId].kind with
        | Special Trash -> ()
        | k -> Assert.Fail(sprintf "trash kind after SQL round-trip: %A" k)

[<Fact>]
let ``graphRoundTrip preserves graph with child`` () =
    let g0 = Graph.create ()
    let childId = NodeId.New()

    let change =
        { id = 0
          changeId = System.Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, "x")
              Op.Replace(Graph.rootId, 0, [], [ { ref = Ownership.Owner; id = childId } ]) ] }

    match History.applyChange change { graph = g0; history = History.empty; revision = Revision 0 } with
    | ApplyResult.Changed st ->
        match GraphProjection.graphRoundTrip st.graph with
        | Error e -> Assert.Fail(e)
        | Ok g2 -> Assert.True(GraphProjection.graphEquals st.graph g2)
    | _ -> Assert.Fail("expected Changed")

[<Fact>]
let ``graphEquals is false when text differs`` () =
    let g0 = Graph.create ()
    let childId = NodeId.New()

    let change =
        { id = 0
          changeId = System.Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, "alpha")
              Op.Replace(Graph.rootId, 0, [], [ { ref = Ownership.Owner; id = childId } ]) ] }

    match History.applyChange change { graph = g0; history = History.empty; revision = Revision 0 } with
    | ApplyResult.Changed st ->
        match Graph.setText childId "alpha" "beta" st.graph with
        | Ok g1 -> Assert.False(GraphProjection.graphEquals st.graph g1)
        | Error e -> Assert.Fail(e)
    | _ -> Assert.Fail("expected Changed")

[<Fact>]
let ``graphFromPersistence fails when root missing from nodes`` () =
    let root = Graph.rootId
    let rows: GraphProjection.NodePersistenceRow list = []

    let err =
        GraphProjection.graphFromPersistence root rows []

    match err with
    | Error _ -> ()
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``graphFromPersistence fails when ordinals not dense`` () =
    let root = Graph.rootId
    let cid = System.Guid.NewGuid()

    let nr: GraphProjection.NodePersistenceRow list =
        [ { id = root.Value
            text = "ROOT"
            name = None
            cssClassNames = [] }
          { id = cid
            text = "c"
            name = None
            cssClassNames = [] } ]

    let cr: GraphProjection.ChildPersistenceRow list =
        [ { parentId = root.Value
            ordinal = 1
            childId = cid
            ownership = Ownership.Owner } ]

    match GraphProjection.graphFromPersistence root nr cr with
    | Error _ -> ()
    | Ok _ -> Assert.Fail("expected Error")

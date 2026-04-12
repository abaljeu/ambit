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
    | Ok g2 -> Assert.True(GraphProjection.graphEquals g g2)

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

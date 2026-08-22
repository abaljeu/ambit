module Gambol.Shared.Tests.ChildListMergeTests

open Xunit
open Gambol.Shared

let private owned (id: NodeId) = ChildNode.owner id

[<Fact>]
let ``diff extracts removes and adds in order`` () =
    let a = NodeId.New()
    let b = NodeId.New()
    let c = NodeId.New()
    let d = NodeId.New()
    let anchor = [ owned a; owned b; owned c ]
    let observed = [ owned a; owned c; owned d ]
    let removes, adds = ChildListMerge.diff anchor observed
    Assert.Equal<ChildNode list>([ owned b ], removes)
    Assert.Equal<ChildNode list>([ owned d ], adds)

[<Fact>]
let ``acceptBoth preserves context prefix insert before intent insert`` () =
    let a = NodeId.New()
    let b = NodeId.New()
    let c = NodeId.New()
    let d = NodeId.New()
    let e = NodeId.New()
    let f = NodeId.New()
    let n = NodeId.New()
    let x = NodeId.New()
    let y = NodeId.New()
    let anchor = [ owned a; owned b; owned c; owned d; owned e; owned f ]
    let newList = [ owned a; owned b; owned c; owned d; owned e; owned n; owned f ]
    let current = [ owned x; owned y; owned a; owned b; owned c; owned d; owned e; owned f ]
    let intentRemoves, intentAdds = ChildListMerge.diff anchor newList
    Assert.Empty(intentRemoves)
    Assert.Equal<ChildNode list>([ owned n ], intentAdds)
    let target =
        ChildListMerge.acceptBoth anchor current intentRemoves intentAdds newList
    Assert.Equal<ChildNode list>(
        [ owned x
          owned y
          owned a
          owned b
          owned c
          owned d
          owned e
          owned n
          owned f ],
        target)

[<Fact>]
let ``acceptBoth keeps both children on same-slot collision`` () =
    let c0 = NodeId.New()
    let cA = NodeId.New()
    let cB = NodeId.New()
    let anchor = [ owned c0 ]
    let current = [ owned cA ]
    let newList = [ owned cB ]
    let intentRemoves, intentAdds = ChildListMerge.diff anchor newList
    Assert.Equal<ChildNode list>([ owned c0 ], intentRemoves)
    Assert.Equal<ChildNode list>([ owned cB ], intentAdds)
    let target =
        ChildListMerge.acceptBoth anchor current intentRemoves intentAdds newList
    Assert.Equal<ChildNode list>([ owned cB; owned cA ], target)

[<Fact>]
let ``acceptBoth merges disjoint concurrent appends`` () =
    let a = NodeId.New()
    let b = NodeId.New()
    let newA = NodeId.New()
    let newB = NodeId.New()
    let anchor = [ owned a; owned b ]
    let current = [ owned a; owned b; owned newA ]
    let newList = [ owned a; owned b; owned newB ]
    let intentRemoves, intentAdds = ChildListMerge.diff anchor newList
    Assert.Empty(intentRemoves)
    Assert.Equal<ChildNode list>([ owned newB ], intentAdds)
    let target =
        ChildListMerge.acceptBoth anchor current intentRemoves intentAdds newList
    Assert.Equal<ChildNode list>(
        [ owned a; owned b; owned newB; owned newA ],
        target)

[<Fact>]
let ``resolve fast path when current equals anchor`` () =
    let a = NodeId.New()
    let b = NodeId.New()
    let c = NodeId.New()
    let anchor = [ owned a; owned b ]
    let newList = [ owned a; owned b; owned c ]
    let target = ChildListMerge.resolve anchor anchor newList
    Assert.Equal<ChildNode list>(newList, target)

[<Fact>]
let ``resolve merges concurrent remove and append`` () =
    let a = NodeId.New()
    let b = NodeId.New()
    let c = NodeId.New()
    let d = NodeId.New()
    let anchor = [ owned a; owned b; owned c ]
    let current = [ owned a; owned c ]
    let newList = [ owned a; owned b; owned c; owned d ]
    let target = ChildListMerge.resolve anchor current newList
    Assert.Equal<ChildNode list>([ owned a; owned c; owned d ], target)

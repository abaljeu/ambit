module Gambol.Tests.ScrollIntoViewNeedTests

open Xunit
open Gambol.Shared

let private query
        (elTop: float) (elBot: float) (viewTop: float) (viewBot: float) (eps: float) =
    {
        ScrollIntoViewNeed.element = { top = elTop; bottom = elBot }
        ScrollIntoViewNeed.view = { top = viewTop; bottom = viewBot }
        ScrollIntoViewNeed.epsilon = eps
    }

[<Fact>]
let ``already-visible row near the bottom must not request scroll`` () =
    let q = query 891. 920. 48. 957. 1.
    Assert.False(ScrollIntoViewNeed.needsScroll q)

[<Fact>]
let ``row flush with the scrollport edges must not request scroll`` () =
    let q = query 48. 957. 48. 957. 1.
    Assert.False(ScrollIntoViewNeed.needsScroll q)

[<Fact>]
let ``subpixel overflow inside epsilon must not request scroll`` () =
    let q = query 890.5 957.4 48. 957. 1.
    Assert.False(ScrollIntoViewNeed.needsScroll q)

[<Fact>]
let ``row clipped below the scrollport must request scroll`` () =
    let q = query 940. 968. 48. 957. 1.
    Assert.True(ScrollIntoViewNeed.needsScroll q)

[<Fact>]
let ``row clipped above the scrollport must request scroll`` () =
    let q = query 20. 48. 48. 957. 1.
    Assert.True(ScrollIntoViewNeed.needsScroll q)

[<Fact>]
let ``scroll-padding shrinks the visible band`` () =
    let q = query 900. 930. 48. 920. 1.
    Assert.True(ScrollIntoViewNeed.needsScroll q)

[<Fact>]
let ``edit Up onto a previous line already inside the padded view does not scroll`` () =
    let q = query 860. 888. 48. 920. 1.
    Assert.Equal(0.0, ScrollIntoViewNeed.scrollDelta q)

[<Fact>]
let ``edit Down onto a next line already inside the padded view does not scroll`` () =
    let q = query 200. 228. 48. 920. 1.
    Assert.Equal(0.0, ScrollIntoViewNeed.scrollDelta q)

[<Fact>]
let ``edit line flush with the padded bottom does not scroll`` () =
    let q = query 892. 920. 48. 920. 1.
    Assert.Equal(0.0, ScrollIntoViewNeed.scrollDelta q)

[<Fact>]
let ``edit line below the padded view scrolls by the overflow`` () =
    let q = query 900. 940. 48. 920. 1.
    Assert.Equal(20.0, ScrollIntoViewNeed.scrollDelta q)

[<Fact>]
let ``edit line above the padded view scrolls by the overflow`` () =
    let q = query 20. 48. 48. 920. 1.
    Assert.Equal(-28.0, ScrollIntoViewNeed.scrollDelta q)

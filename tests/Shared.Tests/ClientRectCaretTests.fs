module Gambol.Tests.ClientRectCaretTests

open Xunit
open Gambol.Shared

[<Fact>]
let ``clamp keeps value inside ordered bounds`` () =
    Assert.Equal(3., ClientRectCaret.clamp 2. 5. 3.)
    Assert.Equal(2., ClientRectCaret.clamp 2. 5. 0.)
    Assert.Equal(5., ClientRectCaret.clamp 2. 5. 9.)

[<Fact>]
let ``clamp works when arguments are reversed`` () =
    Assert.Equal(3., ClientRectCaret.clamp 5. 2. 3.)
    Assert.Equal(2., ClientRectCaret.clamp 5. 2. 0.)

[<Fact>]
let ``probeFirstVisualLine is inset from top left`` () =
    let x, y = ClientRectCaret.probeFirstVisualLine 10. 20. 100. 80. 4.
    Assert.Equal(14., x)
    Assert.Equal(24., y)

[<Fact>]
let ``probeLastVisualLine is inset from bottom left`` () =
    let x, y = ClientRectCaret.probeLastVisualLine 10. 20. 100. 80. 4.
    Assert.Equal(14., x)
    Assert.Equal(76., y)

[<Fact>]
let ``probes shrink inset for tiny rects`` () =
    let x1, y1 = ClientRectCaret.probeFirstVisualLine 0. 0. 2. 2. 4.
    Assert.InRange(x1, 0., 2.)
    Assert.InRange(y1, 0., 2.)
    let x2, y2 = ClientRectCaret.probeLastVisualLine 0. 0. 2. 2. 4.
    Assert.InRange(x2, 0., 2.)
    Assert.InRange(y2, 0., 2.)

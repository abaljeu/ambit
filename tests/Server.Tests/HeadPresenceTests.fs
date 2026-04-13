module Gambol.Server.Tests.HeadPresenceTests

open Xunit
open Gambol.Server

[<Fact>]
let ``hasHeadFromUserInteractive true yields true`` () =
    let actual = HeadPresence.hasHeadFromUserInteractive true
    Assert.True(actual)

[<Fact>]
let ``hasHeadFromUserInteractive false yields false`` () =
    let actual = HeadPresence.hasHeadFromUserInteractive false
    Assert.False(actual)

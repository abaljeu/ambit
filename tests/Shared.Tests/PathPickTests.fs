module PathPickTests

open Gambol.Shared
open Xunit

[<Fact>]
let ``choose returns Git when a remote exists`` () =
    Assert.Equal(LoadSavePath.Git, PathPick.choose true)

[<Fact>]
let ``choose returns Desk when no remote exists`` () =
    Assert.Equal(LoadSavePath.Desk, PathPick.choose false)

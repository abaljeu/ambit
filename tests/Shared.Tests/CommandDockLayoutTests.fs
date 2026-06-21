module Gambol.Shared.Tests.CommandDockLayoutTests

open Xunit
open Gambol.Shared.CommandDockLayout
open Gambol.Shared.CommandEntry

[<Fact>]
let ``base strip has Delete not JumpToTarget`` () =
    let ids = commandIds baseStripSlots
    Assert.Contains(Delete, ids)
    Assert.DoesNotContain(JumpToTarget, ids)

[<Fact>]
let ``more tools has JumpToTarget`` () =
    Assert.Contains(JumpToTarget, commandIds moreToolsSlots)

[<Fact>]
let ``dock command ids are unique within each strip`` () =
    let uniqueCount slots =
        (commandIds slots).Length = (Set.ofList (commandIds slots) |> Set.count)
    Assert.True(uniqueCount baseStripSlots)
    Assert.True(uniqueCount moveToolsSlots)
    Assert.True(uniqueCount selectToolsSlots)
    Assert.True(uniqueCount moreToolsSlots)

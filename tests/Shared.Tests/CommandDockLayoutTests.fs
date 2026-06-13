module Gambol.Shared.Tests.CommandDockLayoutTests

open Xunit
open Gambol.Shared.CommandDockLayout

[<Fact>]
let ``base strip fits mobile row limit`` () =
    Assert.Equal(maxBaseSlots, baseStripSlots.Length)

[<Fact>]
let ``move tools strip fits mobile row limit`` () =
    Assert.Equal(maxMoveSlots, moveToolsSlots.Length)

[<Fact>]
let ``select tools strip fits mobile row limit`` () =
    Assert.Equal(maxSelectSlots, selectToolsSlots.Length)

[<Fact>]
let ``dock command ids are unique within each strip`` () =
    let uniqueCount slots =
        (commandIds slots).Length = (Set.ofList (commandIds slots) |> Set.count)
    Assert.True(uniqueCount baseStripSlots)
    Assert.True(uniqueCount moveToolsSlots)
    Assert.True(uniqueCount selectToolsSlots)
    Assert.True(uniqueCount moreToolsSlots)

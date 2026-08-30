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

[<Fact>]
let ``all dock triggers have distinct categories icons and slots`` () =
    let categories = allDockTriggers |> List.map (fun t -> t.category)
    Assert.Equal(allDockTriggers.Length, Set.ofList categories |> Set.count)
    for trigger in allDockTriggers do
        Assert.False(System.String.IsNullOrEmpty trigger.iconId)
        Assert.NotEmpty(trigger.slots)
        match triggerFor trigger.category with
        | Some t -> Assert.Equal(trigger, t)
        | None -> Assert.Fail($"triggerFor missing {trigger.category}")

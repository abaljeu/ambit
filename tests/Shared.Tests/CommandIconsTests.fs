module Gambol.Shared.Tests.CommandIconsTests

open Xunit
open Gambol.Shared.CommandDockLayout
open Gambol.Shared.CommandIcons

let private distinct (ids: string list) =
    ids.Length = (Set.ofList ids |> Set.count)

let private allDockCommandsHaveIcons (slots: DockSlot list) =
    dockCommandNames slots
    |> List.forall (fun name -> iconForCommand name |> Option.isSome)

[<Fact>]
let ``all base strip commands have icons`` () =
    Assert.True(allDockCommandsHaveIcons baseStripSlots)

[<Fact>]
let ``all move tools commands have icons`` () =
    Assert.True(allDockCommandsHaveIcons moveToolsSlots)

[<Fact>]
let ``all select tools commands have icons`` () =
    Assert.True(allDockCommandsHaveIcons selectToolsSlots)

[<Fact>]
let ``all more tools commands have icons`` () =
    Assert.True(allDockCommandsHaveIcons moreToolsSlots)

[<Fact>]
let ``move tool icons are distinct within the strip`` () =
    Assert.True(distinct (dockCommandIconIds moveToolsSlots))

[<Fact>]
let ``select tool icons are distinct within the strip`` () =
    Assert.True(distinct (dockCommandIconIds selectToolsSlots))

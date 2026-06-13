module Gambol.Shared.Tests.CommandCategoryTests

open Xunit
open Gambol.Shared.CommandCategory

[<Fact>]
let ``dockCssClass maps move and select categories`` () =
    Assert.Equal("amb-dock-move", dockCssClass MoveStructure)
    Assert.Equal("amb-dock-select", dockCssClass Selection)

[<Fact>]
let ``dockCssClass defaults for other categories`` () =
    for cat in [ Primary; Navigate; EditText; Clipboard; Format; FileIO ] do
        Assert.Equal("amb-dock-base", dockCssClass cat)

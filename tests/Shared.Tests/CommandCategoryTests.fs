module Gambol.Shared.Tests.CommandCategoryTests

open Xunit
open Gambol.Shared.CommandCategory

[<Fact>]
let ``dockCssClass maps move, select, and file categories`` () =
    Assert.Equal("amb-dock-move", dockCssClass MoveStructure)
    Assert.Equal("amb-dock-select", dockCssClass Selection)
    Assert.Equal("amb-dock-file", dockCssClass FileIO)

[<Fact>]
let ``dockCssClass defaults for other categories`` () =
    for cat in [ Primary; Navigate; EditText; Clipboard; Format ] do
        Assert.Equal("amb-dock-base", dockCssClass cat)

[<Fact>]
let ``searchDialogDockCssClass maps invoked command labels`` () =
    Assert.Equal("amb-dock-base", searchDialogDockCssClass "Find")
    Assert.Equal("amb-dock-move", searchDialogDockCssClass "Move Selected")
    Assert.Equal("amb-dock-file", searchDialogDockCssClass "Insert…")

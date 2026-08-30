module ViewModelCmdLastResultTests

open Xunit
open Gambol.Shared
open Gambol.Shared.ViewModel

[<Fact>]
let ``formatDisplay empty when result is None`` () =
    Assert.Equal("", CmdLastResult.formatDisplay None)

[<Fact>]
let ``toDisplay Ok without name is bare OK`` () =
    Assert.Equal("OK", CmdLastResult.toDisplay (CmdLastResult.Ok None))

[<Fact>]
let ``toDisplay Ok with name prefixes command`` () =
    Assert.Equal(
        "Move Down: OK",
        CmdLastResult.toDisplay (CmdLastResult.Ok (Some "Move Down")))

[<Fact>]
let ``toDisplay Detail with name prefixes message`` () =
    Assert.Equal(
        "Git status: main ↑0 ↓0",
        CmdLastResult.toDisplay
            (CmdLastResult.Detail (Some "Git status", "main ↑0 ↓0")))

[<Fact>]
let ``toDisplay Error with name prefixes message`` () =
    Assert.Equal(
        "Move Selected: target is not a valid location",
        CmdLastResult.toDisplay
            (CmdLastResult.Error
                (Some "Move Selected", "target is not a valid location")))

[<Fact>]
let ``pull success Detail shows label and local path`` () =
    Assert.Equal(
        "Git Pull to Desktop: home → C:\\dev\\home",
        CmdLastResult.toDisplay
            (CmdLastResult.Detail
                (Some "Git Pull to Desktop", "home → C:\\dev\\home")))

[<Fact>]
let ``pull missing mapping Error names workspace`` () =
    let msg = WorkspaceLocalMapping.missingMappingMessage "home"
    Assert.Equal(
        "Git Pull to Desktop: no local mapping for workspace 'home'",
        CmdLastResult.toDisplay
            (CmdLastResult.Error (Some "Git Pull to Desktop", msg)))

[<Fact>]
let ``withCommandName rewrites Ok Detail and Error`` () =
    Assert.Equal(
        CmdLastResult.Ok (Some "Move Down"),
        CmdLastResult.withCommandName (Some "Move Down") (CmdLastResult.Ok None))
    Assert.Equal(
        CmdLastResult.Detail (Some "Git status", "main ↑0 ↓0"),
        CmdLastResult.withCommandName
            (Some "Git status")
            (CmdLastResult.Detail (None, "main ↑0 ↓0")))
    Assert.Equal(
        CmdLastResult.Error (Some "Rename", "cannot rename this node"),
        CmdLastResult.withCommandName
            (Some "Rename")
            (CmdLastResult.Error (None, "cannot rename this node")))

[<Fact>]
let ``toDisplay Undo success is Undo colon command name`` () =
    Assert.Equal(
        "Undo: Edit node",
        CmdLastResult.toDisplay (CmdLastResult.undoResult (Some "Edit node")))

[<Fact>]
let ``toDisplay Undo empty is nothing to undo`` () =
    Assert.Equal(
        "Undo: nothing to undo",
        CmdLastResult.toDisplay (CmdLastResult.undoResult None))

[<Fact>]
let ``toDisplay Redo success is Redo colon command name`` () =
    Assert.Equal(
        "Redo: Paste",
        CmdLastResult.toDisplay (CmdLastResult.redoResult (Some "Paste")))

[<Fact>]
let ``toDisplay Redo empty is nothing to redo`` () =
    Assert.Equal(
        "Redo: nothing to redo",
        CmdLastResult.toDisplay (CmdLastResult.redoResult None))

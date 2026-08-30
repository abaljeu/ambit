module FileSearchDialogModeTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open Xunit

[<Fact>]
let ``FileSearchDialog mode stores query and return mode`` () =
    let mode =
        FileSearchDialog
            { query = "notes.md"
              selectedIndex = 0
              returnTo = Selecting }
    match mode with
    | FileSearchDialog s ->
        Assert.Equal("notes.md", s.query)
        Assert.Equal(Selecting, s.returnTo)
    | _ -> Assert.Fail "expected FileSearchDialog"

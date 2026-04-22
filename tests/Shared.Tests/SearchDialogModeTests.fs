module SearchDialogModeTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open Xunit

[<Fact>]
let ``SearchDialog mode stores invoked command label`` () =
    let stubPick (_: NodeSearchResult) m = m, []
    let mode =
        SearchDialog
            { invokedCommand = "Find"
              query = ""
              selectedIndex = 0
              returnTo = Selecting
              onPick = stubPick }
    match mode with
    | SearchDialog s ->
        Assert.Equal("Find", s.invokedCommand)
        Assert.Equal(Selecting, s.returnTo)
    | _ -> Assert.Fail "expected SearchDialog"

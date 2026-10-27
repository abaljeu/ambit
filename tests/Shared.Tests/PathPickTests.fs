module PathPickTests

open Gambol.Shared
open Xunit

let private choice operation prePick subject : LoadPathChoice =
    { operation = operation
      prePick = prePick
      subject = subject }

let private plainLoad subject remote =
    PathPick.resolveCommand
        (choice
            LoadSaveOperation.Load
            LoadSavePrePick.Plain
            subject)
        (fun () -> Ok remote)

[<Fact>]
let ``choose returns Git when a remote exists`` () =
    Assert.Equal(LoadSavePath.Git, PathPick.choose true)

[<Fact>]
let ``choose returns Desk when no remote exists`` () =
    Assert.Equal(LoadSavePath.Desk, PathPick.choose false)

[<Fact>]
let ``Plain resolves to Git when a remote exists`` () =
    let actual =
        PathPick.resolve LoadSavePrePick.Plain (fun () -> Ok true)

    Assert.Equal(Ok LoadSavePath.Git, actual)

[<Fact>]
let ``Plain resolves to Desk when no remote exists`` () =
    let actual =
        PathPick.resolve LoadSavePrePick.Plain (fun () -> Ok false)

    Assert.Equal(Ok LoadSavePath.Desk, actual)

[<Theory>]
[<InlineData("git")>]
[<InlineData("desk")>]
let ``explicit pre-pick bypasses the remote chooser`` prePickName =
    let chooser () = Error "chooser must be bypassed"
    let prePick, expected =
        if prePickName = "git" then
            LoadSavePrePick.Git, LoadSavePath.Git
        else
            LoadSavePrePick.Desk, LoadSavePath.Desk

    let actual = PathPick.resolve prePick chooser

    Assert.Equal(Ok expected, actual)

[<Fact>]
let ``plain Load of a Directory, File, or non-subject uses the remote`` () =
    let subjects = [
        Some LoadSubject.Directory
        Some LoadSubject.File
        None
    ]
    for subject in subjects do
        Assert.Equal(Ok LoadSavePath.Git, plainLoad subject true)

[<Fact>]
let ``plain Load of a Directory stays Desk when no remote exists`` () =
    Assert.Equal(
        Ok LoadSavePath.Desk,
        plainLoad (Some LoadSubject.Directory) false)

[<Fact>]
let ``plain Load of a File stays Desk when no remote exists`` () =
    Assert.Equal(
        Ok LoadSavePath.Desk,
        plainLoad (Some LoadSubject.File) false)

[<Fact>]
let ``plain Load of a Workspace uses the remote`` () =
    let actual =
        PathPick.resolveCommand
            (choice
                LoadSaveOperation.Load
                LoadSavePrePick.Plain
                (Some LoadSubject.Workspace))
            (fun () -> Ok true)
    Assert.Equal(Ok LoadSavePath.Git, actual)

[<Fact>]
let ``explicit git Load of a File stays Git`` () =
    let actual =
        PathPick.resolveCommand
            (choice
                LoadSaveOperation.Load
                LoadSavePrePick.Git
                (Some LoadSubject.File))
            (fun () -> Error "remote check must not run")
    Assert.Equal(Ok LoadSavePath.Git, actual)

[<Fact>]
let ``plain Save of a Directory still asks the remote`` () =
    let actual =
        PathPick.resolveCommand
            (choice
                LoadSaveOperation.Save
                LoadSavePrePick.Plain
                (Some LoadSubject.Directory))
            (fun () -> Ok true)
    Assert.Equal(Ok LoadSavePath.Git, actual)


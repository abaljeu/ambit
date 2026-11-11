module PathPickTests

open Gambol.Shared
open Xunit

let private choice operation prePick subject : LoadPathChoice =
    { operation = operation
      prePick = prePick
      subject = subject }

let private plainLoad subject remoteExists =
    PathPick.resolveCommand
        (choice
            LoadSaveOperation.Load
            LoadSavePrePick.Plain
            subject)
        remoteExists

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
let ``plain Load of a Directory or a File chooses the Load actor`` () =
    let untouched () = Error "remote check must not run"
    for subject in
        [ Some LoadSubject.Directory
          Some LoadSubject.File
          None ] do
        Assert.Equal(Ok LoadSavePath.Git, plainLoad subject untouched)
    Assert.Equal(
        Ok LoadSavePath.Git,
        plainLoad (Some LoadSubject.Directory) (fun () -> Ok true))
    Assert.Equal(
        Ok LoadSavePath.Git,
        plainLoad (Some LoadSubject.Directory) (fun () -> Ok false))

[<Fact>]
let ``plain Load of a Workspace chooses the Load actor`` () =
    let actual =
        plainLoad
            (Some LoadSubject.Workspace)
            (fun () -> Error "remote check must not run")
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

let private mustApply (graph: Graph) (ops: Op list) =
    let state = { graph = graph; eventId = EventId.zero }
    let next =
        ops
        |> List.fold
            (fun current op ->
                match Op.apply op current with
                | ApplyResult.Changed next -> next
                | ApplyResult.Unchanged next -> next
                | ApplyResult.Invalid(_, message) ->
                    failwith message)
            state
    next.graph

[<Fact>]
let ``a node in a workspace with both a GitHub mapping and a local mapping gets a plain Load that chooses GitHub (Git path)`` () =
    let graph0 = Graph.create ()
    let workspaceId, workspaceOps =
        FileNodeOps.planCreateWorkspace graph0 "home"
    let graph1 = mustApply graph0 workspaceOps
    let fileId, fileOps =
        FileNodeOps.planCreateOwnedFile graph1 workspaceId "note.txt"
    let graph = mustApply graph1 fileOps
    let presence = {
        githubLabels = Set.singleton "home"
        localLabels = Set.singleton "home"
    }
    let path =
        PathPick.plainLoadPath
            graph
            fileId
            LoadSavePrePick.Plain
            presence
    Assert.Equal(Some LoadSavePath.Git, path)

module PathPickTests

open Gambol.Shared
open Xunit

let private choice operation prePick subject : LoadPathChoice =
    { operation = operation
      prePick = prePick
      subject = subject }

let private loadCommand prePick subject =
    PathPick.resolveCommand
        (choice LoadSaveOperation.Load prePick subject)

let private plainLoad subject =
    loadCommand LoadSavePrePick.Plain subject

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
let ``plain Load of a Directory, File, or non-subject stays Desk`` () =
    for subject in
        [ Some LoadSubject.Directory
          Some LoadSubject.File
          None ] do
        Assert.Equal(Ok LoadSavePath.Desk, plainLoad subject)

[<Fact>]
let ``plain Load of a Workspace chooses the Load actor`` () =
    let actual = plainLoad (Some LoadSubject.Workspace)
    Assert.Equal(Ok LoadSavePath.Git, actual)

[<Fact>]
let ``explicit git Load of a Directory, File, or non-subject stays Desk`` () =
    for subject in
        [ Some LoadSubject.Directory
          Some LoadSubject.File
          None ] do
        Assert.Equal(
            Ok LoadSavePath.Desk,
            loadCommand LoadSavePrePick.Git subject)

[<Fact>]
let ``explicit git Load of a Workspace chooses Git`` () =
    let actual =
        loadCommand LoadSavePrePick.Git (Some LoadSubject.Workspace)
    Assert.Equal(Ok LoadSavePath.Git, actual)

[<Fact>]
let ``plain Save enters the Save actor`` () =
    let actual =
        PathPick.resolveCommand
            (choice
                LoadSaveOperation.Save
                LoadSavePrePick.Plain
                (Some LoadSubject.Directory))
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

let private presence github local =
    PathPick.mappingPresence (Set.ofList github) (Set.ofList local)

let private savePath github local =
    let graph0 = Graph.create ()
    let workspaceId, workspaceOps =
        FileNodeOps.planCreateWorkspace graph0 "home"
    let graph1 = mustApply graph0 workspaceOps
    let fileId, fileOps =
        FileNodeOps.planCreateOwnedFile graph1 workspaceId "note.txt"
    let graph = mustApply graph1 fileOps
    fileId, graph, presence github local

[<Fact>]
let ``plain Save with a remote and a local mapping stays on the Save actor`` () =
    let fileId, graph, labels = savePath [ "home" ] [ "home" ]
    let path =
        PathPick.plainSavePath
            graph fileId LoadSavePrePick.Plain labels
    Assert.Equal(None, path)

[<Fact>]
let ``plain Save with a remote and no local mapping stays on the Save actor`` () =
    let fileId, graph, labels = savePath [ "home" ] []
    let path =
        PathPick.plainSavePath
            graph fileId LoadSavePrePick.Plain labels
    Assert.Equal(None, path)

[<Fact>]
let ``plain Save with no remote and a local mapping chooses Desk`` () =
    let fileId, graph, labels = savePath [] [ "home" ]
    let path =
        PathPick.plainSavePath
            graph fileId LoadSavePrePick.Plain labels
    Assert.Equal(Some LoadSavePath.Desk, path)

[<Fact>]
let ``plain Save with no remote and no local mapping stays on the Save actor`` () =
    let fileId, graph, labels = savePath [] []
    let path =
        PathPick.plainSavePath
            graph fileId LoadSavePrePick.Plain labels
    Assert.Equal(None, path)

let private plainLoadPathWithBothMappings pick =
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
    PathPick.plainLoadPath
        graph
        (pick (workspaceId, fileId))
        LoadSavePrePick.Plain
        presence

[<Fact>]
let ``plain Load of a Workspace with both mappings chooses Git`` () =
    Assert.Equal(Some LoadSavePath.Git, plainLoadPathWithBothMappings fst)

[<Fact>]
let ``a File in a workspace with both mappings keeps a plain Load with no git`` () =
    Assert.Equal(None, plainLoadPathWithBothMappings snd)

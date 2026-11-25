module Gambol.Server.Tests.LoadSaveSaveCommandTests

open System
open System.IO
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend
open Gambol.Server.Tests.LoadSaveCommandFixtures

[<SkippableFact>]
let ``routed Git Save commits then pushes through Peer Actor`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let harness = createGitHarness "save"
    try
        File.WriteAllText(
            Path.Combine(harness.workspace, "saved.txt"),
            "saved")
        do!
            postRoutedFocus
                LoadSaveOperation.Save
                LoadSavePrePick.Git
                harness.seed.workspaceId
                harness
        let! stopped =
            waitForStop harness.host harness.seed.workspaceId
        Assert.Equal(Some ActorSucceeded, stopped)
        let verify = Path.Combine(harness.parent, "verify")
        git harness.parent $"clone {harness.remote} verify" |> ignore
        Assert.True(File.Exists(Path.Combine(verify, "saved.txt")))
        Assert.Equal(
            git verify "rev-parse refs/heads/main",
            git harness.workspace "rev-parse refs/heads/main")
    finally
        CoreMailboxApi.dispose harness.host
}

/// Plain Save of a file in a workspace that has a git remote pushes that work tree.
[<SkippableFact>]
let ``plain Save with a GitHub remote pushes from a file focus`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let harness = createGitHarness "save"
    try
        let fileId, fileOps =
            FileNodeOps.planCreateOwnedFile
                (readGraph harness.host)
                harness.seed.workspaceId
                "note.txt"
        postChange harness.host fileOps
        File.WriteAllText(
            Path.Combine(harness.workspace, "saved.txt"),
            "saved")
        do!
            postRoutedFocus
                LoadSaveOperation.Save
                LoadSavePrePick.Plain
                fileId
                harness
        let! stopped = waitForStop harness.host fileId
        Assert.Equal(Some ActorSucceeded, stopped)
        let verify = Path.Combine(harness.parent, "verify-plain")
        git harness.parent $"clone {harness.remote} verify-plain" |> ignore
        Assert.Equal(
            "saved",
            File.ReadAllText(Path.Combine(verify, "saved.txt")))
        match GitSave.runGit verify "log --oneline --grep=gambol:" with
        | Ok text ->
            let commits =
                text.Split(
                    [| '\r'; '\n' |],
                    StringSplitOptions.RemoveEmptyEntries)
            Assert.Equal(1, commits.Length)
        | Error err -> Assert.Fail(err)
    finally
        CoreMailboxApi.dispose harness.host
}

/// A repository with no remote is Save Neither: persist, one commit, no push.
[<SkippableFact>]
let ``plain Save with no GitHub remote commits once and does not push`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let dataDir = newTempDir ()
    let workspace = Path.Combine(dataDir, "home")
    Directory.CreateDirectory(workspace) |> ignore
    git workspace "init -b master" |> ignore
    configureIdentity workspace
    let host = createProductionHost dataDir
    try
        let seed = seedWorkspace host "save"
        let fileId, fileOps =
            FileNodeOps.planCreateOwnedFile
                (readGraph host)
                seed.workspaceId
                "note.txt"
        postChange host fileOps
        let state =
            CoreMailboxApi.graphAtEvent host
            |> Async.RunSynchronously
            |> requireOk "file event"
        let harness =
            { dataDir = dataDir
              parent = dataDir
              remote = ""
              source = ""
              workspace = workspace
              host = host
              seed =
                { seed with
                    focusId = fileId
                    eventId = state.eventId } }
        let! result =
            postCommand
                LoadSaveOperation.Save
                LoadSavePrePick.Plain
                harness
        let response = requireResponse result
        Assert.Equal(LoadSavePath.Git, response.path)
        let! stopped = waitForStop host fileId
        Assert.Equal(Some ActorSucceeded, stopped)
        let saved = readGraph host
        Assert.Equal(ParseState.Parsed, saved.nodes.[fileId].parseState)
        Assert.True(File.Exists(Path.Combine(workspace, "note.txt")))
        let saveCommits () =
            match GitSave.runGit workspace "log --oneline --grep=gambol:" with
            | Ok text ->
                text.Split(
                    [| '\r'; '\n' |],
                    StringSplitOptions.RemoveEmptyEntries)
                |> Array.length
            | Error err ->
                Assert.Fail(err)
                0
        Assert.Equal(1, saveCommits ())
        let! stopsBefore = actorStops host fileId
        let again =
            CoreMailboxApi.graphAtEvent host
            |> Async.RunSynchronously
            |> requireOk "after save"
        let! second =
            postCommand
                LoadSaveOperation.Save
                LoadSavePrePick.Plain
                { harness with
                    seed =
                        { harness.seed with
                            eventId = again.eventId } }
        Assert.Equal(LoadSavePath.Git, (requireResponse second).path)
        let! stopsAfter =
            waitForStopCount
                host
                fileId
                (stopsBefore + 1)
                (DateTime.UtcNow.AddSeconds 30.0)
        Assert.True(stopsAfter > stopsBefore, "second Save posted no stop")
        Assert.Equal(1, saveCommits ())
    finally
        CoreMailboxApi.dispose host
}

/// Explicit desk Save stays Desk when the work tree has a remote.
[<SkippableFact>]
let ``explicit Desk Save with a GitHub remote takes the Desk path`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let harness = createGitHarness "save"
    try
        let! result =
            postCommand
                LoadSaveOperation.Save
                LoadSavePrePick.Desk
                harness
        assertDeskResponse result
        let! history =
            CoreMailboxApi.eventHistory harness.host |> Async.StartAsTask
        Assert.DoesNotContain(
            history.events,
            hasActorStart harness.seed.focusId)
    finally
        CoreMailboxApi.dispose harness.host
}

[<Fact>]
let ``Desk load-save runs no Actor and posts no StartActor`` () = task {
    let pool = CoreActorPool.create ()
    let host =
        CoreMailbox.host
            pool
            (FileAgent.persist (FileAgent.create (newTempDir ())))
            admittedCredentials
    try
        let nodeId = NodeId.New()
        let router =
            { resolvePath = fun _ _ -> Result.Ok LoadSavePath.Desk
              startCommand =
                fun path command ->
                    LoadSaveStart.startDesk
                        (CoreDoor.create true host testCaller)
                        command }
        let body =
            request
                LoadSaveOperation.Load
                LoadSavePrePick.Desk
                nodeId
                EventId.zero
            |> encodeRequest
        let! result =
            Api.postLoadSaveCommand
                router
                (CoreDoor.create true host testCaller)
                body
            |> Async.StartAsTask
        let response = requireResponse result
        Assert.Equal(LoadSavePath.Desk, response.path)
        Assert.Equal(None, response.command)
        Assert.Empty(pool.liveActorNodeIds ())
        let! history =
            CoreMailboxApi.eventHistory host |> Async.StartAsTask
        let live = ActorLive.applyEvents history.events Set.empty
        Assert.False(Set.contains nodeId live)
        let started =
            history.events
            |> List.exists (fun event ->
                match event.body with
                | EventBody.Order (Order.StartActor _)
                | EventBody.ActorStop _ -> true
                | _ -> false)
        Assert.False(started, "Desk Load posted an actor lifecycle row")
    finally
        CoreMailboxApi.dispose host
}

/// A workspace folder with no repository is Save Neither, and it still persists.
[<SkippableFact>]
let ``plain Save of a non-git workspace persists and does not push`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let dataDir = newTempDir ()
    let host = createProductionHost dataDir
    try
        let seed = seedWorkspace host "save"
        let fileId, fileOps =
            FileNodeOps.planCreateOwnedFile
                (readGraph host)
                seed.workspaceId
                "note.txt"
        postChange host fileOps
        let state =
            CoreMailboxApi.graphAtEvent host
            |> Async.RunSynchronously
            |> requireOk "file event"
        let harness =
            { dataDir = dataDir
              parent = dataDir
              remote = ""
              source = ""
              workspace = Path.Combine(dataDir, "home")
              host = host
              seed =
                { seed with
                    focusId = fileId
                    eventId = state.eventId } }
        let! result =
            postCommand
                LoadSaveOperation.Save
                LoadSavePrePick.Plain
                harness
        let response = requireResponse result
        Assert.Equal(LoadSavePath.Git, response.path)
        let! stopped = waitForStop host fileId
        Assert.Equal(Some ActorSucceeded, stopped)
        let saved = readGraph host
        Assert.Equal(ParseState.Parsed, saved.nodes.[fileId].parseState)
        Assert.True(File.Exists(Path.Combine(dataDir, "home", "note.txt")))
    finally
        CoreMailboxApi.dispose host
}

/// A parent repository is not the workspace. No `.git` in the workspace
/// is Neither: persist, and no commit, pull, or push.
[<SkippableFact>]
let ``plain Save of a non-git workspace ignores a parent repository`` () =
    task {
        Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
        let parent = newTempDir ()
        let remote = Path.Combine(parent, "remote.git")
        let dataDir = Path.Combine(parent, "data")
        Directory.CreateDirectory(remote) |> ignore
        Directory.CreateDirectory(dataDir) |> ignore
        git remote "init --bare -b master" |> ignore
        git dataDir "init -b master" |> ignore
        configureIdentity dataDir
        commitFile dataDir "seed.txt" "seed" "seed"
        git dataDir $"remote add origin {remote}" |> ignore
        git dataDir "push -u origin master" |> ignore
        let host = createProductionHost dataDir
        try
            let seed = seedWorkspace host "save"
            let fileId, fileOps =
                FileNodeOps.planCreateOwnedFile
                    (readGraph host)
                    seed.workspaceId
                    "note.txt"
            postChange host fileOps
            let state =
                CoreMailboxApi.graphAtEvent host
                |> Async.RunSynchronously
                |> requireOk "file event"
            let harness =
                { dataDir = dataDir
                  parent = parent
                  remote = remote
                  source = ""
                  workspace = Path.Combine(dataDir, "home")
                  host = host
                  seed =
                    { seed with
                        focusId = fileId
                        eventId = state.eventId } }
            let! result =
                postCommand
                    LoadSaveOperation.Save
                    LoadSavePrePick.Plain
                    harness
            Assert.Equal(LoadSavePath.Git, (requireResponse result).path)
            let! stopped = waitForStop host fileId
            Assert.Equal(Some ActorSucceeded, stopped)
            let note = Path.Combine(dataDir, "home", "note.txt")
            Assert.True(File.Exists note)
            Assert.False(
                Directory.Exists(Path.Combine(dataDir, "home", ".git")))
            match GitSave.runGit dataDir "log --oneline --grep=gambol:" with
            | Ok text -> Assert.True(String.IsNullOrWhiteSpace text)
            | Error err -> Assert.Fail(err)
            let verify = Path.Combine(parent, "verify-parent")
            git parent $"clone {remote} verify-parent" |> ignore
            Assert.False(
                File.Exists(Path.Combine(verify, "home", "note.txt")))
        finally
            CoreMailboxApi.dispose host
    }

/// No work tree is the same Neither arm: persist, and no commit, pull, or push.
[<Fact>]
let ``plain Save with no work tree persists and does not commit pull or push`` () =
    task {
        let dataDir = newTempDir ()
        let host = createProductionHost dataDir
        let pulls, pushes, dependencies =
            withRemote (fun _ -> Result.Ok true)
        try
            let fileId = NodeId.New()
            postChange
                host
                [ Op.NewSpecialNode(fileId, SpecialKind.File, "loose.txt") ]
            let state =
                CoreMailboxApi.graphAtEvent host
                |> Async.RunSynchronously
                |> requireOk "detached file"
            let seed =
                { workspaceId = Graph.rootId
                  runDefinitionId = fileId
                  focusId = fileId
                  eventId = state.eventId }
            let! result =
                Api.postLoadSaveCommand
                    (actorRouter
                        host
                        dataDir
                        dependencies
                        LoadSaveOperation.Save)
                    (CoreDoor.create true host testCaller)
                    (encodeRequest
                        (requestForFocus
                            LoadSaveOperation.Save
                            LoadSavePrePick.Plain
                            seed
                            fileId))
                |> Async.StartAsTask
            Assert.Equal(LoadSavePath.Git, (requireResponse result).path)
            let! stopped = waitForStop host fileId
            Assert.Equal(Some ActorSucceeded, stopped)
            Assert.Empty(pulls)
            Assert.Empty(pushes)
            let saved = readGraph host
            Assert.Equal(ParseState.Parsed, saved.nodes.[fileId].parseState)
        finally
            CoreMailboxApi.dispose host
    }

/// ROOT's repository is the data directory. A remote there is git Save.
[<SkippableFact>]
let ``plain Save of ROOT with a data-directory remote commits that repository`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let parent = newTempDir ()
    let remote = Path.Combine(parent, "remote.git")
    let source = Path.Combine(parent, "source")
    let dataDir = Path.Combine(parent, "data")
    Directory.CreateDirectory(remote) |> ignore
    Directory.CreateDirectory(source) |> ignore
    git remote "init --bare -b main" |> ignore
    git source "init -b main" |> ignore
    configureIdentity source
    commitFile source "seed.txt" "seed" "seed"
    git source $"remote add origin {remote}" |> ignore
    git source "push -u origin main" |> ignore
    git parent $"clone {remote} data" |> ignore
    configureIdentity dataDir
    let host = createProductionHost dataDir
    try
        let fileId, fileOps =
            FileNodeOps.planCreateOwnedFile
                (readGraph host)
                Graph.rootId
                "root.txt"
        postChange host fileOps
        let state =
            CoreMailboxApi.graphAtEvent host
            |> Async.RunSynchronously
            |> requireOk "root file"
        let harness =
            { dataDir = dataDir
              parent = parent
              remote = remote
              source = source
              workspace = dataDir
              host = host
              seed =
                { workspaceId = Graph.rootId
                  runDefinitionId = fileId
                  focusId = fileId
                  eventId = state.eventId } }
        do!
            postRoutedFocus
                LoadSaveOperation.Save
                LoadSavePrePick.Plain
                fileId
                harness
        let! stopped = waitForStop host fileId
        Assert.Equal(Some ActorSucceeded, stopped)
        let verify = Path.Combine(parent, "verify-root")
        git parent $"clone {remote} verify-root" |> ignore
        Assert.True(File.Exists(Path.Combine(verify, "root.txt")))
    finally
        CoreMailboxApi.dispose host
}

module Gambol.Server.Tests.GithubTransportLoadPullTests

open System
open System.IO
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private postChange (host: MailboxHost) (ops: Op list) =
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change ops }
    CoreMailboxApi.postGraphOnly host testCaller event
    |> Async.RunSynchronously
    |> requireOk "post"
    |> ignore

let private readGraph (host: MailboxHost) =
    CoreMailboxApi.graphAtEvent host
    |> Async.RunSynchronously
    |> requireOk "state"
    |> fun state -> state.graph

let private markCurrent (host: MailboxHost) (nodeId: NodeId) =
    CoreMailboxApi.postNotice host (Notice.NodeParsed nodeId)
    let graph = readGraph host
    match Map.tryFind nodeId graph.nodes with
    | Some node when Node.documentStateEnum node = Current -> ()
    | _ -> Assert.Fail("NodeParsed did not set Current")

let private seedDocs (host: MailboxHost) =
    let workspaceId, workspaceOps =
        FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    postChange host workspaceOps
    let graph = readGraph host
    let docsId, docsOps =
        FileNodeOps.planCreateOwnedDirectory graph workspaceId "docs"
    postChange host docsOps
    markCurrent host docsId
    workspaceId, docsId

let private fileNamed (graph: Graph) parentId name =
    Graph.children graph parentId
    |> List.map (fun child -> graph.nodes.[child.id])
    |> List.find (fun node ->
        Filename.tryValue node.name = Some name)

let private pullAddsExtra root =
    fun _ _ ->
        let docs = Path.Combine(root, "docs")
        Directory.CreateDirectory(docs) |> ignore
        File.WriteAllText(Path.Combine(docs, "extra.txt"), "extra")
        WorkspaceGit.commitAll root "pull" None
        |> Result.map (fun _ -> "pulled")

let private markingDependencies pull push =
    let tracked =
        { branch = "master"
          remote = "origin"
          upstream = "refs/heads/master" }
    { git =
        { remoteExists = fun _ -> Ok true
          fetchTracked = fun _ _ -> Ok "fetched"
          pullTracked = pull
          commitAll =
            fun _ _ _ -> Ok(tracked, GitSave.nothingToCommitMessage)
          pushTracked = push } }

[<SkippableFact>]
let ``git Load pull marks a received file and its directory Unparsed`` () =
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let dataDir = newTempDir ()
    let host, changes = createAdmittedFile dataDir
    try
        let workspaceId, _ = seedDocs host
        let root = Path.Combine(dataDir, "home")
        WorkspaceGit.ensureInit root |> requireOk "init" |> ignore
        File.WriteAllText(Path.Combine(root, "seed.txt"), "seed")
        WorkspaceGit.commitAll root "seed" None
        |> requireOk "seed"
        |> ignore
        GithubTransportActor.runLoad
            (markingDependencies
                (pullAddsExtra root)
                (fun _ _ _ -> Error "unused"))
            dataDir
            changes
            workspaceId
            root
        |> Async.RunSynchronously
        |> requireOk "load pull"
        |> ignore
    finally
        CoreMailboxApi.dispose host

[<Fact>]
let ``git Load commits before pull`` () =
    let steps = ResizeArray<string>()
    let tracked =
        { branch = "master"
          remote = "origin"
          upstream = "refs/heads/master" }
    let commit _ message hint =
        steps.Add("commit")
        Assert.Equal("gambol: git Load", message)
        Assert.Equal(None, hint)
        Ok(tracked, GitSave.nothingToCommitMessage)
    let fetch _ _ =
        steps.Add("fetch")
        Ok "fetched"
    let pull _ _ =
        steps.Add("pull")
        Ok "pulled"
    let push _ _ _ =
        steps.Add("push")
        Error "push unused"
    let deps =
        { git =
            { remoteExists = fun _ -> Ok true
              fetchTracked = fetch
              pullTracked = pull
              commitAll = commit
              pushTracked = push } }
    let dataDir = newTempDir ()
    let host, changes = createAdmittedFile dataDir
    try
        let workspaceId, _ = seedDocs host
        let root = Path.Combine(dataDir, "home")
        GithubTransportActor.runLoad
            deps dataDir changes workspaceId root
        |> Async.RunSynchronously
        |> requireOk "load"
        |> ignore
        Assert.Equal<string list>(
            [ "commit"; "fetch"; "pull" ],
            steps |> Seq.toList)
    finally
        CoreMailboxApi.dispose host

let private git root arguments =
    match GitSave.runGit root arguments with
    | Ok output -> output
    | Error err ->
        Assert.Fail($"{arguments}: {err}")
        ""

let private configureIdentity root =
    git root "config user.email test@gambol" |> ignore
    git root "config user.name test" |> ignore

let private addNormalFocus host workspaceId =
    let focusId = NodeId.New()
    let graph = readGraph host
    let children = Graph.children graph workspaceId
    postChange
        host
        [ Op.NewNode(focusId, "subnode")
          Op.Replace(
              workspaceId,
              children,
              children @ [ ChildNode.owner focusId ]) ]
    focusId

[<SkippableFact>]
let ``git Save pull marks a received file Unparsed before push`` () =
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let dataDir = newTempDir ()
    let host, changes = createAdmittedFile dataDir
    try
        let workspaceId, docsId = seedDocs host
        let focusId = addNormalFocus host workspaceId
        let root = Path.Combine(dataDir, "home")
        WorkspaceGit.ensureInit root |> requireOk "init" |> ignore
        configureIdentity root
        File.WriteAllText(Path.Combine(root, "seed.txt"), "seed")
        WorkspaceGit.commitAll root "seed" None
        |> requireOk "seed"
        |> ignore
        let marked = ResizeArray<string>()
        let push _ _ _ =
            let extra = fileNamed (readGraph host) docsId "extra.txt"
            Assert.Equal(ParseState.Unparsed, extra.parseState)
            marked.Add("unparsed")
            Ok "pushed"
        GithubTransportActor.runSave
            (markingDependencies (pullAddsExtra root) push)
            dataDir
            changes
            focusId
        |> Async.RunSynchronously
        |> requireOk "save pull"
        |> ignore
        Assert.Equal<string list>([ "unparsed" ], marked |> Seq.toList)
    finally
        CoreMailboxApi.dispose host

let private seedCurrentFile (host: MailboxHost) parentId name =
    let graph = readGraph host
    let fileId, ops =
        FileNodeOps.planCreateOwnedFile graph parentId name
    postChange host ops
    markCurrent host fileId
    fileId

let private cloneRemote dataDir root =
    let parent = Directory.GetParent(dataDir).FullName
    let token = Guid.NewGuid().ToString("N")
    let remote = Path.Combine(parent, $"remote-{token}.git")
    let source = Path.Combine(parent, $"source-{token}")
    Directory.CreateDirectory(remote) |> ignore
    git remote "init --bare -b master" |> ignore
    git root $"remote add origin {remote}" |> ignore
    git root "push -u origin master" |> ignore
    git parent $"clone {remote} source-{token}" |> ignore
    configureIdentity source
    source

let private commitRemote source message =
    git source "add -A" |> ignore
    git source $"commit -m {message}" |> ignore
    git source "push origin master" |> ignore

let private runProductionLoad dataDir host changes workspaceId root =
    GithubTransportActor.runLoad
        GithubTransportActor.productionDependencies
        dataDir
        changes
        workspaceId
        root
    |> Async.RunSynchronously
    |> requireOk "load"
    |> ignore

[<SkippableFact>]
let ``git Load pulls when a dirty tracked file would be overwritten`` () =
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let dataDir = newTempDir ()
    let host, changes = createAdmittedFile dataDir
    try
        let workspaceId, _ = seedDocs host
        let root = Path.Combine(dataDir, "home")
        WorkspaceGit.ensureInit root |> requireOk "init" |> ignore
        configureIdentity root
        let note = Path.Combine(root, "note.txt")
        File.WriteAllText(note, "base")
        WorkspaceGit.commitAll root "seed" None
        |> requireOk "seed"
        |> ignore
        let parent = Directory.GetParent(dataDir).FullName
        let token = Guid.NewGuid().ToString("N")
        let remote = Path.Combine(parent, $"remote-{token}.git")
        let source = Path.Combine(parent, $"source-{token}")
        Directory.CreateDirectory(remote) |> ignore
        git remote "init --bare -b master" |> ignore
        git root $"remote add origin {remote}" |> ignore
        git root "push -u origin master" |> ignore
        git parent $"clone {remote} source-{token}" |> ignore
        configureIdentity source
        File.WriteAllText(Path.Combine(source, "note.txt"), "remote")
        git source "add -A" |> ignore
        git source "commit -m remote-change" |> ignore
        git source "push origin master" |> ignore
        File.WriteAllText(note, "local")
        GithubTransportActor.runLoad
            GithubTransportActor.productionDependencies
            dataDir
            changes
            workspaceId
            root
        |> Async.RunSynchronously
        |> requireOk "load"
        |> ignore
        Assert.Equal("remote", File.ReadAllText(note))
        let localLog = git root "log HEAD --format=%s"
        let remoteLog = git root "log origin/master --format=%s"
        Assert.Contains("gambol: git Load", localLog)
        Assert.DoesNotContain("gambol: git Load", remoteLog)
    finally
        CoreMailboxApi.dispose host

[<SkippableFact>]
let ``git Load keeps a dirty file the pull does not change`` () =
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let dataDir = newTempDir ()
    let host, changes = createAdmittedFile dataDir
    try
        let workspaceId, _ = seedDocs host
        let keptId = seedCurrentFile host workspaceId "kept.txt"
        let root = Path.Combine(dataDir, "home")
        WorkspaceGit.ensureInit root |> requireOk "init" |> ignore
        configureIdentity root
        let kept = Path.Combine(root, "kept.txt")
        File.WriteAllText(kept, "base")
        File.WriteAllText(Path.Combine(root, "other.txt"), "base")
        WorkspaceGit.commitAll root "seed" None
        |> requireOk "seed"
        |> ignore
        let source = cloneRemote dataDir root
        File.WriteAllText(Path.Combine(source, "other.txt"), "remote")
        commitRemote source "remote-other"
        File.WriteAllText(kept, "local kept")
        runProductionLoad dataDir host changes workspaceId root
        Assert.Equal("local kept", File.ReadAllText(kept).TrimEnd())
        let graph = readGraph host
        Assert.Equal(Current, graph.nodes.[keptId].documentState.AsEnum)
    finally
        CoreMailboxApi.dispose host

[<SkippableFact>]
let ``git Load replaces a dirty file the pull changes`` () =
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let dataDir = newTempDir ()
    let host, changes = createAdmittedFile dataDir
    try
        let workspaceId, _ = seedDocs host
        let noteId = seedCurrentFile host workspaceId "note.txt"
        let root = Path.Combine(dataDir, "home")
        WorkspaceGit.ensureInit root |> requireOk "init" |> ignore
        configureIdentity root
        let note = Path.Combine(root, "note.txt")
        File.WriteAllText(note, "base")
        WorkspaceGit.commitAll root "seed" None
        |> requireOk "seed"
        |> ignore
        let source = cloneRemote dataDir root
        File.WriteAllText(Path.Combine(source, "note.txt"), "remote newer")
        commitRemote source "remote-note"
        File.WriteAllText(note, "local draft")
        runProductionLoad dataDir host changes workspaceId root
        Assert.Equal("remote newer", File.ReadAllText(note).TrimEnd())
        let graph = readGraph host
        Assert.Equal(Unparsed, graph.nodes.[noteId].documentState.AsEnum)
    finally
        CoreMailboxApi.dispose host

[<SkippableFact>]
let ``git Load pulls a clean work tree and writes no commit`` () =
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let dataDir = newTempDir ()
    let host, changes = createAdmittedFile dataDir
    try
        let workspaceId, _ = seedDocs host
        let root = Path.Combine(dataDir, "home")
        WorkspaceGit.ensureInit root |> requireOk "init" |> ignore
        configureIdentity root
        let note = Path.Combine(root, "note.txt")
        File.WriteAllText(note, "base")
        WorkspaceGit.commitAll root "seed" None
        |> requireOk "seed"
        |> ignore
        let parent = Directory.GetParent(dataDir).FullName
        let token = Guid.NewGuid().ToString("N")
        let remote = Path.Combine(parent, $"remote-{token}.git")
        let source = Path.Combine(parent, $"source-{token}")
        Directory.CreateDirectory(remote) |> ignore
        git remote "init --bare -b master" |> ignore
        git root $"remote add origin {remote}" |> ignore
        git root "push -u origin master" |> ignore
        git parent $"clone {remote} source-{token}" |> ignore
        configureIdentity source
        File.WriteAllText(Path.Combine(source, "note.txt"), "remote")
        git source "add -A" |> ignore
        git source "commit -m remote-change" |> ignore
        git source "push origin master" |> ignore
        let countBefore =
            (git root "rev-list --count HEAD").Trim()
        GithubTransportActor.runLoad
            GithubTransportActor.productionDependencies
            dataDir
            changes
            workspaceId
            root
        |> Async.RunSynchronously
        |> requireOk "load"
        |> ignore
        Assert.Equal("remote", File.ReadAllText(note))
        let localLog = git root "log HEAD --format=%s"
        Assert.DoesNotContain("gambol: git Load", localLog)
        let countAfter =
            (git root "rev-list --count HEAD").Trim()
        Assert.Equal(int countBefore + 1, int countAfter)
    finally
        CoreMailboxApi.dispose host

let private trackedBranch =
    { branch = "master"
      remote = "origin"
      upstream = "refs/heads/master" }

[<Fact>]
let ``git Load and git Save wait while the Workspace lock is held`` () =
    let dataDir = newTempDir ()
    let host, changes = createAdmittedFile dataDir
    try
        let workspaceId, workspaceOps =
            FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
        postChange host workspaceOps
        let focusId = NodeId.New()
        let seeded = readGraph host
        let children = Graph.children seeded workspaceId
        postChange
            host
            [ Op.NewNode(focusId, "subnode")
              Op.Replace(
                  workspaceId,
                  children,
                  children @ [ ChildNode.owner focusId ]) ]
        let graph = readGraph host
        let root = Path.Combine(dataDir, "home")
        let release = TaskCompletionSource<unit>()
        let held = TaskCompletionSource<unit>()
        let before = NodeLockPool.entryCount ()
        let holder =
            Task.Run(fun () ->
                match
                    NodeLockPool.withLock graph workspaceId (fun () ->
                        held.SetResult()
                        release.Task.Wait()
                        ())
                with
                | Ok () -> ()
                | Error rejection -> Assert.Fail($"holder: {rejection}"))
        held.Task.Wait()
        let loadCommit = TaskCompletionSource<unit>()
        let git =
            { remoteExists = fun _ -> Ok true
              fetchTracked = fun _ _ -> Ok "fetched"
              pullTracked = fun _ _ -> Ok "pulled"
              commitAll =
                fun _ _ _ ->
                    loadCommit.SetResult()
                    Ok(trackedBranch, "committed")
              pushTracked = fun _ _ _ -> Ok "pushed" }
        let load =
            Task.Run(fun () ->
                GithubTransportActor.runLoad
                    { git = git }
                    dataDir
                    changes
                    workspaceId
                    root
                |> Async.RunSynchronously)
        (NodeLockPool.waitUntilQueued (before + 2)).Wait()
        Assert.False(loadCommit.Task.IsCompleted)
        release.SetResult()
        load.Wait()
        load.Result |> requireOk "load" |> ignore
        holder.Wait()
        let releaseSave = TaskCompletionSource<unit>()
        let heldSave = TaskCompletionSource<unit>()
        let beforeSave = NodeLockPool.entryCount ()
        let holderSave =
            Task.Run(fun () ->
                match
                    NodeLockPool.withLock graph workspaceId (fun () ->
                        heldSave.SetResult()
                        releaseSave.Task.Wait()
                        ())
                with
                | Ok () -> ()
                | Error rejection ->
                    Assert.Fail($"save holder: {rejection}"))
        heldSave.Task.Wait()
        let saveCommit = TaskCompletionSource<unit>()
        let saveGit =
            { git with
                commitAll =
                    fun _ message _ ->
                        Assert.Equal("gambol: git Save", message)
                        saveCommit.SetResult()
                        Ok(trackedBranch, "committed") }
        let save =
            Task.Run(fun () ->
                GithubTransportActor.runSave
                    { git = saveGit }
                    dataDir
                    changes
                    focusId
                |> Async.RunSynchronously)
        (NodeLockPool.waitUntilQueued (beforeSave + 2)).Wait()
        Assert.False(saveCommit.Task.IsCompleted)
        releaseSave.SetResult()
        save.Wait()
        save.Result |> requireOk "save" |> ignore
        holderSave.Wait()
    finally
        CoreMailboxApi.dispose host

let private holdsWorkspace graph workspaceId =
    match NodeLockPool.withLock graph workspaceId (fun () -> ()) with
    | Error NodeLockRejection.HolderHoldsLock -> true
    | Ok () -> false

[<Fact>]
let ``fetch completes before the workspace lock then the pull merges and marks`` () =
    let steps = ResizeArray<string>()
    let dataDir = newTempDir ()
    let host, changes = createAdmittedFile dataDir
    try
        let workspaceId, docsId = seedDocs host
        let graph = readGraph host
        let root = Path.Combine(dataDir, "home")
        WorkspaceGit.ensureInit root |> requireOk "init" |> ignore
        File.WriteAllText(Path.Combine(root, "seed.txt"), "seed")
        WorkspaceGit.commitAll root "seed" None
        |> requireOk "seed"
        |> ignore
        let marked = ResizeArray<string>()
        let markChanges = {
            postChanges = fun _ -> async.Return (Ok ())
            graphAtEvent =
                fun () ->
                    async.Return (
                        Ok { graph = graph; eventId = EventId.zero })
            loadNode = fun _ -> async.Return (Ok EventId.zero)
            postNotice =
                fun _ ->
                    marked.Add("mark")
                    Assert.True(NodeLockPool.nodeIsHeld workspaceId)
            eventsSince = fun _ -> async.Return []
            postGraphOnly = fun _ -> async.Return (Ok ())
        }
        let mergeAndMark =
            GithubTransportActor.lockMergeAndMark
                { remoteExists = fun _ -> Ok true
                  fetchTracked = fun _ _ -> Ok "fetched"
                  pullTracked =
                    fun workspace tracked ->
                        steps.Add("merge")
                        Assert.True(holdsWorkspace graph workspaceId)
                        pullAddsExtra root workspace tracked
                  commitAll =
                    fun _ _ _ ->
                        Ok(trackedBranch, GitSave.nothingToCommitMessage)
                  pushTracked = fun _ _ _ -> Error "unused" }
                { graph = graph
                  workspaceId = workspaceId
                  root = root }
                { changes = markChanges
                  dataDir = dataDir
                  workspaceId = workspaceId }
                trackedBranch
        mergeAndMark
        |> Async.RunSynchronously
        |> requireOk "locked pull"
        |> ignore
        match
            NodeLockPool.withLock graph workspaceId (fun () -> "free")
        with
        | Ok "free" -> ()
        | other -> Assert.Fail($"released: {other}")
        Assert.Contains("merge", steps)
        Assert.Contains("mark", marked)
        steps.Clear()
        let fetchEntered =
            TaskCompletionSource<unit>(
                TaskCreationOptions.RunContinuationsAsynchronously)
        let fetchRelease = TaskCompletionSource<unit>()
        let mergeEntered =
            TaskCompletionSource<unit>(
                TaskCreationOptions.RunContinuationsAsynchronously)
        let fetch _ _ =
            steps.Add("fetch")
            fetchEntered.TrySetResult() |> ignore
            fetchRelease.Task.Wait()
            Ok "fetched"
        let merge _ _ =
            steps.Add("merge")
            Assert.True(holdsWorkspace graph workspaceId)
            mergeEntered.TrySetResult() |> ignore
            Ok "merged"
        let deps = {
            git = {
                remoteExists = fun _ -> Ok true
                fetchTracked = fetch
                pullTracked = merge
                commitAll =
                    fun _ _ _ ->
                        steps.Add("commit")
                        Ok(trackedBranch, GitSave.nothingToCommitMessage)
                pushTracked = fun _ _ _ -> Error "push unused"
            }
        }
        let load =
            Task.Run(fun () ->
                GithubTransportActor.runLoad
                    deps
                    dataDir
                    changes
                    workspaceId
                    root
                |> Async.RunSynchronously)
        fetchEntered.Task.Wait()
        Assert.False(mergeEntered.Task.IsCompleted)
        let before = NodeLockPool.entryCount ()
        let probeHeld =
            TaskCompletionSource<unit>(
                TaskCreationOptions.RunContinuationsAsynchronously)
        let probeRelease = TaskCompletionSource<unit>()
        let probe =
            Task.Run(fun () ->
                match
                    NodeLockPool.withLock graph docsId (fun () ->
                        probeHeld.TrySetResult() |> ignore
                        probeRelease.Task.Wait()
                        ())
                with
                | Ok () -> ()
                | Error rejection -> Assert.Fail($"probe: {rejection}"))
        probeHeld.Task.Wait()
        fetchRelease.TrySetResult() |> ignore
        (NodeLockPool.waitUntilQueued (before + 2)).Wait()
        Assert.False(mergeEntered.Task.IsCompleted)
        probeRelease.TrySetResult() |> ignore
        load.Wait()
        load.Result |> requireOk "load" |> ignore
        probe.Wait()
        Assert.Equal<string list>(
            [ "commit"; "fetch"; "merge" ],
            steps |> Seq.toList)
        let events =
            CoreMailboxApi.getEventsSince host EventId.zero
            |> Async.RunSynchronously
        Assert.Contains(
            events,
            fun event ->
                match event.body with
                | EventBody.Load id -> id = workspaceId
                | _ -> false)
    finally
        CoreMailboxApi.dispose host

let private noticeCount host notice =
    CoreMailboxApi.getEventsSince host EventId.zero
    |> Async.RunSynchronously
    |> List.filter (fun event -> event.body = EventBody.Notice notice)
    |> List.length

let private uploadHandoff host =
    { readState = fun () -> CoreMailboxApi.graphAtEvent host
      postNotice = CoreMailboxApi.postNotice host
      loadNode = fun _ -> async { return Ok () } }

let private landUpload host relative =
    WorkspaceUploadLock.landMarkRelease
        (uploadHandoff host)
        "home"
        relative
        (fun () -> Ok ())
    |> requireOk "upload"
    |> ignore

let private seedPullingFile (host: MailboxHost) parentId name =
    let graph = readGraph host
    let fileId, ops =
        FileNodeOps.planCreateOwnedFile graph parentId name
    postChange host ops
    CoreMailboxApi.postNotice host (Notice.NodeUploading fileId)
    fileId

let private withPullingUpload body =
    let dataDir = newTempDir ()
    let host, _changes = createAdmittedFile dataDir
    try
        let workspaceId, _ = seedDocs host
        let fileId = seedPullingFile host workspaceId "note.txt"
        body host fileId
    finally
        CoreMailboxApi.dispose host

let private fileDocument host fileId =
    (readGraph host).nodes.[fileId].documentState

[<Fact>]
let ``one upload posts NodeReceived once`` () =
    withPullingUpload (fun host fileId ->
        landUpload host "note.txt"
        Assert.Equal(1, noticeCount host (Notice.NodeReceived fileId))
        Assert.Equal(Unparsed, (fileDocument host fileId).AsEnum)
        Assert.False((fileDocument host fileId).pullingFile))

[<Fact>]
let ``a second upload posts no NodeReceived and marks Unparsed`` () =
    withPullingUpload (fun host fileId ->
        landUpload host "note.txt"
        landUpload host "note.txt"
        Assert.Equal(1, noticeCount host (Notice.NodeReceived fileId))
        Assert.Equal(1, noticeCount host (Notice.NodeUnparsed fileId))
        Assert.Equal(Unparsed, (fileDocument host fileId).AsEnum))

[<Fact>]
let ``upload lands and marks Unparsed inside the workspace lock`` () =
    let steps = ResizeArray<string>()
    let dataDir = newTempDir ()
    let host, _changes = createAdmittedFile dataDir
    try
        let workspaceId, _ = seedDocs host
        let _fileId = seedCurrentFile host workspaceId "note.txt"
        let graph = readGraph host
        let handoff = {
            readState =
                fun () ->
                    async.Return (
                        Ok { graph = graph; eventId = EventId.zero })
            postNotice =
                fun notice ->
                    steps.Add("mark")
                    match notice with
                    | Notice.NodeUnparsed _ -> ()
                    | other -> Assert.Fail($"notice: {other}")
                    Assert.True(holdsWorkspace graph workspaceId)
            loadNode =
                fun _ ->
                    async {
                        steps.Add("parse")
                        match
                            NodeLockPool.withLock
                                graph
                                workspaceId
                                (fun () -> ())
                        with
                        | Ok () -> return Ok ()
                        | Error rejection ->
                            return Error $"parse saw {rejection}"
                    }
        }
        WorkspaceUploadLock.landMarkRelease
            handoff
            "home"
            "note.txt"
            (fun () ->
                steps.Add("land")
                Assert.True(holdsWorkspace graph workspaceId)
                Ok ())
        |> requireOk "upload"
        |> ignore
        Assert.Equal<string list>(
            [ "land"; "mark"; "parse" ],
            steps |> Seq.toList)
    finally
        CoreMailboxApi.dispose host

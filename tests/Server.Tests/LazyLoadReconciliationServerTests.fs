module Gambol.Server.Tests.LazyLoadReconciliationServerTests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err -> failwith $"{label}: {err}"

let private gitOnPath () = DesktopGit.isAvailable()

let private writeAndCommit
    (root: string)
    (relativePath: string)
    (content: string)
    (message: string)
    =
    let path = Path.Combine(root, relativePath)
    Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
    File.WriteAllText(path, content)
    WorkspaceGit.commitAll root message None |> requireOk "commit" |> ignore

let private newRepo () =
    let root = Path.Combine(newTempDir (), "home")
    WorkspaceGit.ensureInit root |> requireOk "init"
    root

[<SkippableFact>]
let ``addedPathsBetween handles initial push and later additions`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let root = newRepo ()
    writeAndCommit root "src/main.fs" "one" "initial"
    writeAndCommit root "README.md" "readme" "initial-readme"
    let firstHead = WorkspaceGit.tryHead root |> requireOk "first head" |> Option.get
    let initial = WorkspaceGit.addedPathsBetween root None firstHead |> requireOk "initial diff"
    Assert.Equal<string list>([ "README.md"; "src/main.fs" ], initial |> List.sort)
    File.WriteAllText(Path.Combine(root, "README.md"), "changed")
    writeAndCommit root "src/lib.fs" "two" "second"
    let secondHead = WorkspaceGit.tryHead root |> requireOk "second head" |> Option.get
    let later =
        WorkspaceGit.addedPathsBetween root (Some firstHead) secondHead
        |> requireOk "later diff"
    Assert.Equal<string list>([ "src/lib.fs" ], later)

[<SkippableFact>]
let ``successful receive triggers reconciliation with added paths`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let root = newRepo ()
    writeAndCommit root "existing.txt" "one" "seed"
    let oldHead = WorkspaceGit.tryHead root
    writeAndCommit root "new.txt" "two" "push"
    let observed =
        TaskCompletionSource<string * LazyLoadReconciliation.ChangedPath list>()
    let reconcile label paths = async {
        observed.SetResult(label, paths)
        return Ok []
    }
    let response = [| 1uy; 2uy; 3uy |]
    let result =
        GitGateway.completeWorkspacePush root "home" oldHead (Ok response) reconcile
        |> Async.RunSynchronously
    Assert.Equal<byte[]>(response, requireOk "receive" result)
    Assert.Equal(
        ("home", [ LazyLoadReconciliation.Added "new.txt" ]),
        observed.Task.Result)

[<SkippableFact>]
let ``successful no-op receive still triggers reconciliation`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let root = newRepo ()
    writeAndCommit root "existing.txt" "one" "seed"
    let head = WorkspaceGit.tryHead root
    let observed =
        TaskCompletionSource<string * LazyLoadReconciliation.ChangedPath list>()
    let reconcile label paths = async {
        observed.SetResult(label, paths)
        return Ok []
    }
    let response = [| 1uy; 2uy; 3uy |]
    let result =
        GitGateway.completeWorkspacePush root "home" head (Ok response) reconcile
        |> Async.RunSynchronously
    Assert.Equal<byte[]>(response, requireOk "receive" result)
    Assert.Equal(("home", []), observed.Task.Result)

[<Fact>]
let ``failed receive does not reconcile`` () =
    let called = TaskCompletionSource<bool>()
    let reconcile _ _ = async {
        called.SetResult(true)
        return Ok []
    }
    let result =
        GitGateway.completeWorkspacePush "" "home" (Ok None) (Error "receive failed") reconcile
        |> Async.RunSynchronously
    Assert.Equal(Error "receive failed", result)
    Assert.False(called.Task.IsCompleted)

[<SkippableFact>]
let ``reconciliation failure preserves successful receive response`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let root = newRepo ()
    writeAndCommit root "existing.txt" "one" "seed"
    let oldHead = WorkspaceGit.tryHead root
    writeAndCommit root "new.txt" "two" "push"
    let reconcile _ _ = async { return Error "planner conflict" }
    let response = [| 9uy; 8uy |]
    let result =
        GitGateway.completeWorkspacePush root "home" oldHead (Ok response) reconcile
        |> Async.RunSynchronously
    Assert.Equal<byte[]>(response, requireOk "receive" result)

[<Fact>]
let ``server reconciler applies planner ops through active agent`` () =
    let tempDir = newTempDir ()
    let fileAgent = FileAgent.create tempDir "gambol"
    let handle = AgentHandle.ofFile fileAgent
    let workspaceId, ops = FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let change = { id = 0; changeId = Guid.NewGuid(); ops = ops }
    let body =
        Thoth.Json.Newtonsoft.Encode.toString 0
            (Serialization.encodeChangeBatch { changes = [ change ] })
    FileAgent.postChange fileAgent body |> Async.RunSynchronously |> requireOk "workspace" |> ignore
    FileAgent.flushSnapshot fileAgent |> Async.RunSynchronously |> requireOk "workspace persist"
    let sourcePath = Path.Combine(tempDir, "home", "src", "main.fs")
    Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)) |> ignore
    File.WriteAllText(sourcePath, "module Main")
    LazyLoadReconciliationServer.reconcileAddedPaths handle tempDir "home" [ "src/main.fs" ]
    |> Async.RunSynchronously
    |> requireOk "reconcile"
    |> ignore
    FileAgent.flushSnapshot fileAgent |> Async.RunSynchronously |> requireOk "reconcile persist"
    let stateJson = FileAgent.getState fileAgent |> Async.RunSynchronously
    let graph = LazyLoadReconciliationServer.decodeGraphState stateJson |> requireOk "state" |> snd
    let srcId = graph.nodes.[workspaceId].children |> List.exactlyOne |> fun child -> child.id
    let fileId = graph.nodes.[srcId].children |> List.exactlyOne |> fun child -> child.id
    Assert.Equal(Special SpecialKind.Directory, graph.nodes.[srcId].kind)
    Assert.Equal(Special SpecialKind.File, graph.nodes.[fileId].kind)
    Assert.Empty(graph.nodes.[fileId].children)
    Assert.Equal("module Main", File.ReadAllText(sourcePath))
    FileAgent.dispose fileAgent

[<Fact>]
let ``server reconciler adds disk files outside the changed path list`` () =
    let tempDir = newTempDir ()
    let fileAgent = FileAgent.create tempDir "gambol"
    let handle = AgentHandle.ofFile fileAgent
    let workspaceId, ops = FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let change = { id = 0; changeId = Guid.NewGuid(); ops = ops }
    let body =
        Thoth.Json.Newtonsoft.Encode.toString 0
            (Serialization.encodeChangeBatch { changes = [ change ] })
    FileAgent.postChange fileAgent body |> Async.RunSynchronously |> requireOk "workspace" |> ignore
    let existingPath = Path.Combine(tempDir, "home", "existing.txt")
    let updatedPath = Path.Combine(tempDir, "home", "updated.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(existingPath)) |> ignore
    File.WriteAllText(existingPath, "already on disk")
    File.WriteAllText(updatedPath, "in the delta")
    LazyLoadReconciliationServer.reconcileChangedPaths
        handle
        tempDir
        "home"
        [ LazyLoadReconciliation.Added "updated.txt" ]
    |> Async.RunSynchronously
    |> requireOk "reconcile"
    |> ignore
    let stateJson = FileAgent.getState fileAgent |> Async.RunSynchronously
    let graph = LazyLoadReconciliationServer.decodeGraphState stateJson |> requireOk "state" |> snd
    let childNames =
        graph.nodes.[workspaceId].children
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "existing.txt"; "updated.txt" ], childNames)
    FileAgent.dispose fileAgent

[<Fact>]
let ``server reconciler adds missing directory and file nodes from discovered paths`` () =
    let tempDir = newTempDir ()
    let fileAgent = FileAgent.create tempDir "gambol"
    let handle = AgentHandle.ofFile fileAgent
    let workspaceId, ops = FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let change = { id = 0; changeId = Guid.NewGuid(); ops = ops }
    let body =
        Thoth.Json.Newtonsoft.Encode.toString 0
            (Serialization.encodeChangeBatch { changes = [ change ] })
    FileAgent.postChange fileAgent body |> Async.RunSynchronously |> requireOk "workspace" |> ignore
    let nestedPath = Path.Combine(tempDir, "home", "docs", "notes.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(nestedPath)) |> ignore
    File.WriteAllText(nestedPath, "hello")
    LazyLoadReconciliationServer.reconcileChangedPaths
        handle
        tempDir
        "home"
        []
    |> Async.RunSynchronously
    |> requireOk "reconcile"
    |> ignore
    let stateJson = FileAgent.getState fileAgent |> Async.RunSynchronously
    let graph =
        LazyLoadReconciliationServer.decodeGraphState stateJson
        |> requireOk "state"
        |> snd
    let docsId =
        graph.nodes.[workspaceId].children
        |> List.find (fun child ->
            Filename.tryValue graph.nodes.[child.id].name = Some "docs")
        |> fun child -> child.id
    let notesId =
        graph.nodes.[docsId].children
        |> List.find (fun child ->
            Filename.tryValue graph.nodes.[child.id].name = Some "notes.txt")
        |> fun child -> child.id
    Assert.Equal(Special SpecialKind.Directory, graph.nodes.[docsId].kind)
    Assert.Equal(Special SpecialKind.File, graph.nodes.[notesId].kind)
    FileAgent.dispose fileAgent

[<Fact>]
let ``post receive rename of unparsed stub is rejected without moving disk twice`` () =
    let tempDir = newTempDir ()
    let fileAgent = FileAgent.create tempDir "gambol"
    let handle = AgentHandle.ofFile fileAgent
    let workspaceId, ops =
        FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let change = { id = 0; changeId = Guid.NewGuid(); ops = ops }
    let body =
        Thoth.Json.Newtonsoft.Encode.toString 0
            (Serialization.encodeChangeBatch { changes = [ change ] })
    FileAgent.postChange fileAgent body
    |> Async.RunSynchronously
    |> requireOk "workspace"
    |> ignore
    let oldPath = Path.Combine(tempDir, "home", "old.txt")
    let newPath = Path.Combine(tempDir, "home", "new.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(oldPath)) |> ignore
    File.WriteAllText(oldPath, "received content")
    LazyLoadReconciliationServer.reconcileChangedPaths
        handle
        tempDir
        "home"
        [ LazyLoadReconciliation.Added "old.txt" ]
    |> Async.RunSynchronously
    |> requireOk "add stub"
    |> ignore
    File.Move(oldPath, newPath)
    let renameResult =
        LazyLoadReconciliationServer.reconcileChangedPaths
            handle
            tempDir
            "home"
            [ LazyLoadReconciliation.Renamed("old.txt", "new.txt") ]
        |> Async.RunSynchronously
    match renameResult with
    | Error err -> Assert.Fail($"expected soft failure, got Error {err}")
    | Ok failures ->
        Assert.NotEmpty(failures)
        Assert.Contains(
            failures,
            fun f -> f.message.Contains("unparsed document"))
    FileAgent.flushSnapshot fileAgent
    |> Async.RunSynchronously
    |> requireOk "flush"
    let stateJson = FileAgent.getState fileAgent |> Async.RunSynchronously
    let graph =
        LazyLoadReconciliationServer.decodeGraphState stateJson
        |> requireOk "state"
        |> snd
    let fileId =
        graph.nodes.[workspaceId].children
        |> List.find (fun child ->
            Filename.tryValue graph.nodes.[child.id].name = Some "old.txt")
        |> fun child -> child.id
    Assert.Equal(
        Special SpecialKind.File,
        graph.nodes.[fileId].kind)
    Assert.False(File.Exists(oldPath))
    Assert.Equal("received content", File.ReadAllText(newPath))
    FileAgent.dispose fileAgent

[<Fact>]
let ``server reconciler posts good sibling when one path fails`` () =
    let tempDir = newTempDir ()
    let fileAgent = FileAgent.create tempDir "gambol"
    let handle = AgentHandle.ofFile fileAgent
    let workspaceId, ops =
        FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let change = { id = 0; changeId = Guid.NewGuid(); ops = ops }
    let body =
        Thoth.Json.Newtonsoft.Encode.toString 0
            (Serialization.encodeChangeBatch { changes = [ change ] })
    FileAgent.postChange fileAgent body
    |> Async.RunSynchronously
    |> requireOk "workspace"
    |> ignore
    let badPath = Path.Combine(tempDir, "home", "bad.txt")
    let goodPath = Path.Combine(tempDir, "home", "good.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(badPath)) |> ignore
    File.WriteAllText(badPath, "unparsed stub")
    File.WriteAllText(goodPath, "sibling")
    LazyLoadReconciliationServer.reconcileAddedPaths
        handle
        tempDir
        "home"
        [ "bad.txt" ]
    |> Async.RunSynchronously
    |> requireOk "seed bad"
    |> ignore
    let result =
        LazyLoadReconciliationServer.reconcileChangedPaths
            handle
            tempDir
            "home"
            [ LazyLoadReconciliation.Deleted "bad.txt"
              LazyLoadReconciliation.Added "good.txt" ]
        |> Async.RunSynchronously
        |> requireOk "reconcile"
    Assert.NotEmpty(result)
    Assert.Contains(
        result,
        fun f ->
            f.path = "bad.txt"
            && f.message.Contains("unparsed document"))
    let stateJson = FileAgent.getState fileAgent |> Async.RunSynchronously
    let graph =
        LazyLoadReconciliationServer.decodeGraphState stateJson
        |> requireOk "state"
        |> snd
    let names =
        graph.nodes.[workspaceId].children
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "bad.txt"; "good.txt" ], names)
    FileAgent.dispose fileAgent

[<Fact>]
let ``latest diagnostics GET returns failures once then empty`` () =
    LazyLoadReconciliationDiagnostics.set
        "home"
        [ { path = "bad.txt"; message = "boom" } ]
    let tempDir = newTempDir ()
    use client = createClientForDir tempDir
    let first =
        client.GetAsync("/ambit/git/reconciliation/latest?workspace=home")
        |> Async.AwaitTask
        |> Async.RunSynchronously
    Assert.Equal(System.Net.HttpStatusCode.OK, first.StatusCode)
    let firstBody =
        first.Content.ReadAsStringAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously
    Assert.Contains("bad.txt", firstBody)
    Assert.Contains("boom", firstBody)
    let second =
        client.GetAsync("/ambit/git/reconciliation/latest?workspace=home")
        |> Async.AwaitTask
        |> Async.RunSynchronously
    Assert.Equal(System.Net.HttpStatusCode.OK, second.StatusCode)
    let secondBody =
        second.Content.ReadAsStringAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously
    Assert.Equal("""{"failures":[]}""", secondBody)

let private postWorkspace (fileAgent: FileAgent) (label: string) =
    let workspaceId, ops = FileNodeOps.planCreateWorkspace (Graph.create ()) label
    let change = { id = 0; changeId = Guid.NewGuid(); ops = ops }
    let body =
        Thoth.Json.Newtonsoft.Encode.toString 0
            (Serialization.encodeChangeBatch { changes = [ change ] })
    FileAgent.postChange fileAgent body
    |> Async.RunSynchronously
    |> requireOk "workspace"
    |> ignore
    workspaceId

let private postOps (fileAgent: FileAgent) (revision: int) (ops: Op list) =
    let change = { id = revision; changeId = Guid.NewGuid(); ops = ops }
    let body =
        Thoth.Json.Newtonsoft.Encode.toString 0
            (Serialization.encodeChangeBatch { changes = [ change ] })
    FileAgent.postChange fileAgent body
    |> Async.RunSynchronously
    |> requireOk "ops"
    |> ignore

let private readGraph (fileAgent: FileAgent) =
    let stateJson = FileAgent.getState fileAgent |> Async.RunSynchronously
    LazyLoadReconciliationServer.decodeGraphState stateJson
    |> requireOk "state"
    |> snd

[<Fact>]
let ``directory reconcile discovers only under directory prefix`` () =
    let tempDir = newTempDir ()
    let fileAgent = FileAgent.create tempDir "gambol"
    let handle = AgentHandle.ofFile fileAgent
    let workspaceId = postWorkspace fileAgent "home"
    let graph1 = readGraph fileAgent
    let docsId, docsOps =
        FileNodeOps.planCreateOwnedDirectory graph1 workspaceId "docs"
    postOps fileAgent 1 docsOps
    let outsidePath = Path.Combine(tempDir, "home", "outside.txt")
    let insidePath = Path.Combine(tempDir, "home", "docs", "inside.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(insidePath)) |> ignore
    File.WriteAllText(outsidePath, "outside")
    File.WriteAllText(insidePath, "inside")
    LazyLoadReconciliationServer.reconcileDirectory handle tempDir "home" "docs"
    |> Async.RunSynchronously
    |> requireOk "directory reconcile"
    |> ignore
    let graph = readGraph fileAgent
    let docsChildren =
        graph.nodes.[docsId].children
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "inside.txt" ], docsChildren)
    let workspaceNames =
        graph.nodes.[workspaceId].children
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "docs" ], workspaceNames)
    FileAgent.dispose fileAgent

[<Fact>]
let ``workspace reconcile discovers under workspace root`` () =
    let tempDir = newTempDir ()
    let fileAgent = FileAgent.create tempDir "gambol"
    let handle = AgentHandle.ofFile fileAgent
    let workspaceId = postWorkspace fileAgent "home"
    let outsidePath = Path.Combine(tempDir, "home", "outside.txt")
    let insidePath = Path.Combine(tempDir, "home", "docs", "inside.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(insidePath)) |> ignore
    File.WriteAllText(outsidePath, "outside")
    File.WriteAllText(insidePath, "inside")
    LazyLoadReconciliationServer.reconcileWorkspace handle tempDir "home"
    |> Async.RunSynchronously
    |> requireOk "workspace reconcile"
    |> ignore
    let graph = readGraph fileAgent
    let workspaceNames =
        graph.nodes.[workspaceId].children
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "docs"; "outside.txt" ], workspaceNames)
    let docsId =
        graph.nodes.[workspaceId].children
        |> List.pick (fun child ->
            match Filename.tryValue graph.nodes.[child.id].name with
            | Some "docs" -> Some child.id
            | _ -> None)
    let docsChildren =
        graph.nodes.[docsId].children
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "inside.txt" ], docsChildren)
    FileAgent.dispose fileAgent

[<Fact>]
let ``directory reconcile does not duplicate Normal-owned present file`` () =
    let tempDir = newTempDir ()
    let fileAgent = FileAgent.create tempDir "gambol"
    let handle = AgentHandle.ofFile fileAgent
    let workspaceId = postWorkspace fileAgent "home"
    let graph1 = readGraph fileAgent
    let docsId, docsOps =
        FileNodeOps.planCreateOwnedDirectory graph1 workspaceId "docs"
    postOps fileAgent 1 docsOps
    let organizerId = NodeId.New()
    postOps
        fileAgent
        2
        [ Op.NewNode(organizerId, "organizer")
          Op.Replace(
              docsId,
              0,
              [],
              [ { ref = Ownership.Owner; id = organizerId } ]) ]
    let graph4 = readGraph fileAgent
    let fileId, fileOps =
        FileNodeOps.planCreateOwnedFile graph4 organizerId "present.txt"
    postOps fileAgent 3 fileOps
    let presentPath = Path.Combine(tempDir, "home", "docs", "present.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(presentPath)) |> ignore
    File.WriteAllText(presentPath, "already owned")
    LazyLoadReconciliationServer.reconcileDirectory handle tempDir "home" "docs"
    |> Async.RunSynchronously
    |> requireOk "directory reconcile"
    |> ignore
    let graph = readGraph fileAgent
    let matches =
        GraphQuery.ownedArtifactsInDirectory graph docsId None None
        |> List.choose (fun nodeId ->
            Filename.tryValue graph.nodes.[nodeId].name
            |> Option.filter ((=) "present.txt")
            |> Option.map (fun _ -> nodeId))
    Assert.Equal(1, matches.Length)
    Assert.Equal(fileId, matches.Head)
    FileAgent.dispose fileAgent

[<Fact>]
let ``directory reconcile creates missing sibling under directory`` () =
    let tempDir = newTempDir ()
    let fileAgent = FileAgent.create tempDir "gambol"
    let handle = AgentHandle.ofFile fileAgent
    let workspaceId = postWorkspace fileAgent "home"
    let graph1 = readGraph fileAgent
    let docsId, docsOps =
        FileNodeOps.planCreateOwnedDirectory graph1 workspaceId "docs"
    postOps fileAgent 1 docsOps
    let missingPath = Path.Combine(tempDir, "home", "docs", "missing.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(missingPath)) |> ignore
    File.WriteAllText(missingPath, "new")
    LazyLoadReconciliationServer.reconcileDirectory handle tempDir "home" "docs"
    |> Async.RunSynchronously
    |> requireOk "directory reconcile"
    |> ignore
    let graph = readGraph fileAgent
    let names =
        graph.nodes.[docsId].children
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "missing.txt" ], names)
    FileAgent.dispose fileAgent

[<Fact>]
let ``directory reconcile with amb outline and missing file posts without ownership error`` () =
    let tempDir = newTempDir ()
    let fileAgent = FileAgent.create tempDir "gambol"
    let handle = AgentHandle.ofFile fileAgent
    let workspaceId = postWorkspace fileAgent "home"
    let graph1 = readGraph fileAgent
    let tasksId, tasksOps =
        FileNodeOps.planCreateOwnedDirectory graph1 workspaceId "tasks"
    postOps fileAgent 1 tasksOps
    let graph2 = readGraph fileAgent
    let activeId, activeOps =
        FileNodeOps.planCreateOwnedDirectory graph2 tasksId "active"
    postOps fileAgent 2 activeOps
    let tasksDir = Path.Combine(tempDir, "home", "tasks")
    Directory.CreateDirectory(Path.Combine(tasksDir, "active")) |> ignore
    let sid = AmbDocument.formatStableId activeId
    File.WriteAllText(
        Path.Combine(tasksDir, ".amb"),
        $"-> //home/tasks/active/^{sid}\n")
    File.WriteAllText(Path.Combine(tasksDir, "inbox.txt"), "new")
    let result =
        LazyLoadReconciliationServer.reconcileDirectory handle tempDir "home" "tasks"
        |> Async.RunSynchronously
    match result with
    | Error err ->
        Assert.False(
            err.Contains("missing owner occurrence"),
            $"ownership error on directory reconcile: {err}")
        Assert.Fail($"directory reconcile failed: {err}")
    | Ok _ ->
        let graph = readGraph fileAgent
        let names =
            graph.nodes.[tasksId].children
            |> List.choose (fun child ->
                Filename.tryValue graph.nodes.[child.id].name)
            |> List.sort
        Assert.Contains("inbox.txt", names)
        let occurrences =
            graph.nodes.[tasksId].children
            |> List.filter (fun c -> c.id = activeId)
        Assert.Equal(1, occurrences.Length)
        Assert.Equal(Ownership.Owner, occurrences.Head.ref)
    FileAgent.dispose fileAgent

[<Fact>]
let ``directory reconcile returns resilient failures and posts good sibling`` () =
    let tempDir = newTempDir ()
    let fileAgent = FileAgent.create tempDir "gambol"
    let handle = AgentHandle.ofFile fileAgent
    let workspaceId = postWorkspace fileAgent "home"
    let graph1 = readGraph fileAgent
    let docsId, docsOps =
        FileNodeOps.planCreateOwnedDirectory graph1 workspaceId "docs"
    postOps fileAgent 1 docsOps
    let badPath = Path.Combine(tempDir, "home", "docs", "bad.txt")
    let goodPath = Path.Combine(tempDir, "home", "docs", "good.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(badPath)) |> ignore
    File.WriteAllText(badPath, "unparsed stub")
    File.WriteAllText(goodPath, "sibling")
    LazyLoadReconciliationServer.reconcileAddedPaths
        handle
        tempDir
        "home"
        [ "docs/bad.txt" ]
    |> Async.RunSynchronously
    |> requireOk "seed bad"
    |> ignore
    let failures =
        LazyLoadReconciliationServer.reconcileChangedPathsWithDiscovery
            handle
            tempDir
            "home"
            (Some "docs")
            [ LazyLoadReconciliation.Deleted "docs/bad.txt" ]
        |> Async.RunSynchronously
        |> requireOk "directory reconcile"
    Assert.NotEmpty(failures)
    Assert.Contains(
        failures,
        fun f ->
            f.path = "docs/bad.txt"
            && f.message.Contains("unparsed document"))
    let graph = readGraph fileAgent
    let names =
        graph.nodes.[docsId].children
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "bad.txt"; "good.txt" ], names)
    FileAgent.dispose fileAgent

[<Fact>]
let ``directory reconciliation POST returns failures JSON`` () =
    let tempDir = newTempDir ()
    use client = createClientForDir tempDir
    let workspaceId, wsOps = FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let wsChange = { id = 0; changeId = Guid.NewGuid(); ops = wsOps }
    let wsBody =
        Thoth.Json.Newtonsoft.Encode.toString 0
            (Serialization.encodeChangeBatch { changes = [ wsChange ] })
    use wsContent = new StringContent(wsBody, Text.Encoding.UTF8, "application/json")
    let wsResp =
        client.PostAsync("/ambit/changes", wsContent)
        |> Async.AwaitTask
        |> Async.RunSynchronously
    Assert.Equal(HttpStatusCode.OK, wsResp.StatusCode)
    let stateResp =
        client.GetAsync("/ambit/state")
        |> Async.AwaitTask
        |> Async.RunSynchronously
    let stateJson =
        stateResp.Content.ReadAsStringAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously
    let graph =
        LazyLoadReconciliationServer.decodeGraphState stateJson
        |> requireOk "state"
        |> snd
    let _, docsOps =
        FileNodeOps.planCreateOwnedDirectory graph workspaceId "docs"
    let docsChange = { id = 1; changeId = Guid.NewGuid(); ops = docsOps }
    let docsBody =
        Thoth.Json.Newtonsoft.Encode.toString 0
            (Serialization.encodeChangeBatch { changes = [ docsChange ] })
    use docsContent =
        new StringContent(docsBody, Text.Encoding.UTF8, "application/json")
    let docsResp =
        client.PostAsync("/ambit/changes", docsContent)
        |> Async.AwaitTask
        |> Async.RunSynchronously
    Assert.Equal(HttpStatusCode.OK, docsResp.StatusCode)
    let docsPath = Path.Combine(tempDir, "home", "docs", "new.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(docsPath)) |> ignore
    File.WriteAllText(docsPath, "via http")
    let body = """{"workspace":"home","path":"docs"}"""
    use content = new StringContent(body, Text.Encoding.UTF8, "application/json")
    let response =
        client.PostAsync("/ambit/git/reconciliation/directory", content)
        |> Async.AwaitTask
        |> Async.RunSynchronously
    let responseBody =
        response.Content.ReadAsStringAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously
    Assert.True(
        response.StatusCode = HttpStatusCode.OK,
        $"expected OK, got {response.StatusCode}: {responseBody}")
    Assert.Equal("""{"failures":[]}""", responseBody)

[<Fact>]
let ``workspace reconciliation POST with empty path discovers root`` () =
    let tempDir = newTempDir ()
    use client = createClientForDir tempDir
    let workspaceId, wsOps = FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let wsChange = { id = 0; changeId = Guid.NewGuid(); ops = wsOps }
    let wsBody =
        Thoth.Json.Newtonsoft.Encode.toString 0
            (Serialization.encodeChangeBatch { changes = [ wsChange ] })
    use wsContent = new StringContent(wsBody, Text.Encoding.UTF8, "application/json")
    let wsResp =
        client.PostAsync("/ambit/changes", wsContent)
        |> Async.AwaitTask
        |> Async.RunSynchronously
    Assert.Equal(HttpStatusCode.OK, wsResp.StatusCode)
    let rootFile = Path.Combine(tempDir, "home", "root.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(rootFile)) |> ignore
    File.WriteAllText(rootFile, "via http workspace")
    let body = """{"workspace":"home","path":""}"""
    use content = new StringContent(body, Text.Encoding.UTF8, "application/json")
    let response =
        client.PostAsync("/ambit/git/reconciliation/directory", content)
        |> Async.AwaitTask
        |> Async.RunSynchronously
    let responseBody =
        response.Content.ReadAsStringAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously
    Assert.True(
        response.StatusCode = HttpStatusCode.OK,
        $"expected OK, got {response.StatusCode}: {responseBody}")
    Assert.Equal("""{"failures":[]}""", responseBody)
    let stateResp =
        client.GetAsync("/ambit/state")
        |> Async.AwaitTask
        |> Async.RunSynchronously
    let stateJson =
        stateResp.Content.ReadAsStringAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously
    let graph =
        LazyLoadReconciliationServer.decodeGraphState stateJson
        |> requireOk "state"
        |> snd
    let names =
        graph.nodes.[workspaceId].children
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "root.txt" ], names)

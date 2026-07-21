module Gambol.Server.Tests.LazyLoadReconciliationServerTests

open System
open System.Diagnostics
open System.IO
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err -> failwith $"{label}: {err}"

let private gitOnPath () =
    try
        let start =
            ProcessStartInfo(
                FileName = "git",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false)
        use proc = Process.Start(start)
        proc.WaitForExit()
        proc.ExitCode = 0
    with _ ->
        false

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
        return Ok ()
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
        return Ok ()
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
        return Ok ()
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
    File.Move(oldPath, newPath)
    let renameResult =
        LazyLoadReconciliationServer.reconcileChangedPaths
            handle
            tempDir
            "home"
            [ LazyLoadReconciliation.Renamed("old.txt", "new.txt") ]
        |> Async.RunSynchronously
    match renameResult with
    | Ok _ -> Assert.Fail("expected unparsed document rejection")
    | Error error -> Assert.Contains("unparsed document", error)
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

module Gambol.Server.Tests.LazyLoadReconciliationServerTests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
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
    let fileAgent, handle = createAdmittedFile tempDir
    let workspaceId, ops = FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body =
            EventBody.Change(
                ops @ [ Op.SetDocumentState(workspaceId, Unparsed, Current) ]) }
    handle.postEvents [ event ]
    |> Async.RunSynchronously
    |> requireOk "workspace"
    |> ignore
    Assert.True(File.Exists(Path.Combine(tempDir, "home", ".amb")))
    Assert.Equal("1", File.ReadAllText(Bookkeeping.metaPath tempDir))
    CoreMailbox.flushSnapshot fileAgent |> Async.RunSynchronously |> requireOk "workspace persist"
    let sourcePath = Path.Combine(tempDir, "home", "src", "main.fs")
    Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)) |> ignore
    File.WriteAllText(sourcePath, "module Main")
    LazyLoadReconciliationServer.reconcileAddedPaths
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
        [ "src/main.fs" ]
    |> Async.RunSynchronously
    |> requireOk "reconcile"
    |> ignore
    CoreMailbox.flushSnapshot fileAgent |> Async.RunSynchronously |> requireOk "reconcile persist"
    let state =
        CoreMailbox.getState fileAgent
        |> Async.RunSynchronously
        |> requireOk "state"
    let graph = state.graph
    let srcId = Graph.children graph workspaceId |> List.exactlyOne |> fun child -> child.id
    let fileId = Graph.children graph srcId |> List.exactlyOne |> fun child -> child.id
    Assert.Equal(Special SpecialKind.Directory, graph.nodes.[srcId].kind)
    Assert.Equal(Special SpecialKind.File, graph.nodes.[fileId].kind)
    Assert.Empty(Graph.children graph fileId)
    Assert.Equal("module Main", File.ReadAllText(sourcePath))
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``server reconciler adds disk files outside the changed path list`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
    let workspaceId, ops = FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change ops }
    handle.postEvents [ event ]
    |> Async.RunSynchronously
    |> requireOk "workspace"
    |> ignore
    let existingPath = Path.Combine(tempDir, "home", "existing.txt")
    let updatedPath = Path.Combine(tempDir, "home", "updated.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(existingPath)) |> ignore
    File.WriteAllText(existingPath, "already on disk")
    File.WriteAllText(updatedPath, "in the delta")
    LazyLoadReconciliationServer.reconcileChangedPaths
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
        [ LazyLoadReconciliation.Added "updated.txt" ]
    |> Async.RunSynchronously
    |> requireOk "reconcile"
    |> ignore
    let state =
        CoreMailbox.getState fileAgent
        |> Async.RunSynchronously
        |> requireOk "state"
    let graph = state.graph
    let childNames =
        Graph.children graph workspaceId
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "existing.txt"; "updated.txt" ], childNames)
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``server reconciler adds missing directory and file nodes from discovered paths`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
    let workspaceId, ops = FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change ops }
    handle.postEvents [ event ]
    |> Async.RunSynchronously
    |> requireOk "workspace"
    |> ignore
    let nestedPath = Path.Combine(tempDir, "home", "docs", "notes.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(nestedPath)) |> ignore
    File.WriteAllText(nestedPath, "hello")
    LazyLoadReconciliationServer.reconcileChangedPaths
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
        []
    |> Async.RunSynchronously
    |> requireOk "reconcile"
    |> ignore
    let state =
        CoreMailbox.getState fileAgent
        |> Async.RunSynchronously
        |> requireOk "state"
    let graph = state.graph
    let docsId =
        Graph.children graph workspaceId
        |> List.find (fun child ->
            Filename.tryValue graph.nodes.[child.id].name = Some "docs")
        |> fun child -> child.id
    let notesId =
        Graph.children graph docsId
        |> List.find (fun child ->
            Filename.tryValue graph.nodes.[child.id].name = Some "notes.txt")
        |> fun child -> child.id
    Assert.Equal(Special SpecialKind.Directory, graph.nodes.[docsId].kind)
    Assert.Equal(Special SpecialKind.File, graph.nodes.[notesId].kind)
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``post receive rename of unparsed stub is rejected without moving disk twice`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
    let workspaceId, ops =
        FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change ops }
    handle.postEvents [ event ]
    |> Async.RunSynchronously
    |> requireOk "workspace"
    |> ignore
    let oldPath = Path.Combine(tempDir, "home", "old.txt")
    let newPath = Path.Combine(tempDir, "home", "new.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(oldPath)) |> ignore
    File.WriteAllText(oldPath, "received content")
    LazyLoadReconciliationServer.reconcileChangedPaths
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
        [ LazyLoadReconciliation.Added "old.txt" ]
    |> Async.RunSynchronously
    |> requireOk "add stub"
    |> ignore
    File.Move(oldPath, newPath)
    let renameResult =
        LazyLoadReconciliationServer.reconcileChangedPaths
            { handle = handle
              dataDir = tempDir
              workspaceLabel = "home" }
            [ LazyLoadReconciliation.Renamed("old.txt", "new.txt") ]
        |> Async.RunSynchronously
    match renameResult with
    | Error err -> Assert.Fail($"expected soft failure, got Error {err}")
    | Ok failures ->
        Assert.NotEmpty(failures)
        Assert.Contains(
            failures,
            fun f -> f.message.Contains("unparsed document"))
    CoreMailbox.flushSnapshot fileAgent
    |> Async.RunSynchronously
    |> requireOk "flush"
    let state =
        CoreMailbox.getState fileAgent
        |> Async.RunSynchronously
        |> requireOk "state"
    let graph = state.graph
    let fileId =
        Graph.children graph workspaceId
        |> List.find (fun child ->
            Filename.tryValue graph.nodes.[child.id].name = Some "old.txt")
        |> fun child -> child.id
    Assert.Equal(
        Special SpecialKind.File,
        graph.nodes.[fileId].kind)
    Assert.False(File.Exists(oldPath))
    Assert.Equal("received content", File.ReadAllText(newPath))
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``server reconciler posts good sibling when one path fails`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
    let workspaceId, ops =
        FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change ops }
    handle.postEvents [ event ]
    |> Async.RunSynchronously
    |> requireOk "workspace"
    |> ignore
    let badPath = Path.Combine(tempDir, "home", "bad.txt")
    let goodPath = Path.Combine(tempDir, "home", "good.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(badPath)) |> ignore
    File.WriteAllText(badPath, "unparsed stub")
    File.WriteAllText(goodPath, "sibling")
    LazyLoadReconciliationServer.reconcileAddedPaths
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
        [ "bad.txt" ]
    |> Async.RunSynchronously
    |> requireOk "seed bad"
    |> ignore
    let result =
        LazyLoadReconciliationServer.reconcileChangedPaths
            { handle = handle
              dataDir = tempDir
              workspaceLabel = "home" }
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
    let state =
        CoreMailbox.getState fileAgent
        |> Async.RunSynchronously
        |> requireOk "state"
    let graph = state.graph
    let names =
        Graph.children graph workspaceId
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "bad.txt"; "good.txt" ], names)
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``latest diagnostics GET returns failures once then empty`` () =
    LazyLoadReconciliationDiagnostics.set
        "home"
        [ { path = "bad.txt"; message = "boom" } ]
    let tempDir = newTempDir ()
    use client = createClientForDir tempDir
    let first =
        client.GetAsync("/ambit/workspace/reconciliation/latest?workspace=home")
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
        client.GetAsync("/ambit/workspace/reconciliation/latest?workspace=home")
        |> Async.AwaitTask
        |> Async.RunSynchronously
    Assert.Equal(System.Net.HttpStatusCode.OK, second.StatusCode)
    let secondBody =
        second.Content.ReadAsStringAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously
    Assert.Equal("""{"failures":[]}""", secondBody)

let private postWorkspace (fileAgent: MailboxHost) (label: string) =
    let workspaceId, ops = FileNodeOps.planCreateWorkspace (Graph.create ()) label
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change ops }
    (admittedChanges fileAgent).postEvents [ event ]
    |> Async.RunSynchronously
    |> requireOk "workspace"
    |> ignore
    workspaceId

let private postOps (fileAgent: MailboxHost) (_revision: int) (ops: Op list) =
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change ops }
    (admittedChanges fileAgent).postEvents [ event ]
    |> Async.RunSynchronously
    |> requireOk "ops"
    |> ignore

let private readGraph (fileAgent: MailboxHost) =
    CoreMailbox.getState fileAgent
    |> Async.RunSynchronously
    |> requireOk "state"
    |> fun state -> state.graph

[<Fact>]
let ``directory reconcile discovers only under directory prefix`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
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
    LazyLoadReconciliationServer.reconcileDirectory
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
        "docs"
    |> Async.RunSynchronously
    |> requireOk "directory reconcile"
    |> ignore
    let graph = readGraph fileAgent
    let docsChildren =
        Graph.children graph docsId
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "inside.txt" ], docsChildren)
    let workspaceNames =
        Graph.children graph workspaceId
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "docs" ], workspaceNames)
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``workspace reconcile discovers under workspace root`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
    let workspaceId = postWorkspace fileAgent "home"
    let outsidePath = Path.Combine(tempDir, "home", "outside.txt")
    let insidePath = Path.Combine(tempDir, "home", "docs", "inside.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(insidePath)) |> ignore
    File.WriteAllText(outsidePath, "outside")
    File.WriteAllText(insidePath, "inside")
    let inner = Path.Combine(tempDir, "home", "docs", "inner")
    Directory.CreateDirectory(inner) |> ignore
    File.WriteAllText(Path.Combine(inner, "deep.txt"), "deep")
    LazyLoadReconciliationServer.reconcileWorkspace
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
    |> Async.RunSynchronously
    |> requireOk "workspace reconcile"
    |> ignore
    let graph = readGraph fileAgent
    let workspaceNames =
        Graph.children graph workspaceId
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "docs"; "outside.txt" ], workspaceNames)
    let docsNode =
        Graph.children graph workspaceId
        |> List.pick (fun child ->
            match Filename.tryValue graph.nodes.[child.id].name with
            | Some "docs" -> Some graph.nodes.[child.id]
            | _ -> None)
    Assert.Equal(Special SpecialKind.Directory, docsNode.kind)
    Assert.Equal(Unparsed, docsNode.documentState)
    let docsChildren =
        Graph.children graph docsNode.id
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
    Assert.Empty(docsChildren)
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``directory reconcile walks one level`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
    let workspaceId = postWorkspace fileAgent "home"
    let graph1 = readGraph fileAgent
    let docsId, docsOps =
        FileNodeOps.planCreateOwnedDirectory graph1 workspaceId "docs"
    postOps fileAgent 1 docsOps
    let docs = Path.Combine(tempDir, "home", "docs")
    let side = Path.Combine(docs, "side", "inner")
    Directory.CreateDirectory(side) |> ignore
    File.WriteAllText(Path.Combine(docs, "note.txt"), "note")
    File.WriteAllText(Path.Combine(docs, "side", "deep.txt"), "deep")
    LazyLoadReconciliationServer.reconcileDirectory
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
        "docs"
    |> Async.RunSynchronously
    |> requireOk "directory reconcile"
    |> ignore
    let graph = readGraph fileAgent
    let names =
        Graph.children graph docsId
        |> List.choose (fun child ->
            Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "note.txt"; "side" ], names)
    let sideNode =
        Graph.children graph docsId
        |> List.pick (fun child ->
            match Filename.tryValue graph.nodes.[child.id].name with
            | Some "side" -> Some graph.nodes.[child.id]
            | _ -> None)
    Assert.Equal(Special SpecialKind.Directory, sideNode.kind)
    Assert.Equal(Unparsed, sideNode.documentState)
    Assert.Empty(Graph.children graph sideNode.id)
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``workspace reconcile leaves a current child directory current`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
    let workspaceId = postWorkspace fileAgent "home"
    let graph1 = readGraph fileAgent
    let keptId, keptOps =
        FileNodeOps.planCreateOwnedDirectory graph1 workspaceId "kept"
    postOps
        fileAgent
        1
        (keptOps @ [ Op.SetDocumentState(keptId, Unparsed, Current) ])
    let keptDir = Path.Combine(tempDir, "home", "kept")
    Directory.CreateDirectory(keptDir) |> ignore
    File.WriteAllText(Path.Combine(keptDir, "secret.txt"), "secret")
    Directory.CreateDirectory(Path.Combine(tempDir, "home", "fresh"))
    |> ignore
    File.WriteAllText(Path.Combine(tempDir, "home", "top.txt"), "top")
    LazyLoadReconciliationServer.reconcileWorkspace
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
    |> Async.RunSynchronously
    |> requireOk "workspace reconcile"
    |> ignore
    let graph = readGraph fileAgent
    Assert.Equal(Current, graph.nodes.[keptId].documentState)
    let keptNames =
        Graph.children graph keptId
        |> List.choose (fun child ->
            Filename.tryValue graph.nodes.[child.id].name)
    Assert.Empty(keptNames)
    let fresh =
        Graph.children graph workspaceId
        |> List.pick (fun child ->
            match Filename.tryValue graph.nodes.[child.id].name with
            | Some "fresh" -> Some graph.nodes.[child.id]
            | _ -> None)
    Assert.Equal(Unparsed, fresh.documentState)
    Assert.Empty(Graph.children graph fresh.id)
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``directory reconcile leaves a current child directory current`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
    let workspaceId = postWorkspace fileAgent "home"
    let graph1 = readGraph fileAgent
    let docsId, docsOps =
        FileNodeOps.planCreateOwnedDirectory graph1 workspaceId "docs"
    postOps fileAgent 1 docsOps
    let graph2 = readGraph fileAgent
    let keptId, keptOps =
        FileNodeOps.planCreateOwnedDirectory graph2 docsId "kept"
    postOps
        fileAgent
        2
        (keptOps @ [ Op.SetDocumentState(keptId, Unparsed, Current) ])
    let docs = Path.Combine(tempDir, "home", "docs")
    Directory.CreateDirectory(Path.Combine(docs, "kept")) |> ignore
    File.WriteAllText(Path.Combine(docs, "kept", "secret.txt"), "secret")
    File.WriteAllText(Path.Combine(docs, "note.txt"), "note")
    LazyLoadReconciliationServer.reconcileDirectory
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
        "docs"
    |> Async.RunSynchronously
    |> requireOk "directory reconcile"
    |> ignore
    let graph = readGraph fileAgent
    Assert.Equal(Current, graph.nodes.[keptId].documentState)
    Assert.Empty(Graph.children graph keptId)
    let names =
        Graph.children graph docsId
        |> List.choose (fun child ->
            Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "kept"; "note.txt" ], names)
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``workspace reconcile creates Directory for empty leading-dot dir`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
    let workspaceId = postWorkspace fileAgent "home"
    let scratch = Path.Combine(tempDir, "home", ".scratch")
    Directory.CreateDirectory scratch |> ignore
    LazyLoadReconciliationServer.reconcileWorkspace
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
    |> Async.RunSynchronously
    |> requireOk "empty .scratch reconcile"
    |> ignore
    let graph = readGraph fileAgent
    let scratchNode =
        Graph.children graph workspaceId
        |> List.pick (fun child ->
            match Filename.tryValue graph.nodes.[child.id].name with
            | Some ".scratch" -> Some graph.nodes.[child.id]
            | _ -> None)
    Assert.Equal(Special SpecialKind.Directory, scratchNode.kind)
    Assert.Equal(Loaded, Graph.childrenStatus graph scratchNode.id)
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``directory reconcile keeps .agents Loaded with discovered children`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
    let workspaceId = postWorkspace fileAgent "home"
    let graph1 = readGraph fileAgent
    let agentsId, agentsOps =
        FileNodeOps.planCreateOwnedDirectory graph1 workspaceId ".agents"
    postOps fileAgent 1 agentsOps
    let skillPath =
        Path.Combine(tempDir, "home", ".agents", "skill.md")
    Directory.CreateDirectory(Path.GetDirectoryName skillPath) |> ignore
    File.WriteAllText(skillPath, "ok")
    LazyLoadReconciliationServer.reconcileDirectory
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
        ".agents"
    |> Async.RunSynchronously
    |> requireOk ".agents reconcile"
    |> ignore
    let graph = readGraph fileAgent
    let agents = graph.nodes.[agentsId]
    Assert.Equal(Special SpecialKind.Directory, agents.kind)
    Assert.Equal(Loaded, Graph.childrenStatus graph agentsId)
    let childNames =
        Graph.children graph agentsId
        |> List.choose (fun child ->
            Filename.tryValue graph.nodes.[child.id].name)
    Assert.Contains("skill.md", childNames)
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``SYSTEM workspace reconcile creates File stubs under systemId`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
    let systemDir = Path.Combine(tempDir, "SYSTEM")
    Directory.CreateDirectory(systemDir) |> ignore
    File.WriteAllText(Path.Combine(systemDir, "user.css"), "body{}")
    File.WriteAllText(Path.Combine(systemDir, "http-responses.log"), "log")
    File.WriteAllText(Path.Combine(systemDir, "gambol.meta"), "1")
    LazyLoadReconciliationServer.reconcileWorkspace
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "SYSTEM" }
    |> Async.RunSynchronously
    |> requireOk "SYSTEM reconcile"
    |> ignore
    let graph = readGraph fileAgent
    let names =
        Graph.children graph Graph.systemId
        |> List.choose (fun child ->
            if child.ref <> Ownership.Owner then
                None
            else
                Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Contains("user.css", names)
    Assert.Contains("http-responses.log", names)
    Assert.DoesNotContain("gambol.meta", names)
    Assert.True(
        names
        |> List.forall (fun name ->
            Graph.children graph Graph.systemId
            |> List.exists (fun c ->
                c.ref = Ownership.Owner
                && Filename.tryValue graph.nodes.[c.id].name = Some name
                && Graph.isSpecialSystemDirectoryMember graph c.id)))
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``directory reconcile does not duplicate Normal-owned present file`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
    let workspaceId = postWorkspace fileAgent "home"
    let graph1 = readGraph fileAgent
    let docsId, docsOps =
        FileNodeOps.planCreateOwnedDirectory graph1 workspaceId "docs"
    postOps
        fileAgent
        1
        (docsOps @ [ Op.SetDocumentState(docsId, Unparsed, Current) ])
    let organizerId = NodeId.New()
    postOps
        fileAgent
        2
        [ Op.NewNode(organizerId, "organizer")
          Op.Replace(docsId, [], [ ChildNode.owner organizerId ]) ]
    let graph4 = readGraph fileAgent
    let fileId, fileOps =
        FileNodeOps.planCreateOwnedFile graph4 organizerId "present.txt"
    postOps fileAgent 3 fileOps
    let presentPath = Path.Combine(tempDir, "home", "docs", "present.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(presentPath)) |> ignore
    File.WriteAllText(presentPath, "already owned")
    LazyLoadReconciliationServer.reconcileDirectory
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
        "docs"
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
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``directory reconcile creates missing sibling under directory`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
    let workspaceId = postWorkspace fileAgent "home"
    let graph1 = readGraph fileAgent
    let docsId, docsOps =
        FileNodeOps.planCreateOwnedDirectory graph1 workspaceId "docs"
    postOps fileAgent 1 docsOps
    let missingPath = Path.Combine(tempDir, "home", "docs", "missing.txt")
    Directory.CreateDirectory(Path.GetDirectoryName(missingPath)) |> ignore
    File.WriteAllText(missingPath, "new")
    LazyLoadReconciliationServer.reconcileDirectory
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
        "docs"
    |> Async.RunSynchronously
    |> requireOk "directory reconcile"
    |> ignore
    let graph = readGraph fileAgent
    let names =
        Graph.children graph docsId
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "missing.txt" ], names)
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``directory reconcile with amb outline and missing file posts without ownership error`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
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
        LazyLoadReconciliationServer.reconcileDirectory
            { handle = handle
              dataDir = tempDir
              workspaceLabel = "home" }
            "tasks"
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
            Graph.children graph tasksId
            |> List.choose (fun child ->
                Filename.tryValue graph.nodes.[child.id].name)
            |> List.sort
        Assert.Contains("inbox.txt", names)
        let occurrences =
            Graph.children graph tasksId
            |> List.filter (fun c -> c.id = activeId)
        Assert.Equal(1, occurrences.Length)
        Assert.Equal(Ownership.Owner, occurrences.Head.ref)
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``directory reconcile returns resilient failures and posts good sibling`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
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
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
        [ "docs/bad.txt" ]
    |> Async.RunSynchronously
    |> requireOk "seed bad"
    |> ignore
    let failures =
        LazyLoadReconciliationServer.reconcileChangedPathsWithDiscovery
            { handle = handle
              dataDir = tempDir
              workspaceLabel = "home" }
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
        Graph.children graph docsId
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "bad.txt"; "good.txt" ], names)
    CoreMailbox.dispose fileAgent

[<Fact>]
let ``directory reconciliation POST returns failures JSON`` () =
    let tempDir = newTempDir ()
    use client = createClientForDir tempDir
    let workspaceId, wsOps = FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let wsChange = SpecialNodeTestHelpers.changeEventZero "" wsOps
    let wsEvent = wsChange
    let wsBody =
        Thoth.Json.Newtonsoft.Encode.toString 0
            (ApiResponseSerialization.encodeChangeRequest
                { events = [ wsEvent ]; want = [] })
    use wsContent = new StringContent(wsBody, Text.Encoding.UTF8, "application/json")
    let wsResp =
        client.PostAsync("/ambit/changes", wsContent)
        |> Async.AwaitTask
        |> Async.RunSynchronously
    Assert.Equal(HttpStatusCode.OK, wsResp.StatusCode)
    let stateResp =
        client.GetAsync("/ambit/state?scope=full")
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
    let docsChange =
        SpecialNodeTestHelpers.changeEvent "" EventId.zero (Guid.NewGuid()) docsOps
    let docsEvent = docsChange
    let docsBody =
        Thoth.Json.Newtonsoft.Encode.toString 0
            (ApiResponseSerialization.encodeChangeRequest
                { events = [ docsEvent ]; want = [] })
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
        client.PostAsync("/ambit/workspace/reconciliation/directory", content)
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
    let wsChange = SpecialNodeTestHelpers.changeEventZero "" wsOps
    let wsEvent = wsChange
    let wsBody =
        Thoth.Json.Newtonsoft.Encode.toString 0
            (ApiResponseSerialization.encodeChangeRequest
                { events = [ wsEvent ]; want = [] })
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
        client.PostAsync("/ambit/workspace/reconciliation/directory", content)
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
        client.GetAsync("/ambit/state?scope=full")
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
        Graph.children graph workspaceId
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "root.txt" ], names)

[<Fact>]
let ``git pull marks received files at any depth and does not discover`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
    let workspaceId = postWorkspace fileAgent "home"
    let graph1 = readGraph fileAgent
    let topId, topOps =
        FileNodeOps.planCreateOwnedFile graph1 workspaceId "top.txt"
    postOps
        fileAgent
        1
        (topOps @ [ Op.SetDocumentState(topId, Unparsed, Current) ])
    let graph2 = readGraph fileAgent
    let docsId, docsOps =
        FileNodeOps.planCreateOwnedDirectory graph2 workspaceId "docs"
    postOps
        fileAgent
        2
        (docsOps @ [ Op.SetDocumentState(docsId, Unparsed, Current) ])
    let graph3 = readGraph fileAgent
    let nestedId, nestedOps =
        FileNodeOps.planCreateOwnedFile graph3 docsId "nested.txt"
    postOps
        fileAgent
        3
        (nestedOps @ [ Op.SetDocumentState(nestedId, Unparsed, Current) ])
    let home = Path.Combine(tempDir, "home")
    let docs = Path.Combine(home, "docs")
    Directory.CreateDirectory(docs) |> ignore
    File.WriteAllText(Path.Combine(home, "top.txt"), "top")
    File.WriteAllText(Path.Combine(home, "other.txt"), "not on the list")
    File.WriteAllText(Path.Combine(docs, "nested.txt"), "nested")
    File.WriteAllText(Path.Combine(docs, ".amb"), "outline")
    LazyLoadReconciliationServer.reconcileReceivedFiles
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
        [ LazyLoadReconciliation.Modified "top.txt"
          LazyLoadReconciliation.Modified "docs/nested.txt"
          LazyLoadReconciliation.Modified "docs/.amb" ]
    |> Async.RunSynchronously
    |> requireOk "pull"
    |> ignore
    let graph = readGraph fileAgent
    Assert.Equal(Unparsed, graph.nodes.[topId].documentState)
    Assert.Equal(Unparsed, graph.nodes.[nestedId].documentState)
    Assert.Equal(Current, graph.nodes.[docsId].documentState)
    let names =
        Graph.children graph workspaceId
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "docs"; "top.txt" ], names)
    let members =
        DocumentPersistPath.discoverImmediateMembers home
        |> requireOk "one-level walk"
    Assert.DoesNotContain("docs/nested.txt", members)
    Assert.Contains("docs", members)
    CoreMailbox.dispose fileAgent

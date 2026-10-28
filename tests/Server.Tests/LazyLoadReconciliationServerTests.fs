module Gambol.Server.Tests.LazyLoadReconciliationServerTests

open System
open System.IO
open System.Net
open System.Net.Http
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
    CoreDoor.postEvents (admittedChanges fileAgent) [ event ]
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
    CoreDoor.postEvents (admittedChanges fileAgent) [ event ]
    |> Async.RunSynchronously
    |> requireOk "ops"
    |> ignore

let private readGraph (fileAgent: MailboxHost) =
    CoreMailboxApi.getState fileAgent
    |> Async.RunSynchronously
    |> requireOk "state"
    |> fun state -> state.graph

/// Current is the state Notice.NodeParsed writes.
let private markCurrent (fileAgent: MailboxHost) (nodeId: NodeId) =
    CoreMailboxApi.postNotice fileAgent (Notice.NodeParsed nodeId)
    readGraph fileAgent |> ignore

[<Fact>]
let ``git pull marks received files at any depth and does not discover`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
    let workspaceId = postWorkspace fileAgent "home"
    let graph1 = readGraph fileAgent
    let topId, topOps =
        FileNodeOps.planCreateOwnedFile graph1 workspaceId "top.txt"
    postOps fileAgent 1 topOps
    markCurrent fileAgent topId
    let graph2 = readGraph fileAgent
    let docsId, docsOps =
        FileNodeOps.planCreateOwnedDirectory graph2 workspaceId "docs"
    postOps fileAgent 2 docsOps
    markCurrent fileAgent docsId
    let graph3 = readGraph fileAgent
    let nestedId, nestedOps =
        FileNodeOps.planCreateOwnedFile graph3 docsId "nested.txt"
    postOps fileAgent 3 nestedOps
    markCurrent fileAgent nestedId
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
    Assert.Equal(Unparsed, graph.nodes.[topId].documentState.AsEnum)
    Assert.Equal(Unparsed, graph.nodes.[nestedId].documentState.AsEnum)
    Assert.Equal(Current, graph.nodes.[docsId].documentState.AsEnum)
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
    CoreMailboxApi.dispose fileAgent

[<Fact>]
let ``git pull add marks the containing directory Unparsed`` () =
    let tempDir = newTempDir ()
    let fileAgent, handle = createAdmittedFile tempDir
    let workspaceId = postWorkspace fileAgent "home"
    let graph1 = readGraph fileAgent
    let docsId, docsOps =
        FileNodeOps.planCreateOwnedDirectory graph1 workspaceId "docs"
    postOps fileAgent 1 docsOps
    markCurrent fileAgent docsId
    let graph2 = readGraph fileAgent
    let keepId, keepOps =
        FileNodeOps.planCreateOwnedFile graph2 docsId "keep.txt"
    postOps fileAgent 2 keepOps
    markCurrent fileAgent keepId
    let home = Path.Combine(tempDir, "home")
    let docs = Path.Combine(home, "docs")
    Directory.CreateDirectory(docs) |> ignore
    File.WriteAllText(Path.Combine(docs, "keep.txt"), "keep")
    File.WriteAllText(Path.Combine(docs, "extra.txt"), "extra")
    File.WriteAllText(Path.Combine(home, "other.txt"), "not on the list")
    LazyLoadReconciliationServer.reconcileReceivedFiles
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home" }
        [ LazyLoadReconciliation.Added "docs/extra.txt" ]
    |> Async.RunSynchronously
    |> requireOk "pull"
    |> ignore
    let graph = readGraph fileAgent
    let names =
        Graph.children graph workspaceId
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal<string list>([ "docs" ], names)
    CoreMailboxApi.dispose fileAgent

module Gambol.Server.Tests.DocumentPathMoveExecutionTests

open System.IO
open System.Threading.Tasks
open Gambol.Server
open Gambol.Server.Tests.TestBackend
open Gambol.Shared
open Xunit

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private lockOk label result =
    match result with
    | Ok () -> ()
    | Error rejection -> failwith $"{label}: {rejection}"

let private persistWhileHeld
    (graph: Graph)
    (postGraph: Graph)
    (heldId: NodeId)
    (dataDir: string)
    (duringHold: unit -> unit)
    =
    let release = TaskCompletionSource<unit>()
    let entered = TaskCompletionSource<unit>()

    let holder =
        Task.Run(fun () ->
            NodeLockPool.withLock postGraph heldId (fun () ->
                entered.SetResult()
                release.Task.Wait())
            |> lockOk "hold")

    entered.Task.Wait()

    let persist =
        Task.Run(fun () ->
            DocumentPersistChange.persistGraphChange dataDir graph postGraph)

    (NodeLockPool.waitUntilQueued 2).Wait()
    Assert.False(persist.IsCompleted)
    duringHold ()
    release.SetResult()
    persist.Result |> requireOk "persistGraphChange" |> ignore
    holder.Wait()
    Assert.Equal(0, NodeLockPool.entryCount ())

let private owned = ChildNode.owners

let private specialNode (id: NodeId) (kind: SpecialKind) (name: string) (owner: NodeId) : Node =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private normalNode (id: NodeId) (text: string) (owner: NodeId) : Node =
    Node.Create(id, text = text, owner = owner)

let private graphWithNestedDocs () : Graph * NodeId * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let normalId = NodeId.New()

    let graph1 =
        graph0
        |> Graph.addDetachedNode (specialNode wsId Workspace "home" Graph.workspacesId)
        |> Graph.addDetachedNode (specialNode dirId Directory "docs" wsId)
        |> Graph.addDetachedNode (specialNode fileId File "readme.txt" dirId)
        |> Graph.addDetachedNode (normalNode normalId "body" fileId)

    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces->ws"

    let graph3 =
        Graph.replace wsId 0 [] (owned [ dirId ]) graph2
        |> requireOk "ws->dir"

    let graph4 =
        Graph.replace dirId 0 [] (owned [ fileId ]) graph3
        |> requireOk "dir->file"

    let graph5 =
        Graph.replace fileId 0 [] (owned [ normalId ]) graph4
        |> requireOk "file->normal"

    graph5, wsId, dirId, fileId, normalId

[<Fact>]
let ``persistGraphChange moves renamed file artifact before writing`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let postGraph = Graph.setName fileId "readme.txt" "notes.txt" graph |> requireOk "rename file"

    DocumentPersistChange.persistGraphChange dataDir graph postGraph
    |> requireOk "persistGraphChange"
    |> ignore

    Assert.False(File.Exists(Path.Combine(dataDir, "home", "docs", "readme.txt")))
    Assert.True(File.Exists(Path.Combine(dataDir, "home", "docs", "notes.txt")))
    DocumentPersistChange.readAllDocuments dataDir |> requireOk "read" |> ignore

[<Fact>]
let ``persistGraphChange moves renamed directory tree before writing`` () =
    let dataDir = newTempDir ()
    let graph, _, dirId, _, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let postGraph = Graph.setName dirId "docs" "archive" graph |> requireOk "rename dir"

    DocumentPersistChange.persistGraphChange dataDir graph postGraph
    |> requireOk "persistGraphChange"
    |> ignore

    Assert.False(Directory.Exists(Path.Combine(dataDir, "home", "docs")))
    Assert.True(File.Exists(Path.Combine(dataDir, "home", "archive", ".amb")))
    Assert.True(File.Exists(Path.Combine(dataDir, "home", "archive", "readme.txt")))

[<Fact>]
let ``persistGraphChange rejects destination artifact conflict`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let postGraph = Graph.setName fileId "readme.txt" "notes.txt" graph |> requireOk "rename file"
    File.WriteAllText(Path.Combine(dataDir, "home", "docs", "notes.txt"), "occupied")

    match DocumentPersistChange.persistGraphChange dataDir graph postGraph with
    | Ok _ -> Assert.Fail("expected destination conflict")
    | Error msg -> Assert.Contains("already exists", msg)

    Assert.True(File.Exists(Path.Combine(dataDir, "home", "docs", "readme.txt")))
    Assert.True(File.Exists(Path.Combine(dataDir, "home", "docs", "notes.txt")))

[<Fact>]
let ``persist path move waits while the moved file is held`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let postGraph = Graph.setName fileId "readme.txt" "notes.txt" graph |> requireOk "rename file"
    let oldPath = Path.Combine(dataDir, "home", "docs", "readme.txt")
    let newPath = Path.Combine(dataDir, "home", "docs", "notes.txt")

    persistWhileHeld graph postGraph fileId dataDir (fun () ->
        Assert.True(File.Exists oldPath)
        Assert.False(File.Exists newPath))

    Assert.False(File.Exists oldPath)
    Assert.True(File.Exists newPath)

let private graphWithSiblingFile () : Graph * NodeId * NodeId =
    let graph, _, dirId, fileId, _ = graphWithNestedDocs ()
    let siblingId = NodeId.New()

    let withSibling =
        graph
        |> Graph.addDetachedNode (specialNode siblingId File "other.txt" dirId)
        |> Graph.replace dirId 0 [] (owned [ fileId; siblingId ])
        |> requireOk "dir owns sibling"

    withSibling, fileId, siblingId

[<Fact>]
let ``persist path move waits while a file under the moved directory is held`` () =
    let dataDir = newTempDir ()
    let graph, _, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let postGraph = Graph.setName dirId "docs" "archive" graph |> requireOk "rename dir"
    let oldDir = Path.Combine(dataDir, "home", "docs")
    let newDir = Path.Combine(dataDir, "home", "archive")

    persistWhileHeld graph postGraph fileId dataDir (fun () ->
        Assert.True(Directory.Exists oldDir))

    Assert.True(Directory.Exists newDir)

[<Fact>]
let ``a path move on a thread that already holds a lock leaves the artifact`` () =
    let dataDir = newTempDir ()
    let graph, fileId, siblingId = graphWithSiblingFile ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let postGraph = Graph.setName fileId "readme.txt" "notes.txt" graph |> requireOk "rename file"
    let oldPath = Path.Combine(dataDir, "home", "docs", "readme.txt")

    NodeLockPool.withLock postGraph siblingId (fun () ->
        Assert.Equal(1, NodeLockPool.entryCount ())

        let persist =
            DocumentPersistChange.persistGraphChange dataDir graph postGraph

        Assert.Equal(Error "holder already holds a lock", persist)
        Assert.True(File.Exists oldPath)
        Assert.False(File.Exists(Path.Combine(dataDir, "home", "docs", "notes.txt")))
        Assert.Equal(1, NodeLockPool.entryCount ()))
    |> lockOk "hold"

    Assert.Equal(0, NodeLockPool.entryCount ())
    Assert.True(File.Exists oldPath)

[<Fact>]
let ``a sibling file lock lets a file path move finish`` () =
    let dataDir = newTempDir ()
    let graph, fileId, siblingId = graphWithSiblingFile ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let postGraph = Graph.setName fileId "readme.txt" "notes.txt" graph |> requireOk "rename file"
    let docsDir = Path.Combine(dataDir, "home", "docs")
    let oldPath = Path.Combine(docsDir, "readme.txt")
    let newPath = Path.Combine(docsDir, "notes.txt")
    let release = TaskCompletionSource<unit>()
    let entered = TaskCompletionSource<unit>()
    let moved = TaskCompletionSource<unit>()

    use watcher = new FileSystemWatcher(docsDir, "notes.txt")
    watcher.NotifyFilter <- NotifyFilters.FileName
    watcher.Created.Add(fun _ -> moved.TrySetResult() |> ignore)
    watcher.Renamed.Add(fun _ -> moved.TrySetResult() |> ignore)
    watcher.EnableRaisingEvents <- true

    let holder =
        Task.Run(fun () ->
            NodeLockPool.withLock postGraph siblingId (fun () ->
                entered.SetResult()
                release.Task.Wait())
            |> lockOk "hold")

    entered.Task.Wait()

    let persist =
        Task.Run(fun () ->
            DocumentPersistChange.persistGraphChange dataDir graph postGraph)

    moved.Task.Wait()
    Assert.False(release.Task.IsCompleted)
    Assert.False(File.Exists oldPath)
    Assert.True(File.Exists newPath)
    release.SetResult()
    persist.Result |> requireOk "persistGraphChange" |> ignore
    holder.Wait()
    Assert.Equal(0, NodeLockPool.entryCount ())

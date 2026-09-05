module Gambol.Server.Tests.GraphOnlyChangePostTests

open System
open System.IO
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err -> failwith $"{label}: {err}"

let private postWorkspace (fileAgent: FileAgent) (label: string) =
    let workspaceId, ops = FileNodeOps.planCreateWorkspace (Graph.create ()) label
    let change = { id = 0; changeId = Guid.NewGuid(); ops = ops }
    (FileAgent.coreChanges fileAgent).postChange [ change ]
    |> Async.RunSynchronously
    |> requireOk "workspace"
    |> ignore
    workspaceId

let private recordingHandle (inner: CoreChanges) =
    let posts = ResizeArray<Change list>()
    let handle =
        { inner with
            postGraphOnlyChange =
                fun changes ->
                    posts.Add(changes)
                    inner.postGraphOnlyChange changes }
    handle, posts

[<Fact>]
let ``reconcile posts graph-only chunks at or under maxOps`` () =
    let tempDir = newTempDir ()
    let fileAgent = FileAgent.create tempDir
    let workspaceId = postWorkspace fileAgent "home"
    let fileCount = GraphOnlyChangeChunks.maxOps
    let home = Path.Combine(tempDir, "home")
    Directory.CreateDirectory(home) |> ignore
    for i in 1 .. fileCount do
        File.WriteAllText(Path.Combine(home, sprintf "n%03d.txt" i), "x")
    let inner = FileAgent.coreChanges fileAgent
    let handle, posts = recordingHandle inner
    LazyLoadReconciliationServer.reconcileChangedPaths handle tempDir "home" []
    |> Async.RunSynchronously
    |> requireOk "reconcile"
    |> ignore
    Assert.True(
        posts.Count >= 2,
        sprintf "expected multiple posts, got %d" posts.Count)
    for changes in posts do
        let n = changes |> List.sumBy (fun change -> change.ops.Length)
        Assert.True(
            n <= GraphOnlyChangeChunks.maxOps,
            sprintf "chunk had %d ops" n)
        Assert.True(n > 0)
    let graph = (FileAgent.getState fileAgent |> Async.RunSynchronously).graph
    let names =
        graph.nodes.[workspaceId].children
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal(fileCount, names.Length)
    FileAgent.dispose fileAgent

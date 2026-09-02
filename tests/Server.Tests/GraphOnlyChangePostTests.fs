module Gambol.Server.Tests.GraphOnlyChangePostTests

open System
open System.IO
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err -> failwith $"{label}: {err}"

let private postWorkspace (fileAgent: FileAgent) (label: string) =
    let workspaceId, ops = FileNodeOps.planCreateWorkspace (Graph.create ()) label
    let change = { id = 0; changeId = Guid.NewGuid(); ops = ops }
    let body =
        Encode.toString 0 (Serialization.encodeChangeBatch { changes = [ change ] })
    FileAgent.postChange fileAgent body
    |> Async.RunSynchronously
    |> requireOk "workspace"
    |> ignore
    workspaceId

let private decodePostedOps (body: string) : int =
    match Decode.fromString Serialization.decodeChangeBatch body with
    | Error err -> failwith $"decode posted change: {err}"
    | Ok batch ->
        batch.changes |> List.sumBy (fun c -> c.ops.Length)

let private recordingHandle (inner: AgentHandle) =
    let posts = ResizeArray<string>()
    let handle =
        { inner with
            postGraphOnlyChange =
                fun body ->
                    posts.Add(body)
                    inner.postGraphOnlyChange body }
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
    let inner = AgentHandle.ofFile fileAgent
    let handle, posts = recordingHandle inner
    LazyLoadReconciliationServer.reconcileChangedPaths handle tempDir "home" []
    |> Async.RunSynchronously
    |> requireOk "reconcile"
    |> ignore
    Assert.True(
        posts.Count >= 2,
        sprintf "expected multiple posts, got %d" posts.Count)
    for body in posts do
        let n = decodePostedOps body
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

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

[<Fact>]
let ``postChunks mints Ev with EventId.zero and commandName`` () =
    let posts = ResizeArray<Ev>()
    let accepted =
        CoreChanges.accepted (EventIdFixtures.storedId 1) true [] false None
    let post event =
        posts.Add event
        async.Return(Ok accepted)
    GraphOnlyChangePost.postChunks
        post
        "Parse"
        [ [ Op.NewNode(NodeId.New(), "a") ]
          [ Op.NewNode(NodeId.New(), "b") ] ]
    |> Async.RunSynchronously
    |> requireOk "chunks"
    |> ignore
    Assert.Equal(2, posts.Count)
    for event in posts do
        Assert.Equal(EventId.zero, event.id)
        Assert.Equal("Parse", event.commandName)
        match event.body with
        | EventBody.Change ops -> Assert.True(ops.Length > 0)
        | _ -> failwith "expected Change Event"

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

let private parsePosts (host: MailboxHost) =
    CoreMailboxApi.eventHistory host
    |> Async.RunSynchronously
    |> fun log ->
        log.events
        |> List.filter (fun event -> event.commandName = "Parse")

[<Fact>]
let ``reconcile posts graph-only chunks at or under maxOps`` () =
    let tempDir = newTempDir ()
    let fileAgent, _ = createAdmittedFile tempDir
    let workspaceId = postWorkspace fileAgent "home"
    let fileCount = GraphOnlyChangeChunks.maxOps
    let home = Path.Combine(tempDir, "home")
    Directory.CreateDirectory(home) |> ignore
    for i in 1 .. fileCount do
        File.WriteAllText(Path.Combine(home, sprintf "n%03d.txt" i), "x")
    let handle = admittedChanges fileAgent
    LazyLoadReconciliationServer.reconcileChangedPaths
        { handle = handle
          dataDir = tempDir
          workspaceLabel = "home"
          enqueueInMsg = CoreMailboxApi.addInMsg fileAgent }
        []
    |> Async.RunSynchronously
    |> requireOk "reconcile"
    |> ignore
    let posts = parsePosts fileAgent
    Assert.True(
        posts.Length >= 2,
        sprintf "expected multiple posts, got %d" posts.Length)
    for event in posts do
        let n = Ev.ops event |> Option.defaultValue [] |> List.length
        Assert.True(
            n <= GraphOnlyChangeChunks.maxOps,
            sprintf "chunk had %d ops" n)
        Assert.True(n > 0)
    let graph =
        CoreMailboxApi.getState fileAgent
        |> Async.RunSynchronously
        |> requireOk "state"
        |> fun state -> state.graph
    let names =
        Graph.children graph workspaceId
        |> List.choose (fun child -> Filename.tryValue graph.nodes.[child.id].name)
        |> List.sort
    Assert.Equal(fileCount, names.Length)
    CoreMailboxApi.dispose fileAgent

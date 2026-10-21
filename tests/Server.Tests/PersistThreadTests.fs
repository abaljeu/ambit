module Gambol.Server.Tests.PersistThreadTests

open System
open System.Diagnostics
open System.IO
open System.Threading
open Xunit
open Gambol.Server
open Gambol.Server.Tests.TestBackend
open Gambol.Shared

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error error -> failwith $"{label}: {error}"

let private owned = ChildNode.owners

let private special id kind name owner persist =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind,
        parseState = ParseState.Parsed,
        persistState = persist)

let private fileGraph (body: string) =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let fileId = NodeId.New()
    let bodyId = NodeId.New()
    let graph1 =
        graph0
        |> Graph.addDetachedNode
            (special wsId Workspace "home" Graph.workspacesId PersistState.Persisted)
        |> Graph.addDetachedNode
            (special fileId File "note.txt" wsId PersistState.Unpersisted)
        |> Graph.addDetachedNode
            (Node.Create(bodyId, text = body, owner = fileId))
    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces"
        |> Graph.replace wsId 0 [] (owned [ fileId ])
        |> requireOk "file"
        |> Graph.replace fileId 0 [] (owned [ bodyId ])
        |> requireOk "body"
    graph2, fileId, bodyId

let private withBody (graph: Graph) (bodyId: NodeId) (text: string) =
    let body = graph.nodes.[bodyId]
    { graph with nodes = Map.add bodyId { body with text = text } graph.nodes }

let private markPersisted (state: State) (nodeId: NodeId) =
    let node =
        Node.withPersistState PersistState.Persisted state.graph.nodes.[nodeId]
    { state with
        graph =
            { state.graph with
                nodes = Map.add nodeId node state.graph.nodes } }

let private waitUntil (label: string) (ready: unit -> bool) =
    let sw = Stopwatch.StartNew()
    while not (ready ()) && sw.ElapsedMilliseconds < 2000 do
        Thread.Sleep 5
    Assert.True(ready (), label)

let private startThread
    (state: State ref)
    (take: unit -> PersistRequest option)
    (persistNode: Graph -> NodeId -> Result<DateTime, string>)
    (postNotice: Notice -> unit)
    =
    PersistThread.start {
        currentState = fun () -> state.Value
        take = take
        persistNode = persistNode
        postNotice = postNotice
    }

[<Fact>]
let ``persist thread writes from the live graph`` () =
    let dataDir = newTempDir ()
    let graph, fileId, bodyId = fileGraph "before"
    let state = ref { graph = graph; eventId = EventId.zero }
    let stack = PersistRequestStack.create ()
    PersistRequestStack.push stack { nodeId = fileId }
    state.Value <-
        { state.Value with
            graph = withBody state.Value.graph bodyId "after-edit" }
    let notices = ResizeArray<Notice>()
    let join =
        startThread
            state
            (fun () -> PersistRequestStack.takeUntilStop stack)
            (fun graph nodeId ->
                DocumentPersistChange.persistNode dataDir graph nodeId)
            notices.Add
    waitUntil "NodePersisted" (fun () -> notices.Count > 0)
    PersistRequestStack.stop stack
    join ()
    let path =
        DocumentPersistPath.resolveArtifactPath dataDir state.Value.graph fileId
        |> requireOk "path"
    let text = File.ReadAllText path
    Assert.Contains("after-edit", text)
    Assert.DoesNotContain("before", text)
    let persisted =
        notices
        |> Seq.tryPick (function
            | Notice.NodePersisted (id, _) -> Some id
            | _ -> None)
    let stamped =
        notices
        |> Seq.exists (function
            | Notice.NodeStamped _ -> true
            | _ -> false)
    Assert.Equal(Some fileId, persisted)
    Assert.True(stamped, "expected NodeStamped after the write")

[<Fact>]
let ``repeated pushes write once`` () =
    let id = NodeId.New()
    let node =
        special id File "note.txt" Graph.rootId PersistState.Unpersisted
    let state =
        ref {
            graph = Graph.addDetachedNode node (Graph.create ())
            eventId = EventId.zero
        }
    let writes = ref 0
    let reads = ref 0
    let stack = PersistRequestStack.create ()
    for _ in 1 .. 4 do
        PersistRequestStack.push stack { nodeId = id }
    let join =
        PersistThread.start {
            currentState =
                fun () ->
                    Interlocked.Increment reads |> ignore
                    state.Value
            take = fun () -> PersistRequestStack.takeUntilStop stack
            persistNode =
                fun _ _ ->
                    Interlocked.Increment writes |> ignore
                    Ok DateTime.UtcNow
            postNotice =
                function
                | Notice.NodePersisted (nodeId, _) ->
                    state.Value <- markPersisted state.Value nodeId
                | _ -> ()
        }
    waitUntil "four takes" (fun () -> reads.Value >= 4)
    PersistRequestStack.stop stack
    join ()
    Assert.Equal(1, writes.Value)

[<Fact>]
let ``persist thread posts NodePersisted on success only`` () =
    let run persistNode =
        let id = NodeId.New()
        let node =
            special id File "note.txt" Graph.rootId PersistState.Unpersisted
        let state =
            ref {
                graph = Graph.addDetachedNode node (Graph.create ())
                eventId = EventId.zero
            }
        let stack = PersistRequestStack.create ()
        let notices = ResizeArray<Notice>()
        PersistRequestStack.push stack { nodeId = id }
        let join =
            startThread
                state
                (fun () -> PersistRequestStack.takeUntilStop stack)
                persistNode
                notices.Add
        waitUntil "notice" (fun () -> notices.Count > 0)
        PersistRequestStack.stop stack
        join ()
        id, List.ofSeq notices

    let written = DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc)
    let okId, okNotices = run (fun _ _ -> Ok written)
    match okNotices with
    | [ Notice.NodePersisted (id, time); Notice.NodeStamped (_, None) ] ->
        Assert.Equal(okId, id)
        Assert.Equal(written, time)
    | other ->
        Assert.Fail($"expected persisted then stamped, got {other}")

    let badId, badNotices = run (fun _ _ -> Error "disk full")
    Assert.Equal<Notice list>(
        [ Notice.PersistFailed(badId, "disk full") ],
        badNotices)
    let persistedBad =
        badNotices
        |> List.exists (function
            | Notice.NodePersisted (id, _) -> id = badId
            | _ -> false)
    Assert.False(persistedBad)

let private applyPersisted (state: State ref) (notice: Notice) =
    let handlers : PersistHandlers = {
        getState = fun () -> Ok state.Value
        getEventId = fun () -> Ok state.Value.eventId
        getEventsSince = fun _ -> Ok []
        getEventLog = fun () -> Ok EventLog.empty
        appendEvent = fun _ -> Ok ()
        applyEvent = fun _ _ -> Error "unused"
        replaceGraph =
            fun _ graph ->
                state.Value <- { state.Value with graph = graph }
        noteDiffDone = ignore
        noteParsed = ignore
        noteStamped = fun _ _ -> ()
        noteWriteFailed = ignore
    }
    match CoreMailboxLoad.applyNotice handlers notice with
    | Ok () -> ()
    | Error err -> Assert.Fail(err)

[<Fact>]
let ``NodePersisted carries the written file time`` () =
    let dataDir = newTempDir ()
    let graph, fileId, _ = fileGraph "saved"
    let state = ref { graph = graph; eventId = EventId.zero }
    let stack = PersistRequestStack.create ()
    PersistRequestStack.push stack { nodeId = fileId }
    let notices = ResizeArray<Notice>()
    let join =
        startThread
            state
            (fun () -> PersistRequestStack.takeUntilStop stack)
            (fun graph nodeId ->
                DocumentPersistChange.persistNode dataDir graph nodeId)
            (fun notice ->
                notices.Add notice
                applyPersisted state notice)
    waitUntil "NodePersisted" (fun () -> notices.Count > 0)
    PersistRequestStack.stop stack
    join ()
    let path =
        DocumentPersistPath.resolveArtifactPath
            dataDir
            state.Value.graph
            fileId
        |> requireOk "path"
    let fileTime =
        File.GetLastWriteTimeUtc path |> NodeUpdateTime.toDbPrecision
    match
        notices
        |> Seq.tryFind (function
            | Notice.NodePersisted _ -> true
            | _ -> false)
    with
    | Some (Notice.NodePersisted (id, updateTime)) ->
        Assert.Equal(fileId, id)
        Assert.Equal(fileTime, updateTime)
        Assert.Equal(fileTime, state.Value.graph.nodes.[fileId].updateTime)
    | other -> Assert.Fail($"expected NodePersisted, got {other}")

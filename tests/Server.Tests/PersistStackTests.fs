module Gambol.Server.Tests.PersistStackTests

open System.Threading
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared

let private fileGraph (parse: ParseState) (persist: PersistState) =
    let id = NodeId.New()
    let node =
        Node.Create(
            id,
            text = "note.txt",
            name = Filename.create "note.txt",
            kind = Special File,
            documentState = ParseState.toDocumentState parse,
            parseState = parse,
            persistState = persist)
    id, Graph.addDetachedNode node (Graph.create ())

let private request (id: NodeId) (graph: Graph) : PersistRequest = {
    graph = graph
    nodeId = id
}

[<Fact>]
let ``persist stack holds a request`` () =
    let id, graph =
        fileGraph ParseState.Parsed PersistState.Unpersisted
    let _, later =
        fileGraph ParseState.Parsed PersistState.Unpersisted
    let requests = PersistStack.create ()
    PersistStack.push requests (request id graph)
    PersistStack.push requests (request id later)
    let first = requests.Take()
    Assert.Equal(id, first.nodeId)
    Assert.Equal(later, first.graph)
    let second = requests.Take()
    Assert.Equal(id, second.nodeId)
    Assert.Equal(graph, second.graph)

[<Fact>]
let ``persist thread takes one request`` () =
    let id, graph =
        fileGraph ParseState.Parsed PersistState.Unpersisted
    let requests = PersistStack.create ()
    let seen = TaskCompletionSource<PersistRequest>()
    PersistNodeThread.start {
        requests = requests
        write = fun taken ->
            seen.TrySetResult taken |> ignore
            Ok { graph = taken.graph; message = None }
        finish = ignore
    }
    PersistStack.push requests (request id graph)
    Assert.True(seen.Task.Wait(2000))
    Assert.Equal(id, seen.Task.Result.nodeId)
    Assert.Equal(graph, seen.Task.Result.graph)
    Assert.Equal(
        PersistState.Unpersisted,
        graph.nodes.[id].documentState.persistState)

[<Fact>]
let ``persist finish posts NodePersisted`` () =
    let id, graph =
        fileGraph ParseState.Parsed PersistState.Unpersisted
    let requests = PersistStack.create ()
    let posted = TaskCompletionSource<Notice>()
    PersistNodeThread.start {
        requests = requests
        write = fun taken -> Ok { graph = taken.graph; message = None }
        finish = fun nodeId ->
            posted.TrySetResult(Notice.NodePersisted nodeId) |> ignore
    }
    PersistStack.push requests (request id graph)
    Assert.True(posted.Task.Wait(2000))
    Assert.Equal(Notice.NodePersisted id, posted.Task.Result)
    Assert.Equal(
        PersistState.Unpersisted,
        graph.nodes.[id].documentState.persistState)

[<Fact>]
let ``failed node write does not post NodePersisted`` () =
    let id, graph =
        fileGraph ParseState.Parsed PersistState.Unpersisted
    let requests = PersistStack.create ()
    let wrote = TaskCompletionSource<unit>()
    let posted = TaskCompletionSource<Notice>()
    PersistNodeThread.start {
        requests = requests
        write = fun _ ->
            wrote.TrySetResult() |> ignore
            Error "disk full"
        finish = fun nodeId ->
            posted.TrySetResult(Notice.NodePersisted nodeId) |> ignore
    }
    PersistStack.push requests (request id graph)
    Assert.True(wrote.Task.Wait(2000))
    Thread.Sleep(50)
    Assert.False(posted.Task.IsCompleted)
    Assert.Equal(
        PersistState.Unpersisted,
        graph.nodes.[id].documentState.persistState)

[<Fact>]
let ``persistNode follows the node id`` () =
    let id, graph =
        fileGraph ParseState.Parsed PersistState.Unpersisted
    let missing = NodeId.New()
    match DocumentPersistChange.persistNode "data" graph missing with
    | Error msg ->
        Assert.Equal("persist node is not in the graph", msg)
    | Ok _ -> Assert.Fail("missing node wrote")
    let unparsedId, unparsed =
        fileGraph ParseState.Unparsed PersistState.Unpersisted
    match
        DocumentPersistChange.persistNode "data" unparsed unparsedId
        with
    | Error msg -> Assert.Equal("persist node is Unparsed", msg)
    | Ok _ -> Assert.Fail("unparsed node wrote")
    match DocumentPersistChange.persistNode "data" graph id with
    | Error msg ->
        Assert.NotEqual<string>("persist node is not in the graph", msg)
        Assert.NotEqual<string>("persist node is Unparsed", msg)
    | Ok written -> Assert.Equal(graph, written.graph)

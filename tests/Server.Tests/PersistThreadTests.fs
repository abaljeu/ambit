module Gambol.Server.Tests.PersistThreadTests

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

let private edit (id: NodeId) =
    [ Op.SetText(id, "note.txt", "next") ]

let private startWith persistOps persistChange =
    let calls = ResizeArray<string>()
    let finishes = ResizeArray<NodeId>()
    let failures = ResizeArray<string>()
    let collect, consumer = PersistCollectors.create ()
    PersistThread.start {
        consumer = consumer
        write = {
            persistOps =
                fun _ _ post ops ->
                    calls.Add("ops")
                    persistOps post ops
            persistChange =
                fun _ _ post ->
                    calls.Add("change")
                    persistChange post
            finish = finishes.Add
            changeFailed = fun _ err -> failures.Add(err)
        }
    }
    collect, calls, finishes, failures

let private okGraph post _ =
    Ok { graph = post; message = None }

[<Fact>]
let ``open node calls persist ops and adds NodePersisted`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let collect, calls, finishes, _failures =
        startWith okGraph (fun post -> okGraph post [])
    let outcome =
        collect {
            nodeIds = [ id ]
            dataDir = Some "data"
            preGraph = graph
            postGraph = graph
            ops = edit id
            kind = PersistKind.Ops
            notify = true
            wait = true
        }
    match outcome with
    | PersistOutcome.Wrote stamped ->
        Assert.Equal(graph, stamped.graph)
    | other -> Assert.Fail($"expected Wrote, got {other}")
    Assert.Equal<string>([ "ops" ], calls)
    Assert.Equal(id, Assert.Single(finishes))
    Assert.Equal(PersistState.Unpersisted, graph.nodes.[id].documentState.persistState)

[<Fact>]
let ``Unparsed node is blocked and does not call persist`` () =
    let id, graph = fileGraph ParseState.Unparsed PersistState.Unpersisted
    let collect, calls, finishes, _failures =
        startWith okGraph (fun post -> okGraph post [])
    let outcome =
        collect {
            nodeIds = [ id ]
            dataDir = Some "data"
            preGraph = graph
            postGraph = graph
            ops = edit id
            kind = PersistKind.Ops
            notify = true
            wait = true
        }
    match outcome with
    | PersistOutcome.Blocked -> ()
    | other -> Assert.Fail($"expected Blocked, got {other}")
    Assert.Empty(calls)
    Assert.Empty(finishes)
    Assert.Equal(PersistState.Unpersisted, graph.nodes.[id].documentState.persistState)

[<Fact>]
let ``snapshot change calls persist change and adds NodePersisted`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let collect, calls, finishes, _failures =
        startWith okGraph (fun post -> okGraph post [])
    let outcome =
        collect {
            nodeIds = [ id ]
            dataDir = Some "data"
            preGraph = graph
            postGraph = graph
            ops = []
            kind = PersistKind.Change
            notify = true
            wait = true
        }
    match outcome with
    | PersistOutcome.Wrote _ -> ()
    | other -> Assert.Fail($"expected Wrote, got {other}")
    Assert.Equal<string>([ "change" ], calls)
    Assert.Equal(id, Assert.Single(finishes))

[<Fact>]
let ``Change is blocked when any submitted node is Unparsed`` () =
    let openId, graph =
        fileGraph ParseState.Parsed PersistState.Unpersisted
    let blockedId = NodeId.New()
    let blockedNode =
        Node.Create(
            blockedId,
            text = "raw.txt",
            name = Filename.create "raw.txt",
            kind = Special File,
            documentState = Unparsed,
            parseState = ParseState.Unparsed,
            persistState = PersistState.Unpersisted)
    let graph = Graph.addDetachedNode blockedNode graph
    let collect, calls, finishes, _failures =
        startWith okGraph (fun post -> okGraph post [])
    let outcome =
        collect {
            nodeIds = [ openId; blockedId ]
            dataDir = Some "data"
            preGraph = graph
            postGraph = graph
            ops = []
            kind = PersistKind.Change
            notify = true
            wait = true
        }
    match outcome with
    | PersistOutcome.Blocked -> ()
    | other -> Assert.Fail($"expected Blocked, got {other}")
    Assert.Empty(calls)
    Assert.Empty(finishes)

[<Fact>]
let ``failed Change does not add NodePersisted`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let collect, calls, finishes, failures =
        startWith okGraph (fun _ -> Error "disk full")
    let outcome =
        collect {
            nodeIds = [ id ]
            dataDir = Some "data"
            preGraph = graph
            postGraph = graph
            ops = []
            kind = PersistKind.Change
            notify = true
            wait = true
        }
    match outcome with
    | PersistOutcome.Failed err -> Assert.Equal("disk full", err)
    | other -> Assert.Fail($"expected Failed, got {other}")
    Assert.Equal<string>([ "change" ], calls)
    Assert.Empty(finishes)
    Assert.Equal<string>([ "disk full" ], failures)
    Assert.Equal(PersistState.Unpersisted, graph.nodes.[id].documentState.persistState)

[<Fact>]
let ``thrown persist ops return Failed`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let collect, _calls, finishes, failures =
        startWith
            (fun _ _ ->
                raise (System.InvalidOperationException("injected persistence failure")))
            (fun post -> okGraph post [])
    let outcome =
        collect {
            nodeIds = [ id ]
            dataDir = Some "data"
            preGraph = graph
            postGraph = graph
            ops = edit id
            kind = PersistKind.Ops
            notify = true
            wait = true
        }
    match outcome with
    | PersistOutcome.Failed err ->
        Assert.Contains("injected persistence failure", err)
    | other -> Assert.Fail($"expected Failed, got {other}")
    Assert.Empty(finishes)
    Assert.Empty(failures)
    Assert.Equal(PersistState.Unpersisted, graph.nodes.[id].documentState.persistState)

let private sourceFile (relative: string) =
    let rec walk (dir: System.IO.DirectoryInfo) =
        let candidate = System.IO.Path.Combine(dir.FullName, relative)
        if System.IO.File.Exists candidate then candidate
        elif isNull dir.Parent then failwith $"missing {relative}"
        else walk dir.Parent
    walk (System.IO.DirectoryInfo(System.AppContext.BaseDirectory))

let private nodeWrite
    (calls: ResizeArray<string>)
    (finish: NodeId -> unit)
    (persistChange: string -> Graph -> Graph -> Result<PersistGraphOk, string>)
    : PersistWrite =
    { persistOps =
        fun _ _ post _ ->
            calls.Add("ops")
            Ok { graph = post; message = None }
      persistChange =
        fun dataDir pre post ->
            calls.Add("change")
            persistChange dataDir pre post
      finish = finish
      changeFailed = fun _ _ -> () }

[<Fact>]
let ``persist stack holds a node`` () =
    let push, take = PersistStack.create ()
    let id = NodeId.New()
    push id
    Assert.Equal(id, take ())
    let route =
        System.IO.File.ReadAllText(sourceFile "src/Server/RouteRegistration.fs")
    Assert.DoesNotContain("PersistStack", route)
    Assert.DoesNotContain("startNodeStack", route)
    Assert.DoesNotContain("persistPush", route)

[<Fact>]
let ``persist thread pulls one node`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let push, take = PersistStack.create ()
    push id
    let calls = ResizeArray<string>()
    PersistThread.pullOne {
        take = take
        dataDir = Some "data"
        graph = fun () -> graph
        write =
            nodeWrite
                calls
                ignore
                (fun _ _ post -> Ok { graph = post; message = None })
    }
    Assert.Equal<string>([ "change" ], calls)
    Assert.Equal(
        PersistState.Unpersisted,
        graph.nodes.[id].documentState.persistState)
    Assert.Equal("note.txt", graph.nodes.[id].text)

[<Fact>]
let ``persist finish posts NodePersisted`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let push, take = PersistStack.create ()
    let posted = ResizeArray<Notice>()
    let slot = ref (Some (fun notice -> posted.Add notice))
    let pull persistChange =
        push id
        PersistThread.pullOne {
            take = take
            dataDir = Some "data"
            graph = fun () -> graph
            write =
                nodeWrite
                    (ResizeArray())
                    (PersistThread.finishWhenBound slot)
                    persistChange
        }
    pull (fun _ _ post -> Ok { graph = post; message = None })
    Assert.Equal(Notice.NodePersisted id, Assert.Single(posted))
    posted.Clear()
    pull (fun _ _ _ -> Error "disk full")
    Assert.Empty(posted)
    Assert.Equal(
        PersistState.Unpersisted,
        graph.nodes.[id].documentState.persistState)

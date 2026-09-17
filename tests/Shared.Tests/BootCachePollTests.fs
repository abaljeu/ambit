module BootCachePollTests

open System
open Gambol.Shared
open Gambol.Shared
open BootCacheTestHelpers
open Xunit

let private mkPoll rev (events: Ev list) : ChangeSuccessResponse =
    { revision = EventId rev
      buildEpochSec = 1
      pageBuildEpochSec = 1
      apiVersion = ApiVersion.current
      isReady = true
      externalChanges = not events.IsEmpty
      events = events
      message = None
      bootstrapHash = None }

let private decide clientRev log poll =
    BootCache.decideBootPoll clientRev log poll None None

[<Fact>]
let ``novelEvents skips Poll Events already in the log by id`` () =
    let local = mkEvent 4
    let pollDup = { local with submissionId = Guid.NewGuid() }
    let novel = mkEvent 5
    let kept = BootCache.novelEvents [ local ] [ pollDup; novel ]
    Assert.Equal(5, kept.Head.id.Value)
    Assert.Equal(1, kept.Length)

[<Fact>]
let ``novelEvents skips Poll Events already in the log by submissionId`` () =
    let local = mkEvent 4
    let pollDup = { mkEvent 99 with submissionId = local.submissionId }
    Assert.Empty(BootCache.novelEvents [ local ] [ pollDup ])

[<Fact>]
let ``decideBootPoll confirms an empty tail at matching Revision`` () =
    match decide 5 [] (mkPoll 5 []) with
    | BootCache.BootPoll.Confirmed true -> ()
    | other -> failwithf "%A" other

[<Fact>]
let ``decideBootPoll confirms when the tail is only local log duplicates`` () =
    let local = mkEvent 6
    let poll = mkPoll 6 [ local ]
    match decide 6 [ local ] poll with
    | BootCache.BootPoll.Confirmed true -> ()
    | other -> failwithf "%A" other

[<Fact>]
let ``decideBootPoll reports CodeOutdated when API version mismatches`` () =
    let poll = { mkPoll 5 [] with apiVersion = ApiVersion.current + 1 }
    match decide 5 [] poll with
    | BootCache.BootPoll.CodeOutdated -> ()
    | other -> failwithf "%A" other

[<Fact>]
let ``decideBootPoll confirms when page stamps differ and API matches`` () =
    let poll = { mkPoll 5 [] with buildEpochSec = 2; pageBuildEpochSec = 99 }
    match decide 5 [] poll with
    | BootCache.BootPoll.Confirmed true -> ()
    | other -> failwithf "%A" other

[<Fact>]
let ``decideBootPoll applies a novel tail`` () =
    let novel = mkEvent 7
    match decide 6 [] (mkPoll 7 [ novel ]) with
    | BootCache.BootPoll.ApplyNovel (events, true) ->
        Assert.Equal(7, events.Head.id.Value)
    | other -> failwithf "%A" other

[<Fact>]
let ``decideBootPoll falls back when Poll Revision is behind the client`` () =
    match decide 9 [] (mkPoll 4 []) with
    | BootCache.BootPoll.FallbackState "revision" -> ()
    | other -> failwithf "%A" other

[<Fact>]
let ``decideBootPoll falls back when the novel tail is oversized`` () =
    let many =
        List.init (BootCache.maxNovelCount + 1) (fun i -> mkEvent (10 + i))
    match decide 9 [] (mkPoll 20 many) with
    | BootCache.BootPoll.FallbackState "oversized" -> ()
    | other -> failwithf "%A" other

[<Fact>]
let ``decideBootPoll falls back when the Revision gap is oversized`` () =
    match decide 1 [] (mkPoll (1 + BootCache.maxPollRevGap + 1) []) with
    | BootCache.BootPoll.FallbackState "oversized" -> ()
    | other -> failwithf "%A" other

[<Fact>]
let ``shouldTruncate is true when the log is longer than the bound`` () =
    Assert.True(BootCache.shouldTruncate (BootCache.maxLogLength + 1) 1 2)
    Assert.False(BootCache.shouldTruncate 1 10 11)

[<Fact>]
let ``shouldTruncate is true when the Revision gap exceeds the bound`` () =
    Assert.True(
        BootCache.shouldTruncate 1 1 (1 + BootCache.maxRevGap + 1))

[<Fact>]
let ``truncationGraph drops Load-only nested Workspace children`` () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let wsNode =
        Node.Create(
            wsId,
            text = "home",
            name = Filename.create "home",
            owner = Graph.workspacesId,
            kind = Special Workspace,
            children = [ ChildNode.owner dirId ])
    let dirNode =
        Node.Create(
            dirId,
            text = "docs",
            name = Filename.create "docs",
            owner = wsId,
            kind = Special Directory)
    let nodes =
        graph0.nodes
        |> Map.add wsId wsNode
        |> Map.add dirId dirNode
        |> Map.add
            Graph.workspacesId
            { graph0.nodes.[Graph.workspacesId] with
                children = [ ChildNode.owner wsId ] }
    let graph = Graph.fromNodes graph0.root nodes
    let scoped = BootCache.truncationGraph graph None
    Assert.True(scoped.nodes.ContainsKey wsId)
    Assert.Equal(Unloaded, scoped.nodes.[wsId].childrenStatus)
    Assert.False(scoped.nodes.ContainsKey dirId)

[<Fact>]
let ``graphFingerprint is stable for the same Graph and changes when ROOT text changes`` () =
    let graph0, noteId = Graph.newNode "hello" (Graph.create ())
    let root = graph0.nodes.[graph0.root]
    let graph =
        Graph.fromNodes
            graph0.root
            (graph0.nodes
             |> Map.add
                    graph0.root
                    { root with children = [ ChildNode.owner noteId ] })
    let same = BootCache.graphFingerprint graph
    Assert.Equal(same, BootCache.graphFingerprint graph)
    match Graph.setText noteId "hello" "world" graph with
    | Error err -> failwith err
    | Ok changed ->
        Assert.NotEqual<string>(same, BootCache.graphFingerprint changed)

[<Fact>]
let ``decideBootPoll falls back on equal Revision hash mismatch`` () =
    match
        BootCache.decideBootPoll
            5 [] (mkPoll 5 []) (Some "aaa") (Some "bbb")
    with
    | BootCache.BootPoll.FallbackState "hash" -> ()
    | other -> failwithf "%A" other

[<Fact>]
let ``decideBootPoll skips hash compare when Poll omits bootstrapHash`` () =
    match
        BootCache.decideBootPoll 5 [] (mkPoll 5 []) None (Some "bbb")
    with
    | BootCache.BootPoll.Confirmed true -> ()
    | other -> failwithf "%A" other

[<Fact>]
let ``cachedHashForBootPoll is None after /state even when a fingerprint is set`` () =
    Assert.Equal(None, BootCache.cachedHashForBootPoll true "fable-poison")
    Assert.Equal(None, BootCache.cachedHashForBootPoll true "")
    Assert.Equal(None, BootCache.cachedHashForBootPoll false "")
    Assert.Equal(Some "srv", BootCache.cachedHashForBootPoll false "srv")

[<Fact>]
let ``decideBootPoll confirms after /state when a client fingerprint disagrees`` () =
    let poll = { mkPoll 5 [] with bootstrapHash = Some "server" }
    let cached = BootCache.cachedHashForBootPoll true "fable-poison"
    match
        BootCache.decideBootPoll 5 [] poll (Some "server") cached
    with
    | BootCache.BootPoll.Confirmed true -> ()
    | other -> failwithf "%A" other

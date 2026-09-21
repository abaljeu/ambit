module CommandRequestTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open Xunit

let private owned = ChildNode.owners

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private zoomWithChildren
    (childTexts: string list)
    : Graph * SiteMap * NodeId * NodeId list =
    let g0 = Graph.create ()
    let g1, zoomIds = ModelBuilder.createNodes [ "?test hello" ] g0
    let zoomId = zoomIds.[0]
    let g2, childIds = ModelBuilder.createNodes childTexts g1
    let g3 =
        Graph.replace g2.root 0 [] (owned [ zoomId ]) g2
        |> requireOk "zoomWithChildren.root"
    let graph =
        Graph.replace zoomId 0 [] (owned childIds) g3
        |> requireOk "zoomWithChildren.zoom"
    let siteMap, _ = buildSiteMapFrom graph zoomId (Sid 0)
    graph, siteMap, zoomId, childIds

/// Root owns texts[0] owns texts[1] … SiteMap Zoom is texts[0].
let private ownerChain
    (texts: string list)
    : Graph * SiteMap * NodeId list =
    let g0 = Graph.create ()
    let g1, ids = ModelBuilder.createNodes texts g0
    let graph =
        ids
        |> List.fold
            (fun (graph, parentId) childId ->
                let next =
                    Graph.replace parentId 0 [] (owned [ childId ]) graph
                    |> requireOk "ownerChain"
                next, childId)
            (g1, g1.root)
        |> fst
    let zoomId = ids.[0]
    let siteMap, _ = buildSiteMapFrom graph zoomId (Sid 0)
    graph, siteMap, ids

[<Fact>]
let ``isCommandText is true when text starts with literal ?`` () =
    Assert.True(CommandRequest.isCommandText "?test hello")
    Assert.True(CommandRequest.isCommandText "?")

[<Fact>]
let ``isCommandText is false when text does not start with ?`` () =
    Assert.False(CommandRequest.isCommandText "hello")
    Assert.False(CommandRequest.isCommandText " ?test hello")
    Assert.False(CommandRequest.isCommandText "")

[<Fact>]
let ``oneNodeStart uses the current Node as Command Zoom and Focus`` () =
    let graph, siteMap, nodeId, _ = zoomWithChildren [ "a" ]
    let request =
        CommandRequest.oneNodeStart graph siteMap nodeId EventId.zero
    Assert.Equal(nodeId, request.zoomId)
    Assert.Equal(nodeId, request.focusId)
    Assert.Equal(nodeId, request.commandId)
    Assert.Equal(EventId.zero, request.eventId)

[<Fact>]
let ``oneNodeStart graphIds are the unfolded Included descendant ids`` () =
    let graph, siteMap, nodeId, childIds = zoomWithChildren [ "a"; "b" ]
    let request =
        CommandRequest.oneNodeStart graph siteMap nodeId EventId.zero
    let expected = IncludedDescendantIds.expand graph siteMap nodeId
    Assert.Equal<NodeId list>(expected, request.graphIds)
    Assert.Equal<NodeId list>([ nodeId; childIds.[0]; childIds.[1] ], request.graphIds)

[<Fact>]
let ``actorNameFromText selects test from ?test hello`` () =
    Assert.Equal(Some "test", CommandRequest.actorNameFromText "?test hello")
    Assert.Equal(Some "test", CommandRequest.actorNameFromText "?TEST hello")
    Assert.Equal(Some "ai", CommandRequest.actorNameFromText "?ai later")
    Assert.Equal(Some "test", CommandRequest.actorNameFromText "?test")

[<Fact>]
let ``actorNameFromText is none when text is not a ? Command`` () =
    Assert.Equal(None, CommandRequest.actorNameFromText "hello")
    Assert.Equal(None, CommandRequest.actorNameFromText "test")
    Assert.Equal(None, CommandRequest.actorNameFromText "")
    Assert.Equal(None, CommandRequest.actorNameFromText "?")

[<Fact>]
let ``behaviorFromText interprets hello from ?test hello`` () =
    Assert.Equal("hello", CommandRequest.behaviorFromText "?test hello")
    Assert.Equal("hello", CommandRequest.behaviorFromText "?TEST HELLO")
    Assert.Equal("hello", CommandRequest.behaviorFromText "hello")
    Assert.Equal("hello", CommandRequest.behaviorFromText "HELLO")
    Assert.Equal("", CommandRequest.behaviorFromText "?test")

[<Fact>]
let ``actorNameFromText extracts any first token after ?`` () =
    Assert.Equal(Some "unknown", CommandRequest.actorNameFromText "?unknown")
    Assert.Equal(Some "nope", CommandRequest.actorNameFromText "?nope hello")

[<Fact>]
let ``behaviorFromText extracts any command text after the Actor name`` () =
    Assert.Equal("unknown", CommandRequest.behaviorFromText "?test unknown")
    Assert.Equal("nope", CommandRequest.behaviorFromText "?test nope")

[<Fact>]
let ``isScanStopText is true for ? prefix or equals in text`` () =
    Assert.True(CommandRequest.isScanStopText "?ai cursor")
    Assert.True(CommandRequest.isScanStopText "?test hello")
    Assert.True(CommandRequest.isScanStopText "count=1+2")
    Assert.True(CommandRequest.isScanStopText "x = 1")
    Assert.True(CommandRequest.isScanStopText "=")
    Assert.False(CommandRequest.isScanStopText "What time is it?")
    Assert.False(CommandRequest.isScanStopText "hello")
    Assert.False(CommandRequest.isScanStopText "")

[<Fact>]
let ``commandOnOwnerPath finds ? ancestor and keeps Focus distinct`` () =
    let graph, _, ids =
        ownerChain [ "?ai cursor"; "What time is it?" ]
    let commandId, focusId = ids.[0], ids.[1]
    Assert.Equal(
        Some commandId,
        CommandRequest.commandOnOwnerPath graph focusId commandId)
    Assert.Equal(
        Some commandId,
        CommandRequest.commandOnOwnerPath graph commandId commandId)

[<Fact>]
let ``scan stops at first equals and does not skip to ?`` () =
    let graph, _, ids =
        ownerChain [ "?ai cursor"; "count=1+2"; "What time is it?" ]
    let question = ids.[2]
    Assert.Equal(
        Some ids.[1],
        CommandRequest.scanStopOnOwnerPath graph question ids.[0])
    Assert.Equal(
        None,
        CommandRequest.commandOnOwnerPath graph question ids.[0])
    Assert.True(CommandRequest.isAmbleScanStop graph question ids.[0])

[<Fact>]
let ``commandOnOwnerPath is none when Zoom is below the Command`` () =
    let graph, _, ids =
        ownerChain [ "?ai cursor"; "plain"; "What time is it?" ]
    Assert.Equal(
        None,
        CommandRequest.commandOnOwnerPath graph ids.[2] ids.[1])

[<Fact>]
let ``tryStart encodes distinct Focus Command and Zoom from owner-scan`` () =
    let graph, siteMap, ids =
        ownerChain [ "?test hello"; "What time is it?" ]
    let commandId, focusId = ids.[0], ids.[1]
    match
        CommandRequest.tryStart
            graph siteMap commandId focusId EventId.zero with
    | Error err -> failwith err
    | Ok request ->
        Assert.Equal(commandId, request.zoomId)
        Assert.Equal(focusId, request.focusId)
        Assert.Equal(commandId, request.commandId)
        Assert.NotEqual(request.commandId, request.focusId)
        Assert.Equal<NodeId list>(
            IncludedDescendantIds.expand graph siteMap commandId,
            request.graphIds)

[<Fact>]
let ``oneNodeStart is actorStart with zoom focus and command equal`` () =
    let graph, siteMap, nodeId, _ = zoomWithChildren [ "a" ]
    let viaOne =
        CommandRequest.oneNodeStart graph siteMap nodeId EventId.zero
    let viaFactory =
        CommandRequest.actorStart
            graph siteMap nodeId nodeId nodeId EventId.zero
    Assert.Equal(viaFactory, viaOne)

[<Fact>]
let ``tryStart one-Node when Focus is the Command`` () =
    let graph, siteMap, nodeId, _ = zoomWithChildren [ "a" ]
    match
        CommandRequest.tryStart
            graph siteMap nodeId nodeId EventId.zero with
    | Error err -> failwith err
    | Ok request ->
        let expected =
            CommandRequest.oneNodeStart graph siteMap nodeId EventId.zero
        Assert.Equal(expected.zoomId, request.zoomId)
        Assert.Equal(expected.focusId, request.focusId)
        Assert.Equal(expected.commandId, request.commandId)
        Assert.Equal<NodeId list>(expected.graphIds, request.graphIds)

[<Fact>]
let ``tryStart errors when no runnable Command is on the path`` () =
    let graph, siteMap, ids =
        ownerChain [ "plain"; "What time is it?" ]
    match
        CommandRequest.tryStart
            graph siteMap ids.[0] ids.[1] EventId.zero with
    | Ok _ -> failwith "expected Error"
    | Error msg ->
        Assert.Equal("No command found", msg)

[<Fact>]
let ``count equals Amble stop does not ActorStart`` () =
    let graph, siteMap, ids =
        ownerChain [ "count=1+2" ]
    let focusId = ids.[0]
    Assert.True(CommandRequest.isAmbleScanStop graph focusId focusId)
    Assert.Equal(None, CommandRequest.commandOnOwnerPath graph focusId focusId)
    match
        CommandRequest.tryStart
            graph siteMap focusId focusId EventId.zero with
    | Ok _ -> failwith "equals line must not ActorStart"
    | Error _ -> ()

[<Fact>]
let ``Focus under count equals takes Amble path not ActorStart`` () =
    let graph, siteMap, ids =
        ownerChain [ "count=1+2"; "child" ]
    let zoomId, focusId = ids.[0], ids.[1]
    Assert.True(CommandRequest.isAmbleScanStop graph focusId zoomId)
    Assert.Equal(None, CommandRequest.commandOnOwnerPath graph focusId zoomId)
    match
        CommandRequest.tryStart
            graph siteMap zoomId focusId EventId.zero with
    | Ok _ -> failwith "equals ancestor must not ActorStart"
    | Error _ -> ()

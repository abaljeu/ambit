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

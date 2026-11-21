module Gambol.Server.Tests.StateEndpointGetTests

open System.Threading.Tasks
open Xunit
open Gambol.Shared
open SpecialNodeTestHelpers
open StateEndpointTests

[<Fact>]
let ``GET state returns revision 0 for fresh server`` () =
    withClient (fun client -> task {
        let! eventId, _ = readState client
        Assert.Equal(EventId.fromJson 0, eventId)
    })

[<Fact>]
let ``GET state returns valid graph with root node`` () =
    withClient (fun client -> task {
        let! _, graph = readState client
        Assert.Equal(0, userNodeCount graph)
        Assert.True(graph.nodes.ContainsKey graph.root)
        let root = graph.nodes.[graph.root]
        Assert.Equal("ROOT", root.text)
        Assert.Empty(userRootChildren graph)
    })

[<Fact>]
let ``GET state default returns ROOT closure excluding nested workspace contents`` () =
    withClient (fun client -> task {
        let! wsId, innerId = addNestedWorkspace client
        let! _, graph = readClosure client
        Assert.False(graph.nodes.ContainsKey wsId)
        Assert.Equal(Unloaded, Graph.childrenStatus graph wsId)
        Assert.Contains(
            wsId,
            Graph.children graph Graph.workspacesId
            |> List.map (fun child -> child.id))
        Assert.False(graph.nodes.ContainsKey innerId)
    })

[<Fact>]
let ``GET state scope full returns canonical graph for tests`` () =
    withClient (fun client -> task {
        let! wsId, innerId = addNestedWorkspace client
        let! _, graph = readFullState client
        Assert.True(graph.nodes.ContainsKey wsId)
        Assert.True(graph.nodes.ContainsKey innerId)
    })

module Gambol.Server.Tests.ApiPostLoadTests

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http.HttpResults
open Xunit
open Gambol.Server
open Gambol.Shared
open Thoth.Json.Newtonsoft

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

let private decodeLoadResponse json =
    Decode.fromString ApiResponseSerialization.decodeLoadResponseDecoder json

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private nestedWorkspaceGraph () : Graph * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let wsNode =
        Node.Create(
            wsId,
            text = "home",
            name = Filename.Ok "home",
            kind = Special Workspace,
            owner = Graph.workspacesId)
    let dirNode =
        Node.Create(
            dirId,
            text = "docs",
            name = Filename.Ok "docs",
            kind = Special Directory,
            owner = wsId)
    let fileNode =
        Node.Create(
            fileId,
            text = "readme.txt",
            name = Filename.Ok "readme.txt",
            kind = Special File,
            owner = dirId)
    let workspaces = graph0.nodes.[Graph.workspacesId]
    let nodes =
        graph0.nodes
        |> Map.add wsId wsNode
        |> Map.add dirId dirNode
        |> Map.add fileId fileNode
        |> Map.add
            Graph.workspacesId
            { workspaces with
                children = workspaces.children @ owned [ wsId ] }
    let graph1 = Graph.fromNodes graph0.root nodes
    let graph2 =
        Graph.replace wsId 0 [] (owned [ dirId ]) graph1
        |> function
            | Ok g -> g
            | Error err -> failwith err
    let graph3 =
        Graph.replace dirId 0 [] (owned [ fileId ]) graph2
        |> function
            | Ok g -> g
            | Error err -> failwith err
    graph3, wsId, dirId, fileId

let private stateJson (graph: Graph) (revision: int) =
    Encode.toString 0 (
        ApiResponseSerialization.encodeStateResponse
            { graph = graph
              revision = Revision revision
              isReady = true })

let private handleForLoad
    (revision: int)
    (changes: Change list)
    (stateJson: string)
    : AgentHandle =
    { getState = fun () -> async.Return(Result.Ok stateJson)
      getRevision = fun () -> async.Return revision
      getChangesSince = fun _ -> async.Return changes
      isReady = fun () -> true
      postChange = fun _ -> async.Return(Result.Error "unused")
      postGraphOnlyChange = fun _ -> async.Return(Result.Error "unused") }

let private encodeRequest (request: LoadRequest) =
    Encode.toString 0 (ApiResponseSerialization.encodeLoadRequest request)

[<Fact>]
let ``postLoad Change-only when includeWorkspace false`` () = task {
    let graph, wsId, _, fileId = nestedWorkspaceGraph ()
    let change =
        { id = 3
          changeId = Guid.NewGuid()
          ops = [ Op.SetText(fileId, "a", "b") ] }
    let handle =
        handleForLoad 5 [ change ] (stateJson graph 5)
    let body =
        encodeRequest
            { revision = 2
              targetId = fileId
              includeWorkspace = false }
    let! result = Api.postLoad handle 100 200 body |> Async.StartAsTask
    match box result with
    | :? ContentHttpResult as content ->
        match decodeLoadResponse content.ResponseContent with
        | Error err -> failwith err
        | Ok (response: LoadResponse) ->
            Assert.Equal(5, response.revision)
            Assert.Equal(100, response.buildEpochSec)
            Assert.Equal(200, response.pageBuildEpochSec)
            Assert.Equal(1, response.changes.Length)
            Assert.Empty(response.packages)
            Assert.False(response.packages |> List.exists (fun n -> n.id = wsId))
    | other ->
        Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
}

[<Fact>]
let ``postLoad Workspace subgraph when includeWorkspace true`` () = task {
    let graph, wsId, dirId, fileId = nestedWorkspaceGraph ()
    let handle =
        handleForLoad 7 [] (stateJson graph 7)
    let body =
        encodeRequest
            { revision = 7
              targetId = fileId
              includeWorkspace = true }
    let! result = Api.postLoad handle 1 2 body |> Async.StartAsTask
    match box result with
    | :? ContentHttpResult as content ->
        match decodeLoadResponse content.ResponseContent with
        | Error err -> failwith err
        | Ok (response: LoadResponse) ->
            Assert.Equal(7, response.revision)
            Assert.Empty(response.changes)
            let byId = response.packages |> List.map (fun n -> n.id, n) |> Map.ofList
            Assert.True(byId.ContainsKey wsId)
            Assert.Equal(Loaded, byId.[wsId].childrenStatus)
            Assert.True(byId.ContainsKey dirId)
            Assert.True(byId.ContainsKey fileId)
    | other ->
        Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
}

[<Fact>]
let ``postLoad missing target returns changes without packages`` () = task {
    let graph, _, _, _ = nestedWorkspaceGraph ()
    let change =
        { id = 1
          changeId = Guid.NewGuid()
          ops = [] }
    let handle =
        handleForLoad 4 [ change ] (stateJson graph 4)
    let body =
        encodeRequest
            { revision = 0
              targetId = NodeId.New()
              includeWorkspace = true }
    let! result = Api.postLoad handle 0 0 body |> Async.StartAsTask
    match box result with
    | :? ContentHttpResult as content ->
        match decodeLoadResponse content.ResponseContent with
        | Error err -> failwith err
        | Ok (response: LoadResponse) ->
            Assert.Equal(4, response.revision)
            Assert.Equal(1, response.changes.Length)
            Assert.Empty(response.packages)
    | other ->
        Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
}

[<Fact>]
let ``postLoad shares one revision for changes and packages`` () = task {
    let graph, wsId, _, fileId = nestedWorkspaceGraph ()
    let change =
        { id = 6
          changeId = Guid.NewGuid()
          ops = [ Op.SetText(fileId, "x", "y") ] }
    let handle =
        handleForLoad 9 [ change ] (stateJson graph 9)
    let body =
        encodeRequest
            { revision = 3
              targetId = fileId
              includeWorkspace = true }
    let! result = Api.postLoad handle 10 20 body |> Async.StartAsTask
    match box result with
    | :? ContentHttpResult as content ->
        match decodeLoadResponse content.ResponseContent with
        | Error err -> failwith err
        | Ok (response: LoadResponse) ->
            Assert.Equal(9, response.revision)
            Assert.Equal(1, response.changes.Length)
            Assert.True(response.packages |> List.exists (fun n -> n.id = wsId))
    | other ->
        Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
}

module Gambol.Shared.Tests.AmbDocumentTestsWorkspaceMarker

open System
open Gambol.Shared
open Xunit

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error error -> failwith $"{label}: {error}"

let private graphWithWorkspace () =
    let graph = Graph.create ()
    let documentId = NodeId.New()
    let workspaceId = NodeId.New()
    let workspace =
        Node.Create(
            workspaceId,
            text = "workspace body",
            name = Filename.Ok "home",
            owner = documentId,
            kind = Special Workspace)
    let document =
        Node.Create(
            documentId,
            name = Filename.Ok "parent.amb",
            owner = graph.root,
            kind = Special File,
            children = [ { ref = Ownership.Owner; id = workspaceId } ])
    let nodes =
        graph.nodes
        |> Map.add documentId document
        |> Map.add workspaceId workspace
    Graph.fromNodes graph.root nodes, documentId, workspaceId

[<Fact>]
let ``write Workspace owner line emits compact marker`` () =
    let graph, documentId, workspaceId = graphWithWorkspace ()
    let actual = AmbDocument.write graph documentId |> requireOk "write"
    let sid = AmbDocument.formatStableId workspaceId
    Assert.Equal("^" + sid + " !W home\tworkspace body" + Environment.NewLine, actual)

[<Fact>]
let ``read compact marker restores Workspace kind`` () =
    let graph, documentId, workspaceId = graphWithWorkspace ()
    let context =
        graph.nodes
        |> Map.remove workspaceId
        |> fun nodes -> Graph.fromNodes graph.root nodes
    let sid = AmbDocument.formatStableId workspaceId
    let text = "^" + sid + " !W home\tworkspace body" + Environment.NewLine
    let result = AmbDocument.read text documentId context |> requireOk "read"
    Assert.Equal(Special Workspace, result.nodes.[workspaceId].kind)
    Assert.Equal(Some "home", Filename.tryValue result.nodes.[workspaceId].name)
    Assert.Equal("workspace body", result.nodes.[workspaceId].text)

[<Fact>]
let ``read historical unmarked owner line remains Normal`` () =
    let graph, documentId, workspaceId = graphWithWorkspace ()
    let context =
        graph.nodes
        |> Map.remove workspaceId
        |> fun nodes -> Graph.fromNodes graph.root nodes
    let sid = AmbDocument.formatStableId workspaceId
    let text = "^" + sid + " home\tworkspace body" + Environment.NewLine
    let result = AmbDocument.read text documentId context |> requireOk "read"
    Assert.Equal(NodeKind.Normal, result.nodes.[workspaceId].kind)
    Assert.Equal(Some "home", Filename.tryValue result.nodes.[workspaceId].name)
    Assert.Equal("workspace body", result.nodes.[workspaceId].text)

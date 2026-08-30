module DocumentAssemblyTestsWorkspaceMarker

open System
open Gambol.Shared
open Xunit

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error error -> failwith $"{label}: {error}"

let private artifactsForChildKind (kind: SpecialKind) =
    let graph = Graph.create ()
    let childId = NodeId.New()
    let child =
        Node.Create(
            childId,
            text = "home",
            name = Filename.Ok "home",
            owner = Graph.workspacesId,
            kind = Special kind)
    let nodes = Map.add childId child graph.nodes
    let graphWithNode = Graph.fromNodes graph.root nodes
    let expected =
        Graph.replace
            Graph.workspacesId
            0
            []
            [ ChildNode.owner childId ]
            graphWithNode
        |> requireOk "place child"
    let rootText = AmbDocument.write expected Graph.rootId |> requireOk "write root"
    let childText = AmbDocument.write expected childId |> requireOk "write child"
    childId, rootText, Map.ofList [ ".amb", rootText; "home/.amb", childText ]

[<Fact>]
let ``assembly round trip restores Workspace only from marker`` () =
    let workspaceId, rootText, artifacts = artifactsForChildKind Workspace
    let marker =
        "^"
        + AmbDocument.formatStableId workspaceId
        + " !W home\thome"
    Assert.Contains(marker, rootText)
    let actual =
        DocumentAssembly.assembleFromArtifacts artifacts
        |> requireOk "assemble"
    Assert.Equal(Special Workspace, actual.nodes.[workspaceId].kind)
    Assert.Equal(workspaceId, actual.nodes.[Graph.workspacesId].children.Head.id)

[<Fact>]
let ``directory under Workspaces remains Directory without marker`` () =
    let directoryId, rootText, artifacts = artifactsForChildKind Directory
    let ownerLine =
        "^"
        + AmbDocument.formatStableId directoryId
        + " home\thome"
    Assert.Contains(ownerLine, rootText)
    Assert.DoesNotContain(" !W ", rootText)
    let actual =
        DocumentAssembly.assembleFromArtifacts artifacts
        |> requireOk "assemble"
    Assert.Equal(Special Directory, actual.nodes.[directoryId].kind)
    Assert.Equal(directoryId, actual.nodes.[Graph.workspacesId].children.Head.id)

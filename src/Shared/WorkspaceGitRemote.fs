namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module WorkspaceGitRemote =

    [<Literal>]
    let remoteName = "ambit"

    let private normalizeLabel (label: string) =
        if isNull label then "" else label.Trim()

    /// Smart HTTP gateway URL for a workspace label, e.g. `https://host/ambit/git/home.git`
    let gatewayUrl (appBaseUrl: string) (label: string) : string =
        let baseUri = Uri(appBaseUrl.TrimEnd('/'))
        let path = baseUri.AbsolutePath.TrimEnd('/')
        let repoLabel = normalizeLabel label
        sprintf "%s://%s%s/git/%s.git" baseUri.Scheme baseUri.Authority path repoLabel

    let findWorkspaceNodeId (graph: Graph) (label: string) : NodeId option =
        let want = normalizeLabel label |> fun s -> s.ToLowerInvariant()

        graph.nodes
        |> Map.toList
        |> List.tryPick (fun (id, node) ->
            match node.kind, node.name with
            | Special Workspace, Filename.Ok name when name.ToLowerInvariant() = want -> Some id
            | _ -> None)

    let listWorkspaceLabels (graph: Graph) : string list =
        graph.nodes
        |> Map.toList
        |> List.choose (fun (_, node) ->
            match node.kind, node.name with
            | Special Workspace, Filename.Ok name when node.owner = Graph.workspacesId -> Some name
            | _ -> None)

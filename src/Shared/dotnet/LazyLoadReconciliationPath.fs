namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module internal LazyLoadReconciliationPath =

    type PathInfo =
        { parts: string list
          kind: SpecialKind
          isDirInfo: bool }

    let private pathParts (path: string) : string list =
        path.Replace('\\', '/').Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries)
        |> Array.toList

    let private classifyPath (path: string) : PathInfo option =
        let parts = pathParts path

        if parts.IsEmpty || (parts |> List.exists (fun part -> part = ".git")) then
            None
        elif parts |> List.last |> Filename.isReservedSystemName then
            None
        else
            match DocumentArtifactPath.tryDirectoryFileOwnerParts path with
            | Some ownerParts ->
                Some
                    { parts = ownerParts
                      kind = if ownerParts.IsEmpty then Workspace else Directory
                      isDirInfo = true }
            | None ->
                Some { parts = parts; kind = File; isDirInfo = false }

    let private validateParts (parts: string list) : Result<unit, string> =
        parts
        |> List.tryFind (fun part ->
            match Filename.create part with
            | Filename.Ok _ -> false
            | _ -> true)
        |> function
            | Some part -> Error $"invalid source path component '{part}'"
            | None -> Ok ()

    let ownedChildNamed (graph: Graph) parentId name : Node option =
        graph.nodes.[parentId].children
        |> List.tryPick (fun child ->
            if Node.childOwnership graph parentId child <> Ownership.Owner then
                None
            else
                let node = graph.nodes.[child.id]
                match Filename.tryValue node.name with
                | Some candidate when
                    String.Equals(
                        candidate,
                        name,
                        StringComparison.OrdinalIgnoreCase) ->
                    Some node
                | _ -> None)

    let ownedArtifactNamed (graph: Graph) parentId name : Node option =
        GraphQuery.ownedArtifactsInDirectory graph parentId None None
        |> List.tryPick (fun nodeId ->
            match Map.tryFind nodeId graph.nodes with
            | Some node ->
                match Filename.tryValue node.name with
                | Some candidate when
                    String.Equals(
                        candidate,
                        name,
                        StringComparison.OrdinalIgnoreCase) ->
                    Some node
                | _ -> None
            | None -> None)

    let classifyValidated
        (path: string)
        : Result<PathInfo option, string>
        =
        match classifyPath path with
        | None -> Ok None
        | Some info ->
            validateParts info.parts
            |> Result.map (fun () -> Some info)

    let resolveInfo
        (graph: Graph)
        (workspaceId: NodeId)
        (info: PathInfo)
        : Result<(NodeId * SpecialKind) option, string> =
        if info.parts.IsEmpty then
            Ok(Some(workspaceId, Workspace))
        else
            let lastIndex = info.parts.Length - 1
            info.parts
            |> List.indexed
            |> List.fold (fun result (index, name) ->
                match result with
                | Error err -> Error err
                | Ok None -> Ok None
                | Ok(Some(parentId, _)) ->
                    match ownedArtifactNamed graph parentId name with
                    | None -> Ok None
                    | Some node ->
                        let expected =
                            if index = lastIndex then info.kind else Directory
                        match node.kind with
                        | Special actual when actual = expected ->
                            Ok(Some(node.id, actual))
                        | _ ->
                            Error
                                $"kind conflict at '{name}': expected {expected}, found {node.kind}")
                (Ok(Some(workspaceId, Workspace)))

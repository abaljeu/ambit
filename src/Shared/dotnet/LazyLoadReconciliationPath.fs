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

    let private classifyPath (parts: string list) : PathInfo option =
        if parts.IsEmpty || (parts |> List.exists (fun part -> part = ".git")) then
            None
        elif parts |> List.last |> Filename.isReservedSystemName then
            None
        elif List.last parts = ".amb" then
            let ownerParts = List.take (parts.Length - 1) parts
            Some
                { parts = ownerParts
                  kind = if ownerParts.IsEmpty then Workspace else Directory
                  isDirInfo = true }
        else
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
            if child.ref <> Ownership.Owner then
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

    let workspaceByLabel (graph: Graph) label : Result<NodeId, string> =
        match ownedChildNamed graph Graph.workspacesId label with
        | Some node when node.kind = Special Workspace -> Ok node.id
        | Some _ -> Error $"kind conflict at workspace '{label}'"
        | None -> Error $"workspace '{label}' not found"

    let classifyValidated
        (path: string)
        : Result<PathInfo option, string>
        =
        match path |> pathParts |> classifyPath with
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
                    match ownedChildNamed graph parentId name with
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

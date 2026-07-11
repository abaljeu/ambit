namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module LazyLoadReconciliation =

    let private pathParts (path: string) : string list =
        path.Replace('\\', '/').Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries)
        |> Array.toList

    let private classifyAddedPath (parts: string list) : (string list * SpecialKind) option =
        if parts.IsEmpty || (parts |> List.exists (fun part -> part = ".git")) then
            None
        elif List.last parts = ".amb" then
            match List.take (parts.Length - 1) parts with
            | [] -> None
            | directoryParts -> Some(directoryParts, Directory)
        else
            Some(parts, File)

    let private validateParts (parts: string list) : Result<unit, string> =
        match
            parts
            |> List.tryFind (fun part ->
                match Filename.create part with
                | Filename.Ok _ -> false
                | _ -> true)
        with
        | Some part -> Error $"invalid source path component '{part}'"
        | None -> Ok ()

    let private ownedChildNamed (graph: Graph) parentId name : Node option =
        graph.nodes.[parentId].children
        |> List.tryPick (fun child ->
            if child.ref <> Ownership.Owner then None
            else
                let node = graph.nodes.[child.id]
                match Filename.tryValue node.name with
                | Some candidate when
                    String.Equals(candidate, name, StringComparison.OrdinalIgnoreCase) ->
                    Some node
                | _ -> None)

    let private workspaceByLabel (graph: Graph) label : Result<NodeId, string> =
        match ownedChildNamed graph Graph.workspacesId label with
        | Some node when node.kind = Special Workspace -> Ok node.id
        | Some _ -> Error $"kind conflict at workspace '{label}'"
        | None -> Error $"workspace '{label}' not found"

    let private applyOps (graph: Graph) (ops: Op list) : Result<Graph, string> =
        let initial =
            { graph = graph
              history = History.empty
              revision = Revision.Zero }
        ops
        |> List.fold (fun result op ->
            match result with
            | Error err -> Error err
            | Ok state ->
                match Op.apply op state with
                | ApplyResult.Changed next
                | ApplyResult.Unchanged next -> Ok next
                | ApplyResult.Invalid(_, err) -> Error err) (Ok initial)
        |> Result.map (fun state -> state.graph)

    let private createChild graph parentId kind name =
        match kind with
        | Directory ->
            FileNodeOps.planCreateOwnedDirectory graph parentId name |> Ok
        | File ->
            FileNodeOps.planCreateOwnedFile graph parentId name |> Ok
        | _ -> Error "reconciliation can create only directory and file stubs"

    let private ensureChild graph parentId kind name =
        match ownedChildNamed graph parentId name with
        | Some node when node.kind = Special kind -> Ok(node.id, graph, [])
        | Some node ->
            Error $"kind conflict at '{name}': expected {kind}, found {node.kind}"
        | None ->
            match createChild graph parentId kind name with
            | Error err -> Error err
            | Ok(childId, ops) ->
                match applyOps graph ops with
                | Error err -> Error err
                | Ok next -> Ok(childId, next, ops)

    let private planPath (graph: Graph) workspaceId finalKind (parts: string list) =
        let lastIndex = parts.Length - 1
        parts
        |> List.indexed
        |> List.fold (fun result (index, name) ->
            match result with
            | Error err -> Error err
            | Ok(parentId, current, planned) ->
                let kind = if index = lastIndex then finalKind else Directory
                match ensureChild current parentId kind name with
                | Error err -> Error err
                | Ok(childId, next, ops) ->
                    Ok(childId, next, planned @ ops)) (Ok(workspaceId, graph, []))

    let planAddedPaths
        (graph: Graph)
        (workspaceLabel: string)
        (addedPaths: string list)
        : Result<Op list, string> =
        match workspaceByLabel graph workspaceLabel with
        | Error err -> Error err
        | Ok workspaceId ->
            addedPaths
            |> List.map pathParts
            |> List.choose classifyAddedPath
            |> List.fold (fun result (parts, finalKind) ->
                match result, validateParts parts with
                | Error err, _ -> Error err
                | _, Error err -> Error err
                | Ok(current, planned), Ok () ->
                    match planPath current workspaceId finalKind parts with
                    | Error err -> Error err
                    | Ok(_, next, ops) -> Ok(next, planned @ ops)) (Ok(graph, []))
            |> Result.map snd

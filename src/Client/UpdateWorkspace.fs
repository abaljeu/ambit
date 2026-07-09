module Gambol.Client.UpdateWorkspace

open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Gambol.Shared.ViewModel

let private applyOpsChange (ops: Op list) (status: StatusMessage option) (model: VM) : VM * Effect list =
    if ops.IsEmpty then
        { model with status = status }, []
    else
        let change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = ops }
        match applyAndPost change model with
        | None, _ -> { model with status = status }, []
        | Some m, effects -> withSiteMap m |> fun vm -> { vm with status = status }, effects

let private focusedNodeId (model: VM) : NodeId option =
    model.selectedNodes
    |> Option.map (fun sel ->
        sel.range.parent.children
        |> List.skip sel.range.start
        |> List.item sel.focus
        |> fun instId -> model.siteMap.entries.[instId].nodeId)

let syncTreeOp (model: VM) : VM * Effect list =
    match focusedNodeId model with
    | None ->
        { model with status = Some(StatusMessage.error "Select a workspace or directory to sync") }, []
    | Some nodeId ->
        match Map.tryFind nodeId model.graph.nodes with
        | Some { kind = Special (Workspace | Directory) } ->
            { model with status = Some(StatusMessage.info "Syncing tree\u2026") },
            [ RequestSyncTreeListing nodeId ]
        | _ ->
            { model with status = Some(StatusMessage.error "Sync tree requires a workspace or directory") }, []

let applySyncTreeListing (nodeId: NodeId) (branches: DiskTreeBranch list) (model: VM) : VM * Effect list =
    match WorkspaceTreeSync.planRecursiveSync model.graph nodeId branches with
    | Error err ->
        { model with status = Some(StatusMessage.error err) }, []
    | Ok plan ->
        applyOpsChange plan.ops plan.status model

let applySyncTreeListingFailed (_nodeId: NodeId) (detail: string) (model: VM) : VM * Effect list =
    { model with status = Some(StatusMessage.error ("Sync failed: " + detail)) }, []

let maybeRequestParseOnExpand (instanceId: SiteId) (model: VM) : VM * Effect list =
    match Map.tryFind instanceId model.siteMap.entries with
    | None -> model, []
    | Some entry ->
        let nodeId = entry.nodeId

        if FileExpand.needsParse model.graph nodeId then
            model, [ RequestParseFile (nodeId, false) ]
        else
            match Map.tryFind nodeId model.graph.nodes with
            | Some { kind = Special File; fileState = FileState.Parsed _ } ->
                model, [ RequestParseFile (nodeId, false) ]
            | _ ->
                model, []

let applyParseFileContent
    (nodeId: NodeId)
    (relativePath: string)
    (text: string)
    (mtimeUtc: int64)
    (model: VM)
    : VM * Effect list =
    match Map.tryFind nodeId model.graph.nodes with
    | None -> model, []
    | Some { kind = Special File; fileState = FileState.Unparsed } ->
        match FileExpand.planParseFile model.graph nodeId relativePath text mtimeUtc with
        | Error err ->
            { model with status = Some(StatusMessage.error ("Parse failed: " + err)) }, []
        | Ok (ops, status) ->
            applyOpsChange ops status model
    | Some { kind = Special File; fileState = FileState.Parsed storedM } when mtimeUtc > storedM ->
        { model with status = Some(StatusMessage.warn "File changed on disk — use Reparse to refresh") }, []
    | Some { kind = Special File } ->
        model, []
    | _ -> model, []

let reparseFileOp (model: VM) : VM * Effect list =
    match focusedNodeId model with
    | None -> { model with status = Some(StatusMessage.error "Select a file to reparse") }, []
    | Some nodeId ->
        match Map.tryFind nodeId model.graph.nodes with
        | Some { kind = Special File } ->
            model, [ RequestParseFile (nodeId, true) ]
        | _ ->
            { model with status = Some(StatusMessage.error "Reparse requires a file node") }, []

let forceApplyParseFileContent
    (nodeId: NodeId)
    (relativePath: string)
    (text: string)
    (mtimeUtc: int64)
    (model: VM)
    : VM * Effect list =
    match FileExpand.planParseFile model.graph nodeId relativePath text mtimeUtc with
    | Error err ->
        { model with status = Some(StatusMessage.error ("Parse failed: " + err)) }, []
    | Ok (ops, status) ->
        applyOpsChange ops status model

let applyParseFileFailed (_nodeId: NodeId) (detail: string) (model: VM) : VM * Effect list =
    { model with status = Some(StatusMessage.error ("Parse failed: " + detail)) }, []

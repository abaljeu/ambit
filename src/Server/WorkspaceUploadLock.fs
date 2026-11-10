namespace Gambol.Server

open Gambol.Shared

/// Graph read, Unparsed mark, and the parse push for one Upload.
type UploadHandoff = {
    readState: unit -> Async<Result<GraphAtEvent, string>>
    postNotice: Notice -> unit
    loadNode: NodeId -> Async<Result<unit, string>>
}

/// Land bytes, mark the file Unparsed, and release one Workspace lock.
[<RequireQualifiedAccess>]
module WorkspaceUploadLock =

    let private fileAt graph label relative =
        WorkspaceUploadStructure.tryResolveFileNode graph label relative

    let private applyMark handoff fileId =
        handoff.postNotice (Notice.NodeReceived fileId)
        Async.RunSynchronously (handoff.readState ())
        |> Result.map (fun _ -> ())

    type private LandWork = {
        handoff: UploadHandoff
        graph: Graph
        workspaceId: NodeId
        label: string
        relative: string
        land: unit -> Result<unit, string>
    }

    let private landLocked (work: LandWork) =
        NodeLockPool.withLockResult work.graph work.workspaceId (fun () ->
            work.land ()
            |> Result.bind (fun () ->
                match fileAt work.graph work.label work.relative with
                | None -> Ok None
                | Some fileId ->
                    applyMark work.handoff fileId
                    |> Result.map (fun () -> Some fileId)))

    let private startParse handoff fileId =
        async {
            let! loaded = handoff.loadNode fileId
            return Result.map ignore loaded
        }

    type private LandRequest = {
        handoff: UploadHandoff
        label: string
        relative: string
        land: unit -> Result<unit, string>
    }

    let private landKnown (request: LandRequest) (state: GraphAtEvent) =
        match GraphQuery.trySyncRootByLabel state.graph request.label with
        | Error _ -> request.land ()
        | Ok workspaceId ->
            match
                landLocked {
                    handoff = request.handoff
                    graph = state.graph
                    workspaceId = workspaceId
                    label = request.label
                    relative = request.relative
                    land = request.land
                }
            with
            | Error err -> Error err
            | Ok None -> Ok ()
            | Ok (Some fileId) ->
                Async.RunSynchronously (startParse request.handoff fileId)

    /// Upload has no fetch. The lock covers the land and the Unparsed mark.
    let landMarkRelease handoff label relative land =
        let request : LandRequest = {
            handoff = handoff
            label = label
            relative = relative
            land = land
        }
        async {
            match! handoff.readState () with
            | Error err -> return Error err
            | Ok state -> return landKnown request state
        }
        |> Async.RunSynchronously

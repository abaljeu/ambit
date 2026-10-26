namespace Gambol.Server

open System
open Microsoft.AspNetCore.Http
open Gambol.Shared
open Gambol.CloudAgents
open Thoth.Json.Newtonsoft

module Api =

    let private jsonResult (json: string) : IResult =
        Results.Content(json, "application/json")

    let private changeSuccessResult (response: ChangeSuccessResponse) =
        response
        |> ApiResponseSerialization.encodeChangeSuccessResponse
        |> Encode.toString 0
        |> jsonResult

    /// Prefer Content over Results.Problem: Problem.ExecuteAsync needs RequestServices
    /// and can leave HTTP 500 with an empty body if execution fails after status is set.
    let private agentErrorResult (error: string) : IResult =
        if CoreAuth.isAuthRefuse error then
            Results.Unauthorized()
        elif error.StartsWith("Internal server error", StringComparison.Ordinal) then
            Results.Content(error, "text/plain; charset=utf-8", statusCode = 500)
        else
            Results.BadRequest({| error = error |})

    let private internalError (detail: string) : IResult =
        Results.Content(detail, "text/plain; charset=utf-8", statusCode = 500)

    let private decodeFileStatusRequest =
        Thoth.Json.Core.Decode.object (fun get ->
            get.Required.Field "path" Thoth.Json.Core.Decode.string)

    type private SnapshotAnswer =
        { state: State
          nodes: Node list
          childMap: Map<NodeId, ChildNode list> }

    let private wantAnswerFromHandle
        (handle: CoreChangesDoor)
        (want: NodeId list)
        : Async<Result<SnapshotAnswer, string>> =
        async {
            match! CoreDoor.getState handle with
            | Error err -> return Error err
            | Ok state ->
                let childMap, nodes =
                    ResidentProjection.wantAnswer state.graph want
                return
                    Ok
                        { state = state
                          nodes = nodes
                          childMap = childMap }
        }

    let private eventsThroughSnapshot
        (handle: CoreChangesDoor)
        (after: EventId)
        (snapshotId: EventId)
        : Async<Ev list> =
        async {
            if snapshotId <= after then
                return []
            else
                let! events = CoreDoor.getEventsSince handle after
                return
                    events
                    |> List.filter (fun event ->
                        event.id <= snapshotId)
        }

    let private countsAsExternal (event: Ev) =
        match event.body with
        | EventBody.Notice _ -> false
        | _ -> true

    let postPoll
        (handle: CoreChangesDoor)
        (buildEpochSec: int)
        (pageBuildEpochSec: int)
        (body: string)
        : Async<IResult> = async {
        match
            Decode.fromString
                ApiResponseSerialization.decodePollRequestDecoder
                body
        with
        | Error err ->
            return Results.BadRequest({| error = $"Invalid poll request: {err}" |})
        | Ok request ->
            match! wantAnswerFromHandle handle request.want with
            | Error err -> return agentErrorResult err
            | Ok answer ->
                let! events =
                    eventsThroughSnapshot
                        handle
                        request.eventId
                        answer.state.eventId
                return
                    changeSuccessResult
                        { eventId = answer.state.eventId
                          buildEpochSec = buildEpochSec
                          pageBuildEpochSec = pageBuildEpochSec
                          apiVersion = ApiVersion.current
                          isReady = CoreDoor.isReady handle
                          externalChanges =
                            List.exists countsAsExternal events
                          events = events
                          message = None
                          bootstrapHash = None
                          nodes = answer.nodes
                          childMap = answer.childMap }
    }

    let private loadWantAnswer
        (handle: CoreChangesDoor)
        (targets: LoadTarget list)
        : Async<Result<Result<SnapshotAnswer, ResidentProjection.LoadRefuse>, string>> =
        async {
            match! CoreDoor.getState handle with
            | Error err -> return Error err
            | Ok state ->
                return
                    ResidentProjection.wantAnswerForTargets state.graph targets
                    |> Result.map (fun (childMap, nodes) ->
                        { state = state
                          nodes = nodes
                          childMap = childMap })
                    |> Ok
        }

    let postLoad
        (handle: CoreChangesDoor)
        (buildEpochSec: int)
        (pageBuildEpochSec: int)
        (body: string)
        : Async<IResult> = async {
        match
            Decode.fromString
                ApiResponseSerialization.decodeLoadRequestDecoder
                body
        with
        | Error err ->
            return Results.BadRequest({| error = $"Invalid load request: {err}" |})
        | Ok request ->
            match! loadWantAnswer handle request.targets with
            | Error err -> return agentErrorResult err
            | Ok(Error ResidentProjection.LoadRefuse.MultiWorkspace) ->
                return
                    Results.BadRequest(
                        {| error =
                            "Load requires all selected targets in one Workspace" |})
            | Ok(Ok answer) ->
                let! events =
                    eventsThroughSnapshot
                        handle
                        request.eventId
                        answer.state.eventId
                let load: LoadResponse =
                    { eventId = answer.state.eventId
                      buildEpochSec = buildEpochSec
                      pageBuildEpochSec = pageBuildEpochSec
                      apiVersion = ApiVersion.current
                      isReady = CoreDoor.isReady handle
                      events = events
                      nodes = answer.nodes
                      childMap = answer.childMap }
                let json =
                    Encode.toString 0 (ApiResponseSerialization.encodeLoadResponse load)
                return jsonResult json
    }

    let private parseBootstrapScope (req: HttpRequest) : BootstrapScope =
        match req.Query.TryGetValue "scope" with
        | true, values when values.Count > 0 && values.[0] = "full" ->
            BootstrapScope.FullGraph
        | _ -> BootstrapScope.RootClosure

    let private parseSavedZoom (req: HttpRequest) : NodeId option =
        match req.Query.TryGetValue "zoom" with
        | true, values when values.Count > 0 ->
            match Guid.TryParse(values.[0]) with
            | true, g -> Some(NodeId g)
            | _ -> None
        | _ -> None

    let getState (handle: CoreChangesDoor) (req: HttpRequest) : Async<IResult> = async {
        try
            let scope = parseBootstrapScope req
            let savedZoom = parseSavedZoom req
            let! result = CoreDoor.getState handle
            match result with
            | Ok state ->
                let response: StateResponse =
                    { graph = state.graph
                      eventId = state.eventId
                      isReady = CoreDoor.isReady handle
                      seedLiveFocusIds =
                        ActorLive.focusIdsFromLockPresent state.graph }
                let scoped =
                    match scope with
                    | BootstrapScope.FullGraph -> response
                    | BootstrapScope.RootClosure ->
                        { response with
                            graph =
                                ResidentProjection.visibleClosureGraph
                                    savedZoom
                                    response.graph }
                let encoded =
                    ApiResponseSerialization.encodeStateResponse scoped
                    |> Encode.toString 0
                return jsonResult encoded
            | Error err -> return agentErrorResult err
        with ex ->
            return
                internalError
                    $"Internal server error in GetState: {ex.Message}"
    }

    let private acceptedChangeResult
        buildEpochSec
        pageBuildEpochSec
        (accepted: CoreChangesAccepted)
        (answer: SnapshotAnswer)
        (laterEvents: Ev list)
        =
        changeSuccessResult
            { eventId = answer.state.eventId
              buildEpochSec = buildEpochSec
              pageBuildEpochSec = pageBuildEpochSec
              apiVersion = ApiVersion.current
              isReady = accepted.isReady
              externalChanges =
                accepted.externalChanges
                || List.exists countsAsExternal laterEvents
              events = accepted.events @ laterEvents
              message = None
              bootstrapHash = None
              nodes = answer.nodes
              childMap = answer.childMap }

    // Known design weakness: see the postEvents Note in
    // plan/core-refinement/arch.md (inferred from Ops, not explicit commands).
    let private inferredNodeEvents (event: Ev) : Notice list =
        match event.body with
        | EventBody.Change ops -> DocumentOpImpact.noticesBeside ops
        | _ -> []

    let postEvents
        (handle: CoreChangesDoor)
        (buildEpochSec: int)
        (pageBuildEpochSec: int)
        (body: string)
        : Async<IResult> = async {
        match
            Decode.fromString
                ApiResponseSerialization.decodeChangeRequestDecoder
                body
        with
        | Error err ->
            return agentErrorResult $"Invalid JSON: {err}"
        | Ok request ->
            match! CoreDoor.postEvents handle request.events with
            | Ok accepted ->
                request.events
                |> List.collect inferredNodeEvents
                |> List.iter (CoreMailboxApi.postNotice handle.host)
                match! wantAnswerFromHandle handle request.want with
                | Error err -> return agentErrorResult err
                | Ok answer ->
                    let! laterEvents =
                        eventsThroughSnapshot
                            handle
                            accepted.eventId
                            answer.state.eventId
                    return
                        acceptedChangeResult
                            buildEpochSec
                            pageBuildEpochSec
                            accepted
                            answer
                            laterEvents
            | Error err -> return agentErrorResult err
    }

    let private nodesForRequest (graph: Graph) (graphIds: NodeId list) =
        graphIds
        |> List.choose (fun id -> Map.tryFind id graph.nodes)

    let private latestEventId (events: Ev list) persistId =
        events
        |> List.map (fun e -> e.id)
        |> List.fold EventId.max persistId

    let private universalResponseFromHandle
        (handle: CoreChangesDoor)
        (after: EventId)
        (nodes: Node list)
        : Async<UniversalResponse> =
        async {
            let! events = CoreDoor.getEventsSince handle after
            let! persistId = CoreDoor.getEventId handle
            return
                { nodes = nodes
                  events = events
                  latestId = latestEventId events persistId }
        }

    let private loadSaveSuccessResult path command =
        { path = path; command = command }
        |> ApiResponseSerialization.encodeLoadSaveCommandResponse
        |> Encode.toString 0
        |> jsonResult

    let postLoadSaveCommand
        (router: LoadSaveCommandRouter)
        (handle: CoreChangesDoor)
        (body: string)
        : Async<IResult> =
        async {
            match Decode.fromString EventJson.decodeLoadSaveCommandRequest body with
            | Error err ->
                return agentErrorResult $"Invalid JSON: {err}"
            | Ok request ->
                match! CoreDoor.getState handle with
                | Error err -> return agentErrorResult err
                | Ok state ->
                    match router.resolvePath state request with
                    | Error err -> return agentErrorResult err
                    | Ok path ->
                        match! router.startCommand path request with
                        | Error err -> return agentErrorResult err
                        | Ok () when path = LoadSavePath.Desk ->
                            return
                                loadSaveSuccessResult
                                    LoadSavePath.Desk
                                    None
                        | Ok () ->
                            let nodes =
                                nodesForRequest state.graph request.start.graphIds
                            let! command =
                                universalResponseFromHandle
                                    handle
                                    request.start.eventId
                                    nodes
                            return
                                loadSaveSuccessResult
                                    LoadSavePath.Git
                                    (Some command)
        }

    let private remoteTipForLabel
        (dataDir: string)
        (graph: Graph)
        (nodeId: NodeId)
        (label: string)
        =
        if String.IsNullOrWhiteSpace label then
            None
        else
            match DocumentPersistPath.workspaceRootFor dataDir graph nodeId with
            | None -> None
            | Some root ->
                match WorkspaceGit.remoteTip root with
                | Ok (Some tip) -> Some (label, tip)
                | _ -> None

    let private workspaceRemoteRow
        (dataDir: string)
        (graph: Graph)
        (child: ChildNode)
        =
        if child.ref <> Ownership.Owner then
            None
        else
            match Map.tryFind child.id graph.nodes with
            | None -> None
            | Some node ->
                match node.kind with
                | Special Workspace ->
                    match Filename.tryValue node.name with
                    | Some label ->
                        remoteTipForLabel dataDir graph node.id label
                    | None -> None
                | _ -> None

    let private workspaceRemoteRows (dataDir: string) (graph: Graph) =
        GraphChildren.get graph Graph.workspacesId
        |> List.choose (workspaceRemoteRow dataDir graph)

    let getWorkspaceRemotes
        (dataDir: string)
        (handle: CoreChangesDoor)
        : Async<IResult> =
        async {
            match! CoreDoor.getState handle with
            | Error err -> return agentErrorResult err
            | Ok state ->
                let json =
                    Encode.toString
                        0
                        (WorkspaceRemoteTip.encodeList
                            (workspaceRemoteRows dataDir state.graph))
                return jsonResult json
        }

    let getCapabilities (dataDir: string) : IResult =
        let capabilities =
            { canGitSave = GitSave.isRepo dataDir
              canFileStatus = true }
        let json =
            Encode.toString 0 (ServerCapabilities.encode capabilities)
        jsonResult json

    let postFileStatus (dataDir: string) (body: string) : IResult =
        match Decode.fromString decodeFileStatusRequest body with
        | Error err -> Results.BadRequest({| error = err |})
        | Ok path ->
            match DocumentPersistPath.fileStatusForReference dataDir path with
            | Error err -> Results.BadRequest({| error = err |})
            | Ok response ->
                response
                |> Serialization.encodeDesktopFileStatusResponse
                |> Encode.toString 0
                |> jsonResult

    let getImportFile (dataDir: string) (path: string) : IResult =
        match DocumentPersistPath.importPackageForReference dataDir path with
        | Error err -> Results.BadRequest({| error = err |})
        | Ok package ->
            package
            |> Serialization.encodeDesktopImportPackage
            |> Encode.toString 0
            |> jsonResult

    let gitSave
        (prepare: unit -> Async<Result<int, string>>)
        (dataDir: string)
        (clientHint: string option)
        : Async<IResult> = async {
        if not (GitSave.isRepo dataDir) then
            let response: GitSaveResponse =
                { ok = false
                  detail = ""
                  error = Some "Git save is not enabled." }
            return jsonResult (Encode.toString 0 (GitSaveResponse.encode response))
        else
            let! prepResult = prepare ()
            match prepResult with
            | Error err ->
                let response: GitSaveResponse =
                    { ok = false; detail = ""; error = Some err }
                return Results.BadRequest(Encode.toString 0 (GitSaveResponse.encode response))
            | Ok rev ->
                let message =
                    ClientIdentity.formatCommitMessage
                        (sprintf "rev %d" rev)
                        clientHint
                match GitSave.commitAll dataDir message with
                | Ok detail ->
                    let response: GitSaveResponse =
                        { ok = true; detail = detail; error = None }
                    return jsonResult (Encode.toString 0 (GitSaveResponse.encode response))
                | Error err ->
                    let response: GitSaveResponse =
                        { ok = false; detail = ""; error = Some err }
                    return Results.BadRequest(Encode.toString 0 (GitSaveResponse.encode response))
    }

    [<Literal>]
    let inboundSecretHeader = "X-Ambit-Inbound-Secret"

    let checkInboundSecret configured provided =
        if String.IsNullOrEmpty configured then
            Error "closed"
        elif provided <> configured then
            Error "unauthorized"
        else
            Ok()

    let private decodeDeliverBody body =
        let decoder =
            Thoth.Json.Core.Decode.object (fun get ->
                {| sessionId =
                    get.Required.Field
                        "sessionId"
                        Thoth.Json.Core.Decode.string
                   text =
                    get.Required.Field
                        "text"
                        Thoth.Json.Core.Decode.string |})
        Decode.fromString decoder body

    /// Search door. `changes` reads State. Start and stop hit the event source.
    type SearchActorDoor =
        { changes: CoreChangesDoor
          recordStart:
            ActorStart -> Async<Result<unit, string>>
          recordStop: NodeId -> Async<unit> }

    let private searchReply
        (door: SearchActorDoor)
        (request: SearchPicture.Request)
        (state: State)
        : Async<IResult> =
        async {
            let start =
                SearchActor.actorStart state.graph state.eventId
            match! door.recordStart start with
            | Error err -> return agentErrorResult err
            | Ok () ->
                let answer =
                    SearchActor.reply
                        (fun () -> state.graph)
                        request
                do! door.recordStop start.zoomId
                return
                    answer
                    |> SearchPicture.encodeReply
                    |> Encode.toString 0
                    |> jsonResult
        }

    /// One Find and Move reply. ActorStart is recorded, then the walk, then ActorStop.
    let postSearch (door: SearchActorDoor) (body: string) : Async<IResult> =
        async {
            match Decode.fromString SearchPicture.decodeRequest body with
            | Error err ->
                return agentErrorResult $"Invalid JSON: {err}"
            | Ok request ->
                match! CoreDoor.getState door.changes with
                | Error err -> return agentErrorResult err
                | Ok state -> return! searchReply door request state
        }

    let postActorsDeliver
        (configuredSecret: string)
        (providedSecret: string)
        (deliver: string * string -> Result<unit, string>)
        (body: string)
        : IResult =
        match checkInboundSecret configuredSecret providedSecret with
        | Error _ -> Results.Unauthorized()
        | Ok() ->
            match decodeDeliverBody body with
            | Error _ ->
                Results.BadRequest({| error = "invalid deliver body" |})
            | Ok parsed ->
                match deliver (parsed.sessionId, parsed.text) with
                | Error _ -> Results.NotFound()
                | Ok() ->
                    match
                        GrokBotRunner.deliver
                            parsed.sessionId
                            parsed.text
                    with
                    | Error _ ->
                        Results.StatusCode 500
                    | Ok() -> Results.Ok()

namespace Gambol.Server

open System
open Microsoft.AspNetCore.Http
open Gambol.Shared
open Thoth.Json.Newtonsoft

/// Thin abstraction over FileAgent and DbAgent so Api functions are backend-agnostic.
type AgentHandle =
    { getState        : unit -> Async<Result<StateResponse, string>>
      getRevision     : unit -> Async<int>
      getChangesSince : int -> Async<Change list>
      isReady         : unit -> bool
      postChange      : string -> Async<Result<string, string>>
      postGraphOnlyChange : string -> Async<Result<string, string>> }

[<RequireQualifiedAccess>]
module AgentHandle =
    let ofFile (agent: FileAgent) : AgentHandle =
        { getState        = fun () -> FileAgent.tryGetState agent
          getRevision     = fun () -> FileAgent.getRevision agent
          getChangesSince = fun after -> FileAgent.getChangesSince agent after
          isReady         = fun () -> true
          postChange      = fun body -> FileAgent.postChange agent body
          postGraphOnlyChange =
            fun body -> FileAgent.postGraphOnlyChange agent body }

    let ofDb (agent: DbAgent) : AgentHandle =
        { getState        = fun () -> DbAgent.tryGetState agent
          getRevision     = fun () -> DbAgent.getRevision agent
          getChangesSince = fun after -> DbAgent.getChangesSince agent after
          isReady         = fun () -> DbAgent.isReady agent
          postChange      = fun body -> DbAgent.postChange agent body
          postGraphOnlyChange =
            fun body -> DbAgent.postGraphOnlyChange agent body }

    let readOnly (handle: AgentHandle) : AgentHandle =
        let rejectWrite (_: string) : Async<Result<string, string>> =
            async.Return(Error "Database persistence is unavailable; file fallback is read-only.")

        { handle with
            postChange = rejectWrite
            postGraphOnlyChange = rejectWrite }

    /// On-disk document is authoritative; when `db` is present, each successful file `postChange`
    /// is mirrored to PostgreSQL (best-effort log on DB failure; response still reflects file ack).
    let ofFileWithDbMirror (file: FileAgent) (db: DbAgent option) : AgentHandle =
        { getState        = fun () -> FileAgent.tryGetState file
          getRevision     = fun () -> FileAgent.getRevision file
          getChangesSince = fun after -> FileAgent.getChangesSince file after
          isReady         = fun () -> true
          postChange      =
            fun body -> async {
                let! fileResult = FileAgent.postChange file body

                match fileResult, db with
                | Ok ackJson, Some dbAgent ->
                    let! dbResult = DbAgent.postChange dbAgent body

                    match dbResult with
                    | Error err -> eprintfn "[Api] Secondary DB write failed after file persist: %s" err
                    | Ok _ -> ()

                    return Ok ackJson
                | Ok ackJson, None -> return Ok ackJson
                | Error err, _ -> return Error err
            }
          postGraphOnlyChange =
            fun body -> async {
                let! fileResult = FileAgent.postGraphOnlyChange file body

                match fileResult, db with
                | Ok ackJson, Some dbAgent ->
                    let! dbResult = DbAgent.postGraphOnlyChange dbAgent body

                    match dbResult with
                    | Error err ->
                        eprintfn
                            "[Api] Secondary DB graph-only write failed: %s"
                            err
                    | Ok _ -> ()

                    return Ok ackJson
                | Ok ackJson, None -> return Ok ackJson
                | Error err, _ -> return Error err
            } }

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
        if error.StartsWith("Internal server error", StringComparison.Ordinal) then
            Results.Content(error, "text/plain; charset=utf-8", statusCode = 500)
        else
            Results.BadRequest({| error = error |})

    let private internalError (detail: string) : IResult =
        Results.Content(detail, "text/plain; charset=utf-8", statusCode = 500)

    let private decodeFileStatusRequest =
        Thoth.Json.Core.Decode.object (fun get ->
            get.Required.Field "path" Thoth.Json.Core.Decode.string)

    let getPoll
        (handle: AgentHandle)
        (buildEpochSec: int)
        (pageBuildEpochSec: int)
        (clientRev: int)
        : Async<IResult> = async {
        let! rev = handle.getRevision ()
        let! changes =
            if rev > clientRev then handle.getChangesSince clientRev
            else async.Return []
        let! bootstrapHash =
            if rev <> clientRev then
                async.Return None
            else
                async {
                    match! handle.getState () with
                    | Ok state ->
                        return Some (BootCache.graphFingerprint state.graph)
                    | Error _ -> return None
                }
        let poll: ChangeSuccessResponse =
            { revision = Revision rev
              buildEpochSec = buildEpochSec
              pageBuildEpochSec = pageBuildEpochSec
              isReady = handle.isReady ()
              externalChanges = not changes.IsEmpty
              changes = changes
              message = None
              bootstrapHash = bootstrapHash }
        return changeSuccessResult poll
    }

    let private loadPackages
        (handle: AgentHandle)
        (targets: LoadTarget list)
        : Async<Result<Result<Node list, ResidentProjection.LoadRefuse>, string>> =
        async {
            match! handle.getState () with
            | Error err -> return Error err
            | Ok stateResponse ->
                return
                    Ok(
                        ResidentProjection.packagesForTargets
                            stateResponse.graph
                            targets)
        }

    let postLoad
        (handle: AgentHandle)
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
            match! loadPackages handle request.targets with
            | Error err -> return agentErrorResult err
            | Ok(Error ResidentProjection.LoadRefuse.MultiWorkspace) ->
                return
                    Results.BadRequest(
                        {| error =
                            "Load requires all selected targets in one Workspace" |})
            | Ok(Ok packages) ->
                let! rev = handle.getRevision ()
                let! changes =
                    if rev > request.revision then
                        handle.getChangesSince request.revision
                    else
                        async.Return []
                let load: LoadResponse =
                    { revision = rev
                      buildEpochSec = buildEpochSec
                      pageBuildEpochSec = pageBuildEpochSec
                      isReady = handle.isReady ()
                      changes = changes
                      packages = packages }
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

    let getState (handle: AgentHandle) (req: HttpRequest) : Async<IResult> = async {
        try
            let scope = parseBootstrapScope req
            let savedZoom = parseSavedZoom req
            let! result = handle.getState ()
            match result with
            | Ok response ->
                let scoped =
                    ResidentProjection.bootstrapStateResponse
                        scope
                        savedZoom
                        response
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

    let postChange
        (handle: AgentHandle)
        (buildEpochSec: int)
        (pageBuildEpochSec: int)
        (body: string)
        : Async<IResult> = async {
        let! result = handle.postChange body

        match result with
        | Ok json ->
            match
                Decode.fromString
                    ApiResponseSerialization.decodeChangeSuccessResponseDecoder
                    json
            with
            | Error err ->
                return
                    internalError
                        $"Internal server error in PostChange decode: {err}"
            | Ok response ->
                return
                    changeSuccessResult
                        { response with
                            buildEpochSec = buildEpochSec
                            pageBuildEpochSec = pageBuildEpochSec
                            isReady = handle.isReady () }
        | Error err -> return agentErrorResult err
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
            match DocumentPersistence.fileStatusForReference dataDir path with
            | Error err -> Results.BadRequest({| error = err |})
            | Ok response ->
                response
                |> Serialization.encodeDesktopFileStatusResponse
                |> Encode.toString 0
                |> jsonResult

    let getImportFile (dataDir: string) (path: string) : IResult =
        match DocumentPersistence.importPackageForReference dataDir path with
        | Error err -> Results.BadRequest({| error = err |})
        | Ok package ->
            package
            |> Serialization.encodeDesktopImportPackage
            |> Encode.toString 0
            |> jsonResult

    let private tryParseFileId (raw: string) : NodeId option =
        match Guid.TryParse raw with
        | true, guid -> Some(NodeId guid)
        | false, _ -> None

    let private encodeGraphOnlyChange revision ops =
        let change =
            { id = revision
              changeId = Guid.NewGuid()
              ops = ops }
        Encode.toString 0 (
            Serialization.encodeChangeBatch
                { changes = [ change ] })

    type private ParseFileBody =
        { fileId: string
          text: string option }

    let private decodeParseFileBody (json: string) : Result<ParseFileBody, string> =
        let decoder =
            Thoth.Json.Core.Decode.object (fun get ->
                { fileId = get.Required.Field "fileId" Thoth.Json.Core.Decode.string
                  text = get.Optional.Field "text" Thoth.Json.Core.Decode.string })
        Decode.fromString decoder json

    /// ParseFile command: optional body text or DataDir read → apply on agent graph.
    let postParseFile
        (handle: AgentHandle)
        (dataDir: string)
        (body: string)
        : Async<IResult> =
        async {
            match decodeParseFileBody body with
            | Error err ->
                return Results.BadRequest({| error = err |})
            | Ok payload ->
                match tryParseFileId payload.fileId with
                | None ->
                    return Results.BadRequest({| error = "fileId is invalid" |})
                | Some fileId ->
                    let! stateResult = handle.getState ()
                    match stateResult with
                    | Error err ->
                        return agentErrorResult err
                    | Ok stateResponse ->
                        match
                            DocumentPersistence.planParseFile
                                dataDir
                                stateResponse.graph
                                fileId
                                payload.text
                        with
                        | Error err ->
                            return Results.BadRequest({| error = err |})
                        | Ok [] ->
                            return jsonResult """{"ok":true}"""
                        | Ok ops ->
                            let! result =
                                handle.postGraphOnlyChange (
                                    encodeGraphOnlyChange
                                        stateResponse.revision.Value
                                        ops)
                            match result with
                            | Ok _ -> return jsonResult """{"ok":true}"""
                            | Error err -> return agentErrorResult err
        }

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

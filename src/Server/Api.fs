namespace Gambol.Server

open System
open Microsoft.AspNetCore.Http
open Gambol.Shared
open Thoth.Json.Newtonsoft

/// Thin abstraction over FileAgent and DbAgent so Api functions are backend-agnostic.
type AgentHandle =
    { getState        : unit -> Async<string>
      getRevision     : unit -> Async<int>
      getChangesSince : int -> Async<Change list>
      postChange      : string -> Async<Result<string, string>>
      postGraphOnlyChange : string -> Async<Result<string, string>> }

[<RequireQualifiedAccess>]
module AgentHandle =
    let ofFile (agent: FileAgent) : AgentHandle =
        { getState        = fun () -> FileAgent.getState agent
          getRevision     = fun () -> FileAgent.getRevision agent
          getChangesSince = fun after -> FileAgent.getChangesSince agent after
          postChange      = fun body -> FileAgent.postChange agent body
          postGraphOnlyChange =
            fun body -> FileAgent.postGraphOnlyChange agent body }

    let ofDb (agent: DbAgent) : AgentHandle =
        { getState        = fun () -> DbAgent.getState agent
          getRevision     = fun () -> DbAgent.getRevision agent
          getChangesSince = fun after -> DbAgent.getChangesSince agent after
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
        { getState        = fun () -> FileAgent.getState file
          getRevision     = fun () -> FileAgent.getRevision file
          getChangesSince = fun after -> FileAgent.getChangesSince file after
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
        let poll: PollResponse =
            { revision = rev
              buildEpochSec = buildEpochSec
              pageBuildEpochSec = pageBuildEpochSec
              changes = changes }
        let json = Encode.toString 0 (Serialization.encodePollResponse poll)
        return jsonResult json
    }

    let getState (handle: AgentHandle) : Async<IResult> = async {
        let! json = handle.getState ()
        return jsonResult json
    }

    let postChange (handle: AgentHandle) (body: string) : Async<IResult> = async {
        let! result = handle.postChange body

        match result with
        | Ok json -> return jsonResult json
        | Error err -> return Results.BadRequest({| error = err |})
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

    let private decodeGraphState (json: string) : Result<int * Graph, string> =
        let decoder =
            Thoth.Json.Core.Decode.object (fun get ->
                let revision =
                    get.Required.Field
                        "revision"
                        Serialization.decodeRevision
                let graph =
                    get.Required.Field "graph" Serialization.decodeGraph
                revision.Value, graph)
        Decode.fromString decoder json

    let private encodeGraphOnlyChange revision ops =
        let change =
            { id = revision
              changeId = Guid.NewGuid()
              ops = ops }
        Encode.toString 0 (
            Serialization.encodeChangeBatch { changes = [ change ] })

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
                    let! stateJson = handle.getState ()
                    match decodeGraphState stateJson with
                    | Error err ->
                        return Results.BadRequest({| error = err |})
                    | Ok(revision, graph) ->
                        match
                            DocumentPersistence.planParseFile
                                dataDir
                                graph
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
                                    encodeGraphOnlyChange revision ops)
                            match result with
                            | Ok _ -> return jsonResult """{"ok":true}"""
                            | Error err ->
                                return Results.BadRequest({| error = err |})
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

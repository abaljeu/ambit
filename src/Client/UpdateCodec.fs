module Gambol.Client.UpdateCodec

open Gambol.Shared
open Thoth.Json.Core

// ---------------------------------------------------------------------------
// Encoding / decoding helpers
// ---------------------------------------------------------------------------

/// Encode a batch as compact JSON for POST /{file}/changes.
let encodePendingBatchBody (changes: Change list) : string =
    let batch: ChangeBatch = { changes = changes }
    Thoth.Json.JavaScript.Encode.toString 0 (Serialization.encodeChangeBatch batch)


/// Decode the response from GET /{file}/state
let decodeStateResponse (text: string) : Result<Graph * Revision, string> =
    let decoder =
        Decode.object (fun get ->
            let g = get.Required.Field "graph" Serialization.decodeGraph
            let r = get.Required.Field "revision" Serialization.decodeRevision
            g, r)
    Thoth.Json.JavaScript.Decode.fromString decoder text

/// Decode the response from GET /{file}/capabilities.
let decodeServerCapabilities (text: string) : Result<ServerCapabilities, string> =
    Thoth.Json.JavaScript.Decode.fromString ServerCapabilities.decoder text

/// Decode POST /{file}/save response.
let decodeGitSaveResponse (text: string) : Result<GitSaveResponse, string> =
    Thoth.Json.JavaScript.Decode.fromString GitSaveResponse.decoder text

/// Decode GET /ambit/git-token.
let decodeGitTokenIssue (text: string) : Result<GitTokenIssue, string> =
    Thoth.Json.JavaScript.Decode.fromString GitTokenIssue.decoder text

/// Decode GET /ambit/git/reconciliation/latest → failure count.
let decodeReconciliationLatest (text: string) : Result<int, string> =
    let failureDecoder =
        Decode.object (fun get ->
            get.Required.Field
                "failures"
                (Decode.list (
                    Decode.object (fun g ->
                        g.Required.Field "path" Decode.string,
                        g.Required.Field "message" Decode.string))))
    match Thoth.Json.JavaScript.Decode.fromString failureDecoder text with
    | Ok failures -> Ok failures.Length
    | Error e -> Error e

/// Decode POST /ambit/git/reconciliation/directory → failure count.
let decodeReconciliationDirectory (text: string) : Result<int, string> =
    decodeReconciliationLatest text

let encodeReconciliationDirectoryRequest (workspace: string) (path: string) : string =
    Encode.object
        [ "workspace", Encode.string workspace
          "path", Encode.string path ]
    |> Thoth.Json.JavaScript.Encode.toString 0

/// Decode desktop `{ok,detail}` / error body.
let decodeDesktopGitOk (text: string) : Result<DesktopGitOkResponse, string> =
    Thoth.Json.JavaScript.Decode.fromString DesktopGitOkResponse.decoder text

/// Decode POST /_desktop/pick-folder.
let decodeDesktopPickFolder (text: string) : Result<DesktopPickFolderResponse, string> =
    Thoth.Json.JavaScript.Decode.fromString DesktopPickFolderResponse.decoder text

/// Decode POST /_desktop/git-status success body.
let decodeWorkspaceGitStatus (text: string) : Result<WorkspaceGitStatus, string> =
    Thoth.Json.JavaScript.Decode.fromString WorkspaceGitStatusJson.decoder text

/// Decode the response from GET /_desktop/capabilities.
let decodeDesktopCapabilities (text: string) : Result<DesktopCapabilities, string> =
    Thoth.Json.JavaScript.Decode.fromString DesktopCapabilities.decoder text

let encodeDesktopFileStatusRequest (path: string) : string =
    Encode.object [ "path", Encode.string path ]
    |> Thoth.Json.JavaScript.Encode.toString 0

let decodeDesktopFileStatusResponse (text: string) : Result<DesktopFileStatusResponse, string> =
    Thoth.Json.JavaScript.Decode.fromString Serialization.decodeDesktopFileStatusResponse text

let decodeDesktopImportPackage (text: string) : Result<DesktopImportPackage, string> =
    Thoth.Json.JavaScript.Decode.fromString Serialization.decodeDesktopImportPackage text

let encodeDesktopExportRequest (request: DesktopExportRequest) : string =
    Serialization.encodeDesktopExportRequest request
    |> Thoth.Json.JavaScript.Encode.toString 0

let decodeDesktopExportResponse (text: string) : Result<DesktopExportResponse, string> =
    Thoth.Json.JavaScript.Decode.fromString Serialization.decodeDesktopExportResponse text

/// Decode `{ "error": "..." }` from POST /{file}/changes 400 body.
let decodePostChangeError (text: string) : string option =
    let decoder =
        Decode.object (fun get -> get.Optional.Field "error" Decode.string)
    match Thoth.Json.JavaScript.Decode.fromString decoder text with
    | Ok (Some e) -> Some e
    | Ok None -> None
    | Error _ -> None

type ChangeAck =
    { ackedChangeIds: System.Guid list
      revision: Revision }

/// Decode POST /{file}/changes success body.
let decodeChangeAckResponse (text: string) : Result<ChangeAck, string> =
    Thoth.Json.JavaScript.Decode.fromString Serialization.decodeChangeBatchAck text
    |> Result.map (fun ack ->
        { ackedChangeIds = ack.ackedChangeIds
          revision = ack.revision })


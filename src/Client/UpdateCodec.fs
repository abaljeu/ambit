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

/// Decode GET /ambit/workspace/reconciliation/latest → failure count.
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

/// Decode POST /ambit/workspace/reconciliation/directory → failure count.
let decodeReconciliationDirectory (text: string) : Result<int, string> =
    decodeReconciliationLatest text

let encodeReconciliationDirectoryRequest (workspace: string) (path: string) : string =
    Encode.object
        [ "workspace", Encode.string workspace
          "path", Encode.string path ]
    |> Thoth.Json.JavaScript.Encode.toString 0

let encodeReconciliationAddedRequest
    (workspace: string)
    (paths: string list)
    : string =
    Encode.object
        [ "workspace", Encode.string workspace
          "paths", Encode.list (List.map Encode.string paths) ]
    |> Thoth.Json.JavaScript.Encode.toString 0

let encodeWorkspaceInventoryRequest
    (label: string)
    (relative: string)
    : string =
    Encode.object
        [ "label", Encode.string label
          "relative", Encode.string relative ]
    |> Thoth.Json.JavaScript.Encode.toString 0

type DesktopInventoryItem =
    { relative: string
      isDirectory: bool }

/// Decode POST /_desktop/workspace-inventory → depth-1 items.
let decodeDesktopInventory
    (text: string)
    : Result<DesktopInventoryItem list, string> =
    let itemDecoder =
        Decode.object (fun get ->
            { relative = get.Required.Field "relative" Decode.string
              isDirectory = get.Required.Field "isDirectory" Decode.bool })
    Thoth.Json.JavaScript.Decode.fromString (Decode.list itemDecoder) text

/// Decode POST /_desktop/workspace-push|pull.
let decodeDesktopWorkspaceSync
    (text: string)
    : Result<DesktopWorkspaceSyncResponse, string> =
    Thoth.Json.JavaScript.Decode.fromString
        DesktopWorkspaceSyncResponse.decoder
        text

/// Decode POST /_desktop/pick-folder.
let decodeDesktopPickFolder (text: string) : Result<DesktopPickFolderResponse, string> =
    Thoth.Json.JavaScript.Decode.fromString DesktopPickFolderResponse.decoder text

/// GET /_desktop/workspace-mappings → optional root for label.
let decodeMappedRootPath
    (text: string)
    (label: string)
    : Result<string option, string> =
    let entryDecoder =
        Decode.object (fun get ->
            get.Required.Field "label" Decode.string,
            get.Required.Field "path" Decode.string)
    let decoder =
        Decode.object (fun get ->
            get.Optional.Field
                "workspaceMappings"
                (Decode.list entryDecoder)
            |> Option.defaultValue [])
    match Thoth.Json.JavaScript.Decode.fromString decoder text with
    | Error e -> Error e
    | Ok entries ->
        entries
        |> List.tryFind (fun (l, _) ->
            System.String.Equals(
                l,
                label,
                System.StringComparison.OrdinalIgnoreCase))
        |> Option.map snd
        |> Ok

/// GET /_desktop/workspace-mappings → all mapped labels.
let decodeMappedWorkspaceLabels (text: string) : Result<Set<string>, string> =
    let entryDecoder =
        Decode.object (fun get ->
            get.Required.Field "label" Decode.string)
    let decoder =
        Decode.object (fun get ->
            get.Optional.Field
                "workspaceMappings"
                (Decode.list entryDecoder)
            |> Option.defaultValue [])
    match Thoth.Json.JavaScript.Decode.fromString decoder text with
    | Error e -> Error e
    | Ok labels -> Ok(Set.ofList labels)

let encodeWorkspaceSyncLedgerRequest (label: string) : string =
    Encode.object [ "label", Encode.string label ]
    |> Thoth.Json.JavaScript.Encode.toString 0

let private parseUtcOption (text: string option) =
    match text with
    | None -> Ok None
    | Some s ->
        match System.DateTime.TryParse s with
        | true, dt -> Ok(Some(dt.ToUniversalTime()))
        | _ -> Error("invalid datetime: " + s)

let private decodeSyncPathFact : Decoder<WorkspaceSyncPathFact> =
    Decode.object (fun get ->
        let relative = get.Required.Field "relative" Decode.string
        let isDirectory = get.Required.Field "isDirectory" Decode.bool
        let presenceText = get.Required.Field "presence" Decode.string
        let localText = get.Optional.Field "localMtimeUtc" Decode.string
        let serverText = get.Optional.Field "serverMtimeUtc" Decode.string
        relative, isDirectory, presenceText, localText, serverText)
    |> Decode.andThen (fun (relative, isDirectory, presenceText, localText, serverText) ->
        match WorkspacePathPresence.ofLedgerString presenceText with
        | None -> Decode.fail ("unknown presence: " + presenceText)
        | Some presence ->
            match parseUtcOption localText, parseUtcOption serverText with
            | Error e, _
            | _, Error e -> Decode.fail e
            | Ok localM, Ok serverM ->
                Decode.succeed
                    { relative = relative
                      isDirectory = isDirectory
                      presence = presence
                      localMtimeUtc = localM
                      serverMtimeUtc = serverM })

type WorkspaceSyncLedgerResponse =
    { label: string
      mapped: bool
      rows: WorkspaceSyncPathFact list }

let decodeWorkspaceSyncLedgerResponse
    (text: string)
    : Result<WorkspaceSyncLedgerResponse, string> =
    let decoder =
        Decode.object (fun get ->
            { label = get.Required.Field "label" Decode.string
              mapped = get.Required.Field "mapped" Decode.bool
              rows =
                  get.Required.Field
                      "rows"
                      (Decode.list decodeSyncPathFact) })
    Thoth.Json.JavaScript.Decode.fromString decoder text

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

let decodeDesktopFileContent (text: string) : Result<string, string> =
    let decoder =
        Decode.object (fun get ->
            get.Required.Field "content" Decode.string)
    Thoth.Json.JavaScript.Decode.fromString decoder text

let encodeParseFileRequest (fileId: NodeId) (text: string option) : string =
    let fields =
        [ "fileId", Encode.guid fileId.Value ]
        @ match text with
          | Some t -> [ "text", Encode.string t ]
          | None -> []
    Encode.object fields
    |> Thoth.Json.JavaScript.Encode.toString 0

let decodeParseFileOk (text: string) : Result<unit, string> =
    let decoder =
        Decode.object (fun get ->
            get.Required.Field "ok" Decode.bool)
    match Thoth.Json.JavaScript.Decode.fromString decoder text with
    | Ok true -> Ok ()
    | Ok false -> Error "parse was not acknowledged"
    | Error err -> Error err

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
      revision: Revision
      stampOps: Op list }

/// Decode POST /{file}/changes success body.
let decodeChangeAckResponse (text: string) : Result<ChangeAck, string> =
    Thoth.Json.JavaScript.Decode.fromString Serialization.decodeChangeBatchAck text
    |> Result.map (fun ack ->
        { ackedChangeIds = ack.ackedChangeIds
          revision = ack.revision
          stampOps = ack.stampOps })


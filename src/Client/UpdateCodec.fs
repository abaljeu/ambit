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


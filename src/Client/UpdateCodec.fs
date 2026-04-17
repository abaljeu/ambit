module Gambol.Client.UpdateCodec

open Gambol.Shared
open Thoth.Json.Core

// ---------------------------------------------------------------------------
// Encoding / decoding helpers
// ---------------------------------------------------------------------------

/// Encode a Change as compact JSON for POST /{file}/changes
let encodeChangeBody (change: Change) : string =
    Thoth.Json.JavaScript.Encode.toString 0 (Serialization.encodeChange change)


/// Decode the response from GET /{file}/state
let decodeStateResponse (text: string) : Result<Graph * Revision, string> =
    let decoder =
        Decode.object (fun get ->
            let g = get.Required.Field "graph" Serialization.decodeGraph
            let r = get.Required.Field "revision" Serialization.decodeRevision
            g, r)
    Thoth.Json.JavaScript.Decode.fromString decoder text

/// Decode `{ "error": "..." }` from POST /{file}/changes 400 body.
let decodePostChangeError (text: string) : string option =
    let decoder =
        Decode.object (fun get -> get.Optional.Field "error" Decode.string)
    match Thoth.Json.JavaScript.Decode.fromString decoder text with
    | Ok (Some e) -> Some e
    | Ok None -> None
    | Error _ -> None

type ChangeAck =
    { ackChangeId: System.Guid
      revision: Revision }

/// Decode POST /{file}/changes success body.
let decodeChangeAckResponse (text: string) : Result<ChangeAck, string> =
    let decoder =
        Decode.object (fun get ->
            let ack = get.Required.Field "ackChangeId" Decode.guid
            let rev = get.Required.Field "revision" Serialization.decodeRevision
            { ackChangeId = ack; revision = rev })
    Thoth.Json.JavaScript.Decode.fromString decoder text


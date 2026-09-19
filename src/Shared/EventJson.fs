namespace Gambol.Shared

open Thoth.Json.Core
open Thoth.Json.JavaScript
open Gambol.Shared

[<RequireQualifiedAccess>]
module EventJson =
    let encodeEventId eventId = Encode.int (EventId.toJson eventId)

    let decodeEventId: Decoder<EventId> =
        Decode.int |> Decode.map EventId.fromJson

    let private encodeAuthority (Authority name) = Encode.string name

    let private decodeAuthority: Decoder<Authority> =
        Decode.string |> Decode.map Authority

    let private encodeActorResult =
        function
        | ActorSucceeded -> Encode.string "succeeded"
        | ActorFailed -> Encode.string "failed"
        | ActorCancelled -> Encode.string "cancelled"

    let private decodeActorResult: Decoder<ActorResult> =
        Decode.string
        |> Decode.andThen (function
            | "succeeded" -> Decode.succeed ActorSucceeded
            | "failed" -> Decode.succeed ActorFailed
            | "cancelled" -> Decode.succeed ActorCancelled
            | other -> Decode.fail ("Unknown actor result: " + other))

    let private encodeOps ops =
        ops |> List.map Serialization.encodeOp |> Encode.list

    let private decodeOps = Decode.list Serialization.decodeOp

    let private startRequestFields (start: ActorStart) =
        [ "zoomId", Serialization.encodeNodeId start.zoomId
          "focusId", Serialization.encodeNodeId start.focusId
          "commandId", Serialization.encodeNodeId start.commandId
          "graphIds",
          start.graphIds
          |> List.map Serialization.encodeNodeId
          |> Encode.list
          "eventId", encodeEventId start.eventId ]

    let encodeStartRequest (start: ActorStart) =
        Encode.object (startRequestFields start)

    let private encodeActorStart (start: ActorStart) =
        Encode.object (
            ("kind", Encode.string "actorStart")
            :: startRequestFields start)

    let decodeStartRequest: Decoder<ActorStart> =
        Decode.object (fun get ->
            { zoomId = get.Required.Field "zoomId" Serialization.decodeNodeId
              focusId = get.Required.Field "focusId" Serialization.decodeNodeId
              commandId =
                get.Required.Field "commandId" Serialization.decodeNodeId
              graphIds =
                get.Required.Field
                    "graphIds"
                    (Decode.list Serialization.decodeNodeId)
              eventId = get.Required.Field "eventId" decodeEventId })

    let encodeCancelRequest (focusId: NodeId) =
        Encode.object
            [ "focusId", Serialization.encodeNodeId focusId ]

    let decodeCancelRequest: Decoder<NodeId> =
        Decode.object (fun get ->
            get.Required.Field "focusId" Serialization.decodeNodeId)

    let private encodeBody (body: EventBody) =
        match body with
        | EventBody.Change ops ->
            Encode.object
                [ "kind", Encode.string "change"
                  "ops", encodeOps ops ]
        | EventBody.Undo(target, ops) ->
            Encode.object
                [ "kind", Encode.string "undo"
                  "target", encodeEventId target
                  "ops", encodeOps ops ]
        | EventBody.Redo(target, ops) ->
            Encode.object
                [ "kind", Encode.string "redo"
                  "target", encodeEventId target
                  "ops", encodeOps ops ]
        | EventBody.ActorStart start -> encodeActorStart start
        | EventBody.ActorStop(focusId, result) ->
            Encode.object
                [ "kind", Encode.string "actorStop"
                  "focusId", Serialization.encodeNodeId focusId
                  "result", encodeActorResult result ]

    let private decodeChangeBody: Decoder<EventBody> =
        Decode.object (fun get ->
            EventBody.Change(get.Required.Field "ops" decodeOps))

    let private decodeUndoBody: Decoder<EventBody> =
        Decode.object (fun get ->
            EventBody.Undo(
                get.Required.Field "target" decodeEventId,
                get.Required.Field "ops" decodeOps))

    let private decodeRedoBody: Decoder<EventBody> =
        Decode.object (fun get ->
            EventBody.Redo(
                get.Required.Field "target" decodeEventId,
                get.Required.Field "ops" decodeOps))

    let private decodeActorStopBody: Decoder<EventBody> =
        Decode.object (fun get ->
            EventBody.ActorStop(
                get.Required.Field "focusId" Serialization.decodeNodeId,
                get.Required.Field "result" decodeActorResult))

    let private decodeBody: Decoder<EventBody> =
        Decode.field "kind" Decode.string
        |> Decode.andThen (fun kind ->
            match kind with
            | "change" -> decodeChangeBody
            | "undo" -> decodeUndoBody
            | "redo" -> decodeRedoBody
            | "actorStart" ->
                decodeStartRequest |> Decode.map EventBody.ActorStart
            | "actorStop" -> decodeActorStopBody
            | other -> Decode.fail ("Unknown event body: " + other))

    let encode (event: Ev) =
        let eventId, submissionId, authority, commandName, body =
            Ev.toJson event
        Encode.object
            [ "eventId", Encode.int eventId
              "submissionId", Encode.guid submissionId
              "authority", encodeAuthority authority
              "commandName", Encode.string commandName
              "body", encodeBody body ]

    let decode: Decoder<Ev> =
        Decode.object (fun get ->
            Ev.fromJson
                (get.Required.Field "eventId" Decode.int)
                (get.Required.Field "submissionId" Decode.guid)
                (get.Required.Field "authority" decodeAuthority)
                (get.Required.Field "commandName" Decode.string)
                (get.Required.Field "body" decodeBody))

    let encodeEventBatch (batch: EventBatch) : IEncodable =
        Encode.object
            [ "events", batch.events |> List.map encode |> Encode.list ]

    let decodeEventBatch: Decoder<EventBatch> =
        Decode.object (fun get ->
            { events = get.Required.Field "events" (Decode.list decode) })
        |> Decode.andThen (fun batch ->
            if batch.events.IsEmpty then
                Decode.fail "events must not be empty"
            else
                Decode.succeed batch)

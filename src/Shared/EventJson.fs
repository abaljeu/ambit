namespace Gambol.Shared.Events

open Thoth.Json.Core
open Thoth.Json.JavaScript
open Gambol.Shared

[<RequireQualifiedAccess>]
module EventJson =
    let encodeEventId (EventId n) = Encode.int n

    let decodeEventId: Decoder<EventId> =
        Decode.int |> Decode.map EventId

    let private encodeAuthority (Authority name) = Encode.string name

    let private decodeAuthority: Decoder<Authority> =
        Decode.string |> Decode.map Authority

    let private encodeActorResult =
        function
        | ActorSucceeded -> Encode.string "succeeded"
        | ActorFailed -> Encode.string "failed"

    let private decodeActorResult: Decoder<ActorResult> =
        Decode.string
        |> Decode.andThen (function
            | "succeeded" -> Decode.succeed ActorSucceeded
            | "failed" -> Decode.succeed ActorFailed
            | other -> Decode.fail ("Unknown actor result: " + other))

    let private encodeOps ops =
        ops |> List.map Serialization.encodeOp |> Encode.list

    let private decodeOps = Decode.list Serialization.decodeOp

    let private encodeActorStart (start: ActorStart) =
        Encode.object
            [ "kind", Encode.string "actorStart"
              "zoomId", Serialization.encodeNodeId start.zoomId
              "focusId", Serialization.encodeNodeId start.focusId
              "commandId", Serialization.encodeNodeId start.commandId
              "graphIds",
              start.graphIds
              |> List.map Serialization.encodeNodeId
              |> Encode.list
              "revision", encodeEventId start.revision ]

    let private decodeActorStart: Decoder<ActorStart> =
        Decode.object (fun get ->
            { zoomId = get.Required.Field "zoomId" Serialization.decodeNodeId
              focusId = get.Required.Field "focusId" Serialization.decodeNodeId
              commandId =
                get.Required.Field "commandId" Serialization.decodeNodeId
              graphIds =
                get.Required.Field
                    "graphIds"
                    (Decode.list Serialization.decodeNodeId)
              revision = get.Required.Field "revision" decodeEventId })

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
                decodeActorStart |> Decode.map EventBody.ActorStart
            | "actorStop" -> decodeActorStopBody
            | other -> Decode.fail ("Unknown event body: " + other))

    let encode (event: Event) =
        Encode.object
            [ "id", encodeEventId event.id
              "submissionId", Encode.guid event.submissionId
              "authority", encodeAuthority event.authority
              "commandName", Encode.string event.commandName
              "body", encodeBody event.body ]

    let decode: Decoder<Event> =
        Decode.object (fun get ->
            { id = get.Required.Field "id" decodeEventId
              submissionId = get.Required.Field "submissionId" Decode.guid
              authority = get.Required.Field "authority" decodeAuthority
              commandName = get.Required.Field "commandName" Decode.string
              body = get.Required.Field "body" decodeBody })

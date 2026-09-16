namespace Gambol.Shared

open Gambol.Shared.Events
open Thoth.Json.Core
open Thoth.Json.JavaScript

[<RequireQualifiedAccess>]
module ApiResponseSerialization =

    let encodeStateResponse (response: StateResponse) : IEncodable =
        Encode.object
            [ "revision", Gambol.Shared.Events.EventJson.encodeEventId response.revision
              "graph", Serialization.encodeGraph response.graph
              "ready", Encode.bool response.isReady ]

    let decodeStateResponseDecoder: Decoder<StateResponse> =
        Decode.object (fun get ->
            { revision =
                get.Required.Field "revision" Gambol.Shared.Events.EventJson.decodeEventId
              graph = get.Required.Field "graph" Serialization.decodeGraph
              isReady =
                get.Optional.Field "ready" Decode.bool
                |> Option.defaultValue true })

    let decodeStateResponse text =
        Decode.fromString decodeStateResponseDecoder text

    let encodeChangeSuccessResponse
        (response: ChangeSuccessResponse)
        : IEncodable =
        Encode.object (
            [ "r", Gambol.Shared.Events.EventJson.encodeEventId response.revision
              "b", Encode.int response.buildEpochSec
              "p", Encode.int response.pageBuildEpochSec
              "v", Encode.int response.apiVersion
              "ready", Encode.bool response.isReady
              "externalChanges", Encode.bool response.externalChanges
              "c",
                response.events
                |> List.map Gambol.Shared.Events.EventJson.encode
                |> Encode.list ]
            @ match response.message with
              | None -> []
              | Some message -> [ "message", Encode.string message ]
            @ match response.bootstrapHash with
              | None -> []
              | Some hash -> [ "bootstrapHash", Encode.string hash ])

    let decodeChangeSuccessResponseDecoder: Decoder<ChangeSuccessResponse> =
        Decode.object (fun get ->
            { revision =
                get.Required.Field "r" Gambol.Shared.Events.EventJson.decodeEventId
              buildEpochSec = get.Required.Field "b" Decode.int
              pageBuildEpochSec = get.Required.Field "p" Decode.int
              apiVersion =
                get.Optional.Field "v" Decode.int
                |> Option.defaultValue 0
              isReady = get.Required.Field "ready" Decode.bool
              externalChanges =
                get.Required.Field "externalChanges" Decode.bool
              events =
                get.Required.Field
                    "c"
                    (Decode.list Gambol.Shared.Events.EventJson.decode)
              message = get.Optional.Field "message" Decode.string
              bootstrapHash =
                get.Optional.Field "bootstrapHash" Decode.string })

    let decodeChangeSuccessResponse text =
        Decode.fromString decodeChangeSuccessResponseDecoder text

    let encodeLoadTarget (target: LoadTarget) : IEncodable =
        Encode.object
            [ "targetId", Serialization.encodeNodeId target.targetId
              "includeWorkspace", Encode.bool target.includeWorkspace ]

    let decodeLoadTargetDecoder: Decoder<LoadTarget> =
        Decode.object (fun get ->
            { targetId = get.Required.Field "targetId" Serialization.decodeNodeId
              includeWorkspace =
                get.Required.Field "includeWorkspace" Decode.bool })

    let encodeLoadRequest (request: LoadRequest) : IEncodable =
        let (EventId rev) = request.revision
        Encode.object
            [ "revision", Encode.int rev
              "targets",
                request.targets
                |> List.map encodeLoadTarget
                |> Encode.list ]

    let decodeLoadRequestDecoder: Decoder<LoadRequest> =
        Decode.object (fun get ->
            { revision = EventId (get.Required.Field "revision" Decode.int)
              targets =
                get.Required.Field
                    "targets"
                    (Decode.list decodeLoadTargetDecoder) })

    let decodeLoadRequest text =
        Decode.fromString decodeLoadRequestDecoder text

    let encodeLoadResponse (response: LoadResponse) : IEncodable =
        let (EventId rev) = response.revision
        Encode.object
            [ "r", Encode.int rev
              "b", Encode.int response.buildEpochSec
              "p", Encode.int response.pageBuildEpochSec
              "v", Encode.int response.apiVersion
              "ready", Encode.bool response.isReady
              "c",
                response.events
                |> List.map Gambol.Shared.Events.EventJson.encode
                |> Encode.list
              "packages",
                response.packages
                |> List.map Serialization.encodeNode
                |> Encode.list ]

    let decodeLoadResponseDecoder: Decoder<LoadResponse> =
        Decode.object (fun get ->
            { revision = EventId (get.Required.Field "r" Decode.int)
              buildEpochSec = get.Required.Field "b" Decode.int
              pageBuildEpochSec = get.Required.Field "p" Decode.int
              apiVersion =
                get.Optional.Field "v" Decode.int
                |> Option.defaultValue 0
              isReady =
                get.Optional.Field "ready" Decode.bool
                |> Option.defaultValue true
              events =
                get.Optional.Field
                    "c"
                    (Decode.list Gambol.Shared.Events.EventJson.decode)
                |> Option.defaultValue []
              packages =
                get.Optional.Field
                    "packages"
                    (Decode.list Serialization.decodeNode)
                |> Option.defaultValue [] })

    let decodeLoadResponse text =
        Decode.fromString decodeLoadResponseDecoder text

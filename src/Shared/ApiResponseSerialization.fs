namespace Gambol.Shared

open Thoth.Json.Core
open Thoth.Json.JavaScript

[<RequireQualifiedAccess>]
module ApiResponseSerialization =

    let encodeStateResponse (response: StateResponse) : IEncodable =
        Encode.object
            [ "revision", Serialization.encodeRevision response.revision
              "graph", Serialization.encodeGraph response.graph
              "ready", Encode.bool response.isReady ]

    let decodeStateResponseDecoder: Decoder<StateResponse> =
        Decode.object (fun get ->
            { revision =
                get.Required.Field "revision" Serialization.decodeRevision
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
            [ "r", Serialization.encodeRevision response.revision
              "b", Encode.int response.buildEpochSec
              "p", Encode.int response.pageBuildEpochSec
              "ready", Encode.bool response.isReady
              "externalChanges", Encode.bool response.externalChanges
              "c",
                response.changes
                |> List.map Serialization.encodeChange
                |> Encode.list ]
            @ match response.message with
              | None -> []
              | Some message -> [ "message", Encode.string message ])

    let decodeChangeSuccessResponseDecoder: Decoder<ChangeSuccessResponse> =
        Decode.object (fun get ->
            { revision =
                get.Required.Field "r" Serialization.decodeRevision
              buildEpochSec = get.Required.Field "b" Decode.int
              pageBuildEpochSec = get.Required.Field "p" Decode.int
              isReady = get.Required.Field "ready" Decode.bool
              externalChanges =
                get.Required.Field "externalChanges" Decode.bool
              changes =
                get.Required.Field
                    "c"
                    (Decode.list Serialization.decodeChange)
              message = get.Optional.Field "message" Decode.string })

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
        Encode.object
            [ "revision", Encode.int request.revision
              "targets",
                request.targets
                |> List.map encodeLoadTarget
                |> Encode.list ]

    let decodeLoadRequestDecoder: Decoder<LoadRequest> =
        Decode.object (fun get ->
            { revision = get.Required.Field "revision" Decode.int
              targets =
                get.Required.Field
                    "targets"
                    (Decode.list decodeLoadTargetDecoder) })

    let decodeLoadRequest text =
        Decode.fromString decodeLoadRequestDecoder text

    let encodeLoadResponse (response: LoadResponse) : IEncodable =
        Encode.object
            [ "r", Encode.int response.revision
              "b", Encode.int response.buildEpochSec
              "p", Encode.int response.pageBuildEpochSec
              "ready", Encode.bool response.isReady
              "c",
                response.changes
                |> List.map Serialization.encodeChange
                |> Encode.list
              "packages",
                response.packages
                |> List.map Serialization.encodeNode
                |> Encode.list ]

    let decodeLoadResponseDecoder: Decoder<LoadResponse> =
        Decode.object (fun get ->
            { revision = get.Required.Field "r" Decode.int
              buildEpochSec = get.Required.Field "b" Decode.int
              pageBuildEpochSec = get.Required.Field "p" Decode.int
              isReady =
                get.Optional.Field "ready" Decode.bool
                |> Option.defaultValue true
              changes =
                get.Optional.Field
                    "c"
                    (Decode.list Serialization.decodeChange)
                |> Option.defaultValue []
              packages =
                get.Optional.Field
                    "packages"
                    (Decode.list Serialization.decodeNode)
                |> Option.defaultValue [] })

    let decodeLoadResponse text =
        Decode.fromString decodeLoadResponseDecoder text

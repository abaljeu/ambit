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
        | ActorFailed _ -> Encode.string "failed"
        | ActorCancelled -> Encode.string "cancelled"
        | ActorQuery _ -> Encode.string "query"

    let private decodeActorResultTag: Decoder<string> =
        Decode.string
        |> Decode.andThen (function
            | ("succeeded" | "failed" | "cancelled" | "query") as tag ->
                Decode.succeed tag
            | other -> Decode.fail ("Unknown actor result: " + other))

    let private actorResultFrom tag message ids =
        match tag with
        | "succeeded" -> ActorSucceeded
        | "cancelled" -> ActorCancelled
        | "query" -> ActorQuery (Option.defaultValue [] ids)
        | _ -> ActorFailed (Option.defaultValue "" message)

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

    let private encodeLoadSaveOperation =
        function
        | LoadSaveOperation.Load -> Encode.string "load"
        | LoadSaveOperation.Save -> Encode.string "save"

    let private decodeLoadSaveOperation: Decoder<LoadSaveOperation> =
        Decode.string
        |> Decode.andThen (function
            | "load" -> Decode.succeed LoadSaveOperation.Load
            | "save" -> Decode.succeed LoadSaveOperation.Save
            | other -> Decode.fail ("Unknown Load/Save operation: " + other))

    let private encodeLoadSavePrePick =
        function
        | LoadSavePrePick.Plain -> Encode.string "plain"
        | LoadSavePrePick.Git -> Encode.string "git"
        | LoadSavePrePick.Desk -> Encode.string "desk"

    let private decodeLoadSavePrePick: Decoder<LoadSavePrePick> =
        Decode.string
        |> Decode.andThen (function
            | "plain" -> Decode.succeed LoadSavePrePick.Plain
            | "git" -> Decode.succeed LoadSavePrePick.Git
            | "desk" -> Decode.succeed LoadSavePrePick.Desk
            | other -> Decode.fail ("Unknown Load/Save pre-pick: " + other))

    let encodeLoadSaveCommandRequest (request: LoadSaveCommandRequest) =
        Encode.object
            [ "operation", encodeLoadSaveOperation request.operation
              "prePick", encodeLoadSavePrePick request.prePick
              "holdStack", Encode.bool request.holdStack
              "start", encodeStartRequest request.start ]

    let decodeLoadSaveCommandRequest: Decoder<LoadSaveCommandRequest> =
        Decode.object (fun get ->
            { operation =
                get.Required.Field "operation" decodeLoadSaveOperation
              prePick =
                get.Required.Field "prePick" decodeLoadSavePrePick
              holdStack =
                get.Optional.Field "holdStack" Decode.bool
                |> Option.defaultValue false
              start = get.Required.Field "start" decodeStartRequest })

    let encodeCancelRequest (request: CancelRequest) =
        Encode.object
            [ "focusId", Serialization.encodeNodeId request.focusId
              "eventId", encodeEventId request.eventId ]

    let decodeCancelRequest: Decoder<CancelRequest> =
        Decode.object (fun get ->
            { focusId =
                get.Required.Field "focusId" Serialization.decodeNodeId
              eventId = get.Required.Field "eventId" decodeEventId })

    let private encodeNodeIds ids =
        ids |> List.map Serialization.encodeNodeId |> Encode.list

    let private encodeActorStop focusId result =
        let fields =
            [ "kind", Encode.string "actorStop"
              "focusId", Serialization.encodeNodeId focusId
              "result", encodeActorResult result ]
        match result with
        | ActorFailed message when message <> "" ->
            Encode.object (fields @ [ "message", Encode.string message ])
        | ActorQuery ids ->
            Encode.object (fields @ [ "ids", encodeNodeIds ids ])
        | ActorSucceeded
        | ActorFailed _
        | ActorCancelled -> Encode.object fields

    let private encodeSingleNotice tag nodeId =
        Encode.object
            [ "kind", Encode.string "notice"
              "notice", Encode.string tag
              "nodeId", Serialization.encodeNodeId nodeId ]

    let private encodeNodePersisted
        nodeId
        (persistedVersion: EventId)
        (updateTime: System.DateTime)
        =
        Encode.object
            [ "kind", Encode.string "notice"
              "notice", Encode.string "nodePersisted"
              "nodeId", Serialization.encodeNodeId nodeId
              "persistedVersion", Encode.int (EventId.toJson persistedVersion)
              "updateTime", Encode.int64 updateTime.Ticks ]

    let private encodeFailedNotice tag nodeId message =
        Encode.object
            [ "kind", Encode.string "notice"
              "notice", Encode.string tag
              "nodeId", Serialization.encodeNodeId nodeId
              "message", Encode.string message ]

    let private encodeNotice (notice: Notice) =
        match notice with
        | Notice.NodeParsed nodeId ->
            encodeSingleNotice "nodeParsed" nodeId
        | Notice.NodePersisted (nodeId, persistedVersion, updateTime) ->
            encodeNodePersisted nodeId persistedVersion updateTime
        | Notice.NodeUnparsed nodeId ->
            encodeSingleNotice "markUnparsed" nodeId
        | Notice.NodeUploading nodeId ->
            encodeSingleNotice "nodeUploading" nodeId
        | Notice.NodeReceived nodeId ->
            encodeSingleNotice "nodeReceived" nodeId
        | Notice.NodeParsing nodeId ->
            encodeSingleNotice "nodeParsing" nodeId
        | Notice.NodePersisting nodeId ->
            encodeSingleNotice "nodePersisting" nodeId
        | Notice.NodeUnpersisted nodeId ->
            encodeSingleNotice "nodeUnpersisted" nodeId
        | Notice.PersistFailed (nodeId, message) ->
            encodeFailedNotice "persistFailed" nodeId message
        | Notice.ParseFailed (nodeId, message) ->
            encodeFailedNotice "parseFailed" nodeId message

    let private noticeFrom tag nodeId =
        match tag with
        | "nodeParsed" -> Ok (Notice.NodeParsed nodeId)
        | "markUnparsed" -> Ok (Notice.NodeUnparsed nodeId)
        | "nodeUploading" -> Ok (Notice.NodeUploading nodeId)
        | "nodeReceived" -> Ok (Notice.NodeReceived nodeId)
        | "nodeParsing" -> Ok (Notice.NodeParsing nodeId)
        | "nodePersisting" -> Ok (Notice.NodePersisting nodeId)
        | "nodeUnpersisted" -> Ok (Notice.NodeUnpersisted nodeId)
        | other -> Error ("Unknown notice: " + other)

    let private decodeSingleNotice: Decoder<Notice> =
        Decode.object (fun get ->
            noticeFrom
                (get.Required.Field "notice" Decode.string)
                (get.Required.Field "nodeId" Serialization.decodeNodeId))
        |> Decode.andThen (function
            | Ok notice -> Decode.succeed notice
            | Error message -> Decode.fail message)

    let private decodePersistedVersion (get: Decode.IGetters) =
        get.Optional.Field "persistedVersion" Decode.int
        |> Option.map EventId.fromJson
        |> Option.defaultValue EventId.zero

    let private decodeNodePersisted: Decoder<Notice> =
        Decode.object (fun get ->
            Notice.NodePersisted(
                get.Required.Field "nodeId" Serialization.decodeNodeId,
                decodePersistedVersion get,
                System.DateTime(
                    get.Required.Field "updateTime" Decode.int64,
                    System.DateTimeKind.Utc)))

    let private decodeFailedNotice make : Decoder<Notice> =
        Decode.object (fun get ->
            make (
                get.Required.Field "nodeId" Serialization.decodeNodeId,
                get.Required.Field "message" Decode.string))

    let private decodeNoticeBody: Decoder<EventBody> =
        Decode.field "notice" Decode.string
        |> Decode.andThen (fun tag ->
            match tag with
            | "nodePersisted"
            | "snapshotDone" ->
                decodeNodePersisted |> Decode.map EventBody.Notice
            | "persistFailed" ->
                decodeFailedNotice Notice.PersistFailed
                |> Decode.map EventBody.Notice
            | "parseFailed" ->
                decodeFailedNotice Notice.ParseFailed
                |> Decode.map EventBody.Notice
            | _ ->
                decodeSingleNotice |> Decode.map EventBody.Notice)

    let private encodeChangeOrigin (origin: ChangeOrigin) =
        match origin with
        | ChangeOrigin.Edit -> Encode.string "edit"
        | ChangeOrigin.Parse -> Encode.string "parse"

    let private decodeChangeOrigin: Decoder<ChangeOrigin> =
        Decode.string
        |> Decode.andThen (function
            | "edit" -> Decode.succeed ChangeOrigin.Edit
            | "parse" -> Decode.succeed ChangeOrigin.Parse
            | other ->
                Decode.fail ("Unknown change origin: " + other))

    let private encodeLoadSavePath (path: LoadSavePath) =
        match path with
        | LoadSavePath.Git -> Encode.string "git"
        | LoadSavePath.Desk -> Encode.string "desk"

    let private decodeLoadSavePath: Decoder<LoadSavePath> =
        Decode.string
        |> Decode.andThen (function
            | "git" -> Decode.succeed LoadSavePath.Git
            | "desk" -> Decode.succeed LoadSavePath.Desk
            | other ->
                Decode.fail ("Unknown load/save path: " + other))

    let private encodePeerActorName (PeerActorName name) =
        Encode.string name

    let private decodePeerActorName: Decoder<PeerActorName> =
        Decode.string |> Decode.map PeerActorName

    let private encodeLoad nodeId =
        Encode.object
            [ "kind", Encode.string "load"
              "nodeId", Serialization.encodeNodeId nodeId ]

    let private encodeCancel focusId =
        Encode.object
            [ "kind", Encode.string "cancel"
              "focusId", Serialization.encodeNodeId focusId ]

    let private encodeOrder (order: Order) =
        match order with
        | Order.Change(ops, origin) ->
            Encode.object
                [ "kind", Encode.string "change"
                  "ops", encodeOps ops
                  "origin", encodeChangeOrigin origin ]
        | Order.Undo(target, ops) ->
            Encode.object
                [ "kind", Encode.string "undo"
                  "target", encodeEventId target
                  "ops", encodeOps ops ]
        | Order.Redo(target, ops) ->
            Encode.object
                [ "kind", Encode.string "redo"
                  "target", encodeEventId target
                  "ops", encodeOps ops ]
        | Order.StartActor start ->
            Encode.object
                [ "kind", Encode.string "startActor"
                  "start", encodeStartRequest start ]
        | Order.StartPeerActor(peerName, start) ->
            Encode.object
                [ "kind", Encode.string "startPeerActor"
                  "peerName", encodePeerActorName peerName
                  "start", encodeStartRequest start ]
        | Order.LoadSave(path, peerName, request) ->
            Encode.object
                [ "kind", Encode.string "loadSave"
                  "path", encodeLoadSavePath path
                  "peerName", encodePeerActorName peerName
                  "request",
                  encodeLoadSaveCommandRequest request ]
        | Order.Load nodeId -> encodeLoad nodeId
        | Order.Cancel focusId -> encodeCancel focusId

    let private decodeOrderChange: Decoder<Order> =
        Decode.object (fun get ->
            Order.Change(
                get.Required.Field "ops" decodeOps,
                get.Required.Field "origin" decodeChangeOrigin))

    let private decodeOrderUndo: Decoder<Order> =
        Decode.object (fun get ->
            Order.Undo(
                get.Required.Field "target" decodeEventId,
                get.Required.Field "ops" decodeOps))

    let private decodeOrderRedo: Decoder<Order> =
        Decode.object (fun get ->
            Order.Redo(
                get.Required.Field "target" decodeEventId,
                get.Required.Field "ops" decodeOps))

    let private decodeOrderStartActor: Decoder<Order> =
        Decode.object (fun get ->
            Order.StartActor(
                get.Required.Field "start" decodeStartRequest))

    let private decodeOrderStartPeerActor: Decoder<Order> =
        Decode.object (fun get ->
            Order.StartPeerActor(
                get.Required.Field "peerName" decodePeerActorName,
                get.Required.Field "start" decodeStartRequest))

    let private decodeOrderLoadSave: Decoder<Order> =
        Decode.object (fun get ->
            Order.LoadSave(
                get.Required.Field "path" decodeLoadSavePath,
                get.Required.Field "peerName" decodePeerActorName,
                get.Required.Field
                    "request"
                    decodeLoadSaveCommandRequest))

    let private decodeOrderLoad: Decoder<Order> =
        Decode.object (fun get ->
            Order.Load(
                get.Required.Field
                    "nodeId"
                    Serialization.decodeNodeId))

    let private decodeOrderCancel: Decoder<Order> =
        Decode.object (fun get ->
            Order.Cancel(
                get.Required.Field
                    "focusId"
                    Serialization.decodeNodeId))

    let private decodeOrder: Decoder<Order> =
        Decode.field "kind" Decode.string
        |> Decode.andThen (fun kind ->
            match kind with
            | "change" -> decodeOrderChange
            | "undo" -> decodeOrderUndo
            | "redo" -> decodeOrderRedo
            | "startActor" -> decodeOrderStartActor
            | "startPeerActor" -> decodeOrderStartPeerActor
            | "loadSave" -> decodeOrderLoadSave
            | "load" -> decodeOrderLoad
            | "cancel" -> decodeOrderCancel
            | other -> Decode.fail ("Unknown order: " + other))

    let private encodeBody (body: EventBody) =
        match body with
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
        | EventBody.Load nodeId -> encodeLoad nodeId
        | EventBody.Cancel focusId -> encodeCancel focusId
        | EventBody.ActorStop(focusId, result) ->
            encodeActorStop focusId result
        | EventBody.Notice notice -> encodeNotice notice
        | EventBody.Order order ->
            Encode.object
                [ "kind", Encode.string "order"
                  "order", encodeOrder order ]

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
                actorResultFrom
                    (get.Required.Field "result" decodeActorResultTag)
                    (get.Optional.Field "message" Decode.string)
                    (get.Optional.Field
                        "ids"
                        (Decode.list Serialization.decodeNodeId))))

    let private decodeBody: Decoder<EventBody> =
        Decode.field "kind" Decode.string
        |> Decode.andThen (fun kind ->
            match kind with
            | "undo" -> decodeUndoBody
            | "redo" -> decodeRedoBody
            | "actorStart" ->
                decodeStartRequest |> Decode.map EventBody.ActorStart
            | "load" ->
                Decode.object (fun get ->
                    EventBody.Load(
                        get.Required.Field
                            "nodeId"
                            Serialization.decodeNodeId))
            | "cancel" ->
                Decode.object (fun get ->
                    EventBody.Cancel(
                        get.Required.Field
                            "focusId"
                            Serialization.decodeNodeId))
            | "actorStop" -> decodeActorStopBody
            | "notice" -> decodeNoticeBody
            | "order" ->
                Decode.field "order" decodeOrder
                |> Decode.map EventBody.Order
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

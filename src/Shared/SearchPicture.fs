namespace Gambol.Shared

open Thoth.Json.Core
open Thoth.Json.JavaScript

/// Quiet gap, reply match, and the shared 200 cap for Find and Move.
module SearchPicture =

    type Match =
        | Text of string
        | Generation of int

    type Reply =
        { ids: NodeId list
          matchKey: Match }

    type Request =
        { text: string
          startId: NodeId
          generation: int option }

    type Gap =
        | Idle
        | Waiting of Match

    type private RawRequest =
        { text: string option
          generation: int option
          startId: NodeId }

    type private RawReply =
        { ids: NodeId list
          text: string option
          generation: int option }

    let matchKeyOf (request: Request) : Match =
        match request.generation with
        | Some generation -> Generation generation
        | None -> Text request.text

    let private searchable (key: Match) : bool =
        match key with
        | Text query ->
            ViewModelSearch.parseSearchTerm query |> Option.isSome
        | Generation _ -> true

    /// Text can still change. At the hit cap, do not arm a Start.
    let awaitGap (clientHitCount: int) (key: Match) : Gap =
        if clientHitCount >= ViewModelSearch.searchHitCap then
            Idle
        else
            Waiting key

    /// The gap ended. One Start when this text is still current.
    let startAfterGap
        (gap: Gap)
        (current: Match)
        (clientHitCount: int)
        : Match option =
        match gap with
        | Waiting key when
            key = current
            && searchable current
            && clientHitCount < ViewModelSearch.searchHitCap ->
            Some key
        | _ -> None

    let private roomFor (clientHitCount: int) : int =
        ViewModelSearch.searchHitCap - max 0 clientHitCount

    /// None when the reply is for other text or another generation.
    let applyReply
        (clientHitCount: int)
        (current: Match)
        (reply: Reply)
        : NodeId list option =
        if reply.matchKey <> current then
            None
        else
            let space = roomFor clientHitCount
            if space <= 0 then Some []
            else Some (List.truncate space reply.ids)

    /// A stale reply leaves the ids already kept.
    let keepReply
        (clientHitCount: int)
        (current: Match)
        (kept: NodeId list)
        (reply: Reply)
        : NodeId list =
        match applyReply clientHitCount current reply with
        | None -> kept
        | Some ids -> ids

    let encodeRequest (request: Request) =
        let generation =
            match request.generation with
            | None -> []
            | Some value -> [ "generation", Encode.int value ]
        Encode.object (
            [ "text", Encode.string request.text
              "startId", Serialization.encodeNodeId request.startId ]
            @ generation)

    let encodeReply (reply: Reply) =
        let ids =
            reply.ids
            |> List.map Serialization.encodeNodeId
            |> Encode.list
        match reply.matchKey with
        | Text text ->
            Encode.object
                [ "ids", ids
                  "text", Encode.string text ]
        | Generation generation ->
            Encode.object
                [ "ids", ids
                  "generation", Encode.int generation ]

    let decodeRequest: Decoder<Request> =
        Decode.object (fun get ->
            { text = get.Optional.Field "text" Decode.string
              generation = get.Optional.Field "generation" Decode.int
              startId =
                get.Required.Field "startId" Serialization.decodeNodeId })
        |> Decode.andThen (fun (raw: RawRequest) ->
            match raw.text with
            | Some text ->
                Decode.succeed
                    { text = text
                      startId = raw.startId
                      generation = raw.generation }
            | None -> Decode.fail "search text is required")

    let decodeReply: Decoder<Reply> =
        Decode.object (fun get ->
            { ids =
                get.Required.Field
                    "ids"
                    (Decode.list Serialization.decodeNodeId)
              text = get.Optional.Field "text" Decode.string
              generation = get.Optional.Field "generation" Decode.int })
        |> Decode.andThen (fun (raw: RawReply) ->
            match raw.generation, raw.text with
            | Some generation, _ ->
                Decode.succeed
                    { ids = raw.ids
                      matchKey = Generation generation }
            | None, Some text ->
                Decode.succeed
                    { ids = raw.ids
                      matchKey = Text text }
            | None, None ->
                Decode.fail "search reply needs text or generation")

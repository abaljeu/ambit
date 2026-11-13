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
    /// Node ids in `exclude` leave before the room under 200 is applied.
    let applyReplyExcluding
        (clientHitCount: int)
        (current: Match)
        (reply: Reply)
        (exclude: Set<NodeId>)
        : NodeId list option =
        if reply.matchKey <> current then
            None
        else
            let space = roomFor clientHitCount
            if space <= 0 then
                Some []
            else
                reply.ids
                |> List.filter (fun id -> not (Set.contains id exclude))
                |> List.truncate space
                |> Some

    /// None when the reply is for other text or another generation.
    let applyReply
        (clientHitCount: int)
        (current: Match)
        (reply: Reply)
        : NodeId list option =
        applyReplyExcluding clientHitCount current reply Set.empty

    /// A stale reply leaves the ids already kept.
    let keepReplyExcluding
        (clientHitCount: int)
        (current: Match)
        (kept: NodeId list)
        (reply: Reply)
        (exclude: Set<NodeId>)
        : NodeId list =
        match
            applyReplyExcluding clientHitCount current reply exclude
        with
        | None -> kept
        | Some ids -> ids

    /// A stale reply leaves the ids already kept.
    let keepReply
        (clientHitCount: int)
        (current: Match)
        (kept: NodeId list)
        (reply: Reply)
        : NodeId list =
        keepReplyExcluding clientHitCount current kept reply Set.empty

    let encodeRequest (request: Request) =
        let generation =
            match request.generation with
            | None -> []
            | Some value -> [ "generation", Encode.int value ]
        Encode.object (
            [ "text", Encode.string request.text
              "startId", Serialization.encodeNodeId request.startId ]
            @ generation)

    let private encodeIds (ids: NodeId list) =
        ids
        |> List.map Serialization.encodeNodeId
        |> Encode.list

    let private encodeNodes (nodes: Node list) =
        nodes
        |> List.map Serialization.encodeNode
        |> Encode.list

    let private encodeMatch (key: Match) =
        match key with
        | Text text -> "text", Encode.string text
        | Generation generation -> "generation", Encode.int generation

    /// Find reply plus the Want answer `nodes` list of hit Headers.
    let encodeReplyWithNodes (reply: Reply) (nodes: Node list) =
        Encode.object
            [ "ids", encodeIds reply.ids
              encodeMatch reply.matchKey
              "nodes", encodeNodes nodes ]

    let encodeReply (reply: Reply) =
        Encode.object
            [ "ids", encodeIds reply.ids
              encodeMatch reply.matchKey ]

    /// Headers on the Want answer `nodes` list. Absent field is an empty list.
    let decodeWantNodes: Decoder<Node list> =
        Decode.object (fun get ->
            get.Optional.Field
                "nodes"
                (Decode.list Serialization.decodeNode)
            |> Option.defaultValue [])

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

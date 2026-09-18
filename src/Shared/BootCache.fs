namespace Gambol.Shared

open Thoth.Json.Core
open Gambol.Shared

[<RequireQualifiedAccess>]
module BootCache =

    [<Literal>]
    let databaseName = "gambol-boot-cache-v1"

    [<Literal>]
    let snapshotStore = "snapshots"

    [<Literal>]
    let changeStore = "changes"

    let codecVersion = 1

    /// Boot read is on: miss and flag-off still fetch `/state`.
    let enabled = true

    type SnapshotRecord =
        { codecVersion: int
          file: string
          scopeKey: string
          eventId: EventId
          isReady: bool
          stateJson: string
          writtenAt: string
          bootstrapHash: string }

    let scopeKey (zoomId: NodeId option) : string =
        match zoomId with
        | None -> "root"
        | Some (NodeId g) -> "root|zoom:" + g.ToString()

    let snapshotRecord
        (file: string)
        (scope: string)
        (stateJson: string)
        (eventId: EventId)
        (isReady: bool)
        (writtenAt: string)
        (bootstrapHash: string)
        : SnapshotRecord =
        { codecVersion = codecVersion
          file = file
          scopeKey = scope
          eventId = eventId
          isReady = isReady
          stateJson = stateJson
          writtenAt = writtenAt
          bootstrapHash = bootstrapHash }

    let encodeSnapshot (record: SnapshotRecord) : IEncodable =
        Encode.object
            [ "codecVersion", Encode.int record.codecVersion
              "file", Encode.string record.file
              "scopeKey", Encode.string record.scopeKey
              "eventId", EventJson.encodeEventId record.eventId
              "ready", Encode.bool record.isReady
              "stateJson", Encode.string record.stateJson
              "writtenAt", Encode.string record.writtenAt
              "bootstrapHash", Encode.string record.bootstrapHash ]

    let decodeSnapshot: Decoder<SnapshotRecord> =
        Decode.object (fun get ->
            { codecVersion =
                get.Required.Field "codecVersion" Decode.int
              file = get.Required.Field "file" Decode.string
              scopeKey = get.Required.Field "scopeKey" Decode.string
              eventId = get.Required.Field "eventId" EventJson.decodeEventId
              isReady = get.Required.Field "ready" Decode.bool
              stateJson = get.Required.Field "stateJson" Decode.string
              writtenAt = get.Required.Field "writtenAt" Decode.string
              bootstrapHash =
                get.Optional.Field "bootstrapHash" Decode.string
                |> Option.defaultValue "" })

    let validateSnapshot
        (currentFile: string)
        (currentScope: string)
        (record: SnapshotRecord)
        : Result<unit, string> =
        if record.codecVersion <> codecVersion then Error "codec"
        elif record.file <> currentFile then Error "file"
        elif record.scopeKey <> currentScope then Error "scope"
        else Ok ()

    let encodeEvent (event: Ev) : IEncodable = EventJson.encode event

    let decodeEvent: Decoder<Ev> = EventJson.decode

    let eventsAfter (snapshotEventId: EventId) (log: Ev list) : Ev list =
        log
        |> List.filter (fun event -> event.id > snapshotEventId)
        |> List.sortBy (fun event -> event.id)

    let acceptedForLog
        (confirmed: Ev list)
        (submitted: Ev list)
        : Ev list =
        if confirmed.IsEmpty then submitted else confirmed

    [<RequireQualifiedAccess>]
    type BootRead =
        | FetchState of reason: string
        | UseCache of StateResponse

    let private clientEventId (snapshotEventId: EventId) (log: Ev list) =
        match eventsAfter snapshotEventId log with
        | [] -> snapshotEventId
        | kept ->
            kept
            |> List.map (fun event -> event.id)
            |> List.fold EventId.max snapshotEventId

    let foldLog
        (snapshot: StateResponse)
        (delta: Ev list)
        : Result<StateResponse, string> =
        let ordered = eventsAfter snapshot.eventId delta
        let state0: State =
            { graph = snapshot.graph
              eventId = snapshot.eventId }
        ordered
        |> List.fold
            (fun acc event ->
                match acc with
                | Error _ -> acc
                | Ok st ->
                    let ops = Ev.ops event |> Option.defaultValue []
                    match ResidentProjection.applyOps ops st with
                    | ApplyResult.Invalid (_, msg) -> Error msg
                    | ApplyResult.Changed next
                    | ApplyResult.Unchanged next -> Ok next)
            (Ok state0)
        |> Result.map (fun st ->
            { graph = st.graph
              eventId = clientEventId snapshot.eventId ordered
              isReady = snapshot.isReady })

    let decideBootRead
        (flagOn: bool)
        (currentFile: string)
        (currentScope: string)
        (record: SnapshotRecord option)
        (log: Ev list)
        (decode: string -> Result<StateResponse, string>)
        : BootRead =
        if not flagOn then
            BootRead.FetchState "disabled"
        else
            match record with
            | None -> BootRead.FetchState "miss"
            | Some snap ->
                match validateSnapshot currentFile currentScope snap with
                | Error reason -> BootRead.FetchState reason
                | Ok () ->
                    match decode snap.stateJson with
                    | Error _ -> BootRead.FetchState "decode"
                    | Ok snapshot ->
                        match foldLog snapshot log with
                        | Error _ -> BootRead.FetchState "fold"
                        | Ok folded -> BootRead.UseCache folded

    /// Hung IndexedDB must not skip `/state`. Above typical warm IDB read.
    let cacheReadTimeoutMs = 2500

    [<RequireQualifiedAccess>]
    type BootReadWait =
        | KeepWaiting
        | Done of BootRead

    let decideBootReadWait
        (elapsedMs: int)
        (cacheReturned: bool)
        (flagOn: bool)
        (currentFile: string)
        (currentScope: string)
        (record: SnapshotRecord option)
        (log: Ev list)
        (decode: string -> Result<StateResponse, string>)
        : BootReadWait =
        if cacheReturned then
            BootReadWait.Done (
                decideBootRead
                    flagOn currentFile currentScope record log decode)
        elif elapsedMs >= cacheReadTimeoutMs then
            BootReadWait.Done (BootRead.FetchState "timeout")
        else
            BootReadWait.KeepWaiting

    let maxNovelCount = 64
    let maxPollRevGap = 64
    let maxLogLength = 32
    let maxRevGap = 32

    let novelEvents
        (log: Ev list)
        (pollEvents: Ev list)
        : Ev list =
        let byId = log |> List.map (fun event -> event.id) |> Set.ofList
        let bySubmission =
            log |> List.map (fun event -> event.submissionId) |> Set.ofList
        pollEvents
        |> List.filter (fun event ->
            not (Set.contains event.id byId)
            && not (Set.contains event.submissionId bySubmission))

    [<RequireQualifiedAccess>]
    type BootPoll =
        | Confirmed of isReady: bool
        | ApplyNovel of Ev list * isReady: bool
        | CodeOutdated
        | FallbackState of reason: string

    /// After `/state`, omit the cached hash. A Fable fingerprint does not match
    /// the server hash, and Poll would refetch `/state` forever.
    let cachedHashForBootPoll
        (justFetchedState: bool)
        (storedHash: string)
        : string option =
        if justFetchedState || storedHash = "" then None
        else Some storedHash

    let decideBootPoll
        (clientEventId: EventId)
        (log: Ev list)
        (poll: ChangeSuccessResponse)
        (pollHash: string option)
        (cachedHash: string option)
        : BootPoll =
        if poll.eventId < clientEventId then
            BootPoll.FallbackState "revision"
        else
            match SyncLogic.getPollOutcome poll clientEventId with
            | Some CodeOutdated -> BootPoll.CodeOutdated
            | Some DataOutdated
            | None ->
                let novel = novelEvents log poll.events
                let gap =
                    EventId.value poll.eventId - EventId.value clientEventId
                if
                    novel.Length > maxNovelCount
                    || gap > maxPollRevGap
                then
                    BootPoll.FallbackState "oversized"
                elif novel.IsEmpty then
                    match cachedHash, pollHash with
                    | Some local, Some remote when local <> remote ->
                        BootPoll.FallbackState "hash"
                    | _ -> BootPoll.Confirmed poll.isReady
                else
                    BootPoll.ApplyNovel(novel, poll.isReady)
            | Some _ -> BootPoll.Confirmed poll.isReady

    let shouldTruncate
        (logLength: int)
        (snapshotRevision: int)
        (clientRev: int)
        : bool =
        logLength > maxLogLength
        || (clientRev - snapshotRevision) > maxRevGap

    let truncationGraph
        (graph: Graph)
        (savedZoom: NodeId option)
        : Graph =
        ResidentProjection.bootstrapGraph
            BootstrapScope.RootClosure
            savedZoom
            graph

    let fingerprint (text: string) : string =
        let hashed =
            text
            |> Seq.fold
                (fun acc ch -> (acc * 33 + int ch) &&& 0x7fffffff)
                5381
        hashed.ToString("x8")

    let graphFingerprint (graph: Graph) : string =
        let scoped = ResidentProjection.rootBootstrapGraph graph
        let parts =
            scoped.nodes
            |> Map.toList
            |> List.map (fun (NodeId guid, node) ->
                let kids =
                    node.children
                    |> List.map (fun child -> child.id.Value.ToString())
                    |> String.concat ","
                guid.ToString() + ":" + node.text + ":" + kids)
            |> String.concat "|"
        fingerprint parts

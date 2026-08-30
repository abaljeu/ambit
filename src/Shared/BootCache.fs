namespace Gambol.Shared

open Thoth.Json.Core

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
          revision: int
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
        (revision: int)
        (isReady: bool)
        (writtenAt: string)
        (bootstrapHash: string)
        : SnapshotRecord =
        { codecVersion = codecVersion
          file = file
          scopeKey = scope
          revision = revision
          isReady = isReady
          stateJson = stateJson
          writtenAt = writtenAt
          bootstrapHash = bootstrapHash }

    let encodeSnapshot (record: SnapshotRecord) : IEncodable =
        Encode.object
            [ "codecVersion", Encode.int record.codecVersion
              "file", Encode.string record.file
              "scopeKey", Encode.string record.scopeKey
              "revision", Encode.int record.revision
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
              revision = get.Required.Field "revision" Decode.int
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

    let changesAfter (snapshotRevision: int) (log: Change list) : Change list =
        log
        |> List.filter (fun change -> change.id > snapshotRevision)
        |> List.sortBy (fun change -> change.id)

    let acceptedForLog
        (confirmed: Change list)
        (submitted: PendingChange list)
        : Change list =
        if confirmed.IsEmpty then
            submitted |> List.map (fun item -> item.change)
        else
            confirmed

    [<RequireQualifiedAccess>]
    type BootRead =
        | FetchState of reason: string
        | UseCache of StateResponse

    let clientRevision (snapshotRevision: int) (log: Change list) : int =
        match changesAfter snapshotRevision log with
        | [] -> snapshotRevision
        | kept ->
            let maxId = kept |> List.map (fun c -> c.id) |> List.max
            max snapshotRevision maxId

    let foldLog
        (snapshot: StateResponse)
        (delta: Change list)
        : Result<StateResponse, string> =
        let ordered = changesAfter snapshot.revision.Value delta
        let state0: State =
            { graph = snapshot.graph
              history = History.empty
              revision = snapshot.revision }
        ordered
        |> List.fold
            (fun acc change ->
                match acc with
                | Error _ -> acc
                | Ok st ->
                    match ResidentProjection.applyChange change st with
                    | ApplyResult.Invalid (_, msg) -> Error msg
                    | ApplyResult.Changed next
                    | ApplyResult.Unchanged next -> Ok next)
            (Ok state0)
        |> Result.map (fun st ->
            { graph = st.graph
              revision = Revision (clientRevision snapshot.revision.Value ordered)
              isReady = snapshot.isReady })

    let decideBootRead
        (flagOn: bool)
        (currentFile: string)
        (currentScope: string)
        (record: SnapshotRecord option)
        (log: Change list)
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
        (log: Change list)
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

    let novelChanges
        (log: Change list)
        (pollChanges: Change list)
        : Change list =
        let byId = log |> List.map (fun c -> c.id) |> Set.ofList
        let byChangeId = log |> List.map (fun c -> c.changeId) |> Set.ofList
        pollChanges
        |> List.filter (fun change ->
            not (Set.contains change.id byId)
            && not (Set.contains change.changeId byChangeId))

    [<RequireQualifiedAccess>]
    type BootPoll =
        | Confirmed of isReady: bool
        | ApplyNovel of Change list * isReady: bool
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
        (clientRev: int)
        (log: Change list)
        (poll: ChangeSuccessResponse)
        (pollHash: string option)
        (cachedHash: string option)
        : BootPoll =
        if poll.revision.Value < clientRev then
            BootPoll.FallbackState "revision"
        else
            match SyncLogic.getPollOutcome poll clientRev with
            | Some CodeOutdated -> BootPoll.CodeOutdated
            | Some DataOutdated
            | None ->
                let novel = novelChanges log poll.changes
                let gap = poll.revision.Value - clientRev
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

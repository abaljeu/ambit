namespace Gambol.Shared

open Gambol.Shared

[<RequireQualifiedAccess>]
type AckReconcile =
    | Applied of ClientSyncState * SyncInfo * Effect list * Op list
    | Ignored
    | Rejected of string

[<RequireQualifiedAccess>]
module SyncLogic =

    /// Determine if the poll response indicates the client is outdated.
    /// Returns Some DataOutdated if server event id is ahead of the client,
    /// or None if the client is up to date.
    /// Does not branch on apiVersion: one live API; apply what arrived.
    /// Callers must only invoke this when there are no pending local events
    /// (otherwise a higher server event id may reflect our own in-flight POST).
    let getPollOutcome
        (poll: ChangeSuccessResponse)
        (clientEventId: EventId)
        : SyncState option =
        if poll.eventId > clientEventId then Some DataOutdated
        else None

    /// Server-restart signal: poll/load `buildEpochSec` (DeployEpochSec) differs
    /// from the webpage's deploy stamp. Not CodeOutdated — same API can keep the tab.
    let serverProcessRestarted
        (webpageDeployEpochSec: int)
        (serverBuildEpochSec: int)
        : bool =
        webpageDeployEpochSec > 0
        && serverBuildEpochSec > 0
        && webpageDeployEpochSec <> serverBuildEpochSec

    let private asProjectionState (state: ClientSyncState) : State =
        { graph = state.graph
          eventId = state.eventId }

    let private withProjectedGraph
        (event: Ev)
        (state: ClientSyncState)
        (projected: State)
        (note: string option)
        : ClientSyncState =
        { state with
            graph = projected.graph
            eventId = event.id
            actorLiveFocusIds =
                ActorLive.applyEvent event state.actorLiveFocusIds
            applyDetail =
                match state.applyDetail with
                | Some _ as existing -> existing
                | None -> note }

    let private isNoticeEvent (event: Ev) =
        match event.body with
        | EventBody.Notice _ -> true
        | _ -> false

    /// Latest write error on this poll. Other events are left alone.
    let persistFailedMessage (events: Ev list) : string option =
        events
        |> List.fold (fun latest event ->
            match event.body with
            | EventBody.Notice (Notice.PersistFailed (_, message)) ->
                Some message
            | _ -> latest)
            None

    /// Change, undo, and redo ops. A notice contributes none.
    let opsForDownload (event: Ev) : Op list =
        Ev.ops event |> Option.defaultValue []

    let private noticeNodeId (notice: Notice) =
        match notice with
        | Notice.NodeParsed nodeId
        | Notice.NodeUploading nodeId
        | Notice.NodeReceived nodeId
        | Notice.NodeParsing nodeId
        | Notice.NodePersisting nodeId
        | Notice.NodeUnpersisted nodeId
        | Notice.NodeUnparsed nodeId
        | Notice.PersistFailed (nodeId, _)
        | Notice.ParseFailed (nodeId, _) ->
            nodeId
        | Notice.NodePersisted (nodeId, _, _) -> nodeId

    /// NoticeGraph.writeGraph. An absent node stays absent.
    /// A writeGraph error leaves the graph unchanged.
    let private applyNoticeGraph (notice: Notice) (st: ClientSyncState) =
        let nodeId = noticeNodeId notice
        if not (Map.containsKey nodeId st.graph.nodes) then
            st
        else
            match NoticeGraph.writeGraph notice st.graph with
            | Error _ -> st
            | Ok graph -> { st with graph = graph }

    let private projectLoggedNotice (st: ClientSyncState) (event: Ev) =
        if event.id <= st.eventId then
            st
        else
            match event.body with
            | EventBody.Notice notice ->
                { applyNoticeGraph notice st with eventId = event.id }
            | _ -> { st with eventId = event.id }

    let private applyLoggedNotice
        (event: Ev)
        (st: ClientSyncState)
        (undid: bool)
        =
        Ok(projectLoggedNotice st event, undid)

    /// EventLog storage is newest-head. Apply from the older id upward.
    let private inStoredOrder (events: Ev list) =
        events |> List.sortBy (fun event -> EventId.value event.id)

    let private foldProjectedEvents
        (events: Ev list)
        (state: ClientSyncState)
        : Result<ClientSyncState * bool, string> =
        let step (st: ClientSyncState, undid) (event: Ev) =
            // Already applied. An append-style Replace is not idempotent.
            if event.id <= st.eventId then
                Ok(st, undid)
            elif isNoticeEvent event then
                applyLoggedNotice event st undid
            else
                let ops = Ev.ops event |> Option.defaultValue []
                let mark =
                    Op.persistMarkVersion event.commandName event.id
                let guard = Op.guardsUnparsedDocument event.commandName
                match
                    ResidentProjection.applyOpsForSyncAllowing
                        guard
                        mark
                        ops
                        (asProjectionState st)
                        { pending = st.pending
                          submissionId = event.submissionId }
                with
                | ApplyResult.Changed newSt, note
                | ApplyResult.Unchanged newSt, note ->
                    Ok(
                        withProjectedGraph event st newSt note,
                        undid || note.IsSome)
                | ApplyResult.Invalid (_, msg), _ -> Error msg
        inStoredOrder events
        |> List.fold
            (fun acc event ->
                match acc with
                | Error _ -> acc
                | Ok cursor -> step cursor event)
            (Ok(state, false))

    let private graphAfterWant
        (response: SyncResponse)
        (graph: Graph)
        : Result<Graph, string> =
        if List.isEmpty response.nodes && Map.isEmpty response.childMap then
            Ok graph
        else
            ResidentProjection.installWantAnswer
                response.childMap
                response.nodes
                graph

    let private undoPendingGraph
        (state: ClientSyncState)
        (pending: Ev list)
        : Graph =
        pending
        |> List.rev
        |> List.fold
            (fun graph event ->
                let inverseOps = Ev.inverseOps event |> Option.defaultValue []
                match
                    ResidentProjection.applyOps
                        inverseOps
                        (asProjectionState { state with graph = graph })
                with
                | ApplyResult.Changed projected
                | ApplyResult.Unchanged projected -> projected.graph
                | ApplyResult.Invalid _ -> graph)
            state.graph

    let private hasWantPayload (response: SyncResponse) =
        not response.nodes.IsEmpty
        || not response.childMap.IsEmpty

    let private rewindPendingBeforeWant
        (response: SyncResponse)
        (state: ClientSyncState)
        : ClientSyncState * bool =
        if not (hasWantPayload response) || state.pending.IsEmpty then
            state, false
        else
            { state with
                graph = undoPendingGraph state state.pending },
            true

    let private applyPendingEvent (state: State) (event: Ev) : State =
        let ops = Ev.ops event |> Option.defaultValue []
        match ResidentProjection.applyOps ops state with
        | ApplyResult.Changed next
        | ApplyResult.Unchanged next -> next
        | ApplyResult.Invalid _ -> state

    let private reapplyLeftoverPending
        (played: Ev list)
        (state: ClientSyncState)
        : ClientSyncState =
        let playedIds =
            played |> List.map (fun event -> event.submissionId) |> Set.ofList
        let next =
            state.pending
            |> List.filter (fun event ->
                not (Set.contains event.submissionId playedIds))
            |> List.fold applyPendingEvent (asProjectionState state)
        { state with graph = next.graph }

    /// Apply a Sync response atomically under Loaded rules.
    /// Event tail first. A failed Poll/sync apply precondition undoes
    /// pending in applyOpForSync. Want install rewinds pending, then applies.
    /// After either undo, leftover pending is re-applied with ordinary apply.
    /// Event-stripped callers use applyWantPreservingPending instead.
    let applySyncResponse
        (response: SyncResponse)
        (state: ClientSyncState)
        : Result<ClientSyncState, string> =
        match foldProjectedEvents response.events state with
        | Error msg -> Error msg
        | Ok (afterEvents, undidPending) ->
            let rewound, didRewind =
                rewindPendingBeforeWant response afterEvents
            match graphAfterWant response rewound.graph with
            | Error msg -> Error msg
            | Ok afterWant ->
                let applied = { rewound with graph = afterWant }
                if undidPending || didRewind then
                    Ok (reapplyLeftoverPending response.events applied)
                else
                    Ok applied

    /// Install a Want answer on the current Graph. Do not rewind pending
    /// and do not re-apply it. PollDone uses this after catch-up play.
    /// A post acknowledgement uses it so the Graph stays optimistic.
    let applyWantPreservingPending
        (response: SyncResponse)
        (state: ClientSyncState)
        : Result<ClientSyncState, string> =
        match graphAfterWant response state.graph with
        | Error msg -> Error msg
        | Ok graph -> Ok { state with graph = graph }

    let applyLoadResponse
        (responseEventId: EventId)
        (hasPendingLocal: bool)
        (response: SyncResponse)
        (state: ClientSyncState)
        : Result<ClientSyncState, string> =
        let substantive =
            response.events
            |> List.filter (fun event -> not (isNoticeEvent event))
        let answerOnly =
            List.isEmpty substantive
            && not (List.isEmpty response.nodes)
        if
            answerOnly
            && (hasPendingLocal || responseEventId <> state.eventId)
        then
            Error "raced Load answer"
        else
            applySyncResponse response state

    let loadResponseToSync (response: LoadResponse) : SyncResponse =
        { events = response.events
          nodes = response.nodes
          childMap = response.childMap }

    let changeSuccessToSync (response: ChangeSuccessResponse) : SyncResponse =
        { events = response.events
          nodes = response.nodes
          childMap = response.childMap }

    let loadResponseToPoll (response: LoadResponse) : ChangeSuccessResponse =
        { eventId = response.eventId
          buildEpochSec = response.buildEpochSec
          pageBuildEpochSec = response.pageBuildEpochSec
          apiVersion = response.apiVersion
          isReady = response.isReady
          externalChanges =
            response.events
            |> List.exists (fun event -> not (isNoticeEvent event))
          events = response.events
          message = None
          bootstrapHash = None
          nodes = []
          childMap = Map.empty }

    /// Apply a server-supplied Ev tail onto local State (Poll path).
    /// Empty list is a no-op that preserves History.
    let applyServerTail
        (events: Ev list)
        (state: ClientSyncState)
        : Result<ClientSyncState, string> =
        applySyncResponse
            { events = events
              nodes = []
              childMap = Map.empty }
            state

    let applyLocalEvent
        (event: Ev)
        (state: ClientSyncState)
        : Result<ClientSyncState * Ev, string> =
        let ops = Ev.ops event |> Option.defaultValue []
        match ResidentProjection.applyOps ops (asProjectionState state) with
        | ApplyResult.Invalid (_, msg) -> Error msg
        | ApplyResult.Unchanged _ -> Error "Change did not change state"
        | ApplyResult.Changed newState ->
            let history = ClientHistory.record event state.history
            let posted = { event with id = EventId.zero }
            Ok(
                { state with
                    graph = newState.graph
                    history = history },
                posted)

    let private applyInverse
        (planned: (Ev * ClientHistory) option)
        (state: ClientSyncState)
        : Result<ClientSyncState * Ev, string> option =
        match planned with
        | None -> None
        | Some (inverse, history) ->
            let ops = Ev.ops inverse |> Option.defaultValue []
            let version =
                Op.persistMarkVersion inverse.commandName inverse.id
            match
                ResidentProjection.applyOpsAllowing
                    true
                    version
                    ops
                    (asProjectionState state)
            with
            | ApplyResult.Invalid (_, msg) -> Some (Error msg)
            | ApplyResult.Unchanged newState
            | ApplyResult.Changed newState ->
                let posted = { inverse with id = EventId.zero }
                Some(
                    Ok(
                        { state with
                            graph = newState.graph
                            history = history },
                        posted))

    let applyLocalUndo
        (submissionId: System.Guid)
        (state: ClientSyncState)
        : Result<ClientSyncState * Ev, string> option =
        applyInverse (ClientHistory.undo submissionId state.history) state

    let applyLocalRedo
        (submissionId: System.Guid)
        (state: ClientSyncState)
        : Result<ClientSyncState * Ev, string> option =
        applyInverse (ClientHistory.redo submissionId state.history) state

    let private isStampOp =
        function
        | Op.SetUpdateTime _ -> true
        | _ -> false

    let private takeSuffix (prefix: Op list) (ops: Op list) : Result<Op list, string> =
        if ops.Length < prefix.Length then
            Error "changed-prefix confirmation"
        elif List.take prefix.Length ops <> prefix then
            Error "changed-prefix confirmation"
        else
            let extra = List.skip prefix.Length ops
            if List.forall isStampOp extra then Ok extra
            else Error "forbidden-suffix confirmation"

    let private eventOps (event: Ev) =
        Ev.ops event |> Option.defaultValue []

    let private dropNotices (events: Ev list) =
        events |> List.filter (fun event -> not (isNoticeEvent event))

    let private identityError
        (submitted: Ev list)
        (confirmed: Ev list)
        : string option =
        let confirmed = dropNotices confirmed
        if confirmed.Length < submitted.Length then
            Some "missing confirmation"
        elif confirmed.Length > submitted.Length then
            Some "unmatched confirmation"
        else
            let subIds =
                submitted |> List.map (fun event -> event.submissionId)
            let confIds =
                confirmed |> List.map (fun event -> event.submissionId)
            if subIds = confIds then
                None
            else
                let subSet = Set.ofList subIds
                let confSet = Set.ofList confIds
                if subSet = confSet then Some "reordered confirmation"
                else Some "unmatched confirmation"

    let private collectSuffixes
        (submitted: Ev list)
        (confirmed: Ev list)
        : Result<Op list, string> =
        let confirmed = dropNotices confirmed
        let rec loop acc submittedItems (confirmedItems: Ev list) =
            match submittedItems, confirmedItems with
            | [], [] -> Ok (List.concat (List.rev acc))
            | event :: items, confirmedEvent :: rest ->
                match
                    takeSuffix
                        (eventOps event)
                        (eventOps confirmedEvent)
                with
                | Error err -> Error err
                | Ok extra -> loop (extra :: acc) items rest
            | _ -> Error "missing confirmation"
        loop [] submitted confirmed

    let private sameBody (left: Ev) (right: Ev) =
        left.submissionId = right.submissionId
        && eventOps left = eventOps right

    let private isQueuePrefix (submitted: Ev list) (pending: Ev list) =
        pending.Length >= submitted.Length
        && List.forall2 sameBody submitted (List.take submitted.Length pending)

    let private isPresent (pending: Ev list) submissionId =
        pending
        |> List.exists (fun event -> event.submissionId = submissionId)

    let private queueOutcome submitted pending serverEventId clientEventId =
        if isQueuePrefix submitted pending then
            Ok "apply"
        else
            let present =
                submitted
                |> List.map (fun event -> isPresent pending event.submissionId)
            if List.forall (fun seen -> not seen) present then
                if serverEventId <= clientEventId then Ok "ignore"
                else Error "forward-event confirmation"
            elif List.forall id present then
                Error "reordered confirmation"
            else
                Error "partial-overlap confirmation"

    let private projectSuffixes (suffixOps: Op list) (state: ClientSyncState) =
        if suffixOps.IsEmpty then
            Ok state.graph
        else
            match ResidentProjection.applyOps suffixOps (asProjectionState state) with
            | ApplyResult.Invalid (_, msg) -> Error msg
            | ApplyResult.Unchanged projected
            | ApplyResult.Changed projected -> Ok projected.graph

    let isConfirmationEcho (submitted: Ev list) (confirmed: Ev list) =
        if submitted.IsEmpty then
            false
        else
            match identityError submitted confirmed with
            | Some _ -> false
            | None ->
                match collectSuffixes submitted confirmed with
                | Error _ -> false
                | Ok _ -> true

    /// Rewind to the noted baseline, replay a Poll Ev list, then re-apply
    /// leftover pending so those edits stay visible. History is not cleared.
    let consumeCatchUpPoll
        (baseline: CatchUpBaseline)
        (events: Ev list)
        (serverEventId: Gambol.Shared.EventId)
        (state: ClientSyncState)
        : Result<ClientSyncState, string> =
        let atBaseline =
            { state with
                graph = baseline.graph
                eventId = baseline.eventId }
        match foldProjectedEvents events atBaseline with
        | Error msg -> Error msg
        | Ok (afterEvents, _) ->
            let replayed =
                { afterEvents with
                    eventId = serverEventId
                    history = ClientHistory.approve events state.history }
            Ok (reapplyLeftoverPending events replayed)

    let reconcileExternalAck
        (submitted: Ev list)
        (serverEventId: Gambol.Shared.EventId)
        (state: ClientSyncState)
        (syncInfo: SyncInfo)
        : AckReconcile =
        if submitted.IsEmpty then
            AckReconcile.Rejected "missing confirmation"
        else
            let catchUp =
                match syncInfo.catchUp with
                | Some noted -> noted
                | None ->
                    { eventId = state.eventId
                      graph = undoPendingGraph state syncInfo.pending }
            let nextSync, _, effects =
                SyncPlanner.retireSubmittedPrefix
                    submitted.Length
                    serverEventId
                    syncInfo
            let nextSync = nextSync |> SyncInfo.withCatchUp (Some catchUp)
            AckReconcile.Applied(state, nextSync, effects, [])

    /// Confirmed Undo and Redo raise changedVersion to their stored id.
    let private markUndoRedoVersion (event: Ev) (graph: Graph) : Graph =
        match event.body with
        | EventBody.Undo _
        | EventBody.Redo _
        | EventBody.Order (Order.Undo _)
        | EventBody.Order (Order.Redo _) ->
            match Op.persistMarkVersion event.commandName event.id with
            | None -> graph
            | Some version ->
                let ops = Ev.ops event |> Option.defaultValue []
                DocumentOpImpact.owningSpecials graph ops
                |> List.fold
                    (fun graph nodeId ->
                        GraphMutate.setUnpersistedAt nodeId version graph)
                    graph
        | _ -> graph

    let private markUndoRedoVersions (events: Ev list) (graph: Graph) =
        List.fold
            (fun graph event -> markUndoRedoVersion event graph)
            graph
            events

    /// Notices in a post reply write the graph before confirmation matching.
    /// The stored event id stays put until confirmation matching sets it.
    let private applyReplyNotices
        (confirmed: Ev list)
        (state: ClientSyncState)
        : ClientSyncState =
        let next =
            confirmed
            |> List.filter isNoticeEvent
            |> inStoredOrder
            |> List.fold projectLoggedNotice state
        { next with eventId = state.eventId }

    let private stateAfterApprovedSuffix
        (confirmed: Ev list)
        (serverEventId: Gambol.Shared.EventId)
        (state: ClientSyncState)
        (graph: Graph)
        : ClientSyncState =
        { state with
            graph = markUndoRedoVersions confirmed graph
            eventId = serverEventId
            history = ClientHistory.approve confirmed state.history }

    let private finishReconcileAck
        (submitted: Ev list)
        (confirmed: Ev list)
        (serverEventId: Gambol.Shared.EventId)
        (state: ClientSyncState)
        (syncInfo: SyncInfo)
        : AckReconcile =
        match identityError submitted confirmed with
        | Some err -> AckReconcile.Rejected err
        | None ->
            match collectSuffixes submitted confirmed with
            | Error err -> AckReconcile.Rejected err
            | Ok suffixOps ->
                match
                    queueOutcome
                        submitted
                        syncInfo.pending
                        serverEventId
                        state.eventId
                with
                | Error err -> AckReconcile.Rejected err
                | Ok "ignore" -> AckReconcile.Ignored
                | Ok _ ->
                    match projectSuffixes suffixOps state with
                    | Error err -> AckReconcile.Rejected err
                    | Ok graph ->
                        let nextSync, _, effects =
                            SyncPlanner.retireSubmittedPrefix
                                submitted.Length
                                serverEventId
                                syncInfo
                        AckReconcile.Applied(
                            stateAfterApprovedSuffix
                                confirmed serverEventId state graph,
                            nextSync,
                            effects,
                            suffixOps)

    let reconcileAck
        (submitted: Ev list)
        (confirmed: Ev list)
        (serverEventId: Gambol.Shared.EventId)
        (state: ClientSyncState)
        (syncInfo: SyncInfo)
        : AckReconcile =
        if submitted.IsEmpty then
            AckReconcile.Rejected "missing confirmation"
        else
            finishReconcileAck
                submitted
                confirmed
                serverEventId
                (applyReplyNotices confirmed state)
                syncInfo

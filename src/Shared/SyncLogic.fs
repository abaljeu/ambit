namespace Gambol.Shared

open Gambol.Shared

/// Browser graph, Revision, and ClientHistory used by local and remote apply.
type ClientSyncState =
    { graph: Graph
      revision: EventId
      history: ClientHistory
      eventLog: EventLog }

[<RequireQualifiedAccess>]
module ClientSyncState =
    let create graph revision history : ClientSyncState =
        { graph = graph
          revision = revision
          history = history
          eventLog = EventLog.empty }

[<RequireQualifiedAccess>]
type AckReconcile =
    | Applied of ClientSyncState * SyncInfo * Effect list * Op list
    | Ignored
    | Rejected of string

[<RequireQualifiedAccess>]
module SyncLogic =

    /// Determine if the poll response indicates the client is outdated.
    /// Returns Some CodeOutdated if Poll apiVersion differs from ApiVersion.current,
    /// Some DataOutdated if server revision is ahead of the client,
    /// or None if the client is up to date.
    /// CodeOutdated takes priority when both conditions hold.
    /// Callers must only invoke this when there are no pending local changes
    /// (otherwise a higher server revision may reflect our own in-flight POST).
    let getPollOutcome
        (poll: ChangeSuccessResponse)
        (clientRev: int)
        : SyncState option =
        let codeOutdated = poll.apiVersion <> ApiVersion.current
        let (Gambol.Shared.EventId pollRev) = poll.revision
        let dataOutdated = pollRev > clientRev
        if codeOutdated then Some CodeOutdated
        elif dataOutdated then Some DataOutdated
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
          revision = EventId.toRevision state.revision }

    let private withProjectedGraph
        (state: ClientSyncState)
        (projected: State)
        : ClientSyncState =
        let (Gambol.Shared.EventId rev) = state.revision
        { state with
            graph = projected.graph
            revision = Gambol.Shared.EventId (rev + 1) }

    let private foldProjectedEvents
        (events: Ev list)
        (state: ClientSyncState)
        : Result<ClientSyncState, string> =
        events
        |> List.fold
            (fun acc event ->
                match acc with
                | Error _ -> acc
                | Ok st ->
                    let ops = Ev.ops event |> Option.defaultValue []
                    match
                        ResidentProjection.applyOps ops (asProjectionState st)
                    with
                    | ApplyResult.Changed newSt
                    | ApplyResult.Unchanged newSt ->
                        Ok (withProjectedGraph st newSt)
                    | ApplyResult.Invalid (_, msg) -> Error msg)
            (Ok state)

    let private pendingItem
        (recordId: int)
        (event: Ev)
        : PendingChange =
        { event = event
          transition =
            Some
                { recordId = recordId
                  submittedChangeId = event.submissionId } }

    /// Apply a Sync response atomically under Loaded rules.
    /// Packages install after the projected tail so authoritative snapshots at the
    /// response revision win. Poll and Post consume paths preserve History.
    let applySyncResponse
        (response: SyncResponse)
        (state: ClientSyncState)
        : Result<ClientSyncState, string> =
        match foldProjectedEvents response.events state with
        | Error msg -> Error msg
        | Ok afterChanges ->
            let graph =
                ResidentProjection.installPackages
                    response.packages
                    afterChanges.graph
            Ok { afterChanges with graph = graph }

    let applyLoadResponse
        (responseRevision: int)
        (hasPendingLocal: bool)
        (response: SyncResponse)
        (state: ClientSyncState)
        : Result<ClientSyncState, string> =
        let packageOnly =
            List.isEmpty response.events
            && not (List.isEmpty response.packages)
        let (Gambol.Shared.EventId stateRev) = state.revision
        if
            packageOnly
            && (hasPendingLocal || responseRevision <> stateRev)
        then
            Error "raced package payload"
        else
            applySyncResponse response state

    let loadResponseToSync (response: LoadResponse) : SyncResponse =
        { events = response.events
          packages = response.packages }

    let loadResponseToPoll (response: LoadResponse) : ChangeSuccessResponse =
        { revision = response.revision
          buildEpochSec = response.buildEpochSec
          pageBuildEpochSec = response.pageBuildEpochSec
          apiVersion = response.apiVersion
          isReady = response.isReady
          externalChanges = not response.events.IsEmpty
          events = response.events
          message = None
          bootstrapHash = None }

    /// Apply a server-supplied Ev tail onto local State (Poll path).
    /// Empty list is a no-op that preserves History.
    let applyServerTail
        (events: Ev list)
        (state: ClientSyncState)
        : Result<ClientSyncState, string> =
        applySyncResponse { events = events; packages = [] } state

    let private undoPendingGraph
        (state: ClientSyncState)
        (pending: PendingChange list)
        : Graph =
        pending
        |> List.rev
        |> List.fold
            (fun graph item ->
                let change =
                    { id = 0
                      submissionId = item.event.submissionId
                      ops = Ev.ops item.event |> Option.defaultValue [] }
                let inverse =
                    Change.inverse
                        (EventId.toRevision state.revision)
                        item.event.submissionId
                        change
                match
                    ResidentProjection.applyChange
                        inverse
                        (asProjectionState { state with graph = graph })
                with
                | ApplyResult.Changed projected
                | ApplyResult.Unchanged projected -> projected.graph
                | ApplyResult.Invalid _ -> graph)
            state.graph

    let applyLocalChange
        (event: Ev)
        (state: ClientSyncState)
        : Result<ClientSyncState * PendingChange, string> =
        let ops = Ev.ops event |> Option.defaultValue []
        match ResidentProjection.applyOps ops (asProjectionState state) with
        | ApplyResult.Invalid (_, msg) -> Error msg
        | ApplyResult.Unchanged _ -> Error "Change did not change state"
        | ApplyResult.Changed newState ->
            let history, recordId = ClientHistory.record event state.history
            let posted = { event with id = EventId.zero }
            Ok(
                { state with
                    graph = newState.graph
                    history = history },
                pendingItem recordId posted)

    let private applyInverse
        (planned: (Ev * ClientHistory * int) option)
        (state: ClientSyncState)
        : Result<ClientSyncState * PendingChange, string> option =
        match planned with
        | None -> None
        | Some (inverse, history, recordId) ->
            let ops = Ev.ops inverse |> Option.defaultValue []
            match ResidentProjection.applyOps ops (asProjectionState state) with
            | ApplyResult.Invalid (_, msg) -> Some (Error msg)
            | ApplyResult.Unchanged newState
            | ApplyResult.Changed newState ->
                let posted = { inverse with id = EventId.zero }
                Some(
                    Ok(
                        { state with
                            graph = newState.graph
                            history = history },
                        pendingItem recordId posted))

    let applyLocalUndo
        (submissionId: System.Guid)
        (state: ClientSyncState)
        : Result<ClientSyncState * PendingChange, string> option =
        applyInverse (ClientHistory.undo submissionId state.history) state

    let applyLocalRedo
        (submissionId: System.Guid)
        (state: ClientSyncState)
        : Result<ClientSyncState * PendingChange, string> option =
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

    let private identityError
        (submitted: PendingChange list)
        (confirmed: Ev list)
        : string option =
        if confirmed.Length < submitted.Length then
            Some "missing confirmation"
        elif confirmed.Length > submitted.Length then
            Some "unmatched confirmation"
        else
            let subIds =
                submitted |> List.map (fun item -> item.event.submissionId)
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
        (submitted: PendingChange list)
        (confirmed: Ev list)
        : Result<Op list, string> =
        let rec loop acc submittedItems (confirmedItems: Ev list) =
            match submittedItems, confirmedItems with
            | [], [] -> Ok (List.concat (List.rev acc))
            | (item: PendingChange) :: items, confirmedEvent :: rest ->
                match
                    takeSuffix
                        item.change.ops
                        (Ev.ops confirmedEvent |> Option.defaultValue [])
                with
                | Error err -> Error err
                | Ok extra -> loop (extra :: acc) items rest
            | _ -> Error "missing confirmation"
        loop [] submitted confirmed

    let private sameBody (left: PendingChange) (right: PendingChange) =
        left.change.submissionId = right.change.submissionId
        && left.change.ops = right.change.ops

    let private isQueuePrefix
        (submitted: PendingChange list)
        (pending: PendingChange list)
        =
        pending.Length >= submitted.Length
        && List.forall2 sameBody submitted (List.take submitted.Length pending)

    let private isPresent (pending: PendingChange list) submissionId =
        pending
        |> List.exists (fun item -> item.change.submissionId = submissionId)

    let private queueOutcome submitted pending serverRev clientRev =
        if isQueuePrefix submitted pending then
            Ok "apply"
        else
            let present =
                submitted
                |> List.map (fun item -> isPresent pending item.change.submissionId)
            if List.forall (fun seen -> not seen) present then
                if serverRev <= clientRev then Ok "ignore"
                else Error "forward-Revision confirmation"
            elif List.forall id present then
                Error "reordered confirmation"
            else
                Error "partial-overlap confirmation"

    let private projectSuffixes (suffixOps: Op list) (state: ClientSyncState) =
        if suffixOps.IsEmpty then
            Ok state.graph
        else
            let change =
                { id = state.revision.Value
                  submissionId = System.Guid.Empty
                  ops = suffixOps }
            match ResidentProjection.applyChange change (asProjectionState state) with
            | ApplyResult.Invalid (_, msg) -> Error msg
            | ApplyResult.Unchanged projected
            | ApplyResult.Changed projected -> Ok projected.graph

    let isConfirmationEcho (submitted: PendingChange list) (confirmed: Ev list) =
        if submitted.IsEmpty then
            false
        else
            match identityError submitted confirmed with
            | Some _ -> false
            | None ->
                match collectSuffixes submitted confirmed with
                | Error _ -> false
                | Ok _ -> true

    /// Rewind to the noted baseline and replay a Poll Ev list without clearing History.
    let consumeCatchUpPoll
        (baseline: CatchUpBaseline)
        (events: Ev list)
        (serverRevision: Gambol.Shared.EventId)
        (state: ClientSyncState)
        : Result<ClientSyncState, string> =
        let atBaseline =
            { state with
                graph = baseline.graph
                revision = baseline.revision }
        match foldProjectedEvents events atBaseline with
        | Error msg -> Error msg
        | Ok afterChanges ->
            Ok
                { afterChanges with
                    revision = serverRevision
                    history = state.history }

    let reconcileExternalAck
        (submitted: PendingChange list)
        (serverRevision: Gambol.Shared.EventId)
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
                    { revision = state.revision
                      graph = undoPendingGraph state syncInfo.pendingChanges }
            let nextSync, _, effects =
                SyncPlanner.retireSubmittedPrefix
                    submitted.Length
                    serverRevision
                    syncInfo
            let nextSync = nextSync |> SyncInfo.withCatchUp (Some catchUp)
            AckReconcile.Applied(state, nextSync, effects, [])

    let reconcileAck
        (submitted: PendingChange list)
        (confirmed: Ev list)
        (serverRevision: Gambol.Shared.EventId)
        (state: ClientSyncState)
        (syncInfo: SyncInfo)
        : AckReconcile =
        if submitted.IsEmpty then
            AckReconcile.Rejected "missing confirmation"
        else
            match identityError submitted confirmed with
            | Some err -> AckReconcile.Rejected err
            | None ->
                match collectSuffixes submitted confirmed with
                | Error err -> AckReconcile.Rejected err
                | Ok suffixOps ->
                    let (Gambol.Shared.EventId serverRev) = serverRevision
                    let (Gambol.Shared.EventId stateRev) = state.revision
                    match
                        queueOutcome
                            submitted
                            syncInfo.pendingChanges
                            serverRev
                            stateRev
                    with
                    | Error err -> AckReconcile.Rejected err
                    | Ok "ignore" -> AckReconcile.Ignored
                    | Ok _ ->
                        match projectSuffixes suffixOps state with
                        | Error err -> AckReconcile.Rejected err
                        | Ok graph ->
                            let nextState =
                                { state with
                                    graph = graph
                                    revision = serverRevision }
                            let nextSync, _, effects =
                                SyncPlanner.retireSubmittedPrefix
                                    submitted.Length
                                    serverRevision
                                    syncInfo
                            AckReconcile.Applied(
                                nextState, nextSync, effects, suffixOps)

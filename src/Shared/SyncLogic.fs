namespace Gambol.Shared

/// Client-side values needed to determine if a poll response implies the client is outdated.
type ClientPollContext =
    { buildEpochSec: int
      pageBuildEpochSec: int }

/// Browser graph, Revision, and ClientHistory used by local and remote apply.
type ClientSyncState =
    { graph: Graph
      revision: Revision
      history: ClientHistory }

[<RequireQualifiedAccess>]
type AckReconcile =
    | Applied of ClientSyncState * SyncInfo * Effect list * Op list
    | Ignored
    | Rejected of string

[<RequireQualifiedAccess>]
module SyncLogic =

    /// Determine if the poll response indicates the client is outdated.
    /// Returns Some CodeOutdated if server has new code (build stamps differ),
    /// Some DataOutdated if server revision is ahead of the client,
    /// or None if the client is up to date.
    /// CodeOutdated takes priority when both conditions hold.
    /// Callers must only invoke this when there are no pending local changes
    /// (otherwise a higher server revision may reflect our own in-flight POST).
    let getPollOutcome
        (poll: ChangeSuccessResponse)
        (clientRev: int)
        (context: ClientPollContext)
        : SyncState option =
        let codeOutdated =
            (context.buildEpochSec <> 0 && context.pageBuildEpochSec <> 0)
            && (poll.buildEpochSec <> context.buildEpochSec
                || poll.pageBuildEpochSec <> context.pageBuildEpochSec)
        let dataOutdated = poll.revision.Value > clientRev
        if codeOutdated then Some CodeOutdated
        elif dataOutdated then Some DataOutdated
        else None

    let private asProjectionState (state: ClientSyncState) : State =
        { graph = state.graph
          revision = state.revision
          history = History.empty }

    let private withProjectedGraph
        (state: ClientSyncState)
        (projected: State)
        : ClientSyncState =
        { state with
            graph = projected.graph
            revision = Revision (state.revision.Value + 1) }

    let private foldProjectedChanges
        (changes: Change list)
        (state: ClientSyncState)
        : Result<ClientSyncState, string> =
        changes
        |> List.fold
            (fun acc change ->
                match acc with
                | Error _ -> acc
                | Ok st ->
                    match ResidentProjection.applyChange change (asProjectionState st) with
                    | ApplyResult.Changed newSt
                    | ApplyResult.Unchanged newSt ->
                        Ok (withProjectedGraph st newSt)
                    | ApplyResult.Invalid (_, msg) -> Error msg)
            (Ok state)

    let private pendingItem
        (kind: PendingKind)
        (recordId: int)
        (change: Change)
        : PendingChange =
        { change = change
          transition =
            Some
                { recordId = recordId
                  submittedChangeId = change.changeId
                  kind = kind } }

    /// Apply a Sync response atomically under Loaded rules.
    /// Packages install after the projected tail so authoritative snapshots at the
    /// response revision win. Poll and Post consume paths preserve History.
    let applySyncResponse
        (response: SyncResponse)
        (state: ClientSyncState)
        : Result<ClientSyncState, string> =
        match foldProjectedChanges response.changes state with
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
            List.isEmpty response.changes
            && not (List.isEmpty response.packages)
        if
            packageOnly
            && (hasPendingLocal || responseRevision <> state.revision.Value)
        then
            Error "raced package payload"
        else
            applySyncResponse response state

    let loadResponseToSync (response: LoadResponse) : SyncResponse =
        { changes = response.changes
          packages = response.packages }

    let loadResponseToPoll (response: LoadResponse) : ChangeSuccessResponse =
        { revision = Revision response.revision
          buildEpochSec = response.buildEpochSec
          pageBuildEpochSec = response.pageBuildEpochSec
          isReady = response.isReady
          externalChanges = not response.changes.IsEmpty
          changes = response.changes
          message = None }

    /// Apply a server-supplied Change tail onto local State (Poll path).
    /// Empty list is a no-op that preserves History.
    let applyServerTail
        (changes: Change list)
        (state: ClientSyncState)
        : Result<ClientSyncState, string> =
        applySyncResponse { changes = changes; packages = [] } state

    let private undoPendingGraph
        (state: ClientSyncState)
        (pending: PendingChange list)
        : Graph =
        pending
        |> List.rev
        |> List.fold
            (fun graph item ->
                let inverse =
                    Change.inverse
                        state.revision
                        item.change.changeId
                        item.change
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
        (commandName: string)
        (change: Change)
        (state: ClientSyncState)
        : Result<ClientSyncState * PendingChange, string> =
        match ResidentProjection.applyChange change (asProjectionState state) with
        | ApplyResult.Invalid (_, msg) -> Error msg
        | ApplyResult.Unchanged _ -> Error "Change did not change state"
        | ApplyResult.Changed newState ->
            let history, recordId =
                ClientHistory.record commandName change state.history
            Ok(
                { state with
                    graph = newState.graph
                    history = history },
                pendingItem PendingKind.Normal recordId change)

    let private applyInverse
        (kind: PendingKind)
        (planned: (Change * string * ClientHistory * int) option)
        (state: ClientSyncState)
        : Result<ClientSyncState * PendingChange, string> option =
        match planned with
        | None -> None
        | Some (inverse, _, history, recordId) ->
            match ResidentProjection.applyChange inverse (asProjectionState state) with
            | ApplyResult.Invalid (_, msg) -> Some (Error msg)
            | ApplyResult.Unchanged newState
            | ApplyResult.Changed newState ->
                Some(
                    Ok(
                        { state with
                            graph = newState.graph
                            history = history },
                        pendingItem kind recordId inverse))

    let applyLocalUndo
        (changeId: System.Guid)
        (state: ClientSyncState)
        : Result<ClientSyncState * PendingChange, string> option =
        applyInverse
            PendingKind.Undo
            (ClientHistory.undo state.revision changeId state.history)
            state

    let applyLocalRedo
        (changeId: System.Guid)
        (state: ClientSyncState)
        : Result<ClientSyncState * PendingChange, string> option =
        applyInverse
            PendingKind.Redo
            (ClientHistory.redo state.revision changeId state.history)
            state

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
        (confirmed: Change list)
        : string option =
        if confirmed.Length < submitted.Length then
            Some "missing confirmation"
        elif confirmed.Length > submitted.Length then
            Some "unmatched confirmation"
        else
            let subIds = submitted |> List.map (fun item -> item.change.changeId)
            let confIds = confirmed |> List.map (fun change -> change.changeId)
            if subIds = confIds then
                None
            else
                let subSet = Set.ofList subIds
                let confSet = Set.ofList confIds
                if subSet = confSet then Some "reordered confirmation"
                else Some "unmatched confirmation"

    let private collectSuffixes
        (submitted: PendingChange list)
        (confirmed: Change list)
        : Result<Op list, string> =
        let rec loop acc submittedItems (confirmedItems: Change list) =
            match submittedItems, confirmedItems with
            | [], [] -> Ok (List.concat (List.rev acc))
            | item :: items, confirmedChange :: rest ->
                match takeSuffix item.change.ops confirmedChange.ops with
                | Error err -> Error err
                | Ok extra -> loop (extra :: acc) items rest
            | _ -> Error "missing confirmation"
        loop [] submitted confirmed

    let private sameBody (left: PendingChange) (right: PendingChange) =
        left.change.changeId = right.change.changeId
        && left.change.ops = right.change.ops

    let private isQueuePrefix
        (submitted: PendingChange list)
        (pending: PendingChange list)
        =
        pending.Length >= submitted.Length
        && List.forall2 sameBody submitted (List.take submitted.Length pending)

    let private isPresent pending changeId =
        pending
        |> List.exists (fun item -> item.change.changeId = changeId)

    let private queueOutcome submitted pending serverRev clientRev =
        if isQueuePrefix submitted pending then
            Ok "apply"
        else
            let present =
                submitted
                |> List.map (fun item -> isPresent pending item.change.changeId)
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
                  changeId = System.Guid.Empty
                  ops = suffixOps }
            match ResidentProjection.applyChange change (asProjectionState state) with
            | ApplyResult.Invalid (_, msg) -> Error msg
            | ApplyResult.Unchanged projected
            | ApplyResult.Changed projected -> Ok projected.graph

    let isConfirmationEcho (submitted: PendingChange list) (confirmed: Change list) =
        if submitted.IsEmpty then
            false
        else
            match identityError submitted confirmed with
            | Some _ -> false
            | None ->
                match collectSuffixes submitted confirmed with
                | Error _ -> false
                | Ok _ -> true

    /// Rewind to the noted baseline and replay a Poll Change list without clearing History.
    let consumeCatchUpPoll
        (baseline: CatchUpBaseline)
        (changes: Change list)
        (serverRevision: Revision)
        (state: ClientSyncState)
        : Result<ClientSyncState, string> =
        let atBaseline =
            { state with
                graph = baseline.graph
                revision = baseline.revision }
        match foldProjectedChanges changes atBaseline with
        | Error msg -> Error msg
        | Ok afterChanges ->
            Ok
                { afterChanges with
                    revision = serverRevision
                    history = state.history }

    let reconcileExternalAck
        (submitted: PendingChange list)
        (serverRevision: Revision)
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
        (confirmed: Change list)
        (serverRevision: Revision)
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
                    match
                        queueOutcome
                            submitted
                            syncInfo.pendingChanges
                            serverRevision.Value
                            state.revision.Value
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

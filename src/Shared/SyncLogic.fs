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
    /// Returns Some CodeOutdated if Poll apiVersion differs from ApiVersion.current,
    /// Some DataOutdated if server event id is ahead of the client,
    /// or None if the client is up to date.
    /// CodeOutdated takes priority when both conditions hold.
    /// Callers must only invoke this when there are no pending local events
    /// (otherwise a higher server event id may reflect our own in-flight POST).
    let getPollOutcome
        (poll: ChangeSuccessResponse)
        (clientEventId: EventId)
        : SyncState option =
        let codeOutdated = poll.apiVersion <> ApiVersion.current
        let dataOutdated = poll.eventId > clientEventId
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
          eventId = state.eventId }

    let private withProjectedGraph
        (event: Ev)
        (state: ClientSyncState)
        (projected: State)
        : ClientSyncState =
        { state with
            graph = projected.graph
            eventId = event.id
            actorLiveFocusIds =
                ActorLive.applyEvent event state.actorLiveFocusIds }

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
                        Ok (withProjectedGraph event st newSt)
                    | ApplyResult.Invalid (_, msg) -> Error msg)
            (Ok state)

    /// Apply a Sync response atomically under Loaded rules.
    /// Packages install after the projected tail so authoritative snapshots at the
    /// response event id win. Poll and Post consume paths preserve History.
    let applySyncResponse
        (response: SyncResponse)
        (state: ClientSyncState)
        : Result<ClientSyncState, string> =
        match foldProjectedEvents response.events state with
        | Error msg -> Error msg
        | Ok afterEvents ->
            let graph =
                ResidentProjection.installPackages
                    response.packages
                    afterEvents.graph
            Ok { afterEvents with graph = graph }

    let applyLoadResponse
        (responseEventId: EventId)
        (hasPendingLocal: bool)
        (response: SyncResponse)
        (state: ClientSyncState)
        : Result<ClientSyncState, string> =
        let packageOnly =
            List.isEmpty response.events
            && not (List.isEmpty response.packages)
        if
            packageOnly
            && (hasPendingLocal || responseEventId <> state.eventId)
        then
            Error "raced package payload"
        else
            applySyncResponse response state

    let loadResponseToSync (response: LoadResponse) : SyncResponse =
        { events = response.events
          packages = response.packages }

    let loadResponseToPoll (response: LoadResponse) : ChangeSuccessResponse =
        { eventId = response.eventId
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

    let private identityError
        (submitted: Ev list)
        (confirmed: Ev list)
        : string option =
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

    /// Rewind to the noted baseline and replay a Poll Ev list without clearing History.
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
        | Ok afterEvents ->
            Ok
                { afterEvents with
                    eventId = serverEventId
                    history = ClientHistory.approve events state.history }

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
                            let nextState =
                                { state with
                                    graph = graph
                                    eventId = serverEventId
                                    history =
                                        ClientHistory.approve
                                            confirmed
                                            state.history }
                            let nextSync, _, effects =
                                SyncPlanner.retireSubmittedPrefix
                                    submitted.Length
                                    serverEventId
                                    syncInfo
                            AckReconcile.Applied(
                                nextState, nextSync, effects, suffixOps)

namespace Gambol.Shared

/// Client-side values needed to determine if a poll response implies the client is outdated.
type ClientPollContext =
    { buildEpochSec: int
      pageBuildEpochSec: int }

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
        (poll: PollResponse)
        (clientRev: int)
        (context: ClientPollContext)
        : SyncState option =
        let codeOutdated =
            (context.buildEpochSec <> 0 && context.pageBuildEpochSec <> 0)
            && (poll.buildEpochSec <> context.buildEpochSec
                || poll.pageBuildEpochSec <> context.pageBuildEpochSec)
        let dataOutdated = poll.revision > clientRev
        if codeOutdated then Some CodeOutdated
        elif dataOutdated then Some DataOutdated
        else None

    let private advanceOne (st: State) (newSt: State) : State =
        { newSt with revision = Revision (st.revision.Value + 1) }

    let private foldProjectedChanges
        (changes: Change list)
        (state: State)
        : Result<State, string> =
        changes
        |> List.fold
            (fun acc change ->
                match acc with
                | Error _ -> acc
                | Ok st ->
                    match ResidentProjection.applyChange change st with
                    | ApplyResult.Changed newSt
                    | ApplyResult.Unchanged newSt ->
                        Ok (advanceOne st newSt)
                    | ApplyResult.Invalid (_, msg) -> Error msg)
            (Ok state)

    /// Apply a Sync response atomically under Loaded rules.
    /// Non-empty Change tails clear local History first; packages install after
    /// the projected tail so authoritative snapshots at the response revision win.
    let applySyncResponse
        (response: SyncResponse)
        (state: State)
        : Result<State, string> =
        let afterHistory =
            if List.isEmpty response.changes then
                state
            else
                { state with history = History.empty }
        match foldProjectedChanges response.changes afterHistory with
        | Error msg -> Error msg
        | Ok afterChanges ->
            let graph =
                ResidentProjection.installPackages
                    response.packages
                    afterChanges.graph
            Ok { afterChanges with graph = graph }

    let loadResponseToSync (response: LoadResponse) : SyncResponse =
        { changes = response.changes
          packages = response.packages }

    let loadResponseToPoll (response: LoadResponse) : PollResponse =
        { revision = response.revision
          buildEpochSec = response.buildEpochSec
          pageBuildEpochSec = response.pageBuildEpochSec
          isReady = response.isReady
          changes = response.changes }

    /// Apply a server-supplied Change tail onto local State (Poll path).
    /// Empty list is a no-op that preserves History.
    let applyServerTail
        (changes: Change list)
        (state: State)
        : Result<State, string> =
        applySyncResponse { changes = changes; packages = [] } state

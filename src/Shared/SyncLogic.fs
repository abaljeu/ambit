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

    /// Apply a server-supplied change tail onto local state.
    /// Trusted apply (no per-change ownership check), then one final ownership
    /// validation. Ok increments revision by one per change; Error if any change
    /// is invalid or the final graph fails ownership. Empty list is a no-op.
    let applyServerTail (changes: Change list) (state: State) : Result<State, string> =
        if List.isEmpty changes then
            Ok state
        else
            let folded =
                changes
                |> List.fold
                    (fun acc change ->
                        match acc with
                        | Error _ -> acc
                        | Ok st ->
                            match History.applyChangeTrusted change st with
                            | ApplyResult.Changed newSt ->
                                Ok
                                    { newSt with
                                          revision =
                                              Revision (st.revision.Value + 1) }
                            | ApplyResult.Unchanged newSt ->
                                Ok
                                    { newSt with
                                          revision =
                                              Revision (st.revision.Value + 1) }
                            | ApplyResult.Invalid (_, msg) -> Error msg)
                    (Ok state)
            match folded with
            | Error _ -> folded
            | Ok finalSt ->
                match History.validateOwnership finalSt.graph with
                | Error msg -> Error msg
                | Ok () -> Ok finalSt

namespace Gambol.Server

open Gambol.Shared

/// Mailbox-owned admitted Caller set. Not a second inbox.
type CoreCredentials = private { callers: Set<Caller> }

[<RequireQualifiedAccess>]
module CoreCredentials =

    let empty = { callers = Set.empty }

    let ofCallers (callers: Set<Caller>) : CoreCredentials =
        { callers = callers }

    let add (caller: Caller) (creds: CoreCredentials) =
        { callers = Set.add caller creds.callers }

    let remove (caller: Caller) (creds: CoreCredentials) =
        { callers = Set.remove caller creds.callers }

    let contains (caller: Caller) (creds: CoreCredentials) =
        Set.contains caller creds.callers

type CoreAdmissionError =
    | Unauthorized
    | UnknownJob
    | UnknownActor
    | Overlap

module CoreAdmissionError =

    let text (err: CoreAdmissionError) =
        match err with
        | Unauthorized -> "Unauthorized"
        | UnknownJob -> "unknown job"
        | UnknownActor -> "unknown actor"
        | Overlap -> "span overlaps a live job"

[<RequireQualifiedAccess>]
module CoreAuth =

    let refuse = CoreAdmissionError.text Unauthorized

    let isAuthRefuse (err: string) = err = refuse

    let admit (live: bool) : Result<unit, CoreAdmissionError> =
        if live then Ok () else Error Unauthorized

    let post
        (live: bool)
        (enqueue:
            Change list -> Async<Result<CoreChangesAccepted, string>>)
        (changes: Change list)
        : Async<Result<CoreChangesAccepted, string>> =
        async {
            match admit live with
            | Error err -> return Error(CoreAdmissionError.text err)
            | Ok () -> return! enqueue changes
        }

    /// Stamp Caller onto posts; mailbox CoreMsg validates before persist.
    let bindHandle (caller: Caller) (handle: CoreChanges) : CoreChanges =
        handle.asCaller caller

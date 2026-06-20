namespace Gambol.Shared

open Gambol.Shared.ViewModel

/// Client POST retry timing for continental / slow server links.
[<RequireQualifiedAccess>]
module SyncRetry =
    /// Client-side POST watchdog; browser may still deliver a late 200 after this fires.
    let postTimeoutMs = 60_000

    /// Wait before resending after a client timeout — first POST may still succeed.
    let postTimeoutRetryGraceMs = 20_000

    let private fetchFailedDelayMs (attempt: int) : int =
        min (1000 * (pown 2 (max 0 (attempt - 1)))) 20_000

    let retryDelayMs (attempt: int) (kind: SubmitNetworkErrorKind) : int =
        match kind with
        | SubmitNetworkErrorKind.ClientTimeout -> postTimeoutRetryGraceMs
        | SubmitNetworkErrorKind.FetchFailed -> fetchFailedDelayMs attempt

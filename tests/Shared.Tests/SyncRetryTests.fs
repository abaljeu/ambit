module Gambol.Shared.Tests.SyncRetryTests

open Xunit
open Gambol.Shared
open Gambol.Shared.ViewModel

[<Fact>]
let ``ClientTimeout retry waits grace period`` () =
    Assert.Equal(20_000, SyncRetry.retryDelayMs 1 SubmitNetworkErrorKind.ClientTimeout)
    Assert.Equal(20_000, SyncRetry.retryDelayMs 3 SubmitNetworkErrorKind.ClientTimeout)

[<Fact>]
let ``FetchFailed retry uses exponential backoff`` () =
    Assert.Equal(1000, SyncRetry.retryDelayMs 1 SubmitNetworkErrorKind.FetchFailed)
    Assert.Equal(2000, SyncRetry.retryDelayMs 2 SubmitNetworkErrorKind.FetchFailed)
    Assert.Equal(4000, SyncRetry.retryDelayMs 3 SubmitNetworkErrorKind.FetchFailed)
    Assert.Equal(20_000, SyncRetry.retryDelayMs 10 SubmitNetworkErrorKind.FetchFailed)

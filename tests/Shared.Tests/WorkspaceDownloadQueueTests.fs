module WorkspaceDownloadQueueTests

open System
open Gambol.Shared
open Xunit

let private scope label =
    { label = label
      relative = ""
      kind = SyncScopeKind.Workspace }

[<Fact>]
let ``first enqueue starts running immediately`` () =
    let result, state = WorkspaceDownloadQueue.tryEnqueue WorkspaceDownloadQueue.empty (scope "home")
    match result with
    | WorkspaceDownloadQueue.EnqueueResult.Started job ->
        Assert.Equal(WorkspaceDownloadQueue.JobState.Running, job.state)
        Assert.True(state.running.IsSome)
        Assert.True(state.queued.IsNone)
    | other -> Assert.Fail($"expected Started, got {other}")

[<Fact>]
let ``second enqueue is queued when one running`` () =
    let _, s1 = WorkspaceDownloadQueue.tryEnqueue WorkspaceDownloadQueue.empty (scope "a")
    let result, s2 = WorkspaceDownloadQueue.tryEnqueue s1 (scope "b")
    match result with
    | WorkspaceDownloadQueue.EnqueueResult.Queued job ->
        Assert.Equal(WorkspaceDownloadQueue.JobState.Queued, job.state)
        Assert.True(s2.running.IsSome)
        Assert.True(s2.queued.IsSome)
    | other -> Assert.Fail($"expected Queued, got {other}")

[<Fact>]
let ``third enqueue is refused when queue full`` () =
    let _, s1 = WorkspaceDownloadQueue.tryEnqueue WorkspaceDownloadQueue.empty (scope "a")
    let _, s2 = WorkspaceDownloadQueue.tryEnqueue s1 (scope "b")
    let result, s3 = WorkspaceDownloadQueue.tryEnqueue s2 (scope "c")
    match result with
    | WorkspaceDownloadQueue.EnqueueResult.Refused msg ->
        Assert.Contains("queue full", msg)
        Assert.Equal(s2, s3)
    | other -> Assert.Fail($"expected Refused, got {other}")

[<Fact>]
let ``finishRunning promotes queued to running`` () =
    let _, s1 = WorkspaceDownloadQueue.tryEnqueue WorkspaceDownloadQueue.empty (scope "a")
    let _, s2 = WorkspaceDownloadQueue.tryEnqueue s1 (scope "b")
    let s3 = WorkspaceDownloadQueue.finishRunning s2 true "done"
    match s3.running with
    | Some job ->
        Assert.Equal(WorkspaceDownloadQueue.JobState.Running, job.state)
        Assert.Equal("b", job.scope.label)
    | None -> Assert.Fail("expected promoted running job")
    Assert.True(s3.queued.IsNone)

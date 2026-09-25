module Gambol.CloudAgents.Tests.AgentRunnerFakeTests

open System
open System.Threading
open System.Threading.Tasks
open Xunit
open Gambol.CloudAgents

[<Collection("CloudAgents runner")>]
type AgentRunnerFakeTests() =

    let emptyOptions =
        { AgentOptions.DisplayName = None
          ModelHint = None
          ModelParams = [] }

    let unusedConfig = { RunnerConfig.ApiKey = "unused" }

    let sampleResult text =
        { AgentResult.Text = text
          Git = [] }

    let clearFake () =
        let deadline = DateTime.UtcNow.AddSeconds 2.0
        let rec spin () =
            if AgentRunner.setFake None then
                true
            elif DateTime.UtcNow > deadline then
                false
            else
                Thread.Sleep 10
                spin ()
        Assert.True(spin ())

    let withFake handler body =
        Assert.True(AgentRunner.setFake (Some handler))
        try
            body ()
        finally
            clearFake ()

    let waitFinished agentId runId remainingMs =
        let rec spin left =
            match AgentRunner.poll unusedConfig agentId runId with
            | Ok(Finished result) -> Some result
            | Ok Running when left > 0 ->
                Thread.Sleep 10
                spin (left - 10)
            | _ -> None
        spin remainingMs

    let streamOnce agentId runId =
        AgentRunner.streamUntilComplete
            { Config = unusedConfig
              AgentId = agentId
              RunId = runId
              MaxWaitMs = Some 2000 }
            { Seed = ()
              OnEvent = fun () _ -> () }

    let withFakeStream events body =
        withFake
            (fun _ -> Finished(sampleResult "ignored"))
            (fun () ->
                Assert.True(AgentRunner.setFakeStream (Some(fun _ -> events)))
                match
                    AgentRunner.start unusedConfig "pack" None emptyOptions
                with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) -> body agentId runId)

    let streamArgs agentId runId maxWaitMs =
        { Config = unusedConfig
          AgentId = agentId
          RunId = runId
          MaxWaitMs = maxWaitMs }

    let deltas count =
        [ for i in 1..count -> AssistantText(string i) ]

    [<Fact>]
    member _.``setFake Some routes start and stream without HTTP``() =
        let seen = ref None
        withFake
            (fun args ->
                seen := Some args.Prompt
                Finished(sampleResult "fake-reply"))
            (fun () ->
                let started =
                    AgentRunner.start
                        unusedConfig "pack-body" None emptyOptions
                match started with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) ->
                    Assert.False(String.IsNullOrEmpty agentId)
                    Assert.False(String.IsNullOrEmpty runId)
                    match streamOnce agentId runId with
                    | Ok(result, _) ->
                        Assert.Equal("fake-reply", result.Text)
                    | Error err -> Assert.Fail($"stream: {err}")
                    Assert.Equal(Some "pack-body", !seen))

    [<Fact>]
    member _.``setFake None restores CursorAdapter reject``() =
        Assert.True(AgentRunner.setFake None)
        let result =
            AgentRunner.start
                { RunnerConfig.ApiKey = "" }
                "live-reject"
                None
                emptyOptions
        match result with
        | Error(AuthenticationFailed msg) ->
            Assert.Equal(
                AgentMessage.couldNotSend "Cursor" "missing key",
                msg)
        | other -> Assert.Fail($"expected missing-key auth, {other}")

    [<Fact>]
    member _.``setFake yields Failed unauthorized named Cursor``() =
        let named =
            AgentMessage.couldNotSend "Cursor" "unauthorized"
        withFake
            (fun _ -> Failed named)
            (fun () ->
                let started =
                    AgentRunner.start
                        unusedConfig "pack" None emptyOptions
                match started with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) ->
                    match streamOnce agentId runId with
                    | Error(ApiError("failed", msg)) ->
                        Assert.Equal(named, msg)
                    | other -> Assert.Fail($"expected failed, {other}"))

    [<Fact>]
    member _.``setFake refuses while a handler is in flight``() =
        let refused = ref false
        withFake
            (fun _ ->
                refused := not (AgentRunner.setFake None)
                Finished(sampleResult "during"))
            (fun () ->
                let started =
                    AgentRunner.start
                        unusedConfig "x" None emptyOptions
                match started with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) ->
                    let deadline = DateTime.UtcNow.AddSeconds 2.0
                    while not !refused && DateTime.UtcNow < deadline do
                        Thread.Sleep 10
                    Assert.True(!refused)
                    streamOnce agentId runId |> ignore)

    [<Fact>]
    member _.``hanging fake stream waits until cancel``() =
        withFake
            (fun _ ->
                AgentRunner.waitForCancel 8000 |> ignore
                Finished(sampleResult "late"))
            (fun () ->
                let started =
                    AgentRunner.start
                        unusedConfig "hang" None emptyOptions
                match started with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) ->
                    let seen = ref "none"
                    let worker =
                        Thread(fun () ->
                            match streamOnce agentId runId with
                            | Error(ApiError("cancelled", _)) ->
                                seen := "cancelled"
                            | _ -> seen := "other")
                    worker.Start()
                    match
                        AgentRunner.cancel unusedConfig agentId runId
                    with
                    | Ok CancelRequested -> ()
                    | other -> Assert.Fail($"cancel: {other}")
                    match AgentRunner.poll unusedConfig agentId runId with
                    | Ok Cancelled -> ()
                    | other ->
                        Assert.Fail($"expected Cancelled, {other}")
                    Assert.True(worker.Join 2000)
                    Assert.Equal("cancelled", !seen)
                    Assert.True(AgentRunner.fakeCancelCount() >= 1)
                    match
                        AgentRunner.waitUntilComplete
                            unusedConfig agentId runId 10 (Some 500)
                    with
                    | Error(ApiError("cancelled", _)) -> ()
                    | other ->
                        Assert.Fail($"expected cancelled wait, {other}"))

    [<Fact>]
    member _.``streamUntilComplete emits fake stream events``() =
        withFake
            (fun _ -> Finished(sampleResult "ignored"))
            (fun () ->
                Assert.True(
                    AgentRunner.setFakeStream (Some (fun _ ->
                        [ AssistantText "hel"
                          AssistantText "lo"
                          RunFinished(sampleResult "hello") ]))
                )
                let started =
                    AgentRunner.start
                        unusedConfig "pack" None emptyOptions
                match started with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) ->
                    match
                        AgentRunner.streamUntilComplete
                            { Config = unusedConfig
                              AgentId = agentId
                              RunId = runId
                              MaxWaitMs = None }
                            { Seed = []
                              OnEvent = fun seen ev -> ev :: seen }
                    with
                    | Ok(result, seen) ->
                        Assert.Equal("hello", result.Text)
                        let expected =
                            [ AssistantText "hel"
                              AssistantText "lo"
                              RunFinished(sampleResult "hello") ]
                        Assert.Equal<AgentStreamEvent list>(
                            expected, List.rev seen)
                    | Error err -> Assert.Fail($"stream: {err}"))

    [<Fact>]
    member _.``fake stream delivers deltas one at a time``() =
        let events = deltas 3 @ [ RunFinished(sampleResult "123") ]
        withFakeStream events (fun agentId runId ->
            let seen = ref []
            let firstDelta = new ManualResetEventSlim(false)
            let fold =
                { Seed = ()
                  OnEvent =
                    fun () ev ->
                        lock seen (fun () -> seen := ev :: !seen)
                        firstDelta.Set() }
            let streaming =
                Tasks.Task.Run(fun () ->
                    AgentRunner.streamUntilComplete
                        (streamArgs agentId runId None)
                        fold)
            Assert.True(firstDelta.Wait 2000)
            let midStream = lock seen (fun () -> List.rev !seen)
            Assert.Equal<AgentStreamEvent list>(
                [ AssistantText "1" ], midStream)
            match streaming.Result with
            | Ok(result, ()) -> Assert.Equal("123", result.Text)
            | Error err -> Assert.Fail($"stream: {err}")
            Assert.Equal<AgentStreamEvent list>(events, List.rev !seen))

    [<Fact>]
    member _.``fake stream error delivers RunFailed to the fold``() =
        let events = [ AssistantText "part"; RunFailed "boom" ]
        withFakeStream events (fun agentId runId ->
            let seen = ref []
            let fold =
                { Seed = ()
                  OnEvent = fun () ev -> seen := ev :: !seen }
            match
                AgentRunner.streamUntilComplete
                    (streamArgs agentId runId None)
                    fold
            with
            | Error(ApiError("failed", "boom")) -> ()
            | other -> Assert.Fail($"expected failed stream, {other}")
            Assert.Equal<AgentStreamEvent list>(events, List.rev !seen))

    [<Fact>]
    member _.``fake stream cancel mid-stream delivers RunCancelled``() =
        let events = deltas 5 @ [ RunFinished(sampleResult "12345") ]
        withFakeStream events (fun agentId runId ->
            let seen = ref []
            let fold =
                { Seed = ()
                  OnEvent =
                    fun () ev ->
                        seen := ev :: !seen
                        AgentRunner.cancel unusedConfig agentId runId
                        |> ignore }
            match
                AgentRunner.streamUntilComplete
                    (streamArgs agentId runId None)
                    fold
            with
            | Error(ApiError("cancelled", _)) -> ()
            | other -> Assert.Fail($"expected cancelled stream, {other}")
            Assert.Equal<AgentStreamEvent list>(
                [ AssistantText "1"; RunCancelled ], List.rev !seen))

    [<Fact>]
    member _.``fake cancel mid-stream is requested once then not cancellable``() =
        let events = deltas 5 @ [ RunFinished(sampleResult "12345") ]
        withFakeStream events (fun agentId runId ->
            let outcomes = ref []
            let fold =
                { Seed = []
                  OnEvent =
                    fun seen ev ->
                        let outcome =
                            AgentRunner.cancel unusedConfig agentId runId
                        outcomes := outcome :: !outcomes
                        ev :: seen }
            match
                AgentRunner.streamUntilComplete
                    (streamArgs agentId runId None)
                    fold
            with
            | Error(ApiError("cancelled", _)) -> ()
            | other -> Assert.Fail($"expected cancelled stream, {other}")
            let expected: Result<CancelOutcome, AgentError> list =
                [ Ok CancelRequested; Ok NotCancellable ]
            Assert.Equal<Result<CancelOutcome, AgentError> list>(
                expected, List.rev !outcomes))

    [<Fact>]
    member _.``fake cancel after stream finished is not cancellable``() =
        let events = [ AssistantText "a"; RunFinished(sampleResult "a") ]
        withFakeStream events (fun agentId runId ->
            let fold = { Seed = (); OnEvent = fun () _ -> () }
            match
                AgentRunner.streamUntilComplete
                    (streamArgs agentId runId None)
                    fold
            with
            | Ok _ -> ()
            | Error err -> Assert.Fail($"stream: {err}")
            match AgentRunner.cancel unusedConfig agentId runId with
            | Ok NotCancellable -> ()
            | other -> Assert.Fail($"expected NotCancellable, {other}")
            Assert.Equal(0, AgentRunner.fakeCancelCount ()))

    [<Fact>]
    member _.``fake cancel after poll Finished is not cancellable``() =
        withFake
            (fun _ -> Finished(sampleResult "done"))
            (fun () ->
                match AgentRunner.start unusedConfig "pack" None emptyOptions with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) ->
                    Assert.True((waitFinished agentId runId 2000).IsSome)
                    match AgentRunner.cancel unusedConfig agentId runId with
                    | Ok NotCancellable -> ()
                    | other ->
                        Assert.Fail($"expected NotCancellable, {other}")
                    match AgentRunner.poll unusedConfig agentId runId with
                    | Ok(Finished _) -> ()
                    | other -> Assert.Fail($"expected Finished, {other}"))

    [<Fact>]
    member _.``fake stream honors MaxWaitMs with RunFailed timeout``() =
        let events = deltas 50 @ [ RunFinished(sampleResult "late") ]
        withFakeStream events (fun agentId runId ->
            let seen = ref []
            let fold =
                { Seed = ()
                  OnEvent = fun () ev -> seen := ev :: !seen }
            match
                AgentRunner.streamUntilComplete
                    (streamArgs agentId runId (Some 100))
                    fold
            with
            | Error AgentError.Timeout -> ()
            | other -> Assert.Fail($"expected timeout, {other}")
            match !seen with
            | RunFailed "timeout" :: earlier ->
                Assert.NotEmpty(earlier)
                Assert.True(List.length earlier < 50)
            | other -> Assert.Fail($"expected RunFailed timeout, {other}"))

    [<Fact>]
    member _.``partial fake stream waits until cancel``() =
        withFake
            (fun _ ->
                AgentRunner.waitForCancel 8000 |> ignore
                Finished(sampleResult "late"))
            (fun () ->
                Assert.True(
                    AgentRunner.setFakeStream (Some (fun _ ->
                        [ AssistantText "kept" ]))
                )
                let started =
                    AgentRunner.start
                        unusedConfig "pack" None emptyOptions
                match started with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) ->
                    let sawText = new ManualResetEvent(false)
                    let seen = ref "none"
                    let worker =
                        Thread(fun () ->
                            match
                                AgentRunner.streamUntilComplete
                                    (streamArgs agentId runId None)
                                    { Seed = ()
                                      OnEvent =
                                        fun () ev ->
                                            match ev with
                                            | AssistantText _ ->
                                                sawText.Set()
                                                |> ignore
                                            | _ -> () }
                            with
                            | Error(ApiError("cancelled", _)) ->
                                seen := "cancelled"
                            | _ -> seen := "other")
                    worker.Start()
                    Assert.True(sawText.WaitOne 2000)
                    match
                        AgentRunner.cancel
                            unusedConfig agentId runId
                    with
                    | Ok CancelRequested -> ()
                    | other -> Assert.Fail($"cancel: {other}")
                    Assert.True(worker.Join 2000)
                    Assert.Equal("cancelled", !seen))

    [<Fact>]
    member _.``setFake handler yields Failed not Finished``() =
        withFake
            (fun _ -> Failed "provider-boom")
            (fun () ->
                let started =
                    AgentRunner.start
                        unusedConfig "pack" None emptyOptions
                match started with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) ->
                    match streamOnce agentId runId with
                    | Error(ApiError("failed", msg)) ->
                        Assert.Equal("provider-boom", msg)
                    | other ->
                        Assert.Fail($"expected failed stream, {other}"))


namespace Gambol.CloudAgents

open System
open System.Threading

module AgentRunner =

    module private Fake =
        let handler =
            ref (None: (StartArgs -> AgentStatus) option)
        let streamHandler =
            ref (None: (StartArgs -> AgentStreamEvent list) option)
        let streamEvents =
            ref Map.empty<string * string, AgentStreamEvent list>
        let results = ref Map.empty<string * string, AgentStatus>
        let cancelled = ref Set.empty<string * string>
        let cancelCount = ref 0
        let inFlight = ref 0
        let gate = obj ()
        let cancelPulse = new ManualResetEvent(false)

        let withFlight work =
            lock gate (fun () -> inFlight := !inFlight + 1)
            try
                work ()
            finally
                lock gate (fun () -> inFlight := !inFlight - 1)

        let trySet next =
            lock gate (fun () ->
                match next with
                | None -> cancelPulse.Set() |> ignore
                | Some _ -> ()
                if !inFlight > 0 then
                    false
                else
                    handler := next
                    streamHandler := None
                    streamEvents := Map.empty
                    results := Map.empty
                    cancelled := Set.empty
                    cancelCount := 0
                    cancelPulse.Reset() |> ignore
                    true)

        let current () =
            lock gate (fun () -> !handler)

        let currentStream () =
            lock gate (fun () -> !streamHandler)

        let trySetStream next =
            lock gate (fun () ->
                if !inFlight > 0 then
                    false
                else
                    streamHandler := next
                    streamEvents := Map.empty
                    true)

        let storeStream ids events =
            lock gate (fun () ->
                streamEvents := Map.add ids events !streamEvents
                events
                |> List.tryPick (function
                    | RunFinished result ->
                        Some(Finished result)
                    | RunFailed msg -> Some(Failed msg)
                    | RunCancelled -> Some Cancelled
                    | _ -> None)
                |> Option.iter (fun status ->
                    if Set.contains ids !cancelled then
                        ()
                    else
                        results := Map.add ids status !results))

        let tryGetStream ids =
            lock gate (fun () -> Map.tryFind ids !streamEvents)

        let store ids result =
            lock gate (fun () ->
                if Set.contains ids !cancelled then
                    ()
                else
                    results := Map.add ids result !results)

        let tryGet ids =
            lock gate (fun () -> Map.tryFind ids !results)

        let markCancelled ids =
            lock gate (fun () ->
                cancelled := Set.add ids !cancelled
                cancelCount := !cancelCount + 1
                cancelPulse.Set() |> ignore)

        let isCancelled ids =
            lock gate (fun () -> Set.contains ids !cancelled)

        let waitForCancel (timeoutMs: int) =
            cancelPulse.WaitOne(timeoutMs)

        let requestedCancels () =
            lock gate (fun () -> !cancelCount)

    let setFake
        (handler: (StartArgs -> AgentStatus) option)
        : bool =
        Fake.trySet handler

    /// Optional fake stream sequence (requires setFake Some).
    let setFakeStream
        (handler: (StartArgs -> AgentStreamEvent list) option)
        : bool =
        Fake.trySetStream handler

    /// Block a setFake handler until cancel, or until timeoutMs.
    let waitForCancel (timeoutMs: int) : bool =
        Fake.waitForCancel timeoutMs

    let fakeCancelCount () = Fake.requestedCancels ()

    let private toStartArgs config prompt repos options : StartArgs =
        { Config = config
          Prompt = prompt
          Repos = repos
          Options = options }

    let private startFake f config prompt repos options =
        let agentId = Guid.NewGuid().ToString("N")
        let runId = Guid.NewGuid().ToString("N")
        ThreadPool.QueueUserWorkItem(fun _ ->
            Fake.withFlight (fun () ->
                let args = toStartArgs config prompt repos options
                match Fake.currentStream () with
                | Some streamF -> Fake.storeStream (agentId, runId) (streamF args)
                | None -> ()
                let result = f args
                Fake.store (agentId, runId) result))
        |> ignore
        Ok(agentId, runId)

    let start
        (config: RunnerConfig)
        (prompt: string)
        (repos: RepoConfig list option)
        (options: AgentOptions)
        : Result<string * string, AgentError> =
        match Fake.current () with
        | Some f -> startFake f config prompt repos options
        | None ->
            Fake.withFlight (fun () ->
                Internal.CursorAdapter.startAgent
                    config prompt repos options)

    let private pollFake agentId runId =
        let ids = agentId, runId
        if Fake.isCancelled ids then
            Ok Cancelled
        else
            match Fake.tryGet ids with
            | Some status -> Ok status
            | None -> Ok Running

    let poll
        (config: RunnerConfig)
        (agentId: string)
        (runId: string)
        : Result<AgentStatus, AgentError> =
        match Fake.current () with
        | Some _ -> pollFake agentId runId
        | None ->
            Fake.withFlight (fun () ->
                Internal.CursorAdapter.pollStatus
                    config agentId runId)

    let cancel
        (config: RunnerConfig)
        (agentId: string)
        (runId: string)
        : Result<unit, AgentError> =
        match Fake.current () with
        | Some _ ->
            Fake.markCancelled (agentId, runId)
            Ok()
        | None ->
            Internal.CursorAdapter.cancelRun config agentId runId

    let private cancelledStream () =
        Error(ApiError("cancelled", "Agent run was cancelled"))

    let private pastDeadline (started: DateTime) maxWaitMs =
        match maxWaitMs with
        | None -> false
        | Some max ->
            let elapsed =
                (DateTime.UtcNow - started).TotalMilliseconds
            elapsed > float max

    let private waitLive
        config
        agentId
        runId
        (pollIntervalMs: int)
        maxWaitMs
        : Result<AgentResult, AgentError> =
        let started = DateTime.UtcNow

        let rec loop () =
            if pastDeadline started maxWaitMs then
                Error AgentError.Timeout
            else
                pollOnce ()

        and pollOnce () =
            match poll config agentId runId with
            | Error err -> Error err
            | Ok status ->
                match status with
                | Finished result -> Ok result
                | Cancelled -> cancelledStream ()
                | Failed msg -> Error(ApiError("failed", msg))
                | Creating
                | Running ->
                    Thread.Sleep pollIntervalMs
                    loop ()

        loop ()

    let waitUntilComplete
        (config: RunnerConfig)
        (agentId: string)
        (runId: string)
        (pollIntervalMs: int)
        (maxWaitMs: int option)
        : Result<AgentResult, AgentError> =
        match Fake.current () with
        | Some _ ->
            waitLive
                config agentId runId pollIntervalMs maxWaitMs
        | None ->
            Fake.withFlight (fun () ->
                waitLive
                    config agentId runId pollIntervalMs maxWaitMs)

    let private synthesizeStream (result: AgentResult) =
        if String.IsNullOrEmpty result.Text then
            [ RunFinished result ]
        else
            let half = result.Text.Length / 2
            let first = result.Text.Substring(0, half)
            let second = result.Text.Substring(half)
            [ AssistantText first
              AssistantText second
              RunFinished result ]

    let private streamFromStored ids =
        if Fake.isCancelled ids then
            Some(cancelledStream ())
        else
            match Fake.tryGetStream ids with
            | Some events -> Some(Ok events)
            | None ->
                match Fake.tryGet ids with
                | Some(Finished result) ->
                    Some(Ok(synthesizeStream result))
                | Some Cancelled -> Some(cancelledStream ())
                | Some(Failed msg) ->
                    Some(Error(ApiError("failed", msg)))
                | Some Creating
                | Some Running
                | None -> None

    let private waitFakeStream
        agentId
        runId
        (pollIntervalMs: int)
        maxWaitMs
        =
        let ids = agentId, runId
        let started = DateTime.UtcNow

        let rec waitForEvents () =
            if pastDeadline started maxWaitMs then
                Error AgentError.Timeout
            else
                match streamFromStored ids with
                | Some ready -> ready
                | None ->
                    Thread.Sleep pollIntervalMs
                    waitForEvents ()

        waitForEvents ()

    let private emitFakeStream
        events
        (onEvent: AgentStreamEvent -> unit)
        =
        let missing =
            Error(ApiError("failed", "stream missing terminal event"))

        (missing, events)
        ||> List.fold (fun acc ev ->
            onEvent ev
            match ev with
            | RunFinished result -> Ok result
            | RunFailed msg -> Error(ApiError("failed", msg))
            | RunCancelled -> cancelledStream ()
            | AssistantText _ -> acc)

    let private streamFake
        agentId
        runId
        pollIntervalMs
        maxWaitMs
        onEvent
        =
        match waitFakeStream agentId runId pollIntervalMs maxWaitMs with
        | Error err -> Error err
        | Ok events -> emitFakeStream events onEvent

    let streamUntilComplete
        (config: RunnerConfig)
        (agentId: string)
        (runId: string)
        (pollIntervalMs: int)
        (maxWaitMs: int option)
        (onEvent: AgentStreamEvent -> unit)
        : Result<AgentResult, AgentError> =
        match Fake.current () with
        | Some _ ->
            streamFake agentId runId pollIntervalMs maxWaitMs onEvent
        | None ->
            Fake.withFlight (fun () ->
                Internal.CursorAdapter.streamRun
                    config
                    agentId
                    runId
                    onEvent)

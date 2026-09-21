namespace Gambol.CloudAgents

open System
open System.Threading

module AgentRunner =

    module private Fake =
        let handler =
            ref (None: (StartArgs -> AgentStatus) option)
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
                    results := Map.empty
                    cancelled := Set.empty
                    cancelCount := 0
                    cancelPulse.Reset() |> ignore
                    true)

        let current () =
            lock gate (fun () -> !handler)

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
                let result =
                    f (toStartArgs config prompt repos options)
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

    let private waitLive
        config
        agentId
        runId
        (pollIntervalMs: int)
        maxWaitMs
        : Result<AgentResult, AgentError> =
        let started = DateTime.UtcNow

        let rec loop () =
            match maxWaitMs with
            | Some max ->
                let elapsed =
                    (DateTime.UtcNow - started).TotalMilliseconds
                if elapsed > float max then
                    Error AgentError.Timeout
                else
                    pollOnce ()
            | None -> pollOnce ()

        and pollOnce () =
            match poll config agentId runId with
            | Error err -> Error err
            | Ok status ->
                match status with
                | Finished result -> Ok result
                | Cancelled ->
                    Error(
                        ApiError("cancelled", "Agent run was cancelled")
                    )
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

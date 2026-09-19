namespace Gambol.CloudAgents

open System
open System.Threading

module AgentRunner =

    module private Fake =
        let handler =
            ref (None: (StartArgs -> AgentResult) option)
        let results = ref Map.empty<string * string, AgentResult>
        let inFlight = ref 0
        let gate = obj ()

        let withFlight work =
            lock gate (fun () -> inFlight := !inFlight + 1)
            try
                work ()
            finally
                lock gate (fun () -> inFlight := !inFlight - 1)

        let trySet next =
            lock gate (fun () ->
                if !inFlight > 0 then
                    false
                else
                    handler := next
                    results := Map.empty
                    true)

        let current () =
            lock gate (fun () -> !handler)

        let store ids result =
            lock gate (fun () ->
                results := Map.add ids result !results)

        let tryGet ids =
            lock gate (fun () -> Map.tryFind ids !results)

    let setFake
        (handler: (StartArgs -> AgentResult) option)
        : bool =
        Fake.trySet handler

    let private toStartArgs config prompt repos options : StartArgs =
        { Config = config
          Prompt = prompt
          Repos = repos
          Options = options }

    let private startFake f config prompt repos options =
        Fake.withFlight (fun () ->
            let result =
                f (toStartArgs config prompt repos options)
            let agentId = Guid.NewGuid().ToString("N")
            let runId = Guid.NewGuid().ToString("N")
            Fake.store (agentId, runId) result
            Ok(agentId, runId))

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
        match Fake.tryGet (agentId, runId) with
        | Some result -> Ok(Finished result)
        | None ->
            Error(ApiError("not-found", "fake agent result missing"))

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
        | Some _ -> Ok()
        | None ->
            Internal.CursorAdapter.cancelRun config agentId runId

    let private waitFake agentId runId =
        match Fake.tryGet (agentId, runId) with
        | Some result -> Ok result
        | None ->
            Error(ApiError("not-found", "fake agent result missing"))

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
        | Some _ -> waitFake agentId runId
        | None ->
            Fake.withFlight (fun () ->
                waitLive
                    config agentId runId pollIntervalMs maxWaitMs)

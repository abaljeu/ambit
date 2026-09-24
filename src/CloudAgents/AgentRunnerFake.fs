namespace Gambol.CloudAgents

open System
open System.Threading

module internal AgentRunnerFake =

    type private Seam =
        { Status: (StartArgs -> AgentStatus) option
          Stream: (StartArgs -> AgentStreamEvent list) option
          Events: Map<string * string, AgentStreamEvent list> }

    let private emptySeam =
        { Status = None
          Stream = None
          Events = Map.empty }

    let private handler = ref emptySeam
    let private results = ref Map.empty<string * string, AgentStatus>
    let private cancelled = ref Set.empty<string * string>
    let private cancelCount = ref 0
    let private inFlight = ref 0
    let private gate = obj ()
    let private cancelPulse = new ManualResetEvent(false)

    let internal withFlight work =
        lock gate (fun () -> inFlight := !inFlight + 1)
        try
            work ()
        finally
            lock gate (fun () -> inFlight := !inFlight - 1)

    let private trySet next =
        lock gate (fun () ->
            match next with
            | None -> cancelPulse.Set() |> ignore
            | Some _ -> ()
            if !inFlight > 0 then
                false
            else
                handler := { emptySeam with Status = next }
                results := Map.empty
                cancelled := Set.empty
                cancelCount := 0
                cancelPulse.Reset() |> ignore
                true)

    let internal statusHandler () =
        lock gate (fun () -> (!handler).Status)

    let private currentStream () =
        lock gate (fun () -> (!handler).Stream)

    let private trySetStream next =
        lock gate (fun () ->
            if !inFlight > 0 then
                false
            else
                handler :=
                    { !handler with
                        Stream = next
                        Events = Map.empty }
                true)

    let private storeStream ids events =
        lock gate (fun () ->
            let seam = !handler
            handler :=
                { seam with
                    Events = Map.add ids events seam.Events }
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

    let private tryGetStream ids =
        lock gate (fun () -> Map.tryFind ids (!handler).Events)

    let private store ids result =
        lock gate (fun () ->
            if Set.contains ids !cancelled then
                ()
            else
                results := Map.add ids result !results)

    let private tryGet ids =
        lock gate (fun () -> Map.tryFind ids !results)

    let private markCancelled ids =
        lock gate (fun () ->
            cancelled := Set.add ids !cancelled
            cancelCount := !cancelCount + 1
            cancelPulse.Set() |> ignore)

    let private isCancelled ids =
        lock gate (fun () -> Set.contains ids !cancelled)

    let internal setFake
        (next: (StartArgs -> AgentStatus) option)
        : bool =
        trySet next

    /// Optional fake stream sequence (requires setFake Some).
    let internal setFakeStream
        (next: (StartArgs -> AgentStreamEvent list) option)
        : bool =
        trySetStream next

    /// Block a setFake handler until cancel, or until timeoutMs.
    let internal waitForCancel (timeoutMs: int) : bool =
        cancelPulse.WaitOne(timeoutMs)

    let internal fakeCancelCount () =
        lock gate (fun () -> !cancelCount)

    let private toStartArgs config prompt repos options : StartArgs =
        { Config = config
          Prompt = prompt
          Repos = repos
          Options = options }

    let internal startFake f config prompt repos options =
        let agentId = Guid.NewGuid().ToString("N")
        let runId = Guid.NewGuid().ToString("N")
        ThreadPool.QueueUserWorkItem(fun _ ->
            withFlight (fun () ->
                let args = toStartArgs config prompt repos options
                match currentStream () with
                | Some streamF ->
                    storeStream (agentId, runId) (streamF args)
                | None -> ()
                let result = f args
                store (agentId, runId) result))
        |> ignore
        Ok(agentId, runId)

    let internal pollFake agentId runId =
        let ids = agentId, runId
        if isCancelled ids then
            Ok Cancelled
        else
            match tryGet ids with
            | Some status -> Ok status
            | None -> Ok Running

    let internal cancelFake agentId runId =
        markCancelled (agentId, runId)
        Ok()

    let private cancelledRun () =
        Error(ApiError("cancelled", "Agent run was cancelled"))

    let private pastDeadline (started: DateTime) maxWaitMs =
        match maxWaitMs with
        | None -> false
        | Some max ->
            let elapsed =
                (DateTime.UtcNow - started).TotalMilliseconds
            elapsed > float max

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
        if isCancelled ids then
            Some(cancelledRun ())
        else
            match tryGetStream ids with
            | Some events -> Some(Ok events)
            | None ->
                match tryGet ids with
                | Some(Finished result) ->
                    Some(Ok(synthesizeStream result))
                | Some Cancelled -> Some(cancelledRun ())
                | Some(Failed msg) ->
                    Some(Error(ApiError("failed", msg)))
                | Some Creating
                | Some Running
                | None -> None

    let private waitFakeStream (args: StreamArgs) =
        let ids = args.AgentId, args.RunId
        let started = DateTime.UtcNow

        let rec waitForEvents () =
            if pastDeadline started args.MaxWaitMs then
                Error AgentError.Timeout
            else
                match streamFromStored ids with
                | Some ready -> ready
                | None ->
                    Thread.Sleep args.PollIntervalMs
                    waitForEvents ()

        waitForEvents ()

    let private stepFakeEvent (outcome, state) fold ev =
        let state = fold.OnEvent state ev
        let outcome =
            match ev with
            | RunFinished result -> Some(Ok result)
            | RunFailed msg -> Some(Error(ApiError("failed", msg)))
            | RunCancelled -> Some(cancelledRun ())
            | AssistantText _ -> outcome
        outcome, state

    let private foldEvents ids fold events =
        let rec loop remaining outcome state =
            match remaining with
            | [] -> outcome, state
            | _ :: _ when isCancelled ids ->
                Some(cancelledRun ()), state
            | ev :: rest ->
                let outcome, state =
                    stepFakeEvent (outcome, state) fold ev
                loop rest outcome state
        loop events None fold.Seed

    let private waitForTerminal args fold state =
        let ids = args.AgentId, args.RunId
        let started = DateTime.UtcNow
        let rec loop state =
            if pastDeadline started args.MaxWaitMs then
                Error AgentError.Timeout
            elif isCancelled ids then
                cancelledRun ()
            else
                match tryGet ids with
                | Some(Finished result) ->
                    let state =
                        fold.OnEvent
                            state
                            (RunFinished result)
                    Ok(result, state)
                | Some(Failed msg) ->
                    Error(ApiError("failed", msg))
                | Some Cancelled -> cancelledRun ()
                | Some Creating
                | Some Running
                | None ->
                    Thread.Sleep args.PollIntervalMs
                    loop state
        loop state

    let private emitFakeStream args (fold: StreamFold<'a>) events =
        let ids = args.AgentId, args.RunId
        let outcome, state = foldEvents ids fold events
        match outcome with
        | Some(Ok result) -> Ok(result, state)
        | Some(Error err) -> Error err
        | None -> waitForTerminal args fold state

    let internal streamFake (args: StreamArgs) (fold: StreamFold<'a>) =
        match waitFakeStream args with
        | Error err -> Error err
        | Ok events -> emitFakeStream args fold events

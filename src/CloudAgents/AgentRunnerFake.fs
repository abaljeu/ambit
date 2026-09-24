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
    let private streamEnded = ref Set.empty<string * string>
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
                streamEnded := Set.empty
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

    let private markStreamEnded ids =
        lock gate (fun () -> streamEnded := Set.add ids !streamEnded)

    let private pollTerminal ids =
        match Map.tryFind ids !results with
        | Some(Finished _)
        | Some(Failed _)
        | Some Cancelled -> true
        | Some Creating
        | Some Running
        | None -> false

    /// Stream fakes end when their terminal event is emitted, not when stored.
    let private isTerminal ids =
        Set.contains ids !cancelled
        || Set.contains ids !streamEnded
        || (not (Map.containsKey ids (!handler).Events) && pollTerminal ids)

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
        let ids = agentId, runId
        lock gate (fun () ->
            if isTerminal ids then
                Ok NotCancellable
            else
                markCancelled ids
                Ok CancelRequested)

    let private cancelledError =
        ApiError("cancelled", "Agent run was cancelled")

    let private cancelledRun () = Error cancelledError

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

    let private fakePollMs = 10
    let private fakeEventDelayMs = 20

    let private streamFromStored ids =
        match tryGetStream ids with
        | Some events -> Some events
        | None ->
            match tryGet ids with
            | Some(Finished result) -> Some(synthesizeStream result)
            | Some Cancelled -> Some [ RunCancelled ]
            | Some(Failed msg) -> Some [ RunFailed msg ]
            | Some Creating
            | Some Running
            | None -> None

    type private FakeRun =
        { Ids: string * string
          Started: DateTime
          MaxWaitMs: int option }

    let private interruption run =
        if isCancelled run.Ids then
            Some(RunCancelled, cancelledError)
        elif pastDeadline run.Started run.MaxWaitMs then
            Some(RunFailed "timeout", AgentError.Timeout)
        else
            None

    let rec private waitFakeStream run =
        match interruption run with
        | Some stop -> Error stop
        | None ->
            match streamFromStored run.Ids with
            | Some events -> Ok events
            | None ->
                Thread.Sleep fakePollMs
                waitFakeStream run

    let private terminalOutcome ev =
        match ev with
        | RunFinished result -> Some(Ok result)
        | RunFailed msg -> Some(Error(ApiError("failed", msg)))
        | RunCancelled -> Some(cancelledRun ())
        | AssistantText _ -> None

    let private stopWith run (fold: StreamFold<'a>) state (ev, err) =
        markStreamEnded run.Ids
        fold.OnEvent state ev |> ignore
        Error err

    let rec private emitFakeStream run (fold: StreamFold<'a>) state events =
        match events, interruption run with
        | _, Some stop -> stopWith run fold state stop
        | [], None ->
            let missing = "stream missing terminal event"
            let stop = RunFailed missing, ApiError("failed", missing)
            stopWith run fold state stop
        | ev :: rest, None ->
            if (terminalOutcome ev).IsSome then markStreamEnded run.Ids
            let state = fold.OnEvent state ev
            match terminalOutcome ev with
            | Some(Ok result) -> Ok(result, state)
            | Some(Error err) -> Error err
            | None ->
                Thread.Sleep fakeEventDelayMs
                emitFakeStream run fold state rest

    let internal streamFake (args: StreamArgs) (fold: StreamFold<'a>) =
        let run =
            { Ids = args.AgentId, args.RunId
              Started = DateTime.UtcNow
              MaxWaitMs = args.MaxWaitMs }
        match waitFakeStream run with
        | Error stop -> stopWith run fold fold.Seed stop
        | Ok events -> emitFakeStream run fold fold.Seed events

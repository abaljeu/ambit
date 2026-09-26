namespace Gambol.CloudAgents

open System
open System.Threading

module internal GrokBotFake =

    type private Seam =
        { Status: (GrokBotWakeArgs -> AgentStatus) option
          Stream:
              (GrokBotWakeArgs -> AgentStreamEvent list) option
          Events: Map<string, AgentStreamEvent list> }

    let private emptySeam =
        { Status = None
          Stream = None
          Events = Map.empty }

    let private handler = ref emptySeam
    let private results = ref Map.empty<string, AgentStatus>
    let private cancelled = ref Set.empty<string>
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

    let private storeStream sessionId events =
        lock gate (fun () ->
            let seam = !handler
            handler :=
                { seam with
                    Events = Map.add sessionId events seam.Events }
            events
            |> List.tryPick (function
                | RunFinished result -> Some(Finished result)
                | RunFailed msg -> Some(Failed msg)
                | RunCancelled -> Some Cancelled
                | _ -> None)
            |> Option.iter (fun status ->
                if Set.contains sessionId !cancelled then
                    ()
                else
                    results := Map.add sessionId status !results))

    let internal deliverFake sessionId text =
        lock gate (fun () ->
            let ev =
                if String.IsNullOrEmpty text then
                    RunFinished { AgentResult.Text = ""; Git = [] }
                else
                    AssistantText text
            let seam = !handler
            let prior =
                Map.tryFind sessionId seam.Events
                |> Option.defaultValue []
            let events = prior @ [ ev ]
            handler :=
                { seam with
                    Events = Map.add sessionId events seam.Events }
            match ev with
            | RunFinished result when String.IsNullOrEmpty result.Text ->
                ()
            | RunFinished result ->
                if Set.contains sessionId !cancelled then
                    ()
                else
                    results :=
                        Map.add sessionId (Finished result) !results
            | _ -> ())
        Ok()

    let private store sessionId result =
        lock gate (fun () ->
            if Set.contains sessionId !cancelled then
                ()
            else
                results := Map.add sessionId result !results)

    let private tryGet sessionId =
        lock gate (fun () -> Map.tryFind sessionId !results)

    let private markCancelled sessionId =
        lock gate (fun () ->
            cancelled := Set.add sessionId !cancelled
            cancelCount := !cancelCount + 1
            cancelPulse.Set() |> ignore)

    let private isCancelled sessionId =
        lock gate (fun () -> Set.contains sessionId !cancelled)

    let internal setFake
        (next: (GrokBotWakeArgs -> AgentStatus) option)
        : bool =
        trySet next

    let internal setFakeStream
        (next: (GrokBotWakeArgs -> AgentStreamEvent list) option)
        : bool =
        trySetStream next

    let internal waitForCancel (timeoutMs: int) : bool =
        cancelPulse.WaitOne(timeoutMs)

    let internal fakeCancelCount () =
        lock gate (fun () -> !cancelCount)

    let internal wakeFake f (args: GrokBotWakeArgs) =
        ThreadPool.QueueUserWorkItem(fun _ ->
            withFlight (fun () ->
                match currentStream () with
                | Some streamF ->
                    storeStream args.SessionId (streamF args)
                | None -> ()
                store args.SessionId (f args)))
        |> ignore
        Ok()

    let internal cancelFake sessionId =
        markCancelled sessionId
        Ok()

    let private cancelledRun () =
        Error(
            ApiError("cancelled", "Grok Bot oneshot was cancelled"))

    let private pastDeadline (started: DateTime) maxWaitMs =
        match maxWaitMs with
        | None -> false
        | Some max ->
            let elapsed =
                (DateTime.UtcNow - started).TotalMilliseconds
            elapsed > float max

    let private waitUntil (args: GrokBotStreamArgs) tryReady =
        let started = DateTime.UtcNow
        let rec loop () =
            if pastDeadline started args.MaxWaitMs then
                Error AgentError.Timeout
            elif isCancelled args.SessionId then
                cancelledRun ()
            else
                match tryReady () with
                | Some ready -> ready
                | None ->
                    Thread.Sleep args.PollIntervalMs
                    loop ()
        loop ()

    let private stepFakeEvent (outcome, state) fold ev =
        let state = fold.OnEvent state ev
        let outcome =
            match ev with
            | RunFinished result when String.IsNullOrEmpty result.Text ->
                outcome
            | RunFinished result -> Some(Ok result)
            | RunFailed msg -> Some(Error(ApiError("failed", msg)))
            | RunCancelled -> Some(cancelledRun ())
            | AssistantText _ -> outcome
        outcome, state

    let private finishFromStatus args state fold =
        waitUntil args (fun () ->
            match tryGet args.SessionId with
            | Some(Finished result) ->
                let state =
                    fold.OnEvent state (RunFinished result)
                Some(Ok(result, state))
            | Some(Failed msg) ->
                Some(Error(ApiError("failed", msg)))
            | Some Cancelled -> Some(cancelledRun ())
            | Some Creating
            | Some Running
            | None -> None)

    let private eventAt sessionId index =
        lock gate (fun () ->
            if Set.contains sessionId !cancelled then
                Some(Choice1Of2(cancelledRun ()))
            else
                match Map.tryFind sessionId (!handler).Events with
                | Some events when index < events.Length ->
                    Some(Choice2Of2 events.[index])
                | _ ->
                    match Map.tryFind sessionId !results with
                    | Some Cancelled ->
                        Some(Choice1Of2(cancelledRun ()))
                    | Some(Failed msg) ->
                        Some(
                            Choice1Of2(
                                Error(ApiError("failed", msg))))
                    | _ -> None)

    let private streamEventsReady sessionId index =
        lock gate (fun () ->
            match Map.tryFind sessionId (!handler).Events with
            | Some events -> Some(index < events.Length)
            | None ->
                match (!handler).Stream with
                | Some _ -> None
                | None -> Some false)

    let internal streamFake
        (args: GrokBotStreamArgs)
        (fold: StreamFold<'a>)
        =
        let started = DateTime.UtcNow
        let rec loop index outcome state =
            if pastDeadline started args.MaxWaitMs then
                Error AgentError.Timeout
            elif isCancelled args.SessionId then
                cancelledRun ()
            else
                match eventAt args.SessionId index with
                | Some(Choice1Of2 err) -> err
                | Some(Choice2Of2 ev) ->
                    let outcome, state =
                        stepFakeEvent (outcome, state) fold ev
                    match outcome with
                    | Some(Ok result) -> Ok(result, state)
                    | Some(Error err) -> Error err
                    | None -> loop (index + 1) outcome state
                | None ->
                    match tryGet args.SessionId with
                    | Some(Finished _) ->
                        match
                            streamEventsReady args.SessionId index
                        with
                        | Some true ->
                            loop index outcome state
                        | Some false ->
                            finishFromStatus args state fold
                        | None ->
                            Thread.Sleep args.PollIntervalMs
                            loop index outcome state
                    | _ ->
                        Thread.Sleep args.PollIntervalMs
                        loop index outcome state
        loop 0 None fold.Seed

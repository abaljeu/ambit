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

    let private tryGetStream sessionId =
        lock gate (fun () ->
            Map.tryFind sessionId (!handler).Events)

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

    let private streamFromStored sessionId =
        if isCancelled sessionId then
            Some(cancelledRun ())
        else
            match tryGetStream sessionId with
            | Some events -> Some(Ok events)
            | None ->
                match tryGet sessionId with
                | Some(Finished result) ->
                    Some(Ok(synthesizeStream result))
                | Some Cancelled -> Some(cancelledRun ())
                | Some(Failed msg) ->
                    Some(Error(ApiError("failed", msg)))
                | Some Creating
                | Some Running
                | None -> None

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
            | RunFinished result -> Some(Ok result)
            | RunFailed msg -> Some(Error(ApiError("failed", msg)))
            | RunCancelled -> Some(cancelledRun ())
            | AssistantText _ -> outcome
        outcome, state

    let private foldEvents sessionId fold events =
        let rec loop remaining outcome state =
            match remaining with
            | [] -> outcome, state
            | _ :: _ when isCancelled sessionId ->
                Some(cancelledRun ()), state
            | ev :: rest ->
                let outcome, state =
                    stepFakeEvent (outcome, state) fold ev
                loop rest outcome state
        loop events None fold.Seed

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

    let internal streamFake
        (args: GrokBotStreamArgs)
        (fold: StreamFold<'a>)
        =
        match
            waitUntil args (fun () ->
                streamFromStored args.SessionId)
        with
        | Error err -> Error err
        | Ok events ->
            let outcome, state =
                foldEvents args.SessionId fold events
            match outcome with
            | Some(Ok result) -> Ok(result, state)
            | Some(Error err) -> Error err
            | None -> finishFromStatus args state fold

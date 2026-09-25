namespace Gambol.CloudAgents.Internal

open System
open System.Collections.Generic
open System.Threading
open Gambol.CloudAgents

module GrokBotAdapter =

    let private providerName = "Grok Bot"

    let private named reason =
        AgentMessage.couldNotSend providerName reason

    let private authFailed reason =
        AuthenticationFailed(named reason)

    let private fromHttpError httpError =
        if httpError = "unauthorized" then
            authFailed "unauthorized"
        else
            NetworkError httpError

    let private cancelledRun () =
        Error(
            ApiError("cancelled", "Grok Bot oneshot was cancelled"))

    type private SessionBox =
        { Events: Queue<AgentStreamEvent>
          Cancelled: bool }

    let private gate = obj ()
    let private boxes = Dictionary<string, SessionBox>()

    let private boxOf sessionId =
        match boxes.TryGetValue sessionId with
        | true, box -> box
        | false, _ ->
            let box =
                { Events = Queue<AgentStreamEvent>()
                  Cancelled = false }
            boxes.[sessionId] <- box
            box

    let private enqueue sessionId ev =
        lock gate (fun () ->
            let box = boxOf sessionId
            if not box.Cancelled then
                box.Events.Enqueue ev)

    let deliver (sessionId: string) (text: string) =
        if String.IsNullOrEmpty text then
            enqueue
                sessionId
                (RunFinished { AgentResult.Text = ""; Git = [] })
        else
            enqueue sessionId (AssistantText text)
        Ok()

    let wake (args: GrokBotWakeArgs) : Result<unit, AgentError> =
        if String.IsNullOrWhiteSpace args.Config.WakeUrl then
            Error(authFailed "missing wake URL")
        elif String.IsNullOrWhiteSpace args.Config.WakeSecret then
            Error(authFailed "missing wake secret")
        else
            let sentAt = DateTime.UtcNow.ToString("o")
            let json = GrokBotHttp.wakeRequestJson sentAt args
            match
                GrokBotHttp.postWake
                    args.Config.WakeUrl
                    args.Config.WakeSecret
                    json
            with
            | Error msg -> Error(fromHttpError msg)
            | Ok() -> Ok()

    let cancel (_config: GrokBotConfig) (sessionId: string) =
        lock gate (fun () ->
            let box = boxOf sessionId
            boxes.[sessionId] <- { box with Cancelled = true })
        Ok()

    let private isCancelled sessionId =
        lock gate (fun () ->
            match boxes.TryGetValue sessionId with
            | true, box -> box.Cancelled
            | false, _ -> false)

    let private tryDequeue sessionId =
        lock gate (fun () ->
            match boxes.TryGetValue sessionId with
            | true, box when box.Events.Count > 0 ->
                Some(box.Events.Dequeue())
            | _ -> None)

    let private pastDeadline (started: DateTime) maxWaitMs =
        match maxWaitMs with
        | None -> false
        | Some max ->
            let elapsed =
                (DateTime.UtcNow - started).TotalMilliseconds
            elapsed > float max

    let private stepEvent (outcome, state, acc) fold ev =
        let state = fold.OnEvent state ev
        match ev with
        | AssistantText chunk ->
            outcome, state, acc + chunk
        | RunFinished result when String.IsNullOrEmpty result.Text ->
            outcome, state, acc
        | RunFinished result ->
            Some(Ok result), state, acc
        | RunFailed msg ->
            Some(Error(ApiError("failed", msg))), state, acc
        | RunCancelled ->
            Some(cancelledRun ()), state, acc

    let streamRun
        (args: GrokBotStreamArgs)
        (fold: StreamFold<'a>)
        : Result<AgentResult * 'a, AgentError> =
        let started = DateTime.UtcNow
        let rec loop outcome state acc =
            if isCancelled args.SessionId then
                cancelledRun ()
            elif pastDeadline started args.MaxWaitMs then
                Error AgentError.Timeout
            else
                match tryDequeue args.SessionId with
                | Some ev ->
                    let outcome, state, acc =
                        stepEvent (outcome, state, acc) fold ev
                    match outcome with
                    | Some(Ok result) -> Ok(result, state)
                    | Some(Error err) -> Error err
                    | None -> loop outcome state acc
                | None ->
                    Thread.Sleep args.PollIntervalMs
                    loop outcome state acc
        loop None fold.Seed ""

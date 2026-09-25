namespace Gambol.CloudAgents.Internal

open System
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

    let wake (args: GrokBotWakeArgs) : Result<unit, AgentError> =
        if String.IsNullOrWhiteSpace args.Config.WakeUrl then
            Error(authFailed "missing wake URL")
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

    let cancel (_config: GrokBotConfig) (_sessionId: string) =
        // No close-notify wake. Local cancel lives on the runner
        // fake / stream fold. Live hub cancel is Unsettled.
        Ok()

    /// Live hub stream is Unsettled (`kind: close` may be Done).
    /// Fake/harness `RunFinished` is the first-slice terminus.
    /// Do not invent inbound body fields for Server deliver.
    let streamRun
        (_args: GrokBotStreamArgs)
        (_fold: StreamFold<'a>)
        : Result<AgentResult * 'a, AgentError> =
        Error(InvalidResponse(named "stream Done seam Unsettled"))

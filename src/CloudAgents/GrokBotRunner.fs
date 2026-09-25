namespace Gambol.CloudAgents

module GrokBotRunner =

    let setFake
        (handler: (GrokBotWakeArgs -> AgentStatus) option)
        : bool =
        GrokBotFake.setFake handler

    /// Optional fake stream sequence (requires setFake Some).
    let setFakeStream
        (handler: (GrokBotWakeArgs -> AgentStreamEvent list) option)
        : bool =
        GrokBotFake.setFakeStream handler

    /// Block a setFake handler until cancel, or until timeoutMs.
    let waitForCancel (timeoutMs: int) : bool =
        GrokBotFake.waitForCancel timeoutMs

    let fakeCancelCount () = GrokBotFake.fakeCancelCount ()

    let wake (args: GrokBotWakeArgs) : Result<unit, AgentError> =
        match GrokBotFake.statusHandler () with
        | Some f -> GrokBotFake.wakeFake f args
        | None ->
            GrokBotFake.withFlight (fun () ->
                Internal.GrokBotAdapter.wake args)

    let cancel
        (config: GrokBotConfig)
        (sessionId: string)
        : Result<unit, AgentError> =
        match GrokBotFake.statusHandler () with
        | Some _ -> GrokBotFake.cancelFake sessionId
        | None -> Internal.GrokBotAdapter.cancel config sessionId

    let streamUntilComplete
        (args: GrokBotStreamArgs)
        (fold: StreamFold<'a>)
        : Result<AgentResult * 'a, AgentError> =
        match GrokBotFake.statusHandler () with
        | Some _ -> GrokBotFake.streamFake args fold
        | None ->
            GrokBotFake.withFlight (fun () ->
                Internal.GrokBotAdapter.streamRun args fold)

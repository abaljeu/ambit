module Gambol.Server.Tests.AgentAuthErrorTests

open Xunit
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.CloudAgents
open Gambol.Server
open Gambol.Server.Tests.AskCancelHarness

module Enc = Thoth.Json.Newtonsoft.Encode

let private lastActorStopEvent host focusId =
    task {
        let! history = CoreMailbox.eventHistory host
        return
            history.events
            |> List.tryFind (fun event ->
                match event.body with
                | EventBody.ActorStop(fid, _) when fid = focusId ->
                    true
                | _ -> false)
    }

[<Collection("Agent ask runner")>]
type AgentAuthErrorTests() =

    [<Fact>]
    member _.``setFake unauthorized names Cursor on ActorStop``() =
        let named =
            AgentMessage.couldNotSend "Cursor" "unauthorized"
        withFake
            (fun _ -> fakeFailed named)
            (fun () ->
                withHost (fun host pool -> task {
                    let! seeded = seedAskTree host "?ai"
                    let! request = startAsk host seeded
                    do! expectActorFailed host pool request.focusId
                    let! stopEvent =
                        lastActorStopEvent host request.focusId
                    match stopEvent with
                    | None -> Assert.Fail("missing ActorStop")
                    | Some event ->
                        match event.body with
                        | EventBody.ActorStop(_, ActorFailed msg) ->
                            Assert.Equal(named, msg)
                        | other ->
                            Assert.Fail($"bad stop, {other}")
                        let json =
                            Enc.toString 0 (EventJson.encode event)
                        Assert.Contains("\"result\":\"failed\"", json)
                        Assert.Contains(named, json)
                        Assert.Equal(
                            Some (
                                CmdLastResult.Error (
                                    Some "AI", named)),
                            ActorLive.lastCmdResult [ event ])
                }))

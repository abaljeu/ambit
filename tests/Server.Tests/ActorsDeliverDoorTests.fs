module Gambol.Server.Tests.ActorsDeliverDoorTests

open System.Collections.Generic
open Microsoft.AspNetCore.Http
open Xunit
open Gambol.Server
open Gambol.CloudAgents

let private unusedConfig =
    { GrokBotConfig.WakeUrl = "http://unused.example/"
      WakeSecret = "wake"
      InboundSecret = "inbound-secret" }

let private statusCode (result: IResult) =
    match result with
    | :? IStatusCodeHttpResult as status ->
        if status.StatusCode.HasValue then
            status.StatusCode.Value
        else
            0
    | _ ->
        let name = result.GetType().Name
        if name.Contains "Unauthorized" then 401
        elif name.Contains "NotFound" then 404
        elif name.Contains "BadRequest" then 400
        elif name.StartsWith "Ok" then 200
        else 0

[<Fact>]
let ``wrong inbound secret is rejected`` () =
    let result =
        Api.postActorsDeliver
            "inbound-secret"
            "wrong"
            (fun _ -> Ok())
            """{"sessionId":"s","text":"hi"}"""
    Assert.Equal(401, statusCode result)

[<Fact>]
let ``empty configured inbound secret fails closed`` () =
    let result =
        Api.postActorsDeliver
            ""
            "inbound-secret"
            (fun _ -> Ok())
            """{"sessionId":"s","text":"hi"}"""
    Assert.Equal(401, statusCode result)

[<Fact>]
let ``unknown sessionId is 404`` () =
    let result =
        Api.postActorsDeliver
            "inbound-secret"
            "inbound-secret"
            (fun _ -> Error "not live")
            """{"sessionId":"missing","text":"hi"}"""
    Assert.Equal(404, statusCode result)

[<Collection("GrokBot actor")>]
type ActorsDeliverDoorGrokTests() =
    [<Fact>]
    member _.``good secret delivers chunk then empty Done``() =
        Assert.True(GrokBotRunner.setFake None)
        let seen = ResizeArray<string * string>()
        let deliver (sessionId, text) =
            seen.Add(sessionId, text)
            Ok()
        let chunk =
            Api.postActorsDeliver
                unusedConfig.InboundSecret
                unusedConfig.InboundSecret
                deliver
                """{"sessionId":"sess-door","text":"<n>Hi</n>"}"""
        Assert.Equal(200, statusCode chunk)
        let doneAck =
            Api.postActorsDeliver
                unusedConfig.InboundSecret
                unusedConfig.InboundSecret
                deliver
                """{"sessionId":"sess-door","text":""}"""
        Assert.Equal(200, statusCode doneAck)
        Assert.Equal(2, seen.Count)
        let fold =
            { Seed = []
              OnEvent = fun acc ev -> ev :: acc }
        match
            GrokBotRunner.streamUntilComplete
                { Config = unusedConfig
                  SessionId = "sess-door"
                  PollIntervalMs = 10
                  MaxWaitMs = Some 200 }
                fold
        with
        | Error AgentError.Timeout -> ()
        | other ->
            Assert.Fail($"expected keep-open Timeout, {other}")

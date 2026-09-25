module Gambol.Server.Tests.AiKeysTests

open Microsoft.Extensions.Configuration
open Xunit
open Gambol.Server
open Gambol.Server.Tests.AskCancelHarness

let private bothKeys =
    { DefaultAiKey = "server"
      Keys =
        Map.ofList
            [ "desktop", "desk-secret"
              "server", "srv-secret" ] }

let private desktopOnly =
    { DefaultAiKey = "desktop"
      Keys = Map.ofList [ "desktop", "desk-secret" ] }

let private configFrom pairs =
    ConfigurationBuilder()
        .AddInMemoryCollection(dict pairs)
        .Build()

[<Fact>]
let ``fromConfig binds name-keyed values and DefaultAiKey`` () =
    let config =
        configFrom
            [ "DefaultAiKey", "desktop"
              "AiKeys:server", "srv-secret"
              "AiKeys:desktop", "desk-secret" ]
    let got = AiKeys.fromConfig config
    Assert.Equal("desktop", got.DefaultAiKey)
    Assert.Equal(Some "desk-secret", Map.tryFind "desktop" got.Keys)
    Assert.Equal(Some "srv-secret", Map.tryFind "server" got.Keys)

[<Fact>]
let ``fromConfig is empty when AiKeys is missing`` () =
    let got = AiKeys.fromConfig (configFrom [])
    Assert.Equal("", got.DefaultAiKey)
    Assert.True(Map.isEmpty got.Keys)

[<Fact>]
let ``fromConfig ignores the old list path`` () =
    let config =
        configFrom
            [ "DefaultAiKey", "0"
              "AiKeys:0:Name", "cursor"
              "AiKeys:0:ApiKey", "old-secret" ]
    let got = AiKeys.fromConfig config
    Assert.Equal("", AiKeys.resolve got None)
    Assert.Equal("", AiKeys.resolve got (Some "cursor"))

[<Fact>]
let ``resolve uses the named entry`` () =
    Assert.Equal(
        "desk-secret",
        AiKeys.resolve bothKeys (Some "desktop"))

[<Fact>]
let ``resolve named entry is case-insensitive`` () =
    Assert.Equal(
        "srv-secret",
        AiKeys.resolve bothKeys (Some "SERVER"))

[<Fact>]
let ``resolve default is DefaultAiKey`` () =
    Assert.Equal("srv-secret", AiKeys.resolve bothKeys None)

[<Fact>]
let ``resolve missing name is empty`` () =
    Assert.Equal(
        "",
        AiKeys.resolve desktopOnly (Some "missing"))

[<Fact>]
let ``resolve missing default or empty value is empty`` () =
    let missingDefault =
        { bothKeys with DefaultAiKey = "" }
    let emptyValue =
        { DefaultAiKey = "desktop"
          Keys = Map.ofList [ "desktop", "" ] }
    Assert.Equal("", AiKeys.resolve missingDefault None)
    Assert.Equal("", AiKeys.resolve emptyValue None)
    Assert.Equal("", AiKeys.resolve AiKeys.empty None)
    Assert.Equal("", AiKeys.resolve AiKeys.empty (Some "desktop"))

[<Fact>]
let ``keynameFromText reads the first token after ?ai`` () =
    Assert.Equal(None, AiKeys.keynameFromText "?ai")
    Assert.Equal(Some "work", AiKeys.keynameFromText "?ai work")
    Assert.Equal(
        Some "work",
        AiKeys.keynameFromText "?ai work extra")
    Assert.Equal(Some "work", AiKeys.keynameFromText "?AI Work")

[<Collection("Agent ask runner")>]
type AiKeysActorTests() =

    [<Fact>]
    member _.``Ask without keyname sends DefaultAiKey``() =
        withFake
            (fun args ->
                Assert.Equal("srv-secret", args.Config.ApiKey)
                fakeReply "from-default")
            (fun () ->
                withHostKeys
                    bothKeys
                    (fun host pool -> task {
                        let! seeded = seedAskTree host "?ai"
                        let! request = startAsk host seeded
                        do! expectActorSucceeded
                                host pool request.focusId
                    }))

    [<Fact>]
    member _.``Ask keyname sends that entry ApiKey``() =
        withFake
            (fun args ->
                Assert.Equal("desk-secret", args.Config.ApiKey)
                fakeReply "from-named")
            (fun () ->
                withHostKeys
                    bothKeys
                    (fun host pool -> task {
                        let! seeded =
                            seedAskTree host "?ai desktop extra"
                        let! request = startAsk host seeded
                        do! expectActorSucceeded
                                host pool request.focusId
                    }))

    [<Fact>]
    member _.``Ask unknown keyname sends empty ApiKey``() =
        withFake
            (fun args ->
                Assert.Equal("", args.Config.ApiKey)
                fakeReply "from-missing")
            (fun () ->
                withHostKeys
                    desktopOnly
                    (fun host pool -> task {
                        let! seeded = seedAskTree host "?ai missing"
                        let! request = startAsk host seeded
                        do! expectActorSucceeded
                                host pool request.focusId
                    }))

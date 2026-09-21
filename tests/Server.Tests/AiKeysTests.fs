module Gambol.Server.Tests.AiKeysTests

open Microsoft.Extensions.Configuration
open Xunit
open Gambol.Server
open Gambol.Server.Tests.AskCancelHarness

let private cursorKey = { Name = "cursor"; ApiKey = "first-secret" }
let private workKey = { Name = "work"; ApiKey = "work-secret" }

let private configFrom pairs =
    ConfigurationBuilder()
        .AddInMemoryCollection(dict pairs)
        .Build()

[<Fact>]
let ``fromConfig binds Name and ApiKey entries`` () =
    let config =
        configFrom
            [ "AiKeys:0:Name", "cursor"
              "AiKeys:0:ApiKey", "first-secret"
              "AiKeys:1:Name", "work"
              "AiKeys:1:ApiKey", "work-secret" ]
    Assert.Equal<AiKey list>(
        [ cursorKey; workKey ],
        AiKeys.fromConfig config)

[<Fact>]
let ``fromConfig is empty when AiKeys is missing`` () =
    Assert.Equal<AiKey list>([], AiKeys.fromConfig (configFrom []))

[<Fact>]
let ``resolve uses the named entry`` () =
    Assert.Equal(
        "work-secret",
        AiKeys.resolve [ cursorKey; workKey ] (Some "work"))

[<Fact>]
let ``resolve named entry is case-insensitive`` () =
    Assert.Equal(
        "work-secret",
        AiKeys.resolve [ cursorKey; workKey ] (Some "WORK"))

[<Fact>]
let ``resolve default is the first entry`` () =
    Assert.Equal(
        "first-secret",
        AiKeys.resolve [ cursorKey; workKey ] None)

[<Fact>]
let ``resolve missing name is empty`` () =
    Assert.Equal(
        "",
        AiKeys.resolve [ cursorKey ] (Some "missing"))

[<Fact>]
let ``resolve empty list is empty`` () =
    Assert.Equal("", AiKeys.resolve [] None)
    Assert.Equal("", AiKeys.resolve [] (Some "cursor"))

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
    member _.``Ask without keyname sends the first AiKeys ApiKey``() =
        withFake
            (fun args ->
                Assert.Equal("first-secret", args.Config.ApiKey)
                fakeReply "from-default")
            (fun () ->
                withHostKeys
                    [ cursorKey; workKey ]
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
                Assert.Equal("work-secret", args.Config.ApiKey)
                fakeReply "from-named")
            (fun () ->
                withHostKeys
                    [ cursorKey; workKey ]
                    (fun host pool -> task {
                        let! seeded =
                            seedAskTree host "?ai work extra"
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
                    [ cursorKey ]
                    (fun host pool -> task {
                        let! seeded = seedAskTree host "?ai missing"
                        let! request = startAsk host seeded
                        do! expectActorSucceeded
                                host pool request.focusId
                    }))

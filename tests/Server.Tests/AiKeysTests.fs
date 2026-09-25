module Gambol.Server.Tests.AiKeysTests

open System
open System.IO
open Microsoft.Extensions.Configuration
open Xunit
open Gambol.Server
open Gambol.Server.Tests.AskCancelHarness

let private cursorKeys =
    { DefaultAiKey = "cursor"
      Keys = Map.ofList [ "cursor", "cursor-secret" ] }

let private configFrom pairs =
    ConfigurationBuilder()
        .AddInMemoryCollection(dict pairs)
        .Build()

[<Fact>]
let ``fromConfig binds name-keyed values and DefaultAiKey`` () =
    let config =
        configFrom
            [ "DefaultAiKey", "cursor"
              "AiKeys:cursor", "cursor-secret" ]
    let got = AiKeys.fromConfig config
    Assert.Equal("cursor", got.DefaultAiKey)
    Assert.Equal(Some "cursor-secret", Map.tryFind "cursor" got.Keys)

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
        "cursor-secret",
        AiKeys.resolve cursorKeys (Some "cursor"))

[<Fact>]
let ``resolve named entry is case-insensitive`` () =
    Assert.Equal(
        "cursor-secret",
        AiKeys.resolve cursorKeys (Some "CURSOR"))

[<Fact>]
let ``resolve default is DefaultAiKey`` () =
    Assert.Equal("cursor-secret", AiKeys.resolve cursorKeys None)

[<Fact>]
let ``resolve missing name is empty`` () =
    Assert.Equal(
        "",
        AiKeys.resolve cursorKeys (Some "missing"))

[<Fact>]
let ``resolve missing default or empty value is empty`` () =
    let missingDefault =
        { cursorKeys with DefaultAiKey = "" }
    let emptyValue =
        { DefaultAiKey = "cursor"
          Keys = Map.ofList [ "cursor", "" ] }
    Assert.Equal("", AiKeys.resolve missingDefault None)
    Assert.Equal("", AiKeys.resolve emptyValue None)
    Assert.Equal("", AiKeys.resolve AiKeys.empty None)
    Assert.Equal("", AiKeys.resolve AiKeys.empty (Some "cursor"))

let private withEnvPairs
    (pairs: (string * string) list)
    (action: unit -> unit)
    =
    let previous =
        pairs
        |> List.map (fun (name, _) ->
            name, Environment.GetEnvironmentVariable name)
    for name, value in pairs do
        Environment.SetEnvironmentVariable(name, value)
    try
        action ()
    finally
        for name, prev in previous do
            Environment.SetEnvironmentVariable(name, prev)

let private withLaterEmptyAiKeysJson (action: string -> unit) =
    let dir =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory dir |> ignore
    let path = Path.Combine(dir, "later.json")
    File.WriteAllText(
        path,
        """{"DefaultAiKey":"","AiKeys":{"cursor":""}}""")
    try
        action path
    finally
        Directory.Delete(dir, true)

[<Fact>]
let ``later empty JSON does not replace environment named AiKeys`` () =
    let envKey = "gambol-test-env-cursor-key"
    withEnvPairs
        [ "DefaultAiKey", "cursor"
          "AiKeys__cursor", envKey ]
        (fun () ->
            withLaterEmptyAiKeysJson (fun jsonPath ->
                let config =
                    ConfigurationBuilder()
                        .AddEnvironmentVariables()
                        .AddJsonFile(jsonPath, optional = false)
                    |> fun builder ->
                        ConfigurationOrder.addOverridesAfterJson
                            false
                            typeof<AiKeySet>.Assembly
                            builder
                    |> fun builder -> builder.Build()
                let got = AiKeys.fromConfig config
                Assert.Equal("cursor", got.DefaultAiKey)
                Assert.Equal(envKey, AiKeys.resolve got None)))

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
                Assert.Equal("cursor-secret", args.Config.ApiKey)
                fakeReply "from-default")
            (fun () ->
                withHostKeys
                    cursorKeys
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
                Assert.Equal("cursor-secret", args.Config.ApiKey)
                fakeReply "from-named")
            (fun () ->
                withHostKeys
                    cursorKeys
                    (fun host pool -> task {
                        let! seeded =
                            seedAskTree host "?ai cursor extra"
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
                    cursorKeys
                    (fun host pool -> task {
                        let! seeded = seedAskTree host "?ai missing"
                        let! request = startAsk host seeded
                        do! expectActorSucceeded
                                host pool request.focusId
                    }))

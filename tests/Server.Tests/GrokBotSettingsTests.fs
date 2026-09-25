module Gambol.Server.Tests.GrokBotSettingsTests

open Microsoft.Extensions.Configuration
open Xunit
open Gambol.Server

let private configFrom pairs =
    ConfigurationBuilder()
        .AddInMemoryCollection(dict pairs)
        .Build()

[<Fact>]
let ``fromConfig binds the three grokbot keys`` () =
    let config =
        configFrom
            [ "grokbot:WakeUrl", "http://wake.example/"
              "grokbot:WakeSecret", "wake-secret"
              "grokbot:InboundSecret", "inbound-secret" ]
    let bound = GrokBotSettings.fromConfig config
    Assert.Equal("http://wake.example/", bound.WakeUrl)
    Assert.Equal("wake-secret", bound.WakeSecret)
    Assert.Equal("inbound-secret", bound.InboundSecret)

[<Fact>]
let ``fromConfig is empty strings when grokbot keys are missing`` () =
    let bound = GrokBotSettings.fromConfig (configFrom [])
    Assert.Equal("", bound.WakeUrl)
    Assert.Equal("", bound.WakeSecret)
    Assert.Equal("", bound.InboundSecret)

[<Fact>]
let ``deliverResponseUrl uses PublicAssetBase when set`` () =
    let config =
        configFrom
            [ "PublicAssetBase", "https://app.azurewebsites.net" ]
    Assert.Equal(
        "https://app.azurewebsites.net/ambit/actors/deliver",
        GrokBotSettings.deliverResponseUrl config)

[<Fact>]
let ``deliverResponseUrl trims a trailing slash on PublicAssetBase`` () =
    let config =
        configFrom
            [ "PublicAssetBase", "https://app.azurewebsites.net/" ]
    Assert.Equal(
        "https://app.azurewebsites.net/ambit/actors/deliver",
        GrokBotSettings.deliverResponseUrl config)

[<Fact>]
let ``deliverResponseUrl uses the production host when PublicAssetBase is empty`` () =
    Assert.Equal(
        "https://collaborative-systems.org/ambit/actors/deliver",
        GrokBotSettings.deliverResponseUrl (configFrom []))

[<Fact>]
let ``fromConfigBinding pairs grok keys with the deliver URL`` () =
    let config =
        configFrom
            [ "grokbot:WakeUrl", "http://wake.example/"
              "grokbot:WakeSecret", "wake-secret"
              "grokbot:InboundSecret", "inbound-secret"
              "PublicAssetBase", "https://app.azurewebsites.net" ]
    let bound = GrokBotSettings.fromConfigBinding config
    Assert.Equal("http://wake.example/", bound.Config.WakeUrl)
    Assert.Equal("wake-secret", bound.Config.WakeSecret)
    Assert.Equal("inbound-secret", bound.Config.InboundSecret)
    Assert.Equal(
        "https://app.azurewebsites.net/ambit/actors/deliver",
        bound.ResponseUrl)

[<Fact>]
let ``deliverResponseUrl omits inbound secret`` () =
    let config =
        configFrom
            [ "PublicAssetBase", "https://app.example"
              "grokbot:InboundSecret", "inbound-secret" ]
    let url = GrokBotSettings.deliverResponseUrl config
    Assert.Equal("https://app.example/ambit/actors/deliver", url)
    Assert.DoesNotContain("inbound-secret", url)

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

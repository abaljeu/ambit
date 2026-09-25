namespace Gambol.Server

open System
open Microsoft.Extensions.Configuration
open Gambol.CloudAgents

/// Composition-owned grok bind. Library stays settings-blind.
type GrokBotBinding =
    { Config: GrokBotConfig
      ResponseUrl: string }

/// Binds GrokBotConfig from IConfiguration. Library stays settings-blind.
[<RequireQualifiedAccess>]
module GrokBotSettings =

    let private productionOrigin =
        "https://collaborative-systems.org"

    let private read (config: IConfiguration) key =
        config.[key]
        |> Option.ofObj
        |> Option.defaultValue ""

    let fromConfig (config: IConfiguration) : GrokBotConfig =
        { WakeUrl = read config "grokbot:WakeUrl"
          WakeSecret = read config "grokbot:WakeSecret"
          InboundSecret = read config "grokbot:InboundSecret" }

    let deliverResponseUrl (config: IConfiguration) : string =
        let raw = read config "PublicAssetBase"
        let trimmed = raw.Trim().TrimEnd('/')
        let origin =
            if String.IsNullOrWhiteSpace trimmed then
                productionOrigin
            else
                trimmed
        origin + "/ambit/actors/deliver"

    let fromConfigBinding (config: IConfiguration) : GrokBotBinding =
        { Config = fromConfig config
          ResponseUrl = deliverResponseUrl config }

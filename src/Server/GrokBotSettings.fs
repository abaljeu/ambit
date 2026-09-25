namespace Gambol.Server

open Microsoft.Extensions.Configuration
open Gambol.Shared
open Gambol.CloudAgents

/// Binds GrokBotConfig from IConfiguration. Library stays settings-blind.
[<RequireQualifiedAccess>]
module GrokBotSettings =

    let private read (config: IConfiguration) key =
        config.[key]
        |> Option.ofObj
        |> Option.defaultValue ""

    let fromConfig (config: IConfiguration) : GrokBotConfig =
        { WakeUrl = read config "grokbot:WakeUrl"
          WakeSecret = read config "grokbot:WakeSecret"
          InboundSecret = read config "grokbot:InboundSecret" }

namespace Gambol.Server

open System.Reflection
open Microsoft.Extensions.Configuration

/// Extra JSON after CreateBuilder. Reload Development user secrets
/// and environment so empty later JSON cannot replace named AiKeys.
module ConfigurationOrder =

    let addOverridesAfterJson
        (isDevelopment: bool)
        (assembly: Assembly)
        (config: IConfigurationBuilder)
        =
        if isDevelopment then
            config.AddUserSecrets(
                assembly,
                optional = true,
                reloadOnChange = true)
            |> ignore
        config.AddEnvironmentVariables() |> ignore
        config

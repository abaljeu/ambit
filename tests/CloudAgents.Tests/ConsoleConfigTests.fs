module Gambol.CloudAgents.Tests.ConsoleConfigTests

open Xunit

[<Fact>]
let ``pick prefers CLI over file over env`` () =
    Assert.Equal(
        Some "cli",
        ConsoleConfig.pick (Some "cli") (Some "file") (Some "env"))
    Assert.Equal(
        Some "file",
        ConsoleConfig.pick None (Some "file") (Some "env"))
    Assert.Equal(
        Some "env",
        ConsoleConfig.pick None None (Some "env"))
    Assert.Equal(None, ConsoleConfig.pick (Some "") (Some "  ") None)

[<Fact>]
let ``resolve fills omitted CLI from file then env`` () =
    let cli =
        { ConsoleConfig.emptyCli with Prompt = Some "ask" }
    let file =
        { ConsoleConfig.emptyFile with
            Model = Some "grok"
            Repo = Some "https://example.com/repo.git" }
    let got = ConsoleConfig.resolve cli file (Some "env-key")
    Assert.Equal(Some "ask", got.Prompt)
    Assert.Equal(Some "env-key", got.ApiKey)
    Assert.Equal(Some "grok", got.Model)
    Assert.Equal(Some "https://example.com/repo.git", got.Repo)

[<Fact>]
let ``parseArgs reads flags and prompt`` () =
    let cli =
        ConsoleConfig.parseArgs
            [ "hello"
              "--model"
              "grok"
              "--api-key"
              "k"
              "--repo"
              "https://x"
              "--ref"
              "main"
              "--name"
              "N" ]
            ConsoleConfig.emptyCli
    Assert.Equal(Some "hello", cli.Prompt)
    Assert.Equal(Some "grok", cli.Model)
    Assert.Equal(Some "k", cli.ApiKey)
    Assert.Equal(Some "https://x", cli.Repo)
    Assert.Equal(Some "main", cli.Ref)
    Assert.Equal(Some "N", cli.Name)

[<Fact>]
let ``resolve CLI wins over file`` () =
    let cli =
        { ConsoleConfig.emptyCli with
            Prompt = Some "ask"
            ApiKey = Some "cli-key"
            Model = Some "cli-model" }
    let file =
        { ConsoleConfig.emptyFile with
            ApiKey = Some "file-key"
            Model = Some "file-model" }
    let got = ConsoleConfig.resolve cli file (Some "env-key")
    Assert.Equal(Some "cli-key", got.ApiKey)
    Assert.Equal(Some "cli-model", got.Model)

module Gambol.CloudAgents.Tests.ConsoleConfigTests

open Xunit
open Gambol.CloudAgents

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
            ModelParams =
                [ { ModelParam.Id = "context"; Value = "256k" } ]
            Repo = Some "https://example.com/repo.git" }
    let got = ConsoleConfig.resolve cli file None (Some "env-key")
    Assert.Equal(Some "ask", got.Prompt)
    Assert.Equal(Some "env-key", got.ApiKey)
    Assert.Equal(Some "grok", got.Model)
    Assert.Equal(1, got.ModelParams.Length)
    Assert.Equal("context", got.ModelParams.[0].Id)
    Assert.Equal(
        Some "https://example.com/repo.git",
        got.Repo)

[<Fact>]
let ``parseArgs reads flags prompt and params`` () =
    let cli =
        ConsoleConfig.parseArgs
            [ "hello"
              "--model"
              "grok"
              "--param"
              "context=256k"
              "--param"
              "fast=true"
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
    Assert.Equal(2, cli.Params.Length)
    Assert.Equal("context", cli.Params.[0].Id)
    Assert.Equal("256k", cli.Params.[0].Value)
    Assert.Equal("fast", cli.Params.[1].Id)
    Assert.Equal("true", cli.Params.[1].Value)
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
            Model = Some "cli-model"
            Params =
                [ { ModelParam.Id = "fast"; Value = "true" } ] }
    let file =
        { ConsoleConfig.emptyFile with
            Model = Some "file-model"
            ModelParams =
                [ { ModelParam.Id = "context"; Value = "256k" } ] }
    let got =
        ConsoleConfig.resolve
            cli
            file
            (Some "secret-key")
            (Some "env-key")
    Assert.Equal(Some "cli-key", got.ApiKey)
    Assert.Equal(Some "cli-model", got.Model)
    Assert.Equal(1, got.ModelParams.Length)
    Assert.Equal("fast", got.ModelParams.[0].Id)

[<Fact>]
let ``resolve api key prefers CLI then user secret then env`` () =
    let cli =
        { ConsoleConfig.emptyCli with Prompt = Some "ask" }
    let file = ConsoleConfig.emptyFile
    let secretWins =
        ConsoleConfig.resolve cli file (Some "secret") (Some "env")
    Assert.Equal(Some "secret", secretWins.ApiKey)
    let envWins =
        ConsoleConfig.resolve cli file None (Some "env")
    Assert.Equal(Some "env", envWins.ApiKey)
    let blankSecret =
        ConsoleConfig.resolve cli file (Some "  ") (Some "env")
    Assert.Equal(Some "env", blankSecret.ApiKey)
    let cliWins =
        ConsoleConfig.resolve
            { cli with ApiKey = Some "cli" }
            file
            (Some "secret")
            (Some "env")
    Assert.Equal(Some "cli", cliWins.ApiKey)

[<Fact>]
let ``apiKeyFromSecrets uses DefaultAiKey not a list index`` () =
    let lookup name =
        match name with
        | "cursor" -> Some "cursor-secret"
        | _ -> None
    Assert.Equal(
        Some "cursor-secret",
        ConsoleConfig.apiKeyFromSecrets (Some "cursor") lookup)
    Assert.Equal(
        None,
        ConsoleConfig.apiKeyFromSecrets None lookup)
    Assert.Equal(
        None,
        ConsoleConfig.apiKeyFromSecrets (Some "missing") lookup)
    Assert.Equal(
        None,
        ConsoleConfig.apiKeyFromSecrets (Some "cursor") (fun _ -> Some "  "))

[<Fact>]
let ``pickParams prefers non-empty CLI list`` () =
    let file =
        [ { ModelParam.Id = "a"; Value = "1" } ]
    let cliOnly =
        [ { ModelParam.Id = "b"; Value = "2" } ]
    let fromCli = ConsoleConfig.pickParams cliOnly file
    Assert.Equal(1, fromCli.Length)
    Assert.Equal("b", fromCli.[0].Id)
    let fromFile = ConsoleConfig.pickParams [] file
    Assert.Equal(1, fromFile.Length)
    Assert.Equal("a", fromFile.[0].Id)

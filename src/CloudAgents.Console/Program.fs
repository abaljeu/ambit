open System
open Gambol.CloudAgents
open Gambol.CloudAgents.Internal

let printUsage () =
    printfn "Usage: CloudAgents.Console <prompt> [options]"
    printfn ""
    printfn "Options:"
    printfn "  --repo <url>         Repository URL"
    printfn "  --ref <ref>          Starting branch or commit"
    printfn "  --name <name>        Display name for the agent"
    printfn "  --model <id>         Cursor model id"
    printfn "  --api-key <key>      Cursor API key"
    printfn ""
    printfn "Config (CLI wins, then appsettings.<level>.json):"
    printfn "  ApiKey, Model, Repo, Ref, Name"
    printfn "  Level: ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT"
    printfn "  CURSOR_API_KEY fills ApiKey when CLI and file omit it"
    printfn ""
    printfn "Example:"
    printfn
        "  CloudAgents.Console \"Add README\" --repo https://github.com/user/repo --ref main"

let printModels (models: CursorTypes.CursorModel list) =
    printfn "=== Cursor models ==="
    if List.isEmpty models then
        printfn "(none)"
    else
        for m in models do
            printfn "  %s  %s" m.id m.displayName
            match m.description with
            | Some d when d <> "" -> printfn "    %s" d
            | _ -> ()
            if not (List.isEmpty m.aliases) then
                printfn
                    "    aliases: %s"
                    (String.concat ", " m.aliases)
            for v in m.variants do
                let label = v.displayName |> Option.defaultValue ""
                printfn "    variant %s %s" v.id label
    printfn ""

let printCatalog apiKey =
    match CursorHttp.listModels apiKey with
    | Error msg ->
        printfn "Could not list models: %s" msg
        printfn ""
    | Ok models -> printModels models

let startRepos repoUrl startingRef =
    match repoUrl with
    | None -> None
    | Some url ->
        Some
            [ { RepoConfig.Url = url
                StartingRef = startingRef } ]

let printGitChanges (git: GitResult list) =
    if not (List.isEmpty git) then
        printfn "=== Git Changes ==="
        for item in git do
            printfn "Repository: %s" item.RepoUrl
            match item.Branch with
            | Some branch -> printfn "Branch: %s" branch
            | None -> ()
            match item.PullRequestUrl with
            | Some pr -> printfn "Pull Request: %s" pr
            | None -> ()
            printfn ""

let waitForResult apiKey agentId runId =
    match
        AgentRunner.waitUntilComplete
            { RunnerConfig.ApiKey = apiKey }
            agentId
            runId
            5000
            None
    with
    | Error err ->
        printfn "Agent failed: %A" err
        1
    | Ok result ->
        printfn ""
        printfn "=== Result ==="
        printfn "%s" result.Text
        printfn ""
        printGitChanges result.Git
        0

let runAgent promptText apiKey repos options =
    printfn "Starting agent..."
    printfn "Prompt: %s" promptText
    match options.ModelHint with
    | Some model -> printfn "Model: %s" model
    | None -> printfn "Model: (Cursor default)"
    match repos with
    | Some(r :: _) -> printfn "Repo: %s" r.Url
    | _ -> printfn "No repository (agent will run standalone)"
    match
        AgentRunner.start
            { RunnerConfig.ApiKey = apiKey }
            promptText
            repos
            options
    with
    | Error err ->
        printfn "Failed to start agent: %A" err
        1
    | Ok(agentId, runId) ->
        printfn "Agent ID: %s" agentId
        printfn "Run ID: %s" runId
        printfn ""
        printfn "Waiting for completion..."
        waitForResult apiKey agentId runId

[<EntryPoint>]
let main argv =
    let cli =
        ConsoleConfig.parseArgs (Array.toList argv) ConsoleConfig.emptyCli
    let file = ConsoleConfig.loadFiles ()
    let envKey =
        Environment.GetEnvironmentVariable "CURSOR_API_KEY"
        |> Option.ofObj
    let settings = ConsoleConfig.resolve cli file envKey
    match settings.Prompt with
    | None ->
        printUsage ()
        1
    | Some promptText ->
        match settings.ApiKey with
        | None ->
            printfn "Error: no API key"
            printfn "Set --api-key, appsettings ApiKey, or CURSOR_API_KEY"
            printfn "Get a key from: https://cursor.com/settings"
            1
        | Some apiKey ->
            printCatalog apiKey
            let repos = startRepos settings.Repo settings.Ref
            let options =
                { AgentOptions.DisplayName = settings.Name
                  ModelHint = settings.Model }
            runAgent promptText apiKey repos options

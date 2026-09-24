open System
open Microsoft.Extensions.Configuration
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
    printfn "  --param id=value     Model param (repeatable)"
    printfn "  --api-key <key>      Cursor API key"
    printfn ""
    printfn "Config (CLI wins, then appsettings.<level>.json):"
    printfn "  Model, ModelParams, Repo, Ref, Name"
    printfn "  Level: ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT"
    printfn "  ApiKey: --api-key, user-secrets, then CURSOR_API_KEY"
    printfn "  Secrets: DefaultAiKey selects AiKeys:desktop"
    printfn "  Catalog: cursor-models.json beside CloudAgents"
    printfn ""
    printfn "Example:"
    printfn
        "  CloudAgents.Console \"Add README\" --repo https://github.com/user/repo"
    printfn "    --ref main --param context=256k"

let private formatParamValues
    (p: CursorTypes.CursorModelParameter)
    =
    p.values
    |> List.map (fun v -> v.value)
    |> String.concat ", "

let private formatVariantParams
    (ps: CursorTypes.CursorParamAssignment list)
    =
    ps
    |> List.map (fun p -> $"{p.id}={p.value}")
    |> String.concat ", "

let printModels (models: CursorTypes.CursorModel list) (source: string) =
    printfn "=== Cursor models (%s) ===" source
    if List.isEmpty models then
        printfn "(none)"
    else
        for m in models do
            printfn "  %s  %s" m.id m.displayName
            if not (List.isEmpty m.aliases) then
                printfn
                    "    aliases: %s"
                    (String.concat ", " m.aliases)
            for p in m.parameters do
                let allowed = formatParamValues p
                if allowed = "" then
                    printfn "    param %s" p.id
                else
                    printfn "    param %s: %s" p.id allowed
            for v in m.variants do
                let label = v.displayName |> Option.defaultValue ""
                let paramText = formatVariantParams v.``params``
                if paramText = "" then
                    printfn "    variant %s %s" v.id label
                else
                    printfn
                        "    variant %s %s (%s)"
                        v.id
                        label
                        paramText
    printfn ""

let printCatalog (apiKey: string) =
    let fromFile = CursorModelsFile.tryFindPath () |> Option.isSome
    match CursorModelsFile.loadStartCatalog apiKey with
    | Error msg ->
        let label =
            if fromFile then "cursor-models.json" else "live API"
        printfn "Could not load model catalog (%s): %s" label msg
        printfn ""
        Error msg
    | Ok models ->
        let source =
            if fromFile then "cursor-models.json" else "live API"
        //printModels models source
        Ok models

let findModel
    (models: CursorTypes.CursorModel list)
    (hint: string)
    =
    models
    |> List.tryFind (fun m ->
        m.id = hint || List.contains hint m.aliases)

let private validateParam
    (model: CursorTypes.CursorModel)
    (p: ModelParam)
    =
    match
        model.parameters
        |> List.tryFind (fun mp -> mp.id = p.Id)
    with
    | None ->
        Error $"Unknown param '{p.Id}' for model '{model.id}'"
    | Some def ->
        let allowed =
            def.values |> List.map (fun v -> v.value)
        if List.isEmpty allowed then Ok()
        elif List.contains p.Value allowed then Ok()
        else
            let allowedText = String.concat ", " allowed
            Error
                $"Invalid value '{p.Value}' for param '{p.Id}' (allowed: {allowedText})"

let validateSelection
    (models: CursorTypes.CursorModel list)
    (modelId: string option)
    (parameters: ModelParam list)
    =
    match modelId with
    | None ->
        if List.isEmpty parameters then Ok()
        else Error "Model params require --model"
    | Some hint ->
        match findModel models hint with
        | None -> Error $"Unknown model id '{hint}'"
        | Some model ->
            parameters
            |> List.fold
                (fun acc p ->
                    match acc with
                    | Error _ -> acc
                    | Ok() -> validateParam model p)
                (Ok())

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

let private printFinished wroteText (result: AgentResult) =
    printfn ""
    printfn "=== Result ==="
    if wroteText then ()
    elif String.IsNullOrEmpty result.Text then printfn "(empty)"
    else printfn "%s" result.Text
    printfn ""
    printGitChanges result.Git

let private applyPrinted wroteText ev =
    match ev with
    | AgentStreamEvent.AssistantText text ->
        stdout.Write text
        stdout.Flush()
        true
    | AgentStreamEvent.RunFinished result ->
        printFinished wroteText result
        wroteText
    | AgentStreamEvent.RunFailed msg ->
        printfn ""
        printfn "Agent failed: %s" msg
        wroteText
    | AgentStreamEvent.RunCancelled ->
        printfn ""
        printfn "Agent cancelled"
        wroteText

let streamForResult apiKey agentId runId =
    printfn "Streaming response..."
    let args =
        { Config = { RunnerConfig.ApiKey = apiKey }
          AgentId = agentId
          RunId = runId
          MaxWaitMs = None }
    let fold = { Seed = false; OnEvent = applyPrinted }
    match AgentRunner.streamUntilComplete args fold with
    | Error err ->
        printfn "Agent failed: %A" err
        1
    | Ok(_, _) -> 0

let runAgent promptText apiKey repos options =
    printfn "Starting agent..."
    printfn "Prompt: %s" promptText
    match options.ModelHint with
    | Some model -> printfn "Model: %s" model
    | None -> printfn "Model: (Cursor default)"
    if not (List.isEmpty options.ModelParams) then
        let parts =
            options.ModelParams
            |> List.map (fun p -> $"{p.Id}={p.Value}")
        printfn "Params: %s" (String.concat ", " parts)
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
        streamForResult apiKey agentId runId

let private loadUserSecretApiKey () =
    let config =
        ConfigurationBuilder()
            .AddUserSecrets(
                System.Reflection.Assembly.GetExecutingAssembly(),
                optional = true)
            .Build()
    ConsoleConfig.apiKeyFromSecrets
        (config.["DefaultAiKey"] |> Option.ofObj)
        (fun name -> config.[$"AiKeys:{name}"] |> Option.ofObj)

let private resolveSettings argv =
    let cli =
        ConsoleConfig.parseArgs
            (Array.toList argv)
            ConsoleConfig.emptyCli
    let file = ConsoleConfig.loadFiles ()
    let secretKey = loadUserSecretApiKey ()
    let envKey =
        Environment.GetEnvironmentVariable "CURSOR_API_KEY"
        |> Option.ofObj
    ConsoleConfig.resolve cli file secretKey envKey

[<EntryPoint>]
let main argv =
    let settings = resolveSettings argv
    match settings.ApiKey with
    | None ->
        printfn "Error: no API key"
        printfn "Set --api-key, user-secrets AiKeys:desktop, or CURSOR_API_KEY"
        printfn "Get a key from: https://cursor.com/settings"
        1
    | Some apiKey ->
        match printCatalog apiKey with
        | Error _ -> 1
        | Ok models ->
            match
                validateSelection
                    models
                    settings.Model
                    settings.ModelParams
            with
            | Error msg ->
                printfn "Error: %s" msg
                1
            | Ok() ->
                match settings.Prompt with
                | None ->
                    printUsage ()
                    1
                | Some promptText ->
                    let repos =
                        startRepos settings.Repo settings.Ref
                    let options =
                        { AgentOptions.DisplayName = settings.Name
                          ModelHint = settings.Model
                          ModelParams = settings.ModelParams }
                    runAgent promptText apiKey repos options

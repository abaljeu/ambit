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
    printfn "  --param id=value     Model param (repeatable)"
    printfn "  --api-key <key>      Cursor API key"
    printfn ""
    printfn "Config (CLI wins, then appsettings.<level>.json):"
    printfn "  ApiKey, Model, ModelParams, Repo, Ref, Name"
    printfn "  Level: ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT"
    printfn "  CURSOR_API_KEY fills ApiKey when CLI and file omit it"
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
        printModels models source
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
    match settings.ApiKey with
    | None ->
        printfn "Error: no API key"
        printfn "Set --api-key, appsettings ApiKey, or CURSOR_API_KEY"
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

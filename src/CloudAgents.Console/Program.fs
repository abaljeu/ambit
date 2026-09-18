open System
open Gambol.CloudAgents

let printUsage () =
    printfn "Usage: CloudAgents.Console <prompt> [options]"
    printfn ""
    printfn "Options:"
    printfn "  --repo <url>         Repository URL"
    printfn "  --ref <ref>          Starting branch or commit"
    printfn "  --name <name>        Display name for the agent"
    printfn ""
    printfn "Environment:"
    printfn "  CURSOR_API_KEY       Required: API key from Cursor"
    printfn ""
    printfn "Example:"
    printfn
        "  CloudAgents.Console \"Add README\" --repo https://github.com/user/repo --ref main"

let rec parseArgs
    (args: string list)
    (prompt: string option)
    (repo: string option)
    (ref: string option)
    (name: string option)
    =
    match args with
    | [] -> (prompt, repo, ref, name)
    | "--repo" :: url :: rest ->
        parseArgs rest prompt (Some url) ref name
    | "--ref" :: r :: rest -> parseArgs rest prompt repo (Some r) name
    | "--name" :: n :: rest -> parseArgs rest prompt repo ref (Some n)
    | text :: rest ->
        match prompt with
        | None -> parseArgs rest (Some text) repo ref name
        | Some _ -> parseArgs rest prompt repo ref name

[<EntryPoint>]
let main argv =
    let (prompt, repoUrl, startingRef, displayName) =
        parseArgs (Array.toList argv) None None None None

    match prompt with
    | None ->
        printUsage ()
        1
    | Some promptText ->
        match Environment.GetEnvironmentVariable "CURSOR_API_KEY" with
        | null
        | "" ->
            printfn "Error: CURSOR_API_KEY environment variable not set"
            printfn
                "Get your API key from: https://cursor.com/settings"
            1
        | apiKey ->
            let config = { RunnerConfig.ApiKey = apiKey }

            let repos =
                match repoUrl with
                | None -> None
                | Some url ->
                    Some
                        [ { RepoConfig.Url = url
                            StartingRef = startingRef } ]

            let options =
                { AgentOptions.DisplayName = displayName
                  ModelHint = None }

            printfn "Starting agent..."
            printfn "Prompt: %s" promptText

            match repoUrl with
            | Some url -> printfn "Repo: %s" url
            | None -> printfn "No repository (agent will run standalone)"

            match
                AgentRunner.start config promptText repos options
            with
            | Error err ->
                printfn "Failed to start agent: %A" err
                1
            | Ok(agentId, runId) ->
                printfn "Agent ID: %s" agentId
                printfn "Run ID: %s" runId
                printfn ""
                printfn "Waiting for completion..."

                match
                    AgentRunner.waitUntilComplete
                        config
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

                    if not (List.isEmpty result.Git) then
                        printfn "=== Git Changes ==="

                        for git in result.Git do
                            printfn "Repository: %s" git.RepoUrl

                            match git.Branch with
                            | Some branch ->
                                printfn "Branch: %s" branch
                            | None -> ()

                            match git.PullRequestUrl with
                            | Some pr -> printfn "Pull Request: %s" pr
                            | None -> ()

                            printfn ""

                    0

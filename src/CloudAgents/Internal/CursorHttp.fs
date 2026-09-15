namespace Gambol.CloudAgents.Internal

open System
open System.Net.Http
open System.Text
open FSharp.Data

module CursorHttp =

    let private baseUrl = "https://api.cursor.com/v1"

    let private createClient (apiKey: string) =
        let client = new HttpClient()
        let auth =
            Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{apiKey}:")
            )
        client.DefaultRequestHeaders.Authorization <-
            Headers.AuthenticationHeaderValue("Basic", auth)
        client

    let createAgent
        (apiKey: string)
        (request: CursorTypes.CursorCreateRequest)
        : Result<CursorTypes.CursorCreateResponse, string> =
        try
            use client = createClient apiKey
            let json =
                JsonValue.Record [|
                    "prompt",
                    JsonValue.Record [|
                        "text", JsonValue.String request.prompt.text
                    |]
                    match request.name with
                    | Some name -> "name", JsonValue.String name
                    | None -> ()
                    match request.repos with
                    | Some repos ->
                        "repos",
                        JsonValue.Array [|
                            for r in repos ->
                                JsonValue.Record [|
                                    "url", JsonValue.String r.url
                                    match r.startingRef with
                                    | Some ref ->
                                        "startingRef",
                                        JsonValue.String ref
                                    | None -> ()
                                |]
                        |]
                    | None -> ()
                |]

            let content =
                new StringContent(
                    json.ToString(),
                    Encoding.UTF8,
                    "application/json"
                )

            let response =
                client.PostAsync($"{baseUrl}/agents", content)
                |> Async.AwaitTask
                |> Async.RunSynchronously

            if not response.IsSuccessStatusCode then
                let body =
                    response.Content.ReadAsStringAsync()
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                Error $"HTTP {int response.StatusCode}: {body}"
            else
                let body =
                    response.Content.ReadAsStringAsync()
                    |> Async.AwaitTask
                    |> Async.RunSynchronously

                let parsed = JsonValue.Parse body

                let agent: CursorTypes.CursorAgent =
                    { id = parsed.["agent"].["id"].AsString()
                      name = parsed.["agent"].["name"].AsString()
                      status = parsed.["agent"].["status"].AsString()
                      latestRunId =
                          parsed.["agent"].["latestRunId"].AsString() }

                let run: CursorTypes.CursorRun =
                    { id = parsed.["run"].["id"].AsString()
                      agentId = parsed.["run"].["agentId"].AsString()
                      status = parsed.["run"].["status"].AsString() }

                Ok
                    { CursorTypes.CursorCreateResponse.agent = agent
                      run = run }
        with ex ->
            Error $"Request failed: {ex.Message}"

    let getRunStatus
        (apiKey: string)
        (agentId: string)
        (runId: string)
        : Result<CursorTypes.CursorRunStatus, string> =
        try
            use client = createClient apiKey
            let url = $"{baseUrl}/agents/{agentId}/runs/{runId}"

            let response =
                client.GetAsync(url)
                |> Async.AwaitTask
                |> Async.RunSynchronously

            if not response.IsSuccessStatusCode then
                let body =
                    response.Content.ReadAsStringAsync()
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                Error $"HTTP {int response.StatusCode}: {body}"
            else
                let body =
                    response.Content.ReadAsStringAsync()
                    |> Async.AwaitTask
                    |> Async.RunSynchronously

                let parsed = JsonValue.Parse body

                let resultText =
                    parsed.TryGetProperty("result")
                    |> Option.map (fun v -> v.AsString())

                let durationMs =
                    parsed.TryGetProperty("durationMs")
                    |> Option.map (fun v -> v.AsInteger())

                let git =
                    parsed.TryGetProperty("git")
                    |> Option.map (fun gitObj ->
                        let branches =
                            gitObj.["branches"].AsArray()
                            |> Array.map (fun b ->
                                { CursorTypes.CursorGitBranch.repoUrl =
                                    b.["repoUrl"].AsString()
                                  CursorTypes.CursorGitBranch.branch =
                                      b.TryGetProperty("branch")
                                      |> Option.map (fun v ->
                                          v.AsString())
                                  CursorTypes.CursorGitBranch.prUrl =
                                      b.TryGetProperty("prUrl")
                                      |> Option.map (fun v ->
                                          v.AsString()) })
                            |> Array.toList

                        { CursorTypes.CursorGit.branches = branches })

                Ok
                    { CursorTypes.CursorRunStatus.id =
                        parsed.["id"].AsString()
                      CursorTypes.CursorRunStatus.agentId =
                          parsed.["agentId"].AsString()
                      CursorTypes.CursorRunStatus.status =
                          parsed.["status"].AsString()
                      CursorTypes.CursorRunStatus.result = resultText
                      CursorTypes.CursorRunStatus.durationMs = durationMs
                      CursorTypes.CursorRunStatus.git = git }
        with ex ->
            Error $"Request failed: {ex.Message}"

    let cancelRun
        (apiKey: string)
        (agentId: string)
        (runId: string)
        : Result<unit, string> =
        try
            use client = createClient apiKey
            let url = $"{baseUrl}/agents/{agentId}/runs/{runId}/cancel"

            let content =
                new StringContent("", Encoding.UTF8, "application/json")

            let response =
                client.PostAsync(url, content)
                |> Async.AwaitTask
                |> Async.RunSynchronously

            if not response.IsSuccessStatusCode then
                let body =
                    response.Content.ReadAsStringAsync()
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                Error $"HTTP {int response.StatusCode}: {body}"
            else
                Ok()
        with ex ->
            Error $"Request failed: {ex.Message}"

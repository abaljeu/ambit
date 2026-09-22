namespace Gambol.CloudAgents.Internal

open System
open System.Net
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

    let private optString (json: JsonValue) name =
        match json.TryGetProperty name with
        | Some(JsonValue.String s) -> Some s
        | Some JsonValue.Null
        | None -> None
        | Some v -> Some(v.AsString())

    let private optString2 json camel snake =
        optString json camel
        |> Option.orElse (optString json snake)

    let private stringList (json: JsonValue) name =
        match json.TryGetProperty name with
        | Some(JsonValue.Array arr) ->
            arr
            |> Array.choose (fun v ->
                match v with
                | JsonValue.String s -> Some s
                | _ -> None)
            |> Array.toList
        | _ -> []

    let private parseVariant (json: JsonValue) =
        match optString json "id" with
        | None -> None
        | Some id ->
            Some
                { CursorTypes.CursorModelVariant.id = id
                  CursorTypes.CursorModelVariant.displayName =
                      optString2 json "displayName" "display_name" }

    let private parseVariants (json: JsonValue) =
        match json.TryGetProperty "variants" with
        | Some(JsonValue.Array arr) ->
            arr
            |> Array.choose parseVariant
            |> Array.toList
        | _ -> []

    let private parseModel (json: JsonValue) =
        match optString json "id" with
        | None -> None
        | Some id ->
            Some
                { CursorTypes.CursorModel.id = id
                  CursorTypes.CursorModel.displayName =
                      optString2 json "displayName" "display_name"
                      |> Option.defaultValue id
                  CursorTypes.CursorModel.description =
                      optString json "description"
                  CursorTypes.CursorModel.aliases =
                      stringList json "aliases"
                  CursorTypes.CursorModel.parameters =
                      json.TryGetProperty "parameters"
                      |> Option.map (fun v -> v.ToString())
                  CursorTypes.CursorModel.variants =
                      parseVariants json }

    let private modelNodes (parsed: JsonValue) =
        match parsed.TryGetProperty "items" with
        | Some(JsonValue.Array arr) -> Array.toList arr
        | _ ->
            match parsed.TryGetProperty "models" with
            | Some(JsonValue.Array arr) -> Array.toList arr
            | _ ->
                match parsed with
                | JsonValue.Array arr -> Array.toList arr
                | _ -> []

    let parseModelCatalog (body: string) =
        try
            let parsed = JsonValue.Parse body
            Ok(
                modelNodes parsed
                |> List.choose parseModel
            )
        with ex ->
            Error $"Invalid models response: {ex.Message}"

    let private repoJson (r: CursorTypes.CursorRepo) =
        JsonValue.Record [|
            "url", JsonValue.String r.url
            match r.startingRef with
            | Some ref -> "startingRef", JsonValue.String ref
            | None -> ()
        |]

    let private modelJson (modelId: string) =
        JsonValue.Record [| "id", JsonValue.String modelId |]

    let createRequestJson
        (request: CursorTypes.CursorCreateRequest)
        : JsonValue =
        JsonValue.Record [|
            "prompt",
            JsonValue.Record [|
                "text", JsonValue.String request.prompt.text
            |]
            match request.name with
            | Some name -> "name", JsonValue.String name
            | None -> ()
            match request.model with
            | Some model -> "model", modelJson model
            | None -> ()
            match request.repos with
            | Some repos ->
                "repos",
                JsonValue.Array [|
                    for r in repos -> repoJson r
                |]
            | None -> ()
        |]

    let listModels
        (apiKey: string)
        : Result<CursorTypes.CursorModel list, string> =
        try
            use client = createClient apiKey
            let response =
                client.GetAsync($"{baseUrl}/models")
                |> Async.AwaitTask
                |> Async.RunSynchronously

            if response.StatusCode = HttpStatusCode.Unauthorized then
                Error "unauthorized"
            elif not response.IsSuccessStatusCode then
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
                parseModelCatalog body
        with ex ->
            Error $"Request failed: {ex.Message}"

    let createAgent
        (apiKey: string)
        (request: CursorTypes.CursorCreateRequest)
        : Result<CursorTypes.CursorCreateResponse, string> =
        try
            use client = createClient apiKey
            let json = createRequestJson request

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

            if response.StatusCode = HttpStatusCode.Unauthorized then
                Error "unauthorized"
            elif not response.IsSuccessStatusCode then
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

            if response.StatusCode = HttpStatusCode.Unauthorized then
                Error "unauthorized"
            elif not response.IsSuccessStatusCode then
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

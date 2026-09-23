namespace Gambol.CloudAgents.Internal

open System
open System.IO
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

    let private parseParamAssignment (json: JsonValue) =
        match optString json "id" with
        | None -> None
        | Some id ->
            match optString json "value" with
            | None -> None
            | Some value ->
                Some
                    { CursorTypes.CursorParamAssignment.id = id
                      CursorTypes.CursorParamAssignment.value = value }

    let private parseParamAssignments (json: JsonValue) =
        match json.TryGetProperty "params" with
        | Some(JsonValue.Array arr) ->
            arr
            |> Array.choose parseParamAssignment
            |> Array.toList
        | _ -> []

    let private parseVariant (json: JsonValue) =
        match optString json "id" with
        | None -> None
        | Some id ->
            Some
                { CursorTypes.CursorModelVariant.id = id
                  CursorTypes.CursorModelVariant.displayName =
                      optString2 json "displayName" "display_name"
                  CursorTypes.CursorModelVariant.``params`` =
                      parseParamAssignments json }

    let private parseVariants (json: JsonValue) =
        match json.TryGetProperty "variants" with
        | Some(JsonValue.Array arr) ->
            arr
            |> Array.choose parseVariant
            |> Array.toList
        | _ -> []

    let private parseParamValue (json: JsonValue) =
        match optString json "value" with
        | None -> None
        | Some value ->
            Some
                { CursorTypes.CursorParamValue.value = value
                  CursorTypes.CursorParamValue.displayName =
                      optString2 json "displayName" "display_name" }

    let private parseParameter (json: JsonValue) =
        match optString json "id" with
        | None -> None
        | Some id ->
            let values =
                match json.TryGetProperty "values" with
                | Some(JsonValue.Array arr) ->
                    arr
                    |> Array.choose parseParamValue
                    |> Array.toList
                | _ -> []
            Some
                { CursorTypes.CursorModelParameter.id = id
                  CursorTypes.CursorModelParameter.displayName =
                      optString2 json "displayName" "display_name"
                  CursorTypes.CursorModelParameter.values = values }

    let private parseParameters (json: JsonValue) =
        match json.TryGetProperty "parameters" with
        | Some(JsonValue.Array arr) ->
            arr
            |> Array.choose parseParameter
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
                      parseParameters json
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

    let private paramJson (p: CursorTypes.CursorParamAssignment) =
        JsonValue.Record [|
            "id", JsonValue.String p.id
            "value", JsonValue.String p.value
        |]

    let private modelJson (model: CursorTypes.CursorModelRef) =
        JsonValue.Record [|
            "id", JsonValue.String model.id
            match model.``params`` with
            | [] -> ()
            | ps ->
                "params",
                JsonValue.Array [| for p in ps -> paramJson p |]
        |]

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

    type SseMessage = { eventType: string; data: string }

    let parseSseDocument (body: string) : SseMessage list =
        let lines =
            body.Replace("\r\n", "\n").Split('\n')
            |> Array.toList

        let flush pending eventType dataLines =
            match eventType with
            | None -> pending
            | Some ev ->
                let data =
                    dataLines
                    |> List.rev
                    |> String.concat "\n"
                { eventType = ev; data = data } :: pending

        let folder (pending, eventType, dataLines) line =
            if line = "" then
                flush pending eventType dataLines, None, []
            elif line.StartsWith("event:", StringComparison.Ordinal) then
                let ev = line.Substring(6).Trim()
                pending, Some ev, []
            elif line.StartsWith("data:", StringComparison.Ordinal) then
                let chunk = line.Substring(5).TrimStart()
                pending, eventType, chunk :: dataLines
            else
                pending, eventType, dataLines

        let pending, eventType, dataLines =
            List.fold folder ([], None, []) lines

        flush pending eventType dataLines |> List.rev

    let private parseGit (gitObj: JsonValue) =
        let branches =
            match gitObj.TryGetProperty "branches" with
            | Some(JsonValue.Array arr) ->
                arr
                |> Array.choose (fun b ->
                    match b with
                    | JsonValue.Record _ ->
                        Some
                            { CursorTypes.CursorGitBranch.repoUrl =
                                b.["repoUrl"].AsString()
                              CursorTypes.CursorGitBranch.branch =
                                  b.TryGetProperty("branch")
                                  |> Option.map (fun v -> v.AsString())
                              CursorTypes.CursorGitBranch.prUrl =
                                  b.TryGetProperty("prUrl")
                                  |> Option.map (fun v -> v.AsString()) }
                    | _ -> None)
                |> Array.toList
            | _ -> []

        { CursorTypes.CursorGit.branches = branches }

    let private parseStreamResult (data: string) =
        try
            let parsed = JsonValue.Parse data
            let text =
                parsed.TryGetProperty("result")
                |> Option.map (fun v -> v.AsString())
                |> Option.defaultValue ""
            let git =
                parsed.TryGetProperty("git")
                |> Option.map parseGit
            Ok(text, git)
        with ex ->
            Error $"Invalid stream result: {ex.Message}"

    type StreamDispatch =
        | Continue
        | Terminal of Gambol.CloudAgents.AgentResult
        | Failed of string
        | Cancelled

    let private dispatchStreamMessage
        (msg: SseMessage)
        (onAssistant: string -> unit)
        : StreamDispatch =
        let ev = msg.eventType.ToLowerInvariant()
        match ev with
        | "assistant" ->
            match JsonValue.TryParse msg.data with
            | Some parsed ->
                match parsed.TryGetProperty "text" with
                | Some(JsonValue.String text) ->
                    onAssistant text
                    Continue
                | _ -> Continue
            | None -> Continue
        | "result" ->
            match parseStreamResult msg.data with
            | Error msg -> Failed msg
            | Ok(text, git) ->
                let gitResults =
                    git
                    |> Option.map (fun g ->
                        g.branches
                        |> List.map (fun b ->
                            { Gambol.CloudAgents.GitResult.RepoUrl =
                                b.repoUrl
                              Gambol.CloudAgents.GitResult.Branch =
                                  b.branch
                              Gambol.CloudAgents.GitResult.PullRequestUrl =
                                  b.prUrl }))
                    |> Option.defaultValue []
                Terminal
                    { Gambol.CloudAgents.AgentResult.Text = text
                      Gambol.CloudAgents.AgentResult.Git = gitResults }
        | "error" ->
            let message =
                match JsonValue.TryParse msg.data with
                | Some parsed ->
                    parsed.TryGetProperty("message")
                    |> Option.map (fun v -> v.AsString())
                    |> Option.defaultValue msg.data
                | None -> msg.data
            Failed message
        | "done" -> Cancelled
        | _ -> Continue

    let streamRun
        (apiKey: string)
        (agentId: string)
        (runId: string)
        (onAssistant: string -> unit)
        : Result<Gambol.CloudAgents.AgentResult, string> =
        try
            use client = createClient apiKey
            let url =
                $"{baseUrl}/agents/{agentId}/runs/{runId}/stream"

            let response =
                client.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead
                )
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
                use stream =
                    response.Content.ReadAsStreamAsync()
                    |> Async.AwaitTask
                    |> Async.RunSynchronously

                use reader = new StreamReader(stream)
                let buffer = StringBuilder()

                let rec consume pending eventType dataLines =
                    match reader.ReadLine() with
                    | null ->
                        let doc = buffer.ToString()
                        buffer.Clear() |> ignore
                        let messages =
                            if doc = "" then []
                            else parseSseDocument doc
                        List.fold folder pending messages
                    | line ->
                        if line = "" then
                            let doc = buffer.ToString()
                            buffer.Clear() |> ignore
                            let messages =
                                if doc = "" then []
                                else parseSseDocument doc
                            consume (List.fold folder pending messages) None []
                        else
                            buffer.AppendLine(line) |> ignore
                            consume pending eventType dataLines

                and folder pending (msg: SseMessage) =
                    match dispatchStreamMessage msg onAssistant with
                    | Continue -> pending
                    | Terminal result -> Error result
                    | Failed msg -> Error(Failed msg)
                    | Cancelled -> Error Cancelled

                let outcome = consume [] None []

                match outcome with
                | Ok result -> Ok result
                | Error(Choice1Of2 result) -> Ok result
                | Error(Choice2Of2 tag) ->
                    match tag with
                    | Failed msg -> Error msg
                    | Cancelled ->
                        Error "cancelled"
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

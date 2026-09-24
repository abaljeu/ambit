namespace Gambol.CloudAgents.Internal

open System
open System.IO
open System.Net
open System.Net.Http
open System.Text
open System.Threading
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

    type private SseAccum =
        { eventType: string option
          dataLines: string list }

    let private emptySse =
        { eventType = None; dataLines = [] }

    type private SseStep =
        | SseKeep of SseAccum
        | SseEmit of SseMessage * SseAccum

    let private sseData (lines: string list) =
        lines |> List.rev |> String.concat "\n"

    let private stepSseLine (acc: SseAccum) (line: string) =
        if line = "" then
            match acc.eventType with
            | None -> SseKeep emptySse
            | Some ev ->
                let msg =
                    { eventType = ev
                      data = sseData acc.dataLines }
                SseEmit(msg, emptySse)
        elif line.StartsWith("event:", StringComparison.Ordinal) then
            let ev = line.Substring(6).Trim()
            SseKeep { eventType = Some ev; dataLines = [] }
        elif line.StartsWith("data:", StringComparison.Ordinal) then
            let chunk = line.Substring(5).TrimStart()
            SseKeep { acc with dataLines = chunk :: acc.dataLines }
        else
            SseKeep acc

    let private foldSseLine (pending, acc) line =
        match stepSseLine acc line with
        | SseKeep next -> pending, next
        | SseEmit(msg, next) -> msg :: pending, next

    let parseSseDocument (body: string) : SseMessage list =
        let lines =
            body.Replace("\r\n", "\n").Split('\n')
            |> Array.toList
        let pending, acc =
            List.fold foldSseLine ([], emptySse) lines
        match stepSseLine acc "" with
        | SseEmit(msg, _) -> List.rev (msg :: pending)
        | SseKeep _ -> List.rev pending

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

    type StreamBody =
        { text: string
          git: CursorTypes.CursorGit option }

    type private StreamDispatch =
        | Continue
        | Assistant of string
        | Terminal of StreamBody
        | Failed of string
        | CancelledRun

    let private isCancelledStatus (data: string) =
        match JsonValue.TryParse data with
        | Some parsed ->
            match optString parsed "status" with
            | Some status ->
                status.ToUpperInvariant() = "CANCELLED"
            | None -> false
        | None -> false

    let private assistantText (data: string) =
        match JsonValue.TryParse data with
        | Some parsed ->
            match parsed.TryGetProperty "text" with
            | Some(JsonValue.String text) -> Some text
            | _ -> None
        | None -> None

    let private errorText (data: string) =
        match JsonValue.TryParse data with
        | Some parsed ->
            parsed.TryGetProperty("message")
            |> Option.map (fun v -> v.AsString())
            |> Option.defaultValue data
        | None -> data

    let private dispatchStreamMessage (msg: SseMessage) =
        match msg.eventType.ToLowerInvariant() with
        | "status"
        | "result"
        | "done" when isCancelledStatus msg.data -> CancelledRun
        | "assistant" ->
            match assistantText msg.data with
            | Some text -> Assistant text
            | None -> Continue
        | "result" ->
            match parseStreamResult msg.data with
            | Error err -> Failed err
            | Ok(text, git) ->
                Terminal { text = text; git = git }
        | "error" -> Failed(errorText msg.data)
        | "done" -> Terminal { text = ""; git = None }
        | _ -> Continue

    type private StreamReadOutcome =
        | StreamIncomplete
        | StreamComplete of StreamBody
        | StreamStop of string

    let private applyStreamMessage onAssistant state msg =
        match dispatchStreamMessage msg with
        | Continue -> StreamIncomplete, state
        | Assistant text ->
            StreamIncomplete, onAssistant text state
        | Terminal body -> StreamComplete body, state
        | Failed reason -> StreamStop reason, state
        | CancelledRun -> StreamStop "cancelled", state

    type private SseAdvance<'s> =
        | SseContinue of SseAccum * 's
        | SseDone of StreamReadOutcome * 's

    let private advanceSse onAssistant state acc line =
        match stepSseLine acc line with
        | SseKeep next -> SseContinue(next, state)
        | SseEmit(msg, next) ->
            match applyStreamMessage onAssistant state msg with
            | StreamIncomplete, nextState ->
                SseContinue(next, nextState)
            | other, nextState ->
                SseDone(other, nextState)

    let private finishSse onAssistant state acc =
        match stepSseLine acc "" with
        | SseEmit(msg, _) ->
            applyStreamMessage onAssistant state msg
        | SseKeep _ -> StreamIncomplete, state

    let private outcomeResult outcome =
        match outcome with
        | StreamComplete body -> Ok body
        | StreamStop msg -> Error msg
        | StreamIncomplete ->
            Error "stream ended without terminal event"

    let private nextSseList lines =
        match lines with
        | [] -> None
        | line :: tail -> Some(line, tail)

    let private nextSseReader
        (reader: StreamReader, token: CancellationToken)
        =
        let line =
            reader.ReadLineAsync(token).AsTask()
            |> Async.AwaitTask
            |> Async.RunSynchronously
        match line with
        | null -> None
        | line -> Some(line, (reader, token))

    let private readSseLines next source onAssistant state =
        let rec loop acc state source =
            match next source with
            | None -> finishSse onAssistant state acc
            | Some(line, rest) ->
                match advanceSse onAssistant state acc line with
                | SseContinue(nextAcc, nextState) ->
                    loop nextAcc nextState rest
                | SseDone(outcome, doneState) ->
                    outcome, doneState

        loop emptySse state source

    let interpretSseDocument
        (body: string)
        (onAssistant: string -> unit)
        : Result<StreamBody, string> =
        let lines =
            body.Replace("\r\n", "\n").Split('\n')
            |> Array.toList

        let notify text () = onAssistant text
        let outcome, _ =
            readSseLines nextSseList lines notify ()
        outcomeResult outcome

    let private responseError (response: HttpResponseMessage) =
        if response.StatusCode = HttpStatusCode.Unauthorized then
            Some "unauthorized"
        elif response.IsSuccessStatusCode then
            None
        else
            let body =
                response.Content.ReadAsStringAsync()
                |> Async.AwaitTask
                |> Async.RunSynchronously
            Some $"HTTP {int response.StatusCode}: {body}"

    let private readSuccessBody
        (response: HttpResponseMessage)
        (token: CancellationToken)
        onAssistant
        state
        =
        use stream =
            response.Content.ReadAsStreamAsync(token)
            |> Async.AwaitTask
            |> Async.RunSynchronously
        use reader = new StreamReader(stream)
        let outcome, state =
            readSseLines nextSseReader (reader, token) onAssistant state
        outcomeResult outcome, state

    let private deadlineSource (maxWaitMs: int option) =
        let source = new CancellationTokenSource()
        maxWaitMs |> Option.iter (fun ms -> source.CancelAfter ms)
        source

    /// Error "timeout" when args.MaxWaitMs elapses before a terminal event.
    /// Error "cancelled" when a status / result / done event says CANCELLED.
    let streamRun
        (args: Gambol.CloudAgents.StreamArgs)
        onAssistant
        state
        =
        use deadline = deadlineSource args.MaxWaitMs
        try
            use client = createClient args.Config.ApiKey
            let url =
                $"{baseUrl}/agents/{args.AgentId}/runs/{args.RunId}/stream"
            let response =
                client.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    deadline.Token
                )
                |> Async.AwaitTask
                |> Async.RunSynchronously
            match responseError response with
            | Some msg -> Error msg, state
            | None ->
                readSuccessBody response deadline.Token onAssistant state
        with ex ->
            if deadline.IsCancellationRequested then
                Error "timeout", state
            else
                Error $"Request failed: {ex.Message}", state

    /// Error "run_not_cancellable" on 409: the run is terminal or never ran.
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

            if response.StatusCode = HttpStatusCode.Conflict then
                Error "run_not_cancellable"
            else
                match responseError response with
                | Some msg -> Error msg
                | None -> Ok()
        with ex ->
            Error $"Request failed: {ex.Message}"

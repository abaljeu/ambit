open System
open System.Net.Http
open System.Text
open Gambol.Shared
open Gambol.Shared.CommandEntry

module Enc = Thoth.Json.Newtonsoft.Encode
module Dec = Thoth.Json.Newtonsoft.Decode

type Session =
    { client: HttpClient
      cookie: string
      ambitBase: string }

type Args =
    { ambitBase: string
      mappedRoot: string
      label: string }

let private addSessionHeaders (session: Session) (req: HttpRequestMessage) =
    req.Headers.TryAddWithoutValidation("Cookie", session.cookie) |> ignore
    req.Headers.TryAddWithoutValidation(
        ClientIdentity.HeaderName,
        "stretch-workspace-upload")
    |> ignore

let private cookieFromResponse (resp: HttpResponseMessage) =
    match resp.Headers.TryGetValues("Set-Cookie") with
    | false, _ -> None
    | true, values ->
        values
        |> Seq.tryPick (fun header ->
            let prefix = "gambol_auth="
            let trimmed = header.Trim()
            if
                trimmed.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase)
            then
                let rest = trimmed.Substring(prefix.Length)
                let semi = rest.IndexOf ';'
                let raw =
                    if semi < 0 then rest
                    else rest.Substring(0, semi)
                Some(prefix + raw.Trim())
            else
                None)

let private send
    (session: Session)
    (method: HttpMethod)
    (url: string)
    (body: string option)
    =
    use req = new HttpRequestMessage(method, url)
    addSessionHeaders session req
    match body with
    | None -> ()
    | Some text ->
        req.Content <-
            new StringContent(text, Encoding.UTF8, "application/json")
    let resp = session.client.Send req
    let text = resp.Content.ReadAsStringAsync().Result
    int resp.StatusCode, text, cookieFromResponse resp

let private login (client: HttpClient) (ambitBase: string) =
    let appUrl = ambitBase.TrimEnd('/')
    use req = new HttpRequestMessage(HttpMethod.Get, appUrl)
    let resp = client.Send req
    let text = resp.Content.ReadAsStringAsync().Result
    match cookieFromResponse resp with
    | None ->
        Error(
            "login GET "
            + string (int resp.StatusCode)
            + " had no gambol_auth: "
            + LogText.summarizeHttpBody 200 text)
    | Some cookie ->
        Ok
            { client = client
              cookie = cookie
              ambitBase = ambitBase.TrimEnd('/') }

let private encodeBatch (events: Ev list) =
    Enc.toString 0 (EventJson.encodeEventBatch { events = events })

let private getFullState (session: Session) =
    let url = session.ambitBase + "/state?scope=full"
    let code, text, _ = send session HttpMethod.Get url None
    if code < 200 || code >= 300 then
        Error("GET /state HTTP " + string code + ": " + text)
    else
        Dec.fromString
            ApiResponseSerialization.decodeStateResponseDecoder
            text

let private postChange (session: Session) (ops: Op list) =
    let event = ClientHistory.mintChange (displayName Load) ops
    let body = encodeBatch [ event ]
    let url = session.ambitBase + "/changes"
    let code, text, _ = send session HttpMethod.Post url (Some body)
    if code < 200 || code >= 300 then
        Error("POST /changes HTTP " + string code + ": " + text)
    else
        match
            Dec.fromString
                ApiResponseSerialization.decodeChangeSuccessResponseDecoder
                text
        with
        | Error e -> Error e
        | Ok ack -> Ok(event, ack)

let private createWorkspace (session: Session) (graph: Graph) (label: string) =
    let _id, ops = FileNodeOps.planCreateWorkspace graph label
    if ops.IsEmpty then
        Error "planCreateWorkspace returned no ops"
    else
        postChange session ops

let private inventoryItems (mappedRoot: string) (scope: WorkspaceSyncScope) =
    match WorkspaceLocalInventory.listForUpload mappedRoot scope with
    | Error e -> Error e
    | Ok(_mode, items) ->
        items
        |> List.map (fun i ->
            ({ relative = i.relative
               isDirectory = i.isDirectory }
             : WorkspaceUploadStructure.InventoryItem))
        |> Ok

let private postStubs
    (session: Session)
    (graph: Graph)
    (label: string)
    (items: WorkspaceUploadStructure.InventoryItem list)
    =
    match WorkspaceUploadStructure.planStubOps graph label items with
    | Error e -> Error e
    | Ok [] -> Ok "no stub ops (paths already present or Unloaded)"
    | Ok ops ->
        match postChange session ops with
        | Error e -> Error e
        | Ok(_, ack) ->
            Ok("stub eventId=" + string (EventId.value ack.eventId))

let private markBodiesPresent
    (session: Session)
    (graph: Graph)
    (label: string)
    (paths: string list)
    =
    let ops =
        WorkspaceUploadStructure.planServerFilePresentOps graph label paths
    if ops.IsEmpty then
        Ok "no NoServerFile rows to mark Unparsed"
    else
        match postChange session ops with
        | Error e -> Error e
        | Ok _ -> Ok("marked Unparsed: " + String.concat "," paths)

let private pushMapped
    (session: Session)
    (mappedRoot: string)
    (scope: WorkspaceSyncScope)
    =
    WorkspaceFileSync.post
        session.client
        session.ambitBase
        mappedRoot
        scope
        (Some session.cookie)
        (Some "stretch-workspace-upload")

let private parseArgs (argv: string[]) =
    let ambitBase =
        if argv.Length > 0 then argv.[0]
        else "http://127.0.0.1:5215/ambit"
    let mappedRoot =
        if argv.Length > 1 then argv.[1]
        else "/tmp/ambit-stretch-upload"
    let label =
        if argv.Length > 2 then argv.[2] else "stretch"
    { ambitBase = ambitBase
      mappedRoot = mappedRoot
      label = label }

let private run (args: Args) =
    use handler = new HttpClientHandler()
    handler.AllowAutoRedirect <- false
    use client = new HttpClient(handler)
    client.Timeout <- TimeSpan.FromMinutes 2.0
    let scope: WorkspaceSyncScope =
        { label = args.label
          relative = ""
          kind = SyncScopeKind.Workspace }
    match login client args.ambitBase with
    | Error e -> Error("login: " + e)
    | Ok session ->
        match getFullState session with
        | Error e -> Error("state: " + e)
        | Ok state0 ->
            match createWorkspace session state0.graph args.label with
            | Error e -> Error("create workspace: " + e)
            | Ok(_, created) ->
                match getFullState session with
                | Error e -> Error("state after create: " + e)
                | Ok state1 ->
                    match inventoryItems args.mappedRoot scope with
                    | Error e -> Error("inventory: " + e)
                    | Ok items ->
                        match
                            postStubs session state1.graph args.label items
                        with
                        | Error e -> Error("stubs: " + e)
                        | Ok stubDetail ->
                            match
                                pushMapped session args.mappedRoot scope
                            with
                            | Error e -> Error("workspace-push: " + e)
                            | Ok pushed ->
                                match getFullState session with
                                | Error e ->
                                    Error("state after push: " + e)
                                | Ok state2 ->
                                    match
                                        markBodiesPresent
                                            session
                                            state2.graph
                                            args.label
                                            pushed.uploadedPaths
                                    with
                                    | Error e ->
                                        Error("mark present: " + e)
                                    | Ok markDetail ->
                                        Ok(
                                            created,
                                            stubDetail,
                                            pushed,
                                            markDetail,
                                            state2)

[<EntryPoint>]
let main argv =
    let args = parseArgs argv
    printfn "ambitBase=%s" args.ambitBase
    printfn "mappedRoot=%s" args.mappedRoot
    printfn "label=%s" args.label
    match run args with
    | Error e ->
        eprintfn "FAIL %s" e
        1
    | Ok(created, stubDetail, pushed, markDetail, state) ->
        let names =
            state.graph.nodes
            |> Map.toList
            |> List.choose (fun (_, node) ->
                match node.kind, Filename.tryValue node.name with
                | Special Workspace, Some name -> Some("workspace:" + name)
                | Special File, Some name -> Some("file:" + name)
                | Special Directory, Some name ->
                    Some("directory:" + name)
                | _ -> None)
        printfn "PASS workspace-created eventId=%d" (EventId.value created.eventId)
        printfn "PASS stubs %s" stubDetail
        printfn
            "PASS upload uploaded=%d detail=%s paths=%s"
            pushed.uploaded
            pushed.detail
            (String.concat "," pushed.uploadedPaths)
        printfn "PASS mark %s" markDetail
        printfn "PASS graph-eventId=%d" (EventId.value state.eventId)
        printfn "PASS nodes %s" (String.concat "," names)
        0

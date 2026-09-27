module Gambol.Server.Tests.LoadSaveCommandClientTests

open System
open System.Net
open System.Net.Http
open System.Text
open System.Threading
open System.Threading.Tasks
open Xunit
open Gambol.Client
open Gambol.Shared
open Gambol.Shared.ViewModel
open Thoth.Json.Newtonsoft

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

type private DeskHttpHandler() as this =
    inherit HttpMessageHandler()

    let requests = ResizeArray<string * string>()

    member _.Requests = requests |> Seq.toList

    member private _.Handle(request: HttpRequestMessage) =
        let path = request.RequestUri.AbsolutePath
        let body =
            if isNull request.Content then ""
            else request.Content.ReadAsStringAsync().Result
        requests.Add(string request.Method, path)
        let responseBody =
            match path with
            | "/_desktop/workspace-inventory" ->
                """{"mode":"Full","items":[]}"""
            | "/_desktop/workspace-push" ->
                """{"ok":true,"uploaded":0,"downloaded":0,"detail":"pushed","error":null}"""
            | "/ambit/save" ->
                """{"ok":true,"detail":"saved","error":null}"""
            | _ -> """{"error":"unexpected path"}"""
        let status =
            if path = "/_desktop/workspace-inventory"
               || path = "/_desktop/workspace-push"
               || path = "/ambit/save" then
                HttpStatusCode.OK
            else
                HttpStatusCode.NotFound
        new HttpResponseMessage(
            status,
            Content = new StringContent(responseBody))

    override _.Send(request, _cancellationToken: CancellationToken) =
        this.Handle request

    override _.SendAsync(request, _cancellationToken: CancellationToken) =
        Task.FromResult(this.Handle request)

let private request prePick operation =
    let nodeId = NodeId.New()
    { operation = operation
      prePick = prePick
      start =
        { zoomId = nodeId
          focusId = nodeId
          commandId = nodeId
          graphIds = [ nodeId ]
          eventId = EventId.zero } }

let private deskResponse =
    { path = LoadSavePath.Desk; command = None }
    |> ApiResponseSerialization.encodeLoadSaveCommandResponse
    |> Encode.toString 0

let private runDesk operation =
    let urls = ResizeArray<string>()
    let messages = ResizeArray<Msg>()
    let post url body onOk _ _ =
        urls.Add url
        Decode.fromString EventJson.decodeLoadSaveCommandRequest body
        |> Result.defaultWith failwith
        |> fun decoded -> Assert.Equal(operation, decoded.operation)
        onOk deskResponse
    let dependencies: LoadSaveCommandClient.Dependencies =
        { encodeRequest =
            fun command ->
                EventJson.encodeLoadSaveCommandRequest command
                |> Encode.toString 0
          decodeResponse =
            Decode.fromString
                ApiResponseSerialization.decodeLoadSaveCommandResponseDecoder
          post = post
          continueDesk =
            LoadSaveCommandClient.continueDesk messages.Add
          commandDone = fun _ -> Assert.Fail("unexpected Git completion")
          commandFailed = fun detail -> Assert.Fail(detail) }
    LoadSaveCommandClient.runWith
        dependencies "ambit" (request LoadSavePrePick.Desk operation)
    Assert.Equal<string list>(
        [ "/ambit/load-save-command" ],
        urls |> Seq.toList)
    match messages |> Seq.toList with
    | [ ApplyOp updater ] -> updater
    | other -> failwith $"expected one ApplyOp, got {other}"

let private gitResponse =
    { path = LoadSavePath.Git
      command =
        Some
            { nodes = []
              events = []
              latestId = EventId.zero } }
    |> ApiResponseSerialization.encodeLoadSaveCommandResponse
    |> Encode.toString 0

[<Theory>]
[<InlineData("load")>]
[<InlineData("save")>]
let ``Git Load and Save cross the Server command request door`` operationName =
    let operation =
        if operationName = "load" then
            LoadSaveOperation.Load
        else
            LoadSaveOperation.Save
    let post url body onOk _ _ =
        Assert.Equal("/ambit/load-save-command", url)
        let decoded =
            Decode.fromString EventJson.decodeLoadSaveCommandRequest body
            |> Result.defaultWith failwith
        Assert.Equal(operation, decoded.operation)
        Assert.Equal(LoadSavePrePick.Git, decoded.prePick)
        onOk gitResponse
    let dependencies: LoadSaveCommandClient.Dependencies =
        { encodeRequest =
            EventJson.encodeLoadSaveCommandRequest >> Encode.toString 0
          decodeResponse =
            Decode.fromString
                ApiResponseSerialization.decodeLoadSaveCommandResponseDecoder
          post = post
          continueDesk = fun _ -> Assert.Fail("unexpected Desk continuation")
          commandDone = fun events -> Assert.Empty(events)
          commandFailed = fun detail -> Assert.Fail(detail) }
    LoadSaveCommandClient.runWith
        dependencies "ambit" (request LoadSavePrePick.Git operation)

[<Theory>]
[<InlineData("Load", "load", "plain")>]
[<InlineData("git Load", "load", "git")>]
[<InlineData("desk Load", "load", "desk")>]
[<InlineData("Save", "save", "plain")>]
[<InlineData("git Save", "save", "git")>]
[<InlineData("desk Save", "save", "desk")>]
let ``Command surface keeps Load and Save pre-picks``
    commandName
    operationName
    prePickName
    =
    let operation =
        if operationName = "load" then
            LoadSaveOperation.Load
        else
            LoadSaveOperation.Save
    let prePick =
        match prePickName with
        | "git" -> LoadSavePrePick.Git
        | "desk" -> LoadSavePrePick.Desk
        | _ -> LoadSavePrePick.Plain
    let command =
        Commands.commandRegistry
        |> List.find (fun entry -> entry.name = commandName)
    let updater =
        command.run ()
        |> Option.defaultWith (fun () -> failwith "command unavailable")
    let _, effects = updater (VmTestHelpers.emptyModel (Graph.create ()))
    match effects with
    | [ SubmitLoadSaveCommand actual ] ->
        Assert.Equal(operation, actual.operation)
        Assert.Equal(prePick, actual.prePick)
    | other -> failwith $"expected load/save request, got {other}"

let private applyOps graph ops =
    match
        ChangeValidation.applyOps
            ops
            { graph = graph; eventId = EventId.zero }
    with
    | ApplyResult.Changed state -> state.graph
    | result -> failwith $"expected changed graph, got {result}"

let private mappedWorkspaceModel () =
    let _, ops =
        FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let graph = applyOps (Graph.create ()) ops
    let model =
        VmTestHelpers.emptyModelAt graph Graph.workspacesId
    let parent = model.siteMap.entries.[model.siteMap.rootId]
    { model with
        selectedNodes =
            Some
                { range = { parent = parent; start = 0; endd = 1 }
                  focus = 0 }
        desktopCapabilities =
            Some(DesktopCapabilities.desktopEnabled true)
        workspaceMappedLabels = Set.singleton "home" }

let private saveEnabledModel () =
    { VmTestHelpers.emptyModel (Graph.create ()) with
        serverCapabilities =
            Some { canGitSave = true; canFileStatus = true } }

let private postHttp
    (client: HttpClient)
    (url: string)
    (body: string)
    (onOk: string -> unit)
    (onHttp: int -> string -> unit)
    (onFail: unit -> unit)
    : unit
    =
    try
        use content =
            new StringContent(body, Encoding.UTF8, "application/json")
        use response =
            client.PostAsync(url, content).GetAwaiter().GetResult()
        let text =
            response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if response.IsSuccessStatusCode then
            onOk text
        else
            onHttp (int response.StatusCode) text
    with _ ->
        onFail ()

let private effectDependencies () =
    let handler = new DeskHttpHandler()
    let client = new HttpClient(handler)
    client.BaseAddress <- Uri("http://localhost")
    let dependencies: DeskLoadSaveEffectClient.Dependencies =
        { defer = fun action -> action ()
          postJson = postHttp client
          prepareWorkspacePush = fun _ -> Ok "{}"
          encodeWorkspaceInventory = fun _ -> "{}"
          decodeWorkspaceInventory =
            fun _ -> Ok { mode = "Full"; items = [] }
          postEmpty =
            fun url onOk onHttp onFail ->
                postHttp client url "" onOk onHttp onFail
          decodeGitSave =
            Decode.fromString GitSaveResponse.decoder
          log = ignore
          fileName = "ambit" }
    dependencies, handler, client

let private assertPosted path (handler: DeskHttpHandler) =
    Assert.Contains(("POST", path), handler.Requests)

let private onlyUpdater messages =
    match messages |> Seq.toList with
    | [ ApplyOp updater ] -> updater
    | other -> failwith $"expected one ApplyOp, got {other}"

[<Fact>]
let ``Desk Load response continues to mapped workspace push`` () =
    let updater = runDesk LoadSaveOperation.Load
    let model, effects = updater (mappedWorkspaceModel ())
    match effects with
    | [ ContinueWorkspaceStubsThenPush(scope, None) ] ->
        let dependencies, handler, client = effectDependencies ()
        use _client = client
        let messages = ResizeArray<Msg>()
        DeskLoadSaveEffectClient.runWorkspaceStubsThenPushWith
            dependencies messages.Add scope None
        let completeInventory = onlyUpdater messages
        let _, nextEffects = completeInventory model
        match nextEffects with
        | [ ContinueWorkspacePush(nextScope, None) ] ->
            DeskLoadSaveEffectClient.runWorkspacePushWith
                dependencies ignore nextScope None
        | other -> failwith $"expected push continuation, got {other}"
        assertPosted "/_desktop/workspace-push" handler
    | other -> failwith $"expected workspace push, got {other}"

[<Fact>]
let ``Desk Save response continues to existing save endpoint`` () =
    let updater = runDesk LoadSaveOperation.Save
    let _, effects = updater (saveEnabledModel ())
    match effects with
    | [ ContinueDeskSave ] ->
        let dependencies, handler, client = effectDependencies ()
        use _client = client
        DeskLoadSaveEffectClient.runDeskSaveWith dependencies
        assertPosted "/ambit/save" handler
    | other -> failwith $"expected desk save continuation, got {other}"

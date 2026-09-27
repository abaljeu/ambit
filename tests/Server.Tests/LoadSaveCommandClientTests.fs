module Gambol.Server.Tests.LoadSaveCommandClientTests

open System
open Xunit
open Gambol.Client
open Gambol.Shared
open Gambol.Shared.ViewModel
open Thoth.Json.Newtonsoft

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

let private request operation =
    let nodeId = NodeId.New()
    { operation = operation
      prePick = LoadSavePrePick.Desk
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
    LoadSaveCommandClient.runWith dependencies "ambit" (request operation)
    Assert.Equal<string list>(
        [ "/ambit/load-save-command" ],
        urls |> Seq.toList)
    match messages |> Seq.toList with
    | [ ApplyOp updater ] -> updater
    | other -> failwith $"expected one ApplyOp, got {other}"

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

let private effectDependencies () =
    let urls = ResizeArray<string>()
    let postJson url _ onOk _ _ =
        urls.Add url
        if url = "/_desktop/workspace-inventory" then
            onOk """{"mode":"Full","items":[]}"""
    let postEmpty url _ _ _ =
        urls.Add url
    let dependencies: DeskLoadSaveEffectClient.Dependencies =
        { defer = fun action -> action ()
          postJson = postJson
          prepareWorkspacePush = fun _ -> Ok "{}"
          encodeWorkspaceInventory = fun _ -> "{}"
          decodeWorkspaceInventory =
            fun _ -> Ok { mode = "Full"; items = [] }
          postEmpty = postEmpty
          fileName = "ambit" }
    dependencies, urls

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
        let dependencies, urls = effectDependencies ()
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
        Assert.Contains("/_desktop/workspace-push", urls)
    | other -> failwith $"expected workspace push, got {other}"

[<Fact>]
let ``Desk Save response continues to existing save endpoint`` () =
    let updater = runDesk LoadSaveOperation.Save
    let _, effects = updater (saveEnabledModel ())
    match effects with
    | [ ContinueDeskSave ] ->
        let dependencies, urls = effectDependencies ()
        DeskLoadSaveEffectClient.runDeskSaveWith dependencies
        Assert.Contains("/ambit/save", urls)
    | other -> failwith $"expected desk save continuation, got {other}"

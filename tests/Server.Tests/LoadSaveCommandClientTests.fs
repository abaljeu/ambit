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

[<Fact>]
let ``Desk Load response continues to mapped workspace push`` () =
    let updater = runDesk LoadSaveOperation.Load
    let _, effects = updater (mappedWorkspaceModel ())
    match effects with
    | [ ContinueWorkspaceStubsThenPush(scope, None) ] ->
        Assert.Equal("home", scope.label)
    | other -> failwith $"expected workspace push, got {other}"

[<Fact>]
let ``Desk Save response continues to existing save endpoint`` () =
    let updater = runDesk LoadSaveOperation.Save
    let _, effects = updater (saveEnabledModel ())
    Assert.Equal<Effect list>([ ContinueDeskSave ], effects)
    Assert.Equal(
        "/ambit/save",
        UpdateSave.deskSaveUrl "ambit")

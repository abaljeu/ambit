module Gambol.Client.DeskLoadSaveEffectClient

open Browser.Dom
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Client.UpdateSave
open Gambol.Client.UpdateWorkspaceSync

type Dependencies =
    { defer: (unit -> unit) -> unit
      postJson:
        string ->
        string ->
        (string -> unit) ->
        (int -> string -> unit) ->
        (unit -> unit) ->
        unit
      prepareWorkspacePush:
        WorkspaceSyncScope -> Result<string, string>
      encodeWorkspaceInventory: WorkspaceSyncScope -> string
      decodeWorkspaceInventory:
        string -> Result<DesktopUploadInventory, string>
      postEmpty:
        string ->
        (string -> unit) ->
        (int -> string -> unit) ->
        (unit -> unit) ->
        unit
      fileName: string }

let runWorkspaceStubsThenPushWith
    dependencies
    dispatch
    scope
    parseFileId
    =
    dependencies.defer (fun () ->
        let body = dependencies.encodeWorkspaceInventory scope
        dependencies.postJson
            "/_desktop/workspace-inventory"
            body
            (fun text ->
                dispatch (
                    ApplyOp (
                        completeUploadInventoryWith
                            dependencies.decodeWorkspaceInventory
                            scope
                            parseFileId
                            text)))
            (fun status text ->
                dispatch (
                    ApplyOp (
                        failWorkspacePushHttp status text)))
            (fun () ->
                dispatch (
                    ApplyOp (
                        failWorkspacePush
                            "workspace-inventory request failed"))))

let runWorkspacePushWith
    dependencies
    dispatch
    scope
    parseFileId
    =
    dependencies.defer (fun () ->
        match dependencies.prepareWorkspacePush scope with
        | Error "cancelled" ->
            dispatch (ApplyOp cancelWorkspacePush)
        | Error error ->
            dispatch (ApplyOp (failWorkspacePush error))
        | Ok body ->
            dependencies.postJson
                "/_desktop/workspace-push"
                body
                (fun text ->
                    dispatch (
                        ApplyOp (
                            completeWorkspacePush
                                scope
                                parseFileId
                                text)))
                (fun status text ->
                    dispatch (
                        ApplyOp (
                            failWorkspacePushHttp status text)))
                (fun () ->
                    dispatch (
                        ApplyOp (
                            failWorkspacePush
                                "workspace-push request failed"))))

let runDeskSaveWith dependencies =
    UpdateSave.runDeskSaveWith
        dependencies.postEmpty
        dependencies.fileName

let private productionDependencies =
    { defer =
        fun action -> setTimeout action 50 |> ignore
      postJson =
        fun url body onOk onHttp onFail ->
            postJson
                url body onOk onHttp onFail
                (jsonMutatingPostHeaders ())
      prepareWorkspacePush = tryPrepareWorkspacePushBody
      encodeWorkspaceInventory = encodeWorkspaceInventoryBody
      decodeWorkspaceInventory = decodeDesktopUploadInventory
      postEmpty =
        fun url onOk onHttp onFail ->
            postEmpty
                url onOk onHttp onFail
                (emptyMutatingPostHeaders ())
      fileName = currentFile }

let runWorkspaceStubsThenPush dispatch scope parseFileId =
    runWorkspaceStubsThenPushWith
        productionDependencies dispatch scope parseFileId

let runWorkspacePush dispatch scope parseFileId =
    runWorkspacePushWith
        productionDependencies dispatch scope parseFileId

let runDeskSave () =
    runDeskSaveWith productionDependencies

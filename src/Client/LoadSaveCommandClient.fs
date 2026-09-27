module Gambol.Client.LoadSaveCommandClient

open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Client.UpdateSave
open Gambol.Client.UpdateWorkspaceLoad

type Dependencies =
    { encodeRequest: LoadSaveCommandRequest -> string
      decodeResponse:
        string -> Result<LoadSaveCommandResponse, string>
      post:
        string ->
        string ->
        (string -> unit) ->
        (int -> string -> unit) ->
        (unit -> unit) ->
        unit
      continueDesk: LoadSaveOperation -> unit
      commandDone: Ev list -> unit
      commandFailed: string -> unit }

let private applyResponse
    (dependencies: Dependencies)
    (operation: LoadSaveOperation)
    (response: LoadSaveCommandResponse)
    =
    match response.path, response.command with
    | LoadSavePath.Desk, _ ->
        dependencies.continueDesk operation
    | LoadSavePath.Git, Some command ->
        dependencies.commandDone command.events
    | LoadSavePath.Git, None ->
        dependencies.commandFailed
            "git Load/Save response omitted command events"

let runWith
    (dependencies: Dependencies)
    (fileName: string)
    (request: LoadSaveCommandRequest)
    : unit =
    let body = dependencies.encodeRequest request
    dependencies.post
        ("/" + fileName + "/load-save-command")
        body
        (fun text ->
            match dependencies.decodeResponse text with
            | Ok response ->
                applyResponse dependencies request.operation response
            | Error err ->
                dependencies.commandFailed err)
        (fun status text ->
            let detail =
                "HTTP "
                + string status
                + " "
                + LogText.summarizeHttpBody 400 text
            dependencies.commandFailed detail)
        (fun () -> dependencies.commandFailed "fetch failed")

let continueDesk (dispatch: Msg -> unit) =
    function
    | LoadSaveOperation.Load ->
        dispatch (ApplyOp deskLoadOp)
    | LoadSaveOperation.Save ->
        dispatch (ApplyOp deskSaveOp)

let private productionDependencies (dispatch: Msg -> unit) =
    { encodeRequest = encodeLoadSaveCommandRequest
      decodeResponse = decodeLoadSaveCommandResponse
      post =
        fun url body onOk onHttp onFail ->
            postJson
                url body onOk onHttp onFail
                (jsonMutatingPostHeaders ())
      continueDesk = continueDesk dispatch
      commandDone =
        fun events -> dispatch (SysMsg (CommandDone events))
      commandFailed =
        fun detail -> dispatch (SysMsg (CommandFailed detail)) }

let run dispatch request =
    runWith (productionDependencies dispatch) currentFile request

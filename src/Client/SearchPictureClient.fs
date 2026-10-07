module Gambol.Client.SearchPictureClient

open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.JsInterop
open Gambol.Client.SearchDialog
open Gambol.Client.UpdateHelpers

let private onBody (dispatch: Msg -> unit) (body: string) : unit =
    match
        Thoth.Json.JavaScript.Decode.fromString
            SearchPicture.decodeReply
            body
    with
    | Ok reply -> dispatch (ApplyOp (applyServerReply reply))
    | Error err -> consoleLog ("[Gambol search] reply " + err)

let private onHttp (status: int) (body: string) : unit =
    consoleLog (
        "[Gambol search] HTTP "
        + string status
        + " "
        + LogText.summarizeHttpBody 200 body)

let runSearch
    (dispatch: Msg -> unit)
    (text: string)
    (zoomRoot: NodeId)
    : unit =
    let request: SearchPicture.Request =
        { text = text
          startId = zoomRoot
          generation = None }
    let body =
        Thoth.Json.JavaScript.Encode.toString
            0
            (SearchPicture.encodeRequest request)
    postJson
        ("/" + currentFile + "/search")
        body
        (onBody dispatch)
        onHttp
        (fun () -> consoleLog "[Gambol search] fetch failed")
        (jsonMutatingPostHeaders ())

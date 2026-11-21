module Gambol.Client.SearchPictureClient

open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.JsInterop
open Gambol.Client.SearchDialog
open Gambol.Client.UpdateHelpers

let private wantNodes (body: string) : Node list =
    match
        Thoth.Json.JavaScript.Decode.fromString
            SearchPicture.decodeWantNodes
            body
    with
    | Ok nodes -> nodes
    | Error _ -> []

let private onBody
    (count: int)
    (dispatch: Msg -> unit)
    (body: string)
    : unit =
    match
        Thoth.Json.JavaScript.Decode.fromString
            SearchPicture.decodeReply
            body
    with
    | Ok reply ->
        let carried = wantNodes body
        dispatch (ApplyOp (fun model ->
            applyServerReply count reply carried model))
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
          generation = None
          count = searchPictureCount }
    let body =
        Thoth.Json.JavaScript.Encode.toString
            0
            (SearchPicture.encodeRequest request)
    postJson
        ("/" + currentFile + "/search")
        body
        (onBody request.count dispatch)
        onHttp
        (fun () -> consoleLog "[Gambol search] fetch failed")
        (jsonMutatingPostHeaders ())

module Gambol.Client.SubmitChangeCallbacks

open Browser.Dom
open Gambol.Shared
open Gambol.Shared.LogText
open Gambol.Shared.ViewModel
open Gambol.Client.JsInterop
open Gambol.Client.Update
open Gambol.Client.UpdateCodec

let onPostOk
    (timeoutId: float)
    (reqId: string)
    (submitted: Ev list)
    (dispatch: Msg -> unit)
    (text: string)
    : unit =
    clearTimeout timeoutId
    let n = text.Length
    match decodeChangeSuccessResponse text with
    | Ok ack ->
        consoleLog (
            "[Gambol sync] POST 200 req=" + reqId
            + " ackRev=" + string ack.eventId.Value
            + " bodyLen=" + string n)
        dispatch (SysMsg (SubmitResponse (submitted, ack)))
    | Error err ->
        consoleLog (
            "[Gambol sync] POST 200 bad ACK JSON req=" + reqId
            + " err=" + err + " bodyLen=" + string n)
        dispatch (SysMsg (SubmitRejected ("ACK decode: " + err)))

let onPostHttp
    (timeoutId: float)
    (reqId: string)
    (dispatch: Msg -> unit)
    (httpStatus: int)
    (bodyText: string)
    : unit =
    clearTimeout timeoutId
    let snippet = summarizeHttpBody 400 bodyText
    consoleLog (
        "[Gambol sync] GAMBOL_HTTP_ERR POST fail req=" + reqId
        + " http=" + string httpStatus + " body=" + snippet)
    let detail =
        decodePostEventError bodyText
        |> Option.map (summarizeHttpBody 400)
        |> Option.defaultValue (summarizeHttpBody 400 bodyText)
    dispatch (SysMsg (SubmitRejected detail))

let onPostFetchFail
    (timeoutId: float)
    (reqId: string)
    (baseEventId: EventId)
    (events: Ev list)
    (dispatch: Msg -> unit)
    ()
    : unit =
    clearTimeout timeoutId
    consoleLog ("[Gambol sync] POST fetch failed req=" + reqId)
    dispatch (
        SysMsg (
            SubmitNetworkError (
                baseEventId,
                events,
                SubmitNetworkErrorKind.FetchFailed)))

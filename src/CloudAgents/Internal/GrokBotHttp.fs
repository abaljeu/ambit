namespace Gambol.CloudAgents.Internal

open System
open System.Net.Http
open System.Text
open FSharp.Data
open Gambol.CloudAgents

module GrokBotHttp =

    let wakeRequestJson
        (sentAt: string)
        (args: GrokBotWakeArgs)
        : JsonValue =
        JsonValue.Record [|
            "source", JsonValue.String "ambit"
            "kind", JsonValue.String "message"
            "sentAt", JsonValue.String sentAt
            "commandId", JsonValue.String args.CommandId
            "focusId", JsonValue.String args.FocusId
            "sessionId", JsonValue.String args.SessionId
            "text", JsonValue.String args.Text
            "payload", JsonValue.Record [||]
        |]

    /// Outbound wake auth: `Authorization: Bearer <WakeSecret>`.
    /// Empty secret adds no header (caller must fail closed).
    let applyWakeAuth
        (request: HttpRequestMessage)
        (secret: string)
        : HttpRequestMessage =
        if String.IsNullOrWhiteSpace secret then
            request
        else
            request.Headers.Authorization <-
                Headers.AuthenticationHeaderValue("Bearer", secret)
            request

    /// Ack-only: success ignores body (never bot reply text).
    let interpretWakeResponse (statusCode: int) (body: string) =
        if statusCode = 401 then
            Error "unauthorized"
        elif statusCode >= 200 && statusCode <= 299 then
            Ok()
        else
            Error $"HTTP {statusCode}: {body}"

    let private readBody (response: HttpResponseMessage) =
        response.Content.ReadAsStringAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let postWake
        (url: string)
        (secret: string)
        (json: JsonValue)
        : Result<unit, string> =
        if String.IsNullOrWhiteSpace secret then
            Error "unauthorized"
        else
            try
                use client = new HttpClient()
                use request =
                    new HttpRequestMessage(
                        System.Net.Http.HttpMethod.Post, url)
                request.Content <-
                    new StringContent(
                        json.ToString(),
                        Encoding.UTF8,
                        "application/json")
                applyWakeAuth request secret |> ignore
                let response =
                    client.SendAsync request
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                let body = readBody response
                interpretWakeResponse
                    (int response.StatusCode) body
            with ex ->
                Error $"Request failed: {ex.Message}"

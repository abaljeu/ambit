namespace Gambol.Shared

open System
open System.Net.Http
open System.Text
open Gambol.Server
open Gambol.Shared.CommandEntry

module Enc = Thoth.Json.Newtonsoft.Encode
module Dec = Thoth.Json.Newtonsoft.Decode

/// Cookie + ClientIdentity session against one Ambit base URL.
type AmbitSession =
    { client: HttpClient
      cookie: string
      ambitBase: string
      clientHint: string option }

/// Login cookie, GET /state, and POST /changes — Desktop non-UI and stretch.
[<RequireQualifiedAccess>]
module AmbitSession =

    let createHttpClient () =
        let handler =
            new HttpClientHandler(
                AllowAutoRedirect = false,
                UseCookies = false)
        new HttpClient(handler, disposeHandler = true)

    let cookieFromCredentials (creds: LoginForm.Credentials) =
        AuthToken.cookieHeaderValue creds.Username creds.Password

    let cookieHeader (creds: LoginForm.Credentials option) =
        creds |> Option.map cookieFromCredentials

    let cookieFromSetCookie (headers: string seq) =
        AuthToken.applySetCookieHeaders None headers
        |> Option.map (fun value ->
            AuthToken.cookieName + "=" + value)

    let private cookieFromResponse (resp: HttpResponseMessage) =
        match resp.Headers.TryGetValues("Set-Cookie") with
        | false, _ -> None
        | true, values -> cookieFromSetCookie values

    let private addSessionHeaders
        (session: AmbitSession)
        (req: HttpRequestMessage)
        =
        req.Headers.TryAddWithoutValidation("Cookie", session.cookie)
        |> ignore
        match session.clientHint with
        | None -> ()
        | Some hint when hint = "" -> ()
        | Some hint ->
            req.Headers.TryAddWithoutValidation(
                ClientIdentity.HeaderName,
                ClientIdentity.normalize hint)
            |> ignore

    let private send
        (session: AmbitSession)
        (method: HttpMethod)
        (url: string)
        (body: string option)
        =
        try
            use req = new HttpRequestMessage(method, url)
            addSessionHeaders session req
            match body with
            | None -> ()
            | Some text ->
                req.Content <-
                    new StringContent(text, Encoding.UTF8, "application/json")
            let resp = session.client.Send req
            let text = resp.Content.ReadAsStringAsync().Result
            Ok(int resp.StatusCode, text, cookieFromResponse resp)
        with ex ->
            Error ex.Message

    let fromCookie
        (client: HttpClient)
        (ambitBase: string)
        (cookie: string)
        (clientHint: string option)
        =
        { client = client
          cookie = cookie
          ambitBase = ambitBase.TrimEnd('/')
          clientHint = clientHint }

    let fromCredentials
        (client: HttpClient)
        (ambitBase: string)
        (creds: LoginForm.Credentials)
        (clientHint: string option)
        =
        fromCookie
            client
            ambitBase
            (cookieFromCredentials creds)
            clientHint

    let loginByGet
        (client: HttpClient)
        (ambitBase: string)
        (clientHint: string option)
        =
        let appUrl = ambitBase.TrimEnd('/')
        try
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
                Ok(fromCookie client ambitBase cookie clientHint)
        with ex ->
            Error ex.Message

    let private encodeBatch (events: Ev list) =
        Enc.toString 0 (EventJson.encodeEventBatch { events = events })

    let getFullState (session: AmbitSession) =
        let url = session.ambitBase + "/state?scope=full"
        match send session HttpMethod.Get url None with
        | Error e -> Error e
        | Ok(code, text, _) when code < 200 || code >= 300 ->
            Error("GET /state HTTP " + string code + ": " + text)
        | Ok(_, text, _) ->
            Dec.fromString
                ApiResponseSerialization.decodeStateResponseDecoder
                text

    let postOps (session: AmbitSession) (ops: Op list) =
        let event = ClientHistory.mintChange (displayName Load) ops
        let body = encodeBatch [ event ]
        let url = session.ambitBase + "/changes"
        match send session HttpMethod.Post url (Some body) with
        | Error e -> Error e
        | Ok(code, text, _) when code < 200 || code >= 300 ->
            Error("POST /changes HTTP " + string code + ": " + text)
        | Ok(_, text, _) ->
            match
                Dec.fromString
                    ApiResponseSerialization.decodeChangeSuccessResponseDecoder
                    text
            with
            | Error e -> Error e
            | Ok ack -> Ok(event, ack)

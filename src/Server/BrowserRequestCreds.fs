namespace Gambol.Server

open System
open Microsoft.AspNetCore.Http
open Gambol.Shared

/// Request-carried Browser secret from `gambol_auth`. No closed-over fallback.
[<RequireQualifiedAccess>]
module BrowserRequestCreds =

    let trySecretFromCookieValue (cookie: string option) : Credential option =
        match cookie with
        | Some value when not (String.IsNullOrWhiteSpace value) ->
            Some(Credential value)
        | _ -> None

    let tryCookieSecret (req: HttpRequest) : Credential option =
        match req.Cookies.TryGetValue(AuthToken.cookieName) with
        | true, cookie -> trySecretFromCookieValue (Some cookie)
        | _ -> trySecretFromCookieValue None

    /// Request-carried Browser Caller. Empty name is the cookie session key.
    let callerFromSecret (secret: Credential) : Caller =
        { authority = Authority "Browser"
          name = "Browser"
          secret = secret }

    let tryCookieCaller (req: HttpRequest) : Caller option =
        tryCookieSecret req |> Option.map callerFromSecret

namespace Gambol.Server

open System
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Gambol.Shared

[<RequireQualifiedAccess>]
module RouteAuthentication =

    let private gitAuthenticated gitAuthOpen expectedUser gitToken (req: HttpRequest) =
        if gitAuthOpen then true
        else
            match req.Headers.TryGetValue("Authorization") with
            | true, values ->
                match AuthToken.tryParseBasicAuth (string values.[0]) with
                | Some(user, pass) ->
                    user = expectedUser && pass = gitToken
                | None -> false
            | _ -> false

    let private setAuthCookie validToken (resp: HttpResponse) =
        let opts =
            CookieOptions(
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = Nullable(DateTimeOffset.UtcNow.AddYears(10)))
        resp.Cookies.Append(AuthToken.cookieName, validToken, opts)

    let private clearAuthCookie (resp: HttpResponse) =
        let opts =
            CookieOptions(
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax)
        resp.Cookies.Delete(AuthToken.cookieName, opts)

    /// Factory admission is false. Present cookie is mailbox admit only, after Core exists.
    let create (config: IConfiguration) : Authentication =
        let expectedUser =
            config.["Auth:Username"] |> Option.ofObj |> Option.defaultValue ""
        let expectedPass =
            config.["Auth:Password"] |> Option.ofObj |> Option.defaultValue ""
        let validToken = AuthToken.deriveToken expectedUser expectedPass
        let gitToken = AuthToken.deriveGitToken expectedUser expectedPass
        let gitAuthOpen = expectedUser = "" && expectedPass = ""
        {
            ExpectedUser = expectedUser
            ExpectedPass = expectedPass
            GitToken = gitToken
            IsAuthenticated = fun _ -> false
            IsGitAuthenticated = gitAuthenticated gitAuthOpen expectedUser gitToken
            SetCookie = setAuthCookie validToken
            ClearCookie = clearAuthCookie
        }

    /// Login first, then SetCookie. Do not issue a secret the mailbox set lacks.
    let loginThenSetCookie
        (host: MailboxHost)
        (auth: Authentication)
        (response: HttpResponse)
        : Async<Result<unit, string>> =
        let secret =
            Credential(
                AuthToken.deriveToken auth.ExpectedUser auth.ExpectedPass)
        async {
            let! result = CoreMailbox.login host "" secret
            match result with
            | Ok () ->
                auth.SetCookie response
                return Ok ()
            | Error err -> return Error err
        }

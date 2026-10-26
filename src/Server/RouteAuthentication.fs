namespace Gambol.Server

open System
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Gambol.Shared

[<RequireQualifiedAccess>]
module RouteAuthentication =

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
        {
            ExpectedUser = expectedUser
            ExpectedPass = expectedPass
            IsAuthenticated = fun _ -> false
            SetCookie = setAuthCookie validToken
            ClearCookie = clearAuthCookie
        }

    /// Session login, then SetCookie.
    let loginThenSetCookie
        (host: MailboxHost)
        (auth: Authentication)
        (response: HttpResponse)
        : Async<Result<unit, string>> =
        let secret =
            Credential(
                AuthToken.deriveToken auth.ExpectedUser auth.ExpectedPass)
        async {
            let! result = CoreMailboxApi.login host "" secret
            match result with
            | Ok () ->
                auth.SetCookie response
                return Ok ()
            | Error err -> return Error err
        }

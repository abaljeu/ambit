module Gambol.Server.Tests.BrowserCredentialTests

open System.Net
open System.Net.Http
open System.Text
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Xunit
open Gambol.Server
open Gambol.Server.Tests.TestBackend

let private authFromMemory (pairs: (string * string) list) =
    let config =
        ConfigurationBuilder()
            .AddInMemoryCollection(dict pairs)
            .Build()
    RouteAuthentication.create config

let private requestWithCookie (cookie: string) =
    let ctx = DefaultHttpContext()
    ctx.Request.Headers.Cookie <- cookie
    ctx.Request

let private jsonContent body =
    new StringContent(body, Encoding.UTF8, "application/json")

let private addLiveCookie (client: HttpClient) =
    client.DefaultRequestHeaders.Add(
        "Cookie",
        AuthToken.cookieHeaderValue "alice" "secret")

[<Fact>]
let ``Browser message without a live cookie is the same auth refuse as inactive Actor``
    () =
    task {
        let dataDir = newTempDir ()
        use client = createClientForDirWithAuth dataDir "alice" "secret"
        let! state = client.GetAsync("/ambit/state")
        let! poll = client.GetAsync("/ambit/poll")
        use changesBody = jsonContent "{}"
        let! changes = client.PostAsync("/ambit/events", changesBody)
        use loadBody = jsonContent "{}"
        let! load = client.PostAsync("/ambit/load", loadBody)
        Assert.Equal(HttpStatusCode.Unauthorized, state.StatusCode)
        Assert.Equal(HttpStatusCode.Unauthorized, poll.StatusCode)
        Assert.Equal(HttpStatusCode.Unauthorized, changes.StatusCode)
        Assert.Equal(HttpStatusCode.Unauthorized, load.StatusCode)
        Assert.Equal("Unauthorized", CoreAuth.refuse)
    }

[<Fact>]
let ``Browser message with live cookie is not auth-refused`` () = task {
    let dataDir = newTempDir ()
    use client = createClientForDirWithAuth dataDir "alice" "secret"
    addLiveCookie client
    let! state = client.GetAsync("/ambit/state")
    let! poll = client.GetAsync("/ambit/poll")
    Assert.Equal(HttpStatusCode.OK, state.StatusCode)
    Assert.Equal(HttpStatusCode.OK, poll.StatusCode)
    use changesBody = jsonContent "{}"
    let! changes = client.PostAsync("/ambit/events", changesBody)
    use loadBody = jsonContent "{}"
    let! load = client.PostAsync("/ambit/load", loadBody)
    Assert.NotEqual(HttpStatusCode.Unauthorized, changes.StatusCode)
    Assert.NotEqual(HttpStatusCode.Unauthorized, load.StatusCode)
}

[<Fact>]
let ``present cookie is admitted only when mailbox contains the Caller`` () =
    task {
        let dataDir = newTempDir ()
        use client = createClientForDirWithAuth dataDir "alice" "secret"
        client.DefaultRequestHeaders.Add("Cookie", "gambol_auth=not-seeded")
        let! state = client.GetAsync("/ambit/state")
        Assert.Equal(HttpStatusCode.Unauthorized, state.StatusCode)
    }

[<Fact>]
let ``Empty Auth Browser APIs without cookie are refused`` () = task {
    let dataDir = newTempDir ()
    use client = createClientForDirWithoutCookie dataDir
    let! state = client.GetAsync("/ambit/state")
    let! poll = client.GetAsync("/ambit/poll")
    use changesBody = jsonContent "{}"
    let! changes = client.PostAsync("/ambit/events", changesBody)
    use loadBody = jsonContent """{"revision":0,"targets":[]}"""
    let! load = client.PostAsync("/ambit/load", loadBody)
    let! capabilities = client.GetAsync("/ambit/capabilities")
    Assert.Equal(HttpStatusCode.Unauthorized, state.StatusCode)
    Assert.Equal(HttpStatusCode.Unauthorized, poll.StatusCode)
    Assert.Equal(HttpStatusCode.Unauthorized, changes.StatusCode)
    Assert.Equal(HttpStatusCode.Unauthorized, load.StatusCode)
    Assert.Equal(HttpStatusCode.Unauthorized, capabilities.StatusCode)
}

[<Fact>]
let ``Empty Auth GET /ambit auto-issues development cookie`` () =
    task {
        let dataDir = newTempDir ()
        use client = createClientForDirWithoutCookie dataDir
        let! page = client.GetAsync("/ambit")
        Assert.Equal(HttpStatusCode.OK, page.StatusCode)
        let setCookies =
            match page.Headers.TryGetValues("Set-Cookie") with
            | true, values -> values |> Seq.toList
            | _ ->
                match page.Content.Headers.TryGetValues("Set-Cookie") with
                | true, values -> values |> Seq.toList
                | _ -> []
        let issued =
            AuthToken.applySetCookieHeaders None setCookies
        Assert.Equal(Some(AuthToken.deriveToken "" ""), issued)
    }

[<Fact>]
let ``Auth-enabled GET /ambit without cookie redirects to login`` () = task {
    let dataDir = newTempDir ()
    use client = createClientForDirWithAuth dataDir "alice" "secret"
    let! page = client.GetAsync("/ambit")
    let! body = page.Content.ReadAsStringAsync()
    let finalUri =
        match page.RequestMessage with
        | null -> ""
        | msg -> string msg.RequestUri
    Assert.Contains("/ambit/login", finalUri)
    Assert.Contains("Log in", body)
    let setCookies =
        match page.Headers.TryGetValues("Set-Cookie") with
        | true, values -> values |> Seq.toList
        | _ -> []
    Assert.Equal(None, AuthToken.applySetCookieHeaders None setCookies)
}

[<Fact>]
let ``Empty Auth Browser APIs with request cookie are not refused`` () =
    task {
        let dataDir = newTempDir ()
        use client = createClientForDir dataDir
        let! state = client.GetAsync("/ambit/state")
        let! poll = client.GetAsync("/ambit/poll")
        Assert.Equal(HttpStatusCode.OK, state.StatusCode)
        Assert.Equal(HttpStatusCode.OK, poll.StatusCode)
        use changesBody = jsonContent "{}"
        let! changes = client.PostAsync("/ambit/events", changesBody)
        use loadBody = jsonContent """{"revision":0,"targets":[]}"""
        let! load = client.PostAsync("/ambit/load", loadBody)
        Assert.NotEqual(HttpStatusCode.Unauthorized, changes.StatusCode)
        Assert.NotEqual(HttpStatusCode.Unauthorized, load.StatusCode)
    }

[<Fact>]
let ``createAuthentication IsAuthenticated is false for a present unknown cookie`` () =
    let auth =
        authFromMemory [ "Auth:Username", "alice"; "Auth:Password", "secret" ]
    let req = requestWithCookie "gambol_auth=not-in-any-mailbox"
    Assert.False(auth.IsAuthenticated req)

[<Fact>]
let ``createAuthentication IsAuthenticated is false when a cookie is missing`` () =
    let auth =
        authFromMemory [ "Auth:Username", "alice"; "Auth:Password", "secret" ]
    let req = DefaultHttpContext().Request
    Assert.False(auth.IsAuthenticated req)

[<Fact>]
let ``after logout the development cookie is not admitted without login`` () =
    task {
        let dataDir = newTempDir ()
        use client = createClientForDir dataDir
        let! logout = client.GetAsync("/ambit/logout")
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode)
        let! state = client.GetAsync("/ambit/state")
        Assert.Equal(HttpStatusCode.Unauthorized, state.StatusCode)
    }

[<Fact>]
let ``after logout GET /ambit login auto-issue admits the development cookie`` () =
    task {
        let dataDir = newTempDir ()
        use client = createClientForDir dataDir
        let! logout = client.GetAsync("/ambit/logout")
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode)
        let! page = client.GetAsync("/ambit")
        Assert.Equal(HttpStatusCode.OK, page.StatusCode)
        let! state = client.GetAsync("/ambit/state")
        Assert.Equal(HttpStatusCode.OK, state.StatusCode)
    }

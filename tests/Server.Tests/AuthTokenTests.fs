module AuthTokenTests

open System
open Gambol.Server
open Xunit

[<Fact>]
let ``deriveToken is stable for the same credentials`` () =
    let a = AuthToken.deriveToken "alice" "secret"
    let b = AuthToken.deriveToken "alice" "secret"
    Assert.Equal(a, b)

[<Fact>]
let ``deriveToken changes when password changes`` () =
    let a = AuthToken.deriveToken "alice" "one"
    let b = AuthToken.deriveToken "alice" "two"
    Assert.NotEqual<string>(a, b)

[<Fact>]
let ``cookieHeaderValue includes cookie name`` () =
    let header = AuthToken.cookieHeaderValue "alice" "secret"
    Assert.StartsWith("gambol_auth=", header)

[<Fact>]
let ``deriveGitToken is stable and distinct from cookie token`` () =
    let cookie = AuthToken.deriveToken "alice" "secret"
    let git = AuthToken.deriveGitToken "alice" "secret"
    let git2 = AuthToken.deriveGitToken "alice" "secret"
    Assert.Equal(git, git2)
    Assert.NotEqual<string>(cookie, git)

[<Fact>]
let ``basicAuthHeaderValue round-trips via tryParseBasicAuth`` () =
    let token = AuthToken.deriveGitToken "alice" "secret"
    let header = AuthToken.basicAuthHeaderValue "alice" token
    match AuthToken.tryParseBasicAuth header with
    | None -> Assert.Fail("expected parse")
    | Some(user, pass) ->
        Assert.Equal("alice", user)
        Assert.Equal(token, pass)

[<Fact>]
let ``tryParseBasicAuth rejects non-Basic`` () =
    Assert.Equal(None, AuthToken.tryParseBasicAuth "Bearer abc")
    Assert.Equal(None, AuthToken.tryParseBasicAuth null)

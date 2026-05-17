module AuthTokenTests

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

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
let ``proxyCookieHeader uses development token when nothing stored`` () =
    let header =
        AuthToken.proxyCookieHeader
            { storedUsername = None
              storedPassword = None
              serverIssuedValue = None }
    Assert.Equal(AuthToken.cookieHeaderValue "" "", header)

[<Fact>]
let ``proxyCookieHeader prefers server-issued over stored credentials`` () =
    let issued = AuthToken.deriveToken "" ""
    let header =
        AuthToken.proxyCookieHeader
            { storedUsername = Some "alice"
              storedPassword = Some "secret"
              serverIssuedValue = Some issued }
    Assert.Equal("gambol_auth=" + issued, header)

[<Fact>]
let ``proxyCookieHeader stored matches boot-seed deriveToken after restart`` () =
    // Server restart re-seeds CoreCredentials with deriveToken(user,pass).
    // Desktop AuthStore rebuilds the same cookie — DeployEpochSec is not required.
    let bootSeed = AuthToken.deriveToken "alice" "secret"
    let header =
        AuthToken.proxyCookieHeader
            { storedUsername = Some "alice"
              storedPassword = Some "secret"
              serverIssuedValue = None }
    Assert.Equal("gambol_auth=" + bootSeed, header)

[<Fact>]
let ``applySetCookieHeaders captures gambol_auth and clears on empty`` () =
    let token = AuthToken.deriveToken "" ""
    let captured =
        AuthToken.applySetCookieHeaders
            None
            [ "gambol_auth=" + token + "; path=/; secure; httponly" ]
    Assert.Equal(Some token, captured)
    let cleared =
        AuthToken.applySetCookieHeaders
            captured
            [ "gambol_auth=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT" ]
    Assert.Equal(None, cleared)

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

let private uploadClaim expires : UploadCapability.Claim =
    { user = "alice"
      label = "home"
      relative = "employment/research/targets/priorities.md"
      size = 42L
      sha256 = String.replicate 64 "a"
      sourceMtimeTicks = 638900000000000000L
      expiresUnix = expires
      nonce = Guid.NewGuid() }

[<Fact>]
let ``upload capability binds user path size digest and expiry`` () =
    let now = DateTimeOffset(2026, 7, 24, 2, 0, 0, TimeSpan.Zero)
    let claim = uploadClaim (now.AddMinutes(2).ToUnixTimeSeconds())
    let token = UploadCapability.issue "secret" claim
    Assert.Equal(
        Ok claim,
        UploadCapability.validate "secret" "alice" now token)

[<Fact>]
let ``upload capability rejects tampering wrong user and expiry`` () =
    let now = DateTimeOffset(2026, 7, 24, 2, 0, 0, TimeSpan.Zero)
    let claim = uploadClaim (now.AddSeconds(30).ToUnixTimeSeconds())
    let token = UploadCapability.issue "secret" claim
    let replacement = if token.EndsWith("A") then "B" else "A"
    let tampered = token.Substring(0, token.Length - 1) + replacement
    Assert.Equal(
        Error "invalid_upload_capability",
        UploadCapability.validate "secret" "alice" now tampered)
    Assert.Equal(
        Error "invalid_upload_capability",
        UploadCapability.validate "secret" "bob" now token)
    Assert.Equal(
        Error "expired_upload_capability",
        UploadCapability.validate "secret" "alice" (now.AddMinutes 1) token)

[<Fact>]
let ``upload capability replay validates the same bound claim`` () =
    let now = DateTimeOffset(2026, 7, 24, 2, 0, 0, TimeSpan.Zero)
    let claim = uploadClaim (now.AddMinutes(2).ToUnixTimeSeconds())
    let token = UploadCapability.issue "secret" claim
    let first = UploadCapability.validate "secret" "alice" now token
    let retry = UploadCapability.validate "secret" "alice" now token
    Assert.Equal(first, retry)

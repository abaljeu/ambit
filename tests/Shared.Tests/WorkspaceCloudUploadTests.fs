module WorkspaceCloudUploadTests

open Gambol.Shared
open Xunit

[<Fact>]
let ``workspaceScope is the named workspace root`` () =
    let scope = WorkspaceCloudUpload.workspaceScope "home"
    Assert.Equal("home", scope.label)
    Assert.Equal("", scope.relative)
    Assert.Equal(SyncScopeKind.Workspace, scope.kind)

[<Fact>]
let ``stubItemsFromLocal copies relative path and directory flag`` () =
    let items =
        WorkspaceCloudUpload.stubItemsFromLocal
            [ { relative = "a/b.md"; isDirectory = false }
              { relative = "a"; isDirectory = true } ]
    Assert.Equal(2, items.Length)
    Assert.Equal("a/b.md", items.[0].relative)
    Assert.False(items.[0].isDirectory)
    Assert.Equal("a", items.[1].relative)
    Assert.True(items.[1].isDirectory)

[<Fact>]
let ``cookieFromSetCookie reads gambol_auth value`` () =
    let cookie =
        AmbitSession.cookieFromSetCookie
            [ "gambol_auth=abc123; Path=/; Secure" ]
    Assert.Equal(Some "gambol_auth=abc123", cookie)

[<Fact>]
let ``cookieHeader is None when credentials are missing`` () =
    Assert.Equal(None, AmbitSession.cookieHeader None)

[<Fact>]
let ``Desktop host cookie is development token when creds are absent`` () =
    let header = AmbitSession.requestCookieHeader None None
    Assert.Equal(AuthToken.cookieHeaderValue "" "", header)

[<Fact>]
let ``Desktop host cookie uses stored creds when no server-issued cookie`` () =
    let creds =
        { LoginForm.Username = "alice"
          LoginForm.Password = "secret" }
    let header = AmbitSession.requestCookieHeader (Some creds) None
    Assert.Equal(AuthToken.cookieHeaderValue "alice" "secret", header)

[<Fact>]
let ``Desktop host cookie prefers server-issued over stored creds`` () =
    let creds =
        { LoginForm.Username = "alice"
          LoginForm.Password = "secret" }
    let issued = AuthToken.deriveToken "" ""
    let header = AmbitSession.requestCookieHeader (Some creds) (Some issued)
    Assert.Equal("gambol_auth=" + issued, header)

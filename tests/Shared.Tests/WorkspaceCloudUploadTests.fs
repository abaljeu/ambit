module WorkspaceCloudUploadTests

open Gambol.Shared
open Xunit

[<Fact>]
let ``parseArgs uses stretch defaults when argv is empty`` () =
    let args = WorkspaceCloudUpload.parseArgs [||]
    Assert.Equal("http://127.0.0.1:5215/ambit", args.ambitBase)
    Assert.Equal("/tmp/ambit-stretch-upload", args.mappedRoot)
    Assert.Equal("stretch", args.label)

[<Fact>]
let ``parseArgs reads ambit mapped root and label`` () =
    let args =
        WorkspaceCloudUpload.parseArgs
            [| "http://127.0.0.1:9/ambit"
               "/tmp/mapped"
               "lab" |]
    Assert.Equal("http://127.0.0.1:9/ambit", args.ambitBase)
    Assert.Equal("/tmp/mapped", args.mappedRoot)
    Assert.Equal("lab", args.label)

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

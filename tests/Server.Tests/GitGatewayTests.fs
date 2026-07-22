module Gambol.Server.Tests.GitGatewayTests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private gitOnPath () = DesktopGit.isAvailable()

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private seedWorkspace (dataDir: string) (label: string) =
    let home = Path.Combine(dataDir, label)
    requireOk "ensureInit" (WorkspaceGit.ensureInit home)
    File.WriteAllText(Path.Combine(home, "a.txt"), "one")
    requireOk "commit"
        (WorkspaceGit.commitAll home "seed" None)
    |> ignore
    home

[<Fact>]
let ``urlServiceName uses stock git-*-pack`` () =
    Assert.Equal(
        "git-upload-pack",
        GitGateway.urlServiceName GitGateway.WorkspacePull)
    Assert.Equal(
        "git-receive-pack",
        GitGateway.urlServiceName GitGateway.WorkspacePush)

[<Fact>]
let ``resolveWorkspaceRoot rejects invalid repo name`` () =
    match GitGateway.resolveWorkspaceRoot (newTempDir ()) "not-a-repo" with
    | Error msg -> Assert.Equal("invalid workspace git repo name", msg)
    | Ok _ -> Assert.Fail("expected invalid repo name")

[<SkippableFact>]
let ``resolveWorkspaceRoot inits missing repo under label`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let root = Path.Combine(dataDir, "life")
    Assert.False(WorkspaceGit.isRepo root)
    match GitGateway.resolveWorkspaceRoot dataDir "life.git" with
    | Error err -> Assert.Fail(err)
    | Ok(label, resolved) ->
        Assert.Equal("life", label)
        Assert.Equal(root, resolved)
        Assert.True(WorkspaceGit.isRepo root)
        match WorkspaceGit.tryHead root with
        | Ok None -> ()
        | Ok(Some _) -> Assert.Fail("expected unborn HEAD")
        | Error err -> Assert.Fail(err)

[<SkippableFact>]
let ``resolveWorkspaceRoot inits when label folder exists without dot git`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let root = Path.Combine(dataDir, "life")
    Directory.CreateDirectory(root) |> ignore
    File.WriteAllText(Path.Combine(root, "kept.txt"), "worktree")
    Assert.False(WorkspaceGit.isRepo root)
    match GitGateway.resolveWorkspaceRoot dataDir "life.git" with
    | Error err -> Assert.Fail(err)
    | Ok(label, resolved) ->
        Assert.Equal("life", label)
        Assert.Equal(root, resolved)
        Assert.True(WorkspaceGit.isRepo root)
        Assert.True(File.Exists(Path.Combine(root, "kept.txt")))
        match WorkspaceGit.tryHead root with
        | Ok None -> ()
        | Ok(Some _) -> Assert.Fail("expected unborn HEAD")
        | Error err -> Assert.Fail(err)

[<SkippableFact>]
let ``advertiseRefs prefixes stock service name`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = seedWorkspace (newTempDir ()) "home"
    match GitGateway.advertiseRefs home GitGateway.WorkspacePull with
    | Error err -> Assert.Fail(err)
    | Ok bytes ->
        let text = Encoding.UTF8.GetString(bytes)
        Assert.Contains("# service=git-upload-pack", text)

[<SkippableFact>]
let ``advertiseRefs symrefs actual checked out branch`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = Path.Combine(newTempDir (), "home")
    Directory.CreateDirectory(home) |> ignore
    requireOk "init" (GitSave.runGit home "init -b master") |> ignore
    File.WriteAllText(Path.Combine(home, "a.txt"), "one")
    requireOk "commit" (WorkspaceGit.commitAll home "seed" None) |> ignore
    requireOk "push config" (WorkspaceGit.ensurePushConfig home)
    match WorkspaceGit.currentBranch home with
    | Ok branch -> Assert.Equal("master", branch)
    | Error err -> Assert.Fail(err)
    match GitGateway.advertiseRefs home GitGateway.WorkspacePull with
    | Error err -> Assert.Fail(err)
    | Ok bytes ->
        let text = Encoding.UTF8.GetString(bytes)
        Assert.Contains("symref=HEAD:refs/heads/master", text)

[<SkippableFact>]
let ``jitCommitIfDirty commits before pull path`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = seedWorkspace (newTempDir ()) "home"
    File.WriteAllText(Path.Combine(home, "a.txt"), "two")
    match WorkspaceGit.isDirty home with
    | Ok dirty -> Assert.True(dirty)
    | Error err -> Assert.Fail(err)
    requireOk "jit"
        (WorkspaceGit.jitCommitIfDirty home (Some "test-client"))
    |> ignore
    match WorkspaceGit.isDirty home with
    | Ok dirty -> Assert.False(dirty)
    | Error err -> Assert.Fail(err)
    match GitSave.runGit home "log -1 --pretty=%s" with
    | Ok subject ->
        Assert.Contains("workspace-pull", subject)
        Assert.Contains("client: test-client", subject)
    | Error err -> Assert.Fail(err)

[<SkippableFact>]
let ``jitCommitBeforeWorkspacePush commits dirty tree`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = seedWorkspace (newTempDir ()) "home"
    File.WriteAllText(Path.Combine(home, "a.txt"), "dirty")
    match WorkspaceGit.isDirty home with
    | Ok dirty -> Assert.True(dirty)
    | Error err -> Assert.Fail(err)
    match WorkspaceGit.jitCommitBeforeWorkspacePush home (Some "test-client") with
    | Ok () -> ()
    | Error err -> Assert.Fail(err)
    match WorkspaceGit.isDirty home with
    | Ok dirty -> Assert.False(dirty)
    | Error err -> Assert.Fail(err)
    match GitSave.runGit home "log -1 --pretty=%s" with
    | Ok subject ->
        Assert.Contains("workspace-push", subject)
        Assert.Contains("client: test-client", subject)
    | Error err -> Assert.Fail(err)

[<SkippableFact>]
let ``GET info refs git-upload-pack returns advertisement`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    seedWorkspace dataDir "home" |> ignore
    use client = createClientForDir dataDir
    let url =
        "/ambit/git/home.git/info/refs?service=git-upload-pack"
    let! resp = client.GetAsync(url)
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    Assert.Equal(
        "application/x-git-upload-pack-advertisement",
        resp.Content.Headers.ContentType.MediaType)
    let! body = resp.Content.ReadAsStringAsync()
    Assert.Contains("# service=git-upload-pack", body)
}

[<SkippableFact>]
let ``GET info refs git-receive-pack does not JIT dirty tree`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = seedWorkspace dataDir "home"
    File.WriteAllText(Path.Combine(home, "a.txt"), "dirty")
    use client = createClientForDir dataDir
    let url =
        "/ambit/git/home.git/info/refs?service=git-receive-pack"
    let! resp = client.GetAsync(url)
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    let! body = resp.Content.ReadAsStringAsync()
    Assert.Contains("# service=git-receive-pack", body)
    match WorkspaceGit.isDirty home with
    | Ok dirty -> Assert.True(dirty)
    | Error err -> Assert.Fail(err)
}

[<SkippableFact>]
let ``GET info refs git-receive-pack allows dirty when unborn`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    requireOk "ensureInit" (WorkspaceGit.ensureInit home)
    File.WriteAllText(Path.Combine(home, "insert.txt"), "from Insert")
    match WorkspaceGit.tryHead home with
    | Ok None -> ()
    | other -> Assert.Fail($"expected unborn HEAD, got {other}")
    use client = createClientForDir dataDir
    let url =
        "/ambit/git/home.git/info/refs?service=git-receive-pack"
    let! resp = client.GetAsync(url)
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
}

let private plantOrphanBranch
    (root: string)
    (branch: string)
    (fileName: string)
    (content: string)
    =
    File.WriteAllText(Path.Combine(root, fileName), content)
    requireOk "add" (GitSave.runGit root "add -A") |> ignore
    let tree = requireOk "write-tree" (GitSave.runGit root "write-tree")
    let commit =
        requireOk
            "commit-tree"
            (GitSave.runGit
                root
                $"-c user.email=t@test -c user.name=test commit-tree {tree} -m seed")
    requireOk
        "update-ref"
        (GitSave.runGit root $"update-ref refs/heads/{branch} {commit}")
    |> ignore

[<SkippableFact>]
let ``completeWorkspacePush points HEAD at seeded non-master branch`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let root = Path.Combine(newTempDir (), "home")
    requireOk "ensureInit" (WorkspaceGit.ensureInit root)
    plantOrphanBranch root "main" "seed.txt" "from client"
    match WorkspaceGit.currentBranch root with
    | Ok "master" -> ()
    | other -> Assert.Fail($"expected unborn master symref, got {other}")
    let reconcile _ _ = async { return Ok [] }
    let response = [| 1uy; 2uy |]
    let result =
        GitGateway.completeWorkspacePush
            root
            "home"
            (Ok None)
            (Ok response)
            reconcile
        |> Async.RunSynchronously
    Assert.Equal(Ok response, result)
    match WorkspaceGit.currentBranch root with
    | Ok branch -> Assert.Equal("main", branch)
    | Error err -> Assert.Fail(err)

[<SkippableFact>]
let ``GET info refs rejects unknown custom service name`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    seedWorkspace dataDir "home" |> ignore
    use client = createClientForDir dataDir
    let url =
        "/ambit/git/home.git/info/refs?service=workspace-pull"
    let! resp = client.GetAsync(url)
    Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode)
}

[<SkippableFact>]
let ``GET info refs git-upload-pack JITs dirty tree`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = seedWorkspace dataDir "home"
    File.WriteAllText(Path.Combine(home, "a.txt"), "autosaved")
    use client = createClientForDir dataDir
    let url =
        "/ambit/git/home.git/info/refs?service=git-upload-pack"
    let! resp = client.GetAsync(url)
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    match WorkspaceGit.isDirty home with
    | Ok dirty -> Assert.False(dirty)
    | Error err -> Assert.Fail(err)
    match GitSave.runGit home "log -1 --pretty=%s" with
    | Ok subject -> Assert.Contains("workspace-pull", subject)
    | Error err -> Assert.Fail(err)
}

[<SkippableFact>]
let ``ensureInit sets denyCurrentBranch updateInstead`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = Path.Combine(newTempDir (), "home")
    requireOk "ensureInit" (WorkspaceGit.ensureInit home)
    match GitSave.runGit home "config --get receive.denyCurrentBranch" with
    | Ok value -> Assert.Equal("updateInstead", value)
    | Error err -> Assert.Fail(err)

let private pullInfoRefsUrl =
    "/ambit/git/home.git/info/refs?service=git-upload-pack"

[<SkippableFact>]
let ``gateway rejects unauthenticated when Auth enabled`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    seedWorkspace dataDir "home" |> ignore
    use client = createClientForDirWithAuth dataDir "alice" "secret"
    let! resp = client.GetAsync(pullInfoRefsUrl)
    Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode)
    let www = resp.Headers.WwwAuthenticate.ToString()
    Assert.Contains("Basic", www)
    Assert.Contains(AuthToken.gitBasicRealm, www)
}

[<SkippableFact>]
let ``gateway rejects browser cookie alone`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    seedWorkspace dataDir "home" |> ignore
    use client = createClientForDirWithAuth dataDir "alice" "secret"
    client.DefaultRequestHeaders.Add(
        "Cookie",
        AuthToken.cookieHeaderValue "alice" "secret")
    let! resp = client.GetAsync(pullInfoRefsUrl)
    Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode)
}

[<SkippableFact>]
let ``gateway accepts Basic auth with git PAT`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    seedWorkspace dataDir "home" |> ignore
    use client = createClientForDirWithAuth dataDir "alice" "secret"
    let pat = AuthToken.deriveGitToken "alice" "secret"
    client.DefaultRequestHeaders.Add(
        "Authorization",
        AuthToken.basicAuthHeaderValue "alice" pat)
    let! resp = client.GetAsync(pullInfoRefsUrl)
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    let! body = resp.Content.ReadAsStringAsync()
    Assert.Contains("# service=git-upload-pack", body)
}

[<SkippableFact>]
let ``gateway rejects wrong git PAT`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    seedWorkspace dataDir "home" |> ignore
    use client = createClientForDirWithAuth dataDir "alice" "secret"
    client.DefaultRequestHeaders.Add(
        "Authorization",
        AuthToken.basicAuthHeaderValue "alice" "not-the-pat")
    let! resp = client.GetAsync(pullInfoRefsUrl)
    Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode)
}

[<Fact>]
let ``git-token requires cookie when Auth enabled`` () = task {
    let dataDir = newTempDir ()
    use client = createClientForDirWithAuth dataDir "alice" "secret"
    let! resp = client.GetAsync("/ambit/git-token")
    Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode)
}

[<Fact>]
let ``git-token issues PAT after cookie login`` () = task {
    let dataDir = newTempDir ()
    use client = createClientForDirWithAuth dataDir "alice" "secret"
    client.DefaultRequestHeaders.Add(
        "Cookie",
        AuthToken.cookieHeaderValue "alice" "secret")
    let! resp = client.GetAsync("/ambit/git-token")
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    let! json = resp.Content.ReadAsStringAsync()
    Assert.Contains("alice", json)
    Assert.Contains(AuthToken.deriveGitToken "alice" "secret", json)
}

[<Fact>]
let ``git-token reports disabled when Auth empty`` () = task {
    let dataDir = newTempDir ()
    use client = createClientForDir dataDir
    let! resp = client.GetAsync("/ambit/git-token")
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    let! json = resp.Content.ReadAsStringAsync()
    Assert.Contains("disabled", json)
}

[<Fact>]
let ``gateway-error slot take returns once`` () =
    GitGatewayDiagnostics.set
        "home"
        { status = 500
          message = "receive-pack failed" }
    match GitGatewayDiagnostics.take "home" with
    | Some err ->
        Assert.Equal(500, err.status)
        Assert.Equal("receive-pack failed", err.message)
    | None -> Assert.Fail("expected stored gateway error")
    Assert.Equal(None, GitGatewayDiagnostics.take "home")

[<SkippableFact>]
let ``receive-pack failure stores gateway error for GET`` () = task {
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    seedWorkspace dataDir "home" |> ignore
    use client = createClientForDir dataDir
    use content = new ByteArrayContent([| 0uy; 1uy; 2uy |])
    content.Headers.ContentType <-
        MediaTypeHeaderValue("application/x-git-receive-pack-request")
    let! resp =
        client.PostAsync(
            "/ambit/git/home.git/git-receive-pack",
            content)
    Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode)
    let! errBody = resp.Content.ReadAsStringAsync()
    Assert.False(String.IsNullOrWhiteSpace errBody)
    let! diagResp =
        client.GetAsync("/ambit/git/gateway-error?workspace=home")
    Assert.Equal(HttpStatusCode.OK, diagResp.StatusCode)
    let! diagJson = diagResp.Content.ReadAsStringAsync()
    Assert.Contains("\"status\":500", diagJson)
    Assert.Contains(errBody, diagJson)
    let! emptyResp =
        client.GetAsync("/ambit/git/gateway-error?workspace=home")
    let! emptyJson = emptyResp.Content.ReadAsStringAsync()
    Assert.Equal("{}", emptyJson.Trim())
}

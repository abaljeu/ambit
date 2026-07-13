module DesktopGitTests

open System
open System.Diagnostics
open System.IO
open Gambol.Shared
open Xunit

let private gitOnPath () =
    try
        let psi =
            ProcessStartInfo(
                FileName = "git",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false)
        use proc = Process.Start(psi)
        proc.WaitForExit()
        proc.ExitCode = 0
    with _ ->
        false

let private newTempDir () =
    let dir =
        Path.Combine(Path.GetTempPath(), $"gambol-desktop-git-{Guid.NewGuid()}")
    Directory.CreateDirectory(dir) |> ignore
    dir

let private initRepo (dir: string) =
    DesktopGit.runGit dir "-c user.email=t@test -c user.name=test init"
    |> function
        | Ok _ -> ()
        | Error err -> failwith err
    DesktopGit.runGit dir "checkout -b main"
    |> function
        | Ok _ | Error _ -> ()

[<Fact>]
let ``remoteUrl appends locked gateway path to ambit base`` () =
    Assert.Equal(
        "https://host/ambit/git/home.git",
        WorkspaceGitRemote.remoteUrl "https://host/ambit/" "home")

[<Fact>]
let ``parseShortStatus reads ahead behind and dirty`` () =
    let text =
        "## main...ambit/main [ahead 2, behind 1]\n M note.txt\n"
    let status = WorkspaceGitRemote.parseShortStatus text
    Assert.Equal(Some "main", status.branch)
    Assert.Equal(2, status.ahead)
    Assert.Equal(1, status.behind)
    Assert.True(status.dirty)

[<Fact>]
let ``parseShortStatus clean tracking branch`` () =
    let status =
        WorkspaceGitRemote.parseShortStatus "## main...ambit/main\n"
    Assert.Equal(Some "main", status.branch)
    Assert.Equal(0, status.ahead)
    Assert.Equal(0, status.behind)
    Assert.False(status.dirty)

[<Fact>]
let ``formatStatusLine shows ahead behind dirty`` () =
    let line =
        WorkspaceGitRemote.formatStatusLine
            { branch = Some "main"; ahead = 2; behind = 1; dirty = true }
    Assert.Equal("main ↑2 ↓1 *", line)

[<Fact>]
let ``formatStatusLine clean branch`` () =
    let line =
        WorkspaceGitRemote.formatStatusLine
            { branch = Some "main"; ahead = 0; behind = 0; dirty = false }
    Assert.Equal("main", line)

[<Fact>]
let ``canDesktopGit requires git capability`` () =
    Assert.False(WorkspaceGitRemote.canDesktopGit None)
    Assert.False(
        WorkspaceGitRemote.canDesktopGit (Some DesktopCapabilities.disabled))
    Assert.True(
        WorkspaceGitRemote.canDesktopGit
            (Some (DesktopCapabilities.desktopEnabled true)))
    Assert.False(
        WorkspaceGitRemote.canDesktopGit
            (Some (DesktopCapabilities.desktopEnabled false)))

[<Fact>]
let ``basicAuthHeaderValue encodes user and token`` () =
    let header = DesktopGit.basicAuthHeaderValue "alice" "pat"
    Assert.StartsWith("Basic ", header)
    let b64 = header.Substring(6)
    let decoded =
        Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64))
    Assert.Equal("alice:pat", decoded)

[<Fact>]
let ``gitAuthConfigPairs clears credential helper without auth`` () =
    let pairs = DesktopGit.gitAuthConfigPairs None
    Assert.Equal(1, pairs.Length)
    Assert.Equal(("credential.helper", ""), pairs.[0])

[<Fact>]
let ``gitAuthConfigPairs adds Authorization header with auth`` () =
    let pairs = DesktopGit.gitAuthConfigPairs (Some("alice", "pat"))
    Assert.Equal(2, pairs.Length)
    Assert.Equal(("credential.helper", ""), pairs.[0])
    let key, value = pairs.[1]
    Assert.Equal("http.extraHeader", key)
    Assert.Equal(
        "Authorization: " + DesktopGit.basicAuthHeaderValue "alice" "pat",
        value)

[<Fact>]
let ``filterGitErrorDetail strips unencrypted HTTP warning`` () =
    let raw =
        "warning: use of unencrypted HTTP remote URLs is not recommended; "
        + "see https://aka.ms/gcm/http\n"
        + "fatal: Authentication failed for "
        + "'http://localhost:5115/ambit/git/d.git/'"
    let filtered = DesktopGit.filterGitErrorDetail raw
    Assert.False(
        filtered.Contains("unencrypted HTTP"),
        "GCM HTTP warning should be stripped")
    Assert.True(
        filtered.Contains("Authentication failed"),
        "fatal auth message should remain")

[<Fact>]
let ``filterGitErrorDetail leaves unrelated stderr intact`` () =
    let raw = "fatal: not a git repository"
    Assert.Equal(raw, DesktopGit.filterGitErrorDetail raw)

[<SkippableFact>]
let ``setAmbitRemoteForLabel adds ambit remote`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dir = newTempDir ()
    initRepo dir
    match
        DesktopGit.setAmbitRemoteForLabel
            dir
            "home"
            "https://example.org/ambit"
    with
    | Error err -> Assert.Fail(err)
    | Ok () ->
        match DesktopGit.runGit dir "remote get-url ambit" with
        | Ok url ->
            Assert.Equal("https://example.org/ambit/git/home.git", url)
        | Error err -> Assert.Fail(err)

[<SkippableFact>]
let ``setAmbitRemoteForLabel updates existing ambit remote`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dir = newTempDir ()
    initRepo dir
    DesktopGit.setAmbitRemote dir "https://old.example/ambit/git/home.git"
    |> ignore
    match
        DesktopGit.setAmbitRemoteForLabel
            dir
            "home"
            "https://new.example/ambit"
    with
    | Error err -> Assert.Fail(err)
    | Ok () ->
        match DesktopGit.runGit dir "remote get-url ambit" with
        | Ok url ->
            Assert.Equal("https://new.example/ambit/git/home.git", url)
        | Error err -> Assert.Fail(err)

[<SkippableFact>]
let ``status reports dirty working tree`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dir = newTempDir ()
    initRepo dir
    File.WriteAllText(Path.Combine(dir, "a.txt"), "x")
    match DesktopGit.status dir with
    | Error err -> Assert.Fail(err)
    | Ok status -> Assert.True(status.dirty)

[<SkippableFact>]
let ``clone copies a local bare-ish repo path via file url`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let src = newTempDir ()
    initRepo src
    File.WriteAllText(Path.Combine(src, "note.txt"), "hi")
    DesktopGit.runGit
        src
        "-c user.email=t@test -c user.name=test add -A"
    |> ignore
    DesktopGit.runGit
        src
        "-c user.email=t@test -c user.name=test commit -m init"
    |> function
        | Ok _ -> ()
        | Error err -> failwith err
    let dest = Path.Combine(newTempDir (), "clone")
    let uri = Uri(src + Path.DirectorySeparatorChar.ToString()).AbsoluteUri
    match DesktopGit.clone uri dest None with
    | Error err -> Assert.Fail(err)
    | Ok _ ->
        Assert.True(File.Exists(Path.Combine(dest, "note.txt")))

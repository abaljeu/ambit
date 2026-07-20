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
let ``pullArguments targets server branch on ambit remote`` () =
    Assert.Equal("pull ambit refs/heads/master", DesktopGit.pullArguments "master")

[<Fact>]
let ``pullArgumentsIgnoringAttrs disables gitattributes via empty tree`` () =
    Assert.Equal(
        "-c attr.tree="
        + DesktopGit.emptyAttrTree
        + " pull ambit refs/heads/master",
        DesktopGit.pullArgumentsIgnoringAttrs "master")

[<Fact>]
let ``isOverwrittenByMergeError detects stock git abort`` () =
    Assert.True(
        DesktopGit.isOverwrittenByMergeError
            "error: Your local changes to the following files would be overwritten by merge:\n\t.amb")
    Assert.False(
        DesktopGit.isOverwrittenByMergeError
            "refusing to merge unrelated histories")

[<Fact>]
let ``pushArguments maps HEAD to server branch on ambit`` () =
    Assert.Equal(
        "push ambit HEAD:refs/heads/master",
        DesktopGit.pushArguments "master")

[<Fact>]
let ``parseRemoteHeadBranch reads advertised server branch`` () =
    let text =
        "ref: refs/heads/master\tHEAD\n"
        + "0123456789abcdef\tHEAD\n"
    match DesktopGit.parseRemoteHeadBranch text with
    | Ok branch -> Assert.Equal("master", branch)
    | Error err -> Assert.Fail(err)

[<Fact>]
let ``parseRemoteHeadBranch preserves nested server branch`` () =
    let text = "ref: refs/heads/server/live\tHEAD\n"
    match DesktopGit.parseRemoteHeadBranch text with
    | Ok branch -> Assert.Equal("server/live", branch)
    | Error err -> Assert.Fail(err)

[<Fact>]
let ``parseRemoteHeadBranch rejects HEAD without branch symref`` () =
    let text = "0123456789abcdef\tHEAD\n"
    match DesktopGit.parseRemoteHeadBranch text with
    | Ok branch -> Assert.Fail($"expected missing branch error, got {branch}")
    | Error err ->
        Assert.Equal("Ambit remote HEAD does not identify a branch.", err)

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
let ``gitPull returns Error for unrelated histories`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let server = newTempDir ()
    initRepo server
    File.WriteAllText(Path.Combine(server, "server.txt"), "server")
    DesktopGit.runGit
        server
        "-c user.email=t@test -c user.name=test add -A"
    |> ignore
    DesktopGit.runGit
        server
        "-c user.email=t@test -c user.name=test commit -m server"
    |> function
        | Ok _ -> ()
        | Error err -> failwith err
    let desktop = newTempDir ()
    initRepo desktop
    File.WriteAllText(Path.Combine(desktop, "desktop.txt"), "desktop")
    DesktopGit.runGit
        desktop
        "-c user.email=t@test -c user.name=test add -A"
    |> ignore
    DesktopGit.runGit
        desktop
        "-c user.email=t@test -c user.name=test commit -m desktop"
    |> function
        | Ok _ -> ()
        | Error err -> failwith err
    let uri = Uri(server + Path.DirectorySeparatorChar.ToString()).AbsoluteUri
    DesktopGit.setAmbitRemote desktop uri |> ignore
    match DesktopGit.gitPull desktop None with
    | Ok _ -> Assert.Fail("expected unrelated histories Error")
    | Error err ->
        Assert.True(
            err.IndexOf(
                "refusing to merge unrelated histories",
                StringComparison.OrdinalIgnoreCase)
            >= 0,
            err)

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

let private commitAll (dir: string) (message: string) =
    DesktopGit.runGit
        dir
        "-c user.email=t@test -c user.name=test add -A"
    |> ignore
    DesktopGit.runGit
        dir
        $"-c user.email=t@test -c user.name=test commit -m {message}"
    |> function
        | Ok _ -> ()
        | Error err -> failwith err

[<SkippableFact>]
let ``push targets local branch name not remote HEAD`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let server = newTempDir ()
    match DesktopGit.runGit server "init -b master" with
    | Ok _ -> ()
    | Error err -> failwith err
    let desktop = newTempDir ()
    initRepo desktop
    File.WriteAllText(Path.Combine(desktop, "seed.txt"), "from client")
    commitAll desktop "seed"
    let uri = Uri(server + Path.DirectorySeparatorChar.ToString()).AbsoluteUri
    match DesktopGit.setAmbitRemote desktop uri with
    | Error err -> Assert.Fail(err)
    | Ok () -> ()
    match DesktopGit.push desktop None with
    | Error err -> Assert.Fail(err)
    | Ok _ ->
        match DesktopGit.runGit server "show-ref --verify refs/heads/main" with
        | Ok _ -> ()
        | Error err -> Assert.Fail($"expected main on server: {err}")
        match
            DesktopGit.runGit server "show-ref --verify refs/heads/master"
        with
        | Ok _ ->
            Assert.Fail(
                "push must use local branch, not remote HEAD (master)")
        | Error _ -> ()

let private plantCrlfBlob (dir: string) (relPath: string) (content: string) =
    let abs = Path.Combine(dir, relPath)
    let bytes = Text.Encoding.UTF8.GetBytes(content)
    File.WriteAllBytes(abs, bytes)
    // Path form of hash-object may normalize CRLF on Windows; stdin +
    // --no-filters keeps the CRLF blob that triggers eol=lf false dirt.
    let psi =
        ProcessStartInfo(
            FileName = "git",
            Arguments = "hash-object -w --stdin --no-filters",
            WorkingDirectory = dir,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false)
    use proc = Process.Start(psi)
    proc.StandardInput.BaseStream.Write(bytes, 0, bytes.Length)
    proc.StandardInput.Close()
    let hash = proc.StandardOutput.ReadToEnd().Trim()
    let stderr = proc.StandardError.ReadToEnd()
    proc.WaitForExit()
    if proc.ExitCode <> 0 || hash.Length = 0 then
        failwith $"hash-object failed: {stderr}"
    DesktopGit.runGit
        dir
        $"update-index --add --cacheinfo 100644,{hash},{relPath}"
    |> function
        | Ok _ -> ()
        | Error err -> failwith err

[<SkippableFact>]
let ``gitPull retries past eol=lf false dirt on CRLF-indexed files`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let server = newTempDir ()
    initRepo server
    File.WriteAllText(Path.Combine(server, ".gitattributes"), "* text eol=lf\n")
    commitAll server "attrs"
    plantCrlfBlob server ".amb" "tp\r\n-> x\r\n"
    DesktopGit.runGit
        server
        "-c user.email=t@test -c user.name=test commit -m crlf-amb"
    |> function
        | Ok _ -> ()
        | Error err -> failwith err
    File.WriteAllText(Path.Combine(server, ".amb"), "tp\n-> y\n")
    commitAll server "server-update"
    let desktop = newTempDir ()
    let uri = Uri(server + Path.DirectorySeparatorChar.ToString()).AbsoluteUri
    match DesktopGit.clone uri desktop None with
    | Error err -> Assert.Fail(err)
    | Ok _ -> ()
    DesktopGit.runGit desktop "reset --hard HEAD~1"
    |> function
        | Ok _ -> ()
        | Error err -> failwith err
    DesktopGit.runGit desktop "remote rename origin ambit"
    |> function
        | Ok _ -> ()
        | Error err -> failwith err
    match DesktopGit.status desktop with
    | Ok status -> Assert.True(status.dirty, "expected eol=lf false dirt before pull")
    | Error err -> Assert.Fail(err)
    match DesktopGit.gitPull desktop None with
    | Error err -> Assert.Fail(err)
    | Ok _ ->
        let text = File.ReadAllText(Path.Combine(desktop, ".amb"))
        Assert.Contains("-> y", text.Replace("\r", ""))

[<SkippableFact>]
let ``gitPull still fails when real local edits would be overwritten`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let server = newTempDir ()
    initRepo server
    File.WriteAllText(Path.Combine(server, "file.txt"), "base")
    commitAll server "base"
    File.WriteAllText(Path.Combine(server, "file.txt"), "server")
    commitAll server "server"
    let desktop = newTempDir ()
    let uri = Uri(server + Path.DirectorySeparatorChar.ToString()).AbsoluteUri
    match DesktopGit.clone uri desktop None with
    | Error err -> Assert.Fail(err)
    | Ok _ -> ()
    DesktopGit.runGit desktop "reset --hard HEAD~1"
    |> function
        | Ok _ -> ()
        | Error err -> failwith err
    DesktopGit.runGit desktop "remote rename origin ambit"
    |> function
        | Ok _ -> ()
        | Error err -> failwith err
    File.WriteAllText(Path.Combine(desktop, "file.txt"), "local real edit")
    match DesktopGit.gitPull desktop None with
    | Ok _ -> Assert.Fail("expected overwrite Error for real local edits")
    | Error err ->
        Assert.True(DesktopGit.isOverwrittenByMergeError err, err)

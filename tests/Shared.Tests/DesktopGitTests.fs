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
let ``hostFromAmbitBase extracts host`` () =
    match DesktopGit.hostFromAmbitBase "https://example.org/ambit" with
    | Ok host -> Assert.Equal("example.org", host)
    | Error err -> Assert.Fail(err)

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
    match DesktopGit.clone uri dest with
    | Error err -> Assert.Fail(err)
    | Ok _ ->
        Assert.True(File.Exists(Path.Combine(dest, "note.txt")))

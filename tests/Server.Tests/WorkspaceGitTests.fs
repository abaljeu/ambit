module Gambol.Server.Tests.WorkspaceGitTests

open System
open System.Diagnostics
open System.IO
open Xunit
open Gambol.Server

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
    let dir = Path.Combine(Path.GetTempPath(), $"gambol-ws-git-{Guid.NewGuid()}")
    Directory.CreateDirectory(dir) |> ignore
    dir

[<Fact>]
let ``workspaceRepoDir uses at-label under data dir`` () =
    let dataDir = newTempDir ()
    let repoDir = WorkspaceGit.workspaceRepoDir dataDir "home"
    Assert.Equal(Path.Combine(dataDir, "home"), repoDir)

[<SkippableFact>]
let ``ensureRepo initializes git only under workspace label dir`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    File.WriteAllText(Path.Combine(dataDir, "outside.txt"), "x")
    match WorkspaceGit.ensureRepo dataDir "home" with
    | Error err -> Assert.Fail(err)
    | Ok repoDir ->
        Assert.True(GitSave.isRepo repoDir)
        Assert.False(GitSave.isRepo dataDir)
        File.WriteAllText(Path.Combine(repoDir, "inside.txt"), "y")
        match WorkspaceGit.commit dataDir "home" "workspace commit" with
        | Error err -> Assert.Fail(err)
        | Ok _ ->
            match GitSave.runGit repoDir "log --oneline" with
            | Ok log -> Assert.Contains("workspace commit", log)
            | Error err -> Assert.Fail(err)

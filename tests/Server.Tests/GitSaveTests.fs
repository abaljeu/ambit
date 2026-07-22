module Gambol.Server.Tests.GitSaveTests

open System
open System.IO
open Xunit
open Gambol.Server
open Gambol.Shared

let private gitOnPath () = DesktopGit.isAvailable()

let private newTempDir () =
    let dir = Path.Combine(Path.GetTempPath(), $"gambol-git-{Guid.NewGuid()}")
    Directory.CreateDirectory(dir) |> ignore
    dir

let private initRepo (dir: string) =
    GitSave.runGit dir "-c user.email=t@test -c user.name=test init"
    |> function
        | Ok _ -> ()
        | Error err -> failwith err

[<Fact>]
let ``isRepo is false without git directory`` () =
    let dir = newTempDir ()
    Assert.False(GitSave.isRepo dir)

[<SkippableFact>]
let ``isRepo is true after git init`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dir = newTempDir ()
    initRepo dir
    Assert.True(GitSave.isRepo dir)

[<SkippableFact>]
let ``commitAll creates a commit for tracked changes`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dir = newTempDir ()
    initRepo dir
    File.WriteAllText(Path.Combine(dir, "note.txt"), "one")
    match GitSave.commitAll dir "first" with
    | Ok _ -> ()
    | Error err -> Assert.Fail(err)
    Assert.True(File.Exists(Path.Combine(dir, ".git", "HEAD")))

[<SkippableFact>]
let ``commitAll succeeds when nothing to commit`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dir = newTempDir ()
    initRepo dir
    File.WriteAllText(Path.Combine(dir, "note.txt"), "one")
    GitSave.commitAll dir "first" |> ignore
    match GitSave.commitAll dir "second" with
    | Ok detail -> Assert.Contains("nothing", detail, StringComparison.OrdinalIgnoreCase)
    | Error err -> Assert.Fail(err)

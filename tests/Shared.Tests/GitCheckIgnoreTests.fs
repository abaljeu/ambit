module GitCheckIgnoreTests

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
        Path.Combine(
            Path.GetTempPath(),
            $"gambol-check-ignore-{Guid.NewGuid()}")
    Directory.CreateDirectory(dir) |> ignore
    dir

let private writeIgnore root (text: string) =
    Directory.CreateDirectory(root) |> ignore
    File.WriteAllText(Path.Combine(root, ".gitignore"), text)

[<Fact>]
let ``isGitignorePath allows root and nested gitignore`` () =
    Assert.True(GitCheckIgnore.isGitignorePath ".gitignore")
    Assert.True(GitCheckIgnore.isGitignorePath "docs/.gitignore")
    Assert.False(GitCheckIgnore.isGitignorePath "gitignore")
    Assert.False(GitCheckIgnore.isGitignorePath "docs/notes.txt")

[<SkippableFact>]
let ``isIgnored detects ignored and allowed paths`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let root = newTempDir ()
    writeIgnore root "*.tmp\nblocked.txt\n"
    match GitCheckIgnore.isIgnored root "notes.txt" with
    | Error e -> Assert.Fail(e)
    | Ok ignored -> Assert.False(ignored)
    match GitCheckIgnore.isIgnored root "blocked.txt" with
    | Error e -> Assert.Fail(e)
    | Ok ignored -> Assert.True(ignored)
    match GitCheckIgnore.isIgnored root "x.tmp" with
    | Error e -> Assert.Fail(e)
    | Ok ignored -> Assert.True(ignored)

[<SkippableFact>]
let ``isEffectivelyIgnored never blocks gitignore file`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let root = newTempDir ()
    writeIgnore root ".*\n"
    match GitCheckIgnore.isEffectivelyIgnored root ".gitignore" with
    | Error e -> Assert.Fail(e)
    | Ok ignored -> Assert.False(ignored)
    match GitCheckIgnore.isIgnored root ".gitignore" with
    | Error e -> Assert.Fail(e)
    | Ok ignored -> Assert.True(ignored)

[<SkippableFact>]
let ``classify batches ignored and not ignored`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let root = newTempDir ()
    writeIgnore root "ignored/\n*.log\n"
    match
        GitCheckIgnore.classify
            root
            [ "ok.txt"; "ignored/a.txt"; "x.log"; "keep.md" ]
    with
    | Error e -> Assert.Fail(e)
    | Ok rows ->
        let map = Map.ofList rows
        Assert.False(map.["ok.txt"])
        Assert.True(map.["ignored/a.txt"])
        Assert.True(map.["x.log"])
        Assert.False(map.["keep.md"])

[<SkippableFact>]
let ``nested directory gitignore is honored`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let root = newTempDir ()
    writeIgnore root ""
    let nested = Path.Combine(root, "pkg")
    writeIgnore nested "secret.bin\n"
    match GitCheckIgnore.isIgnored root "pkg/secret.bin" with
    | Error e -> Assert.Fail(e)
    | Ok ignored -> Assert.True(ignored)
    match GitCheckIgnore.isIgnored root "pkg/visible.txt" with
    | Error e -> Assert.Fail(e)
    | Ok ignored -> Assert.False(ignored)

module GitCheckIgnoreTests

open System
open System.IO
open Gambol.Shared
open Xunit

let private gitOnPath () = DesktopGit.isAvailable()

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

/// Shared TEMP check-ignore .git can exist without HEAD/config; must reinit.
[<SkippableFact>]
let ``isIgnored recovers from incomplete shared check-ignore git dir`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let sharedRoot =
        Path.Combine(Path.GetTempPath(), "gambol-check-ignore-git")
    let sharedGit = Path.Combine(sharedRoot, ".git")
    if Directory.Exists sharedGit then
        Directory.Delete(sharedGit, true)
    elif File.Exists sharedGit then
        File.Delete sharedGit
    Directory.CreateDirectory sharedGit |> ignore
    Assert.False(File.Exists(Path.Combine(sharedGit, "HEAD")))
    let root = newTempDir ()
    writeIgnore root "blocked.txt\n"
    match GitCheckIgnore.isIgnored root "notes.txt" with
    | Error e -> Assert.Fail(e)
    | Ok ignored -> Assert.False(ignored)
    match GitCheckIgnore.isIgnored root "blocked.txt" with
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
    writeIgnore root "ignored/\n*.cache\n"
    match
        GitCheckIgnore.classify
            root
            [ "ok.txt"; "ignored/a.txt"; "x.cache"; "keep.md" ]
    with
    | Error e -> Assert.Fail(e)
    | Ok rows ->
        let map = Map.ofList rows
        Assert.False(map.["ok.txt"])
        Assert.True(map.["ignored/a.txt"])
        Assert.True(map.["x.cache"])
        Assert.False(map.["keep.md"])

/// One git process lists included files; ignored trees stay out.
[<SkippableFact>]
let ``listIncluded returns non-ignored files under directory`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let root = newTempDir ()
    writeIgnore root "ignored/\n*.cache\n"
    Directory.CreateDirectory(Path.Combine(root, "ignored")) |> ignore
    Directory.CreateDirectory(Path.Combine(root, "src")) |> ignore
    File.WriteAllText(Path.Combine(root, "ok.txt"), "y")
    File.WriteAllText(Path.Combine(root, "x.cache"), "n")
    File.WriteAllText(Path.Combine(root, "ignored", "a.txt"), "n")
    File.WriteAllText(Path.Combine(root, "src", "keep.md"), "y")
    match GitCheckIgnore.listIncluded root "" with
    | Error e -> Assert.Fail(e)
    | Ok paths ->
        let set = Set.ofList paths
        Assert.True(Set.contains "ok.txt" set)
        Assert.True(Set.contains "src/keep.md" set)
        Assert.True(Set.contains ".gitignore" set)
        Assert.False(Set.contains "x.cache" set)
        Assert.False(Set.contains "ignored/a.txt" set)
    match GitCheckIgnore.listIncluded root "src" with
    | Error e -> Assert.Fail(e)
    | Ok underSrc ->
        Assert.Contains("src/keep.md", underSrc)
        Assert.DoesNotContain("ok.txt", underSrc)

[<SkippableFact>]
let ``listIncluded keeps gitignore when pattern would ignore it`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let root = newTempDir ()
    writeIgnore root ".*\n"
    File.WriteAllText(Path.Combine(root, "ok.txt"), "y")
    match GitCheckIgnore.listIncluded root "" with
    | Error e -> Assert.Fail(e)
    | Ok paths ->
        let set = Set.ofList paths
        Assert.True(Set.contains "ok.txt" set)
        Assert.True(Set.contains ".gitignore" set)
    Assert.True(
        GitCheckIgnore.isIncludedIn
            (Set.ofList [ "src/a.txt" ])
            "src"
            true)
    Assert.False(
        GitCheckIgnore.isIncludedIn
            (Set.ofList [ "ok.txt" ])
            "other"
            true)
    Assert.True(
        GitCheckIgnore.isIncludedIn Set.empty ".gitignore" false)

/// Repro: stdin write before draining stdout deadlocks once ignored output
/// exceeds the OS pipe buffer (~64KiB). Must finish well under a minute.
[<SkippableFact>]
let ``classify large ignored set does not pipe-deadlock`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let root = newTempDir ()
    writeIgnore root "node_modules/\n"
    let paths =
        [ for i in 1..2500 ->
            sprintf "node_modules/pkg%d/file%d.js" i i ]
        @ [ "keep.txt" ]
    let sw = System.Diagnostics.Stopwatch.StartNew()
    match GitCheckIgnore.classify root paths with
    | Error e -> Assert.Fail(e)
    | Ok rows ->
        sw.Stop()
        if sw.ElapsedMilliseconds >= 30_000L then
            Assert.Fail(
                sprintf
                    "classify took %dms (pipe deadlock?)"
                    sw.ElapsedMilliseconds)
        let map = Map.ofList rows
        Assert.False(map.["keep.txt"])
        Assert.True(map.["node_modules/pkg1/file1.js"])
        Assert.Equal(2501, rows.Length)

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

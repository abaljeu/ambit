module WorkspaceLocalInventoryTests

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
            $"gambol-local-inv-{Guid.NewGuid()}")
    Directory.CreateDirectory(dir) |> ignore
    dir

let private writeIgnore root (text: string) =
    File.WriteAllText(Path.Combine(root, ".gitignore"), text)

[<Fact>]
let ``orderForUpload sorts directories by depth then files`` () =
    let items =
        [ { relative = "a/b"; isDirectory = true }
          { relative = "z.txt"; isDirectory = false }
          { relative = "a"; isDirectory = true }
          { relative = "a/b/c.txt"; isDirectory = false } ]
    let ordered = WorkspaceLocalInventory.orderForUpload items
    let rels = ordered |> List.map (fun i -> i.relative)
    Assert.Equal<string list>(
        [ "a"; "a/b"; "a/b/c.txt"; "z.txt" ],
        rels)

[<SkippableFact>]
let ``listForPush excludes gitignored and skips .git`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let root = newTempDir ()
    writeIgnore root "*.tmp\n.venv/\n"
    Directory.CreateDirectory(Path.Combine(root, "docs")) |> ignore
    Directory.CreateDirectory(Path.Combine(root, ".git")) |> ignore
    Directory.CreateDirectory(Path.Combine(root, ".venv")) |> ignore
    File.WriteAllText(Path.Combine(root, "docs", "a.txt"), "ok")
    File.WriteAllText(Path.Combine(root, "skip.tmp"), "no")
    File.WriteAllText(Path.Combine(root, ".venv", "x"), "no")
    File.WriteAllText(Path.Combine(root, ".git", "config"), "no")
    File.WriteAllText(Path.Combine(root, ".gitignore"), "*.tmp\n.venv/\n")

    let scope =
        { label = "home"
          relative = ""
          kind = SyncScopeKind.Workspace }

    match WorkspaceLocalInventory.listForPush root scope with
    | Error e -> Assert.Fail(e)
    | Ok items ->
        let rels =
            items |> List.map (fun i -> i.relative) |> Set.ofList
        Assert.True(Set.contains "docs" rels)
        Assert.True(Set.contains "docs/a.txt" rels)
        Assert.True(Set.contains ".gitignore" rels)
        Assert.False(Set.contains "skip.tmp" rels)
        Assert.False(Set.contains ".venv" rels)
        Assert.False(Set.contains ".venv/x" rels)
        Assert.False(Set.contains ".git" rels)
        Assert.False(Set.contains ".git/config" rels)

[<SkippableFact>]
let ``listForPush directory scope is prefix-limited`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let root = newTempDir ()
    writeIgnore root ""
    Directory.CreateDirectory(Path.Combine(root, "docs")) |> ignore
    Directory.CreateDirectory(Path.Combine(root, "other")) |> ignore
    File.WriteAllText(Path.Combine(root, "docs", "a.txt"), "a")
    File.WriteAllText(Path.Combine(root, "other", "b.txt"), "b")

    let scope =
        { label = "home"
          relative = "docs"
          kind = SyncScopeKind.Directory }

    match WorkspaceLocalInventory.listForPush root scope with
    | Error e -> Assert.Fail(e)
    | Ok items ->
        let rels =
            items |> List.map (fun i -> i.relative) |> Set.ofList
        Assert.True(Set.contains "docs" rels)
        Assert.True(Set.contains "docs/a.txt" rels)
        Assert.False(Set.contains "other" rels)
        Assert.False(Set.contains "other/b.txt" rels)

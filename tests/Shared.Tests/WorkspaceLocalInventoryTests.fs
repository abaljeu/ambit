module WorkspaceLocalInventoryTests

open System
open System.IO
open Gambol.Shared
open Xunit

let private gitOnPath () = DesktopGit.isAvailable()

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

[<SkippableFact>]
let ``listForPush without git still returns walk`` () =
    Skip.If(gitOnPath (), "git present — soft-skip path not exercised")
    let root = newTempDir ()
    writeIgnore root "*.tmp\n"
    Directory.CreateDirectory(Path.Combine(root, ".git")) |> ignore
    File.WriteAllText(Path.Combine(root, "keep.txt"), "ok")
    File.WriteAllText(Path.Combine(root, "skip.tmp"), "no")
    File.WriteAllText(Path.Combine(root, ".git", "config"), "no")

    let scope =
        { label = "home"
          relative = ""
          kind = SyncScopeKind.Workspace }

    match WorkspaceLocalInventory.listForPush root scope with
    | Error e -> Assert.Fail(e)
    | Ok items ->
        let rels =
            items |> List.map (fun i -> i.relative) |> Set.ofList
        Assert.True(Set.contains "keep.txt" rels)
        // Without git, ignore filter is skipped — .tmp is kept.
        Assert.True(Set.contains "skip.tmp" rels)
        Assert.False(Set.contains ".git" rels)
        Assert.False(Set.contains ".git/config" rels)

[<Fact>]
let ``listImmediateChildren returns only depth-1`` () =
    let root = newTempDir ()
    Directory.CreateDirectory(Path.Combine(root, "docs")) |> ignore
    Directory.CreateDirectory(Path.Combine(root, "docs", "nested")) |> ignore
    File.WriteAllText(Path.Combine(root, "top.txt"), "ok")
    File.WriteAllText(Path.Combine(root, "docs", "a.txt"), "a")

    match WorkspaceLocalInventory.listImmediateChildren root "" with
    | Error e -> Assert.Fail(e)
    | Ok items ->
        let rels =
            items |> List.map (fun i -> i.relative) |> Set.ofList
        Assert.True(Set.contains "docs" rels)
        Assert.True(Set.contains "top.txt" rels)
        Assert.False(Set.contains "docs/a.txt" rels)
        Assert.False(Set.contains "docs/nested" rels)
        let docs =
            items |> List.find (fun i -> i.relative = "docs")
        Assert.True(docs.isDirectory)
        let top =
            items |> List.find (fun i -> i.relative = "top.txt")
        Assert.False(top.isDirectory)

[<Fact>]
let ``planUploadInventory TopLevel caps to immediate children`` () =
    let root = newTempDir ()
    Directory.CreateDirectory(Path.Combine(root, "docs")) |> ignore
    Directory.CreateDirectory(Path.Combine(root, "docs", "nested")) |> ignore
    File.WriteAllText(Path.Combine(root, "top.txt"), "ok")
    File.WriteAllText(Path.Combine(root, "docs", "a.txt"), "a")
    File.WriteAllText(Path.Combine(root, "docs", "nested", "deep.txt"), "d")
    let pad =
        [ 1..1500 ]
        |> List.map (fun i ->
            File.WriteAllText(Path.Combine(root, $"pad{i}.txt"), "p")
            $"pad{i}.txt")
    ignore pad
    let scope =
        { label = "home"
          relative = ""
          kind = SyncScopeKind.Workspace }
    match WorkspaceLocalInventory.listForUpload root scope with
    | Error e -> Assert.Fail(e)
    | Ok(mode, items) ->
        Assert.Equal(WorkspaceSyncLimits.Mode.TopLevel, mode)
        let rels = items |> List.map (fun i -> i.relative) |> Set.ofList
        Assert.True(Set.contains "docs" rels)
        Assert.True(Set.contains "top.txt" rels)
        Assert.False(Set.contains "docs/a.txt" rels)
        Assert.False(Set.contains "docs/nested" rels)

[<Fact>]
let ``listForUpload Full keeps nested ignore-filtered paths`` () =
    let root = newTempDir ()
    Directory.CreateDirectory(Path.Combine(root, "docs")) |> ignore
    Directory.CreateDirectory(Path.Combine(root, "docs", "nested")) |> ignore
    File.WriteAllText(Path.Combine(root, "docs", "a.txt"), "a")
    File.WriteAllText(Path.Combine(root, "docs", "nested", "b.txt"), "b")
    File.WriteAllText(Path.Combine(root, "top.txt"), "t")

    let scope =
        { label = "home"
          relative = ""
          kind = SyncScopeKind.Workspace }

    match WorkspaceLocalInventory.listForUpload root scope with
    | Error e -> Assert.Fail(e)
    | Ok(mode, items) ->
        Assert.Equal(WorkspaceSyncLimits.Mode.Full, mode)
        let rels =
            items |> List.map (fun i -> i.relative) |> Set.ofList
        Assert.True(Set.contains "docs" rels)
        Assert.True(Set.contains "docs/a.txt" rels)
        Assert.True(Set.contains "docs/nested" rels)
        Assert.True(Set.contains "docs/nested/b.txt" rels)
        Assert.True(Set.contains "top.txt" rels)

[<Fact>]
let ``listForUpload TopLevel returns only immediate children`` () =
    let root = newTempDir ()
    Directory.CreateDirectory(Path.Combine(root, "docs")) |> ignore
    Directory.CreateDirectory(Path.Combine(root, "docs", "nested")) |> ignore
    File.WriteAllText(Path.Combine(root, "docs", "a.txt"), "a")
    File.WriteAllText(Path.Combine(root, "docs", "nested", "b.txt"), "b")
    File.WriteAllText(Path.Combine(root, "top.txt"), "t")

    for i in 1..1500 do
        File.WriteAllText(Path.Combine(root, $"pad{i}.txt"), "p")

    let scope =
        { label = "home"
          relative = ""
          kind = SyncScopeKind.Workspace }

    match WorkspaceLocalInventory.listForUpload root scope with
    | Error e -> Assert.Fail(e)
    | Ok(mode, items) ->
        Assert.Equal(WorkspaceSyncLimits.Mode.TopLevel, mode)
        let rels =
            items |> List.map (fun i -> i.relative) |> Set.ofList
        Assert.True(Set.contains "docs" rels)
        Assert.True(Set.contains "top.txt" rels)
        Assert.True(Set.contains "pad1.txt" rels)
        Assert.False(Set.contains "docs/a.txt" rels)
        Assert.False(Set.contains "docs/nested" rels)
        Assert.False(Set.contains "docs/nested/b.txt" rels)

[<SkippableFact>]
let ``listForUpload excludes gitignored like listForPush`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let root = newTempDir ()
    writeIgnore root "*.tmp\n.venv/\n"
    Directory.CreateDirectory(Path.Combine(root, "docs")) |> ignore
    Directory.CreateDirectory(Path.Combine(root, ".venv")) |> ignore
    File.WriteAllText(Path.Combine(root, "docs", "a.txt"), "ok")
    File.WriteAllText(Path.Combine(root, "skip.tmp"), "no")
    File.WriteAllText(Path.Combine(root, ".venv", "x"), "no")

    let scope =
        { label = "home"
          relative = ""
          kind = SyncScopeKind.Workspace }

    match WorkspaceLocalInventory.listForUpload root scope with
    | Error e -> Assert.Fail(e)
    | Ok(_, items) ->
        let rels =
            items |> List.map (fun i -> i.relative) |> Set.ofList
        Assert.True(Set.contains "docs" rels)
        Assert.True(Set.contains "docs/a.txt" rels)
        Assert.False(Set.contains "skip.tmp" rels)
        Assert.False(Set.contains ".venv" rels)

module Gambol.Server.Tests.WorkspaceGitTests

open System
open System.Diagnostics
open System.IO
open Xunit
open Gambol.Server
open Gambol.Shared

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
    let dir = Path.Combine(Path.GetTempPath(), $"gambol-wsgit-{Guid.NewGuid()}")
    Directory.CreateDirectory(dir) |> ignore
    dir

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private graphWithWorkspace (label: string) : Graph * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let wsNode =
        Node.Create(
            wsId,
            text = label,
            name = Filename.create label,
            owner = Graph.workspacesId,
            kind = Special Workspace)
    let graph1 =
        graph0.nodes
        |> Map.add wsId wsNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces->ws"
    graph2, wsId

[<Fact>]
let ``isRepo is false without git directory`` () =
    let dir = newTempDir ()
    Assert.False(WorkspaceGit.isRepo dir)

[<SkippableFact>]
let ``ensureInit creates .git under workspace root not parent`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    requireOk "ensureInit" (WorkspaceGit.ensureInit home)
    Assert.True(WorkspaceGit.isRepo home)
    Assert.False(WorkspaceGit.isRepo dataDir)
    Assert.True(Directory.Exists(Path.Combine(home, ".git")))

[<SkippableFact>]
let ``ensureInit is idempotent when .git already present`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = Path.Combine(newTempDir (), "home")
    requireOk "first" (WorkspaceGit.ensureInit home)
    requireOk "second" (WorkspaceGit.ensureInit home)
    Assert.True(WorkspaceGit.isRepo home)

[<SkippableFact>]
let ``ensureInit sets receive.denyNonFastForwards`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = Path.Combine(newTempDir (), "home")
    requireOk "ensureInit" (WorkspaceGit.ensureInit home)
    match GitSave.runGit home "config --get receive.denyNonFastForwards" with
    | Ok value -> Assert.Equal("true", value)
    | Error err -> Assert.Fail(err)

[<SkippableFact>]
let ``writeDocument for Workspace inits repo under label`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let graph, wsId = graphWithWorkspace "home"
    DocumentPersistence.writeDocument dataDir graph wsId
    |> requireOk "writeDocument"
    |> ignore
    Assert.True(WorkspaceGit.isRepo (Path.Combine(dataDir, "home")))
    Assert.False(WorkspaceGit.isRepo dataDir)

[<SkippableFact>]
let ``isDirty is false on clean repo and true after edit`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = Path.Combine(newTempDir (), "home")
    requireOk "ensureInit" (WorkspaceGit.ensureInit home)
    File.WriteAllText(Path.Combine(home, "a.txt"), "one")
    requireOk "commit"
        (WorkspaceGit.commitAll home "rev 1" (Some "test-client"))
    |> ignore
    match WorkspaceGit.isDirty home with
    | Ok dirty -> Assert.False(dirty)
    | Error err -> Assert.Fail(err)
    File.WriteAllText(Path.Combine(home, "a.txt"), "two")
    match WorkspaceGit.isDirty home with
    | Ok dirty -> Assert.True(dirty)
    | Error err -> Assert.Fail(err)

[<SkippableFact>]
let ``commitAll message includes client hint`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = Path.Combine(newTempDir (), "home")
    requireOk "ensureInit" (WorkspaceGit.ensureInit home)
    File.WriteAllText(Path.Combine(home, "note.txt"), "x")
    let hint = "Win32; Mozilla/5.0"
    requireOk "commit"
        (WorkspaceGit.commitAll home "rev 3" (Some hint))
    |> ignore
    match GitSave.runGit home "log -1 --pretty=%s" with
    | Ok subject ->
        Assert.Equal("rev 3 | client: Win32; Mozilla/5.0", subject)
    | Error err -> Assert.Fail(err)

[<SkippableFact>]
let ``commitAll does not touch sibling workspace`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let dataDir = newTempDir ()
    let home = Path.Combine(dataDir, "home")
    let other = Path.Combine(dataDir, "other")
    requireOk "init home" (WorkspaceGit.ensureInit home)
    requireOk "init other" (WorkspaceGit.ensureInit other)
    File.WriteAllText(Path.Combine(home, "h.txt"), "h1")
    File.WriteAllText(Path.Combine(other, "o.txt"), "o1")
    requireOk "seed home"
        (WorkspaceGit.commitAll home "seed" None)
    |> ignore
    requireOk "seed other"
        (WorkspaceGit.commitAll other "seed" None)
    |> ignore
    File.WriteAllText(Path.Combine(home, "h.txt"), "h2")
    File.WriteAllText(Path.Combine(other, "o.txt"), "o2")
    requireOk "commit home"
        (WorkspaceGit.commitAll home "rev 9" (Some "client-a"))
    |> ignore
    match WorkspaceGit.isDirty home with
    | Ok dirty -> Assert.False(dirty)
    | Error err -> Assert.Fail(err)
    match WorkspaceGit.isDirty other with
    | Ok dirty -> Assert.True(dirty)
    | Error err -> Assert.Fail(err)
    match GitSave.runGit other "log -1 --pretty=%s" with
    | Ok subject -> Assert.Equal("seed", subject)
    | Error err -> Assert.Fail(err)

[<SkippableFact>]
let ``statusPorcelain reports untracked file`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = Path.Combine(newTempDir (), "home")
    requireOk "ensureInit" (WorkspaceGit.ensureInit home)
    File.WriteAllText(Path.Combine(home, "new.txt"), "n")
    match WorkspaceGit.statusPorcelain home with
    | Ok text ->
        Assert.False(String.IsNullOrWhiteSpace text)
        Assert.Contains("new.txt", text)
    | Error err -> Assert.Fail(err)

[<Fact>]
let ``parseChangedPaths handles NUL separated A D R and M rows`` () =
    let raw =
        "A\000added.txt\000"
        + "D\000deleted.txt\000"
        + "R087\000old name.txt\000new name.txt\000"
        + "M\000modified.txt\000"
    let parsed = WorkspaceGit.parseChangedPaths raw |> requireOk "parse"
    let expected =
        [ LazyLoadReconciliation.Added "added.txt"
          LazyLoadReconciliation.Deleted "deleted.txt"
          LazyLoadReconciliation.Renamed("old name.txt", "new name.txt")
          LazyLoadReconciliation.Modified "modified.txt" ]
    Assert.Equal<LazyLoadReconciliation.ChangedPath list>(expected, parsed)

[<SkippableFact>]
let ``changedPathsBetween extracts rename delete add and modify`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let root = Path.Combine(newTempDir (), "home")
    requireOk "init" (WorkspaceGit.ensureInit root)
    File.WriteAllText(Path.Combine(root, "rename.txt"), "rename")
    File.WriteAllText(Path.Combine(root, "delete.txt"), "delete")
    File.WriteAllText(Path.Combine(root, "modify.txt"), "before")
    requireOk "seed" (WorkspaceGit.commitAll root "seed" None) |> ignore
    let oldHead = WorkspaceGit.tryHead root |> requireOk "old head" |> Option.get
    File.Move(
        Path.Combine(root, "rename.txt"),
        Path.Combine(root, "renamed.txt"))
    File.Delete(Path.Combine(root, "delete.txt"))
    File.WriteAllText(Path.Combine(root, "modify.txt"), "after")
    File.WriteAllText(Path.Combine(root, "added.txt"), "added")
    requireOk "change" (WorkspaceGit.commitAll root "change" None) |> ignore
    let newHead = WorkspaceGit.tryHead root |> requireOk "new head" |> Option.get
    let changes =
        WorkspaceGit.changedPathsBetween root (Some oldHead) newHead
        |> requireOk "changed paths"
    Assert.Contains(LazyLoadReconciliation.Added "added.txt", changes)
    Assert.Contains(LazyLoadReconciliation.Deleted "delete.txt", changes)
    Assert.Contains(LazyLoadReconciliation.Modified "modify.txt", changes)
    Assert.Contains(
        LazyLoadReconciliation.Renamed("rename.txt", "renamed.txt"),
        changes)

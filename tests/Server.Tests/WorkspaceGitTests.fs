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

let private git root arguments =
    GitSave.runGit root arguments |> requireOk arguments

let private currentBranch root =
    git root "symbolic-ref --short HEAD"

let private branchOid root branch =
    git root $"rev-parse refs/heads/{branch}"

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
    match GitSave.runGit home "symbolic-ref --short HEAD" with
    | Ok branch when not (String.IsNullOrWhiteSpace branch) -> ()
    | _ -> Assert.Fail("expected a checked-out branch after init")

[<SkippableFact>]
let ``ensureInit creates master as default branch`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = Path.Combine(newTempDir (), "home")
    requireOk "ensureInit" (WorkspaceGit.ensureInit home)
    Assert.Equal("master", currentBranch home)

[<SkippableFact>]
let ``ensureInit renames lone main branch to master`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = Path.Combine(newTempDir (), "home")
    Directory.CreateDirectory(home) |> ignore
    git home "init -b main" |> ignore
    File.WriteAllText(Path.Combine(home, "note.txt"), "main")
    git home "add -A" |> ignore
    git home "-c user.email=test@gambol -c user.name=test commit -m seed"
    |> ignore
    let mainOid = branchOid home "main"

    requireOk "ensureInit" (WorkspaceGit.ensureInit home)

    Assert.Equal("master", currentBranch home)
    Assert.Equal(mainOid, branchOid home "master")

[<SkippableFact>]
let ``ensureInit preserves master branch without renaming`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = Path.Combine(newTempDir (), "home")
    Directory.CreateDirectory(home) |> ignore
    git home "init -b master" |> ignore
    File.WriteAllText(Path.Combine(home, "note.txt"), "existing")
    git home "add -A" |> ignore
    git home "-c user.email=test@gambol -c user.name=test commit -m seed"
    |> ignore
    let originalOid = branchOid home "master"

    requireOk "ensureInit" (WorkspaceGit.ensureInit home)

    Assert.Equal("master", currentBranch home)
    Assert.Equal(originalOid, branchOid home "master")

[<SkippableFact>]
let ``currentBranch reads attached branch from HEAD file`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = Path.Combine(newTempDir (), "home")
    Directory.CreateDirectory(home) |> ignore
    git home "init -b master" |> ignore
    match WorkspaceGit.currentBranch home with
    | Ok branch -> Assert.Equal("master", branch)
    | Error err -> Assert.Fail(err)

[<SkippableFact>]
let ``ensureInit does not switch away from existing checked out branch`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = Path.Combine(newTempDir (), "home")
    requireOk "initial ensureInit" (WorkspaceGit.ensureInit home)
    git home "branch -m main" |> ignore
    File.WriteAllText(Path.Combine(home, "note.txt"), "main")
    WorkspaceGit.commitAll home "main commit" None
    |> requireOk "main commit"
    |> ignore
    let mainOid = branchOid home "main"
    git home "checkout -b master" |> ignore
    File.WriteAllText(Path.Combine(home, "note.txt"), "master")
    WorkspaceGit.commitAll home "master commit" None
    |> requireOk "master commit"
    |> ignore
    let masterOid = branchOid home "master"

    requireOk "ensureInit" (WorkspaceGit.ensureInit home)

    Assert.Equal("master", currentBranch home)
    Assert.Equal(mainOid, branchOid home "main")
    Assert.Equal(masterOid, branchOid home "master")

[<SkippableFact>]
let ``ensureInit is idempotent when .git already present`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = Path.Combine(newTempDir (), "home")
    requireOk "first" (WorkspaceGit.ensureInit home)
    requireOk "second" (WorkspaceGit.ensureInit home)
    Assert.True(WorkspaceGit.isRepo home)

[<SkippableFact>]
let ``ensureInit excludes reserved gambol dot files from tracking`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = Path.Combine(newTempDir (), "home")
    requireOk "ensureInit" (WorkspaceGit.ensureInit home)
    File.WriteAllText(Path.Combine(home, "gambol.log"), "bookkeeping")
    File.WriteAllText(Path.Combine(home, "GAMBOL.meta"), "bookkeeping")
    File.WriteAllText(Path.Combine(home, "gambol"), "ordinary")
    let status = WorkspaceGit.statusPorcelain home |> requireOk "status"
    Assert.DoesNotContain("gambol.log", status)
    Assert.DoesNotContain("GAMBOL.meta", status)
    Assert.Contains("gambol", status)

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
    | Ok text -> Assert.False(String.IsNullOrWhiteSpace text)
    | Error err -> Assert.Fail(err)

[<SkippableFact>]
let ``jitCommitBeforeWorkspacePush commits dirty tree`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let home = Path.Combine(newTempDir (), "home")
    requireOk "ensureInit" (WorkspaceGit.ensureInit home)
    File.WriteAllText(Path.Combine(home, "a.txt"), "one")
    match WorkspaceGit.isDirty home with
    | Ok dirty -> Assert.True(dirty)
    | Error err -> Assert.Fail(err)
    requireOk "jit"
        (WorkspaceGit.jitCommitBeforeWorkspacePush home (Some "test-client"))
    |> ignore
    match WorkspaceGit.isDirty home with
    | Ok dirty -> Assert.False(dirty)
    | Error err -> Assert.Fail(err)
    match GitSave.runGit home "log -1 --pretty=%s" with
    | Ok subject ->
        Assert.Contains("workspace-push", subject)
        Assert.Contains("client: test-client", subject)
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

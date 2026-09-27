module Gambol.Server.Tests.WorkspaceGitTests

open System
open System.Collections.Concurrent
open System.IO
open System.Threading
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared

let private gitOnPath () = DesktopGit.isAvailable()

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

let private configureIdentity root =
    git root "config user.email test@gambol" |> ignore
    git root "config user.name test" |> ignore

let private commitFile
    (root: string)
    (path: string)
    (text: string)
    (message: string)
    =
    File.WriteAllText(Path.Combine(root, path), text)
    git root "add -A" |> ignore
    git root $"commit -m {message}" |> ignore

let private trackedWorkspace () =
    let parent = newTempDir ()
    let remote = Path.Combine(parent, "remote.git")
    let source = Path.Combine(parent, "source")
    let workspace = Path.Combine(parent, "workspace")
    Directory.CreateDirectory(remote) |> ignore
    git remote "init --bare -b main" |> ignore
    Directory.CreateDirectory(source) |> ignore
    git source "init -b main" |> ignore
    configureIdentity source
    commitFile source "note.txt" "seed" "seed"
    git source $"remote add origin {remote}" |> ignore
    git source "push -u origin main" |> ignore
    git parent $"clone {remote} workspace" |> ignore
    configureIdentity workspace
    parent, remote, source, workspace

let private owned = ChildNode.owners

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
        Graph.addDetachedNode wsNode graph0
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
    File.WriteAllText(Path.Combine(home, "gambol.events"), "bookkeeping")
    File.WriteAllText(Path.Combine(home, "GAMBOL.meta"), "bookkeeping")
    File.WriteAllText(Path.Combine(home, "gambol"), "ordinary")
    let status = WorkspaceGit.statusPorcelain home |> requireOk "status"
    Assert.DoesNotContain("gambol.events", status)
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
    DocumentPersistWrite.writeDocument dataDir graph wsId
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

[<SkippableFact>]
let ``remoteExists reports any configured remote`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let root = Path.Combine(newTempDir (), "home")
    requireOk "init" (WorkspaceGit.ensureInit root)
    Assert.False(WorkspaceGit.remoteExists root |> requireOk "without remote")
    git root "remote add backup https://example.invalid/repo.git" |> ignore
    Assert.True(WorkspaceGit.remoteExists root |> requireOk "with remote")

[<SkippableFact>]
let ``pullTracked fast forwards the current tracked branch`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let _, _, source, workspace = trackedWorkspace ()
    commitFile source "note.txt" "from remote" "remote-change"
    git source "push origin main" |> ignore

    let tracked = WorkspaceGit.trackedBranch workspace |> requireOk "tracked"
    Assert.Equal("main", tracked.branch)
    Assert.Equal("origin", tracked.remote)
    Assert.Equal("refs/heads/main", tracked.upstream)
    WorkspaceGit.pullTracked workspace |> requireOk "pull" |> ignore

    Assert.Equal("from remote", File.ReadAllText(Path.Combine(workspace, "note.txt")))
    Assert.Equal(branchOid source "main", branchOid workspace "main")

[<SkippableFact>]
let ``saveTracked commits then pushes while honoring gitignore`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let parent, remote, _, workspace = trackedWorkspace ()
    File.WriteAllText(Path.Combine(workspace, ".gitignore"), "ignored.txt\n")
    File.WriteAllText(Path.Combine(workspace, "saved.txt"), "saved")
    File.WriteAllText(Path.Combine(workspace, "ignored.txt"), "ignored")

    WorkspaceGit.saveTracked workspace "save" None
    |> requireOk "save"
    |> ignore

    let verify = Path.Combine(parent, "verify")
    git parent $"clone {remote} verify" |> ignore
    Assert.True(File.Exists(Path.Combine(verify, "saved.txt")))
    Assert.False(File.Exists(Path.Combine(verify, "ignored.txt")))
    Assert.Equal(branchOid verify "main", branchOid workspace "main")

[<SkippableFact>]
let ``saveTracked rejects a non fast forward push`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let parent, remote, _, workspace = trackedWorkspace ()
    let peer = Path.Combine(parent, "peer")
    git parent $"clone {remote} peer" |> ignore
    configureIdentity peer
    commitFile peer "peer.txt" "peer" "peer-change"
    git peer "push origin main" |> ignore
    File.WriteAllText(Path.Combine(workspace, "local.txt"), "local")

    match WorkspaceGit.saveTracked workspace "save" None with
    | Ok _ -> Assert.Fail("expected non-fast-forward rejection")
    | Error error ->
        Assert.True(
            error.Contains("non-fast-forward", StringComparison.OrdinalIgnoreCase)
            || error.Contains("fetch first", StringComparison.OrdinalIgnoreCase))
        Assert.True(error.Length <= 400)

[<SkippableFact>]
let ``saveTracked conflict error names an unmerged path`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let _, _, _, workspace = trackedWorkspace ()
    git workspace "checkout -b other" |> ignore
    commitFile workspace "note.txt" "other" "other-change"
    git workspace "checkout main" |> ignore
    commitFile workspace "note.txt" "main" "main-change"
    GitSave.runGit workspace "merge other" |> ignore

    match WorkspaceGit.saveTracked workspace "save" None with
    | Ok _ -> Assert.Fail("expected conflict rejection")
    | Error error ->
        Assert.Contains("note.txt", error)
        Assert.True(error.Length <= 400)

[<SkippableFact>]
let ``pullTracked local conflict error names the path`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let _, _, source, workspace = trackedWorkspace ()
    commitFile source "note.txt" "remote" "remote-change"
    git source "push origin main" |> ignore
    File.WriteAllText(Path.Combine(workspace, "note.txt"), "local")

    match WorkspaceGit.pullTracked workspace with
    | Ok _ -> Assert.Fail("expected local conflict rejection")
    | Error error ->
        Assert.Contains("note.txt", error)
        Assert.True(error.Length <= 400)

[<SkippableFact>]
let ``pullTracked condenses other git failures`` () =
    Skip.IfNot(gitOnPath(), "git not on PATH")
    let root = Path.Combine(newTempDir (), "home")
    requireOk "init" (WorkspaceGit.ensureInit root)
    commitFile root "note.txt" "seed" "seed"
    let missing = Path.Combine(newTempDir (), "missing.git")
    git root $"remote add origin {missing}" |> ignore
    git root "config branch.master.remote origin" |> ignore
    git root "config branch.master.merge refs/heads/master" |> ignore

    match WorkspaceGit.pullTracked root with
    | Ok _ -> Assert.Fail("expected pull failure")
    | Error error ->
        Assert.Contains("does not appear to be a git repository", error)
        Assert.True(error.Length <= 400)

[<Fact>]
let ``work tree gate waits then continues without overlap`` () =
    let root = Path.Combine(newTempDir (), "home")
    let entered = ConcurrentQueue<string>()
    let holderEntered = TaskCompletionSource<unit>()
    let releaseHolder = TaskCompletionSource<unit>()
    let waiterAttempted = TaskCompletionSource<unit>()
    let waiterEntered = TaskCompletionSource<unit>()

    let holder =
        Task.Run(fun () ->
            WorkspaceGit.withWorkTreeGate root (fun () ->
                entered.Enqueue("holder entered")
                holderEntered.SetResult()
                releaseHolder.Task.Wait()
                entered.Enqueue("holder exited")
                Ok ()))

    holderEntered.Task.Wait()
    let waiter =
        Task.Run(fun () ->
            waiterAttempted.SetResult()
            WorkspaceGit.withWorkTreeGate root (fun () ->
                entered.Enqueue("waiter entered")
                waiterEntered.SetResult()
                Ok ()))
    waiterAttempted.Task.Wait()
    Assert.False(waiterEntered.Task.Wait(100))
    releaseHolder.SetResult()
    Task.WaitAll(holder, waiter)
    Assert.Equal<string list>(
        [ "holder entered"; "holder exited"; "waiter entered" ],
        entered |> Seq.toList)

[<Fact>]
let ``Persist write waits on the Workspace work tree gate`` () =
    let dataDir = newTempDir ()
    let graph, wsId = graphWithWorkspace "home"
    let root = Path.Combine(dataDir, "home")
    let holderEntered = TaskCompletionSource<unit>()
    let releaseHolder = TaskCompletionSource<unit>()

    let holder =
        Task.Run(fun () ->
            WorkspaceGit.withWorkTreeGate root (fun () ->
                holderEntered.SetResult()
                releaseHolder.Task.Wait()
                Ok ()))

    holderEntered.Task.Wait()
    let persist =
        Task.Run(fun () -> DocumentPersistWrite.writeDocument dataDir graph wsId)
    Assert.False(persist.Wait(100))
    Assert.False(File.Exists(Path.Combine(root, ".amb")))
    releaseHolder.SetResult()
    Task.WaitAll(holder, persist)
    persist.Result |> requireOk "Persist" |> ignore
    Assert.True(File.Exists(Path.Combine(root, ".amb")))

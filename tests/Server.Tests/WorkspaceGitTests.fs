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

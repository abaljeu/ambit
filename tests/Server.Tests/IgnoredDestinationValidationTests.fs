module Gambol.Server.Tests.IgnoredDestinationValidationTests

open System
open System.IO
open Gambol.Server
open Gambol.Server.Tests.TestBackend
open Gambol.Shared
open Xunit

module Encode = Thoth.Json.Newtonsoft.Encode

let private gitOnPath () = DesktopGit.isAvailable()

let private ownedChild = ChildNode.owner

let private setChildren parentId kids (graph: Graph) =
    Graph.fromNodes
        graph.root
        graph.nodes
        (Map.add parentId kids graph.childMap)

let private addSpecial parentId kind name (graph: Graph) =
    let id = NodeId.New()
    let node =
        Node.Create(
            id,
            text = name,
            name = Filename.create name,
            owner = parentId,
            kind = Special kind)
    let graph1 = Graph.addDetachedNode node graph
    let graph2 =
        setChildren
            parentId
            (Graph.children graph1 parentId @ [ ownedChild id ])
            graph1
    graph2, id

let private rename id name (graph: Graph) =
    Graph.setName id (Filename.tryValue graph.nodes.[id].name |> Option.get) name graph
    |> function
        | Ok changed -> changed
        | Error error -> failwith error

let private reparent id newParentId (graph: Graph) =
    let oldParentId = graph.ownerParentByChild.[id]
    let moved = { graph.nodes.[id] with owner = newParentId }
    let nodes = graph.nodes |> Map.add id moved
    let childMap =
        graph.childMap
        |> Map.add
            oldParentId
            (Graph.children graph oldParentId
             |> List.filter (fun child -> child.id <> id))
        |> Map.add
            newParentId
            (Graph.children graph newParentId @ [ ownedChild id ])
    Graph.fromNodes graph.root nodes childMap

let private writeIgnore root (text: string) =
    Directory.CreateDirectory(root) |> ignore
    File.WriteAllText(Path.Combine(root, ".gitignore"), text)

let private assertIgnored dataDir preGraph postGraph =
    match DocumentPersistence.validateGraphDiskEffects dataDir preGraph postGraph with
    | Ok () -> Assert.Fail("expected ignored destination rejection")
    | Error error -> Assert.Contains("ignored by .gitignore", error)

let private assertAllowed dataDir preGraph postGraph =
    match DocumentPersistence.validateGraphDiskEffects dataDir preGraph postGraph with
    | Ok () -> ()
    | Error error -> Assert.Fail(error)

let private workspaceGraph name =
    addSpecial Graph.workspacesId Workspace name (Graph.create ())

[<SkippableFact>]
let ``named workspace rejects ignored create rename and reparent destinations`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let dataDir = newTempDir ()
    writeIgnore (Path.Combine(dataDir, "home")) "blocked.txt\nignored/\n"
    let baseGraph, workspaceId = workspaceGraph "home"
    let created, _ = addSpecial workspaceId File "blocked.txt" baseGraph
    assertIgnored dataDir baseGraph created

    let withFile, fileId = addSpecial workspaceId File "allowed.txt" baseGraph
    assertIgnored dataDir withFile (rename fileId "blocked.txt" withFile)

    let withTarget, targetId = addSpecial workspaceId Directory "ignored" withFile
    assertIgnored dataDir withTarget (reparent fileId targetId withTarget)

[<SkippableFact>]
let ``named workspace allows gitignore negation`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let dataDir = newTempDir ()
    writeIgnore (Path.Combine(dataDir, "home")) "*.tmp\n!important.tmp\n"
    let graph, workspaceId = workspaceGraph "home"
    let postGraph, _ = addSpecial workspaceId File "important.tmp" graph
    assertAllowed dataDir graph postGraph

[<SkippableFact>]
let ``cross workspace move uses destination workspace rules`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let dataDir = newTempDir ()
    writeIgnore (Path.Combine(dataDir, "source")) ""
    writeIgnore (Path.Combine(dataDir, "target")) "blocked.txt\n"
    let graph1, sourceId = workspaceGraph "source"
    let graph2, targetId = addSpecial Graph.workspacesId Workspace "target" graph1
    let graph3, fileId = addSpecial sourceId File "blocked.txt" graph2
    assertIgnored dataDir graph3 (reparent fileId targetId graph3)

[<SkippableFact>]
let ``ROOT and TRASH use data gitignore`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let dataDir = newTempDir ()
    writeIgnore dataDir "root.tmp\nTRASH/trash.tmp\n"
    let graph = Graph.create ()
    let rootPost, _ = addSpecial Graph.rootId File "root.tmp" graph
    assertIgnored dataDir graph rootPost
    let trashPost, _ = addSpecial Graph.trashId File "trash.tmp" graph
    assertIgnored dataDir graph trashPost

[<SkippableFact>]
let ``gitignore destination is always allowed`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let dataDir = newTempDir ()
    writeIgnore (Path.Combine(dataDir, "home")) ".*\n"
    let graph, workspaceId = workspaceGraph "home"
    let postGraph, _ = addSpecial workspaceId File ".gitignore" graph
    assertAllowed dataDir graph postGraph

[<SkippableFact>]
let ``non ignored destination succeeds`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let dataDir = newTempDir ()
    writeIgnore (Path.Combine(dataDir, "home")) "*.tmp\n"
    let graph, workspaceId = workspaceGraph "home"
    let postGraph, _ = addSpecial workspaceId File "notes.txt" graph
    assertAllowed dataDir graph postGraph

[<SkippableFact>]
let ``many new destinations reject ignored and keep gitignore`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let dataDir = newTempDir ()
    writeIgnore (Path.Combine(dataDir, "home")) ".*\nblocked.txt\n"
    let graph, workspaceId = workspaceGraph "home"
    let allowedNames =
        [ 1..20 ] |> List.map (fun i -> sprintf "notes-%02d.txt" i)
    let withAllowed =
        allowedNames
        |> List.fold (fun g name ->
            addSpecial workspaceId File name g |> fst) graph
    let withGitignore, _ =
        addSpecial workspaceId File ".gitignore" withAllowed
    assertAllowed dataDir graph withGitignore
    let withBlocked, _ =
        addSpecial workspaceId File "blocked.txt" withGitignore
    assertIgnored dataDir graph withBlocked

let private encodeChange graph parentId name =
    let _, ops = FileNodeOps.planCreateOwnedFile graph parentId name
    [ SpecialNodeTestHelpers.changeEventZero "" ops ]

[<SkippableFact>]
let ``file persist rejects ignored graph state before acceptance`` () =
    Skip.IfNot(gitOnPath (), "git unavailable")
    let dataDir = newTempDir ()
    writeIgnore dataDir "blocked.txt\n"
    let agent, _ = createAdmittedFile dataDir
    let body = encodeChange (Graph.create ()) Graph.rootId "blocked.txt"
    let result =
        (admittedChanges agent).postEvents (body)
        |> Async.RunSynchronously
    Assert.True(Result.isError result)
    Assert.Equal(EventId.zero, CoreMailbox.getEventId agent |> Async.RunSynchronously)
    CoreMailbox.dispose agent

[<SkippableFact>]
let ``db persist rejects ignored graph state before acceptance`` () = task {
    Skip.IfNot(gitOnPath (), "git unavailable")
    let connectionString = requireDbConnStr ()
    do! resetTestDatabase connectionString
    let dataDir = newTempDir ()
    writeIgnore dataDir "blocked.txt\n"
    let agent =
        CoreMailbox.createDbWithDataDir
            connectionString
            dataDir
            admittedCredentials
    let body = encodeChange (Graph.create ()) Graph.rootId "blocked.txt"
    let! result =
        (admittedChanges agent).postEvents (body)
        |> Async.StartAsTask
    Assert.True(Result.isError result)
    let! revision = CoreMailbox.getEventId agent |> Async.StartAsTask
    Assert.Equal(EventId.zero, revision)
}

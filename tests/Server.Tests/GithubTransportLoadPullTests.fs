module Gambol.Server.Tests.GithubTransportLoadPullTests

open System
open System.IO
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private postChange (host: MailboxHost) (ops: Op list) =
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change ops }
    CoreMailboxApi.postGraphOnly host testCaller event
    |> Async.RunSynchronously
    |> requireOk "post"
    |> ignore

let private readGraph (host: MailboxHost) =
    CoreMailboxApi.getState host
    |> Async.RunSynchronously
    |> requireOk "state"
    |> fun state -> state.graph

let private seedDocs (host: MailboxHost) =
    let workspaceId, workspaceOps =
        FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    postChange host workspaceOps
    let graph = readGraph host
    let docsId, docsOps =
        FileNodeOps.planCreateOwnedDirectory graph workspaceId "docs"
    postChange
        host
        (docsOps
         @ [ Op.SetDocumentState(docsId, Unparsed, Current) ])
    workspaceId, docsId

let private fileNamed (graph: Graph) parentId name =
    Graph.children graph parentId
    |> List.map (fun child -> graph.nodes.[child.id])
    |> List.find (fun node ->
        Filename.tryValue node.name = Some name)

let private pullAddsExtra root =
    fun _ _ ->
        let docs = Path.Combine(root, "docs")
        Directory.CreateDirectory(docs) |> ignore
        File.WriteAllText(Path.Combine(docs, "extra.txt"), "extra")
        WorkspaceGit.commitAll root "pull" None
        |> Result.map (fun _ -> "pulled")

let private markingDependencies pull =
    let tracked =
        { branch = "master"
          remote = "origin"
          upstream = "refs/heads/master" }
    { git =
        { withWorkTreeGate = fun _ action -> action ()
          trackedBranch = fun _ -> Ok tracked
          pullTracked = pull
          commitAll = fun _ _ _ -> Error "unused"
          pushTracked = fun _ _ _ -> Error "unused" } }

[<SkippableFact>]
let ``git Load pull marks a received file and its directory Unparsed`` () =
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let dataDir = newTempDir ()
    let host, changes = createAdmittedFile dataDir
    try
        let workspaceId, docsId = seedDocs host
        let root = Path.Combine(dataDir, "home")
        WorkspaceGit.ensureInit root |> requireOk "init" |> ignore
        File.WriteAllText(Path.Combine(root, "seed.txt"), "seed")
        WorkspaceGit.commitAll root "seed" None
        |> requireOk "seed"
        |> ignore
        GithubTransportActor.runLoad
            (markingDependencies (pullAddsExtra root))
            dataDir
            changes
            workspaceId
            root
        |> Async.RunSynchronously
        |> requireOk "load pull"
        |> ignore
        let graph = readGraph host
        Assert.Equal(Unparsed, graph.nodes.[docsId].documentState.AsEnum)
        Assert.Equal(
            Unparsed,
            (fileNamed graph docsId "extra.txt").documentState.AsEnum)
    finally
        CoreMailboxApi.dispose host

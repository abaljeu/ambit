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
    CoreMailbox.postGraphOnly host testCaller event
    |> Async.RunSynchronously
    |> requireOk "post"
    |> ignore

let private readGraph (host: MailboxHost) =
    CoreMailbox.getState host
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

[<Fact>]
let ``git Load pull marks a received file and its directory Unparsed`` () =
    let dataDir = newTempDir ()
    let host, changes = createAdmittedFile dataDir
    try
        let workspaceId, docsId = seedDocs host
        let docs = Path.Combine(dataDir, "home", "docs")
        Directory.CreateDirectory(docs) |> ignore
        File.WriteAllText(Path.Combine(docs, "extra.txt"), "extra")
        GithubTransportActor.finishLoadPull
            { handle = changes
              dataDir = dataDir
              workspaceLabel = "home" }
            workspaceId
            [ LazyLoadReconciliation.Added "docs/extra.txt" ]
        |> Async.RunSynchronously
        |> requireOk "load pull"
        |> ignore
        let graph = readGraph host
        Assert.Equal(Unparsed, graph.nodes.[docsId].documentState)
        Assert.Equal(
            Unparsed,
            (fileNamed graph docsId "extra.txt").documentState)
    finally
        CoreMailbox.dispose host

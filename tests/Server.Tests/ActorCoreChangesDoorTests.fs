module Gambol.Server.Tests.ActorCoreChangesDoorTests

open System
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

/// Seam: Actor CoreChanges is CoreMailbox.coreChanges (the public door).

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private sampleRequest: Gambol.Shared.ActorStart =
    { zoomId = Graph.rootId
      focusId = Graph.rootId
      commandId = Graph.rootId
      graphIds = [ Graph.rootId ]
      revision = Gambol.Shared.EventId 0 }

let private withPersist persist body =
    task {
        let pool = CoreActorPool.create ()
        let host = CoreMailbox.host pool persist admittedCredentials
        try
            do! body host pool
        finally
            CoreMailbox.dispose host
    }

let private filePersist () =
    FileAgent.persist (FileAgent.create (newTempDir ()))

let private startRootActor host pool actorFn =
    pool.register (ActorName "root") actorFn
    CoreMailbox.startActor host testCaller sampleRequest

[<Fact>]
let ``Actor getRevision surfaces persist error instead of Revision 0`` () =
    let persist = filePersist ()
    let filling =
        { persist with
            handlers =
                { persist.handlers with
                    getRevision = fun () -> Error "revision unavailable" } }
    let seen = TaskCompletionSource<string option>()
    withPersist filling (fun host pool -> task {
        let! started =
            startRootActor host pool (fun _ coreChanges -> async {
                try
                    let! _ = coreChanges.getRevision ()
                    seen.TrySetResult None |> ignore
                with ex ->
                    seen.TrySetResult (Some ex.Message) |> ignore
            })
            |> Async.StartAsTask
        requireOk "startActor" started
        let! observed = seen.Task.WaitAsync(TimeSpan.FromSeconds 5.0)
        Assert.Equal(Some "revision unavailable", observed)
    })


[<Fact>]
let ``Actor isReady uses mailbox host isReady`` () =
    let persist = filePersist ()
    let filling = { persist with isReady = fun () -> false }
    let seen = TaskCompletionSource<bool>()
    withPersist filling (fun host pool -> task {
        let doorReady = (CoreMailbox.coreChanges host testCaller).isReady ()
        let! started =
            startRootActor host pool (fun _ coreChanges -> async {
                seen.TrySetResult (coreChanges.isReady ()) |> ignore
            })
            |> Async.StartAsTask
        requireOk "startActor" started
        let! actorReady = seen.Task.WaitAsync(TimeSpan.FromSeconds 5.0)
        Assert.False(doorReady)
        Assert.False(actorReady)
    })

[<Fact>]
let ``Actor postChange on scheduled handle reaches persist`` () =
    let persist = filePersist ()
    let seen = TaskCompletionSource<Result<CoreChangesAccepted, string>>()
    let childId = NodeId.New()
    let change =
        { id = 0
          submissionId = Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, "from-actor")
              Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }
    withPersist persist (fun host pool -> task {
        let! started =
            startRootActor host pool (fun _ coreChanges -> async {
                let! result = coreChanges.postChange [ change ]
                seen.TrySetResult result |> ignore
            })
            |> Async.StartAsTask
        requireOk "startActor" started
        let! posted = seen.Task.WaitAsync(TimeSpan.FromSeconds 5.0)
        let accepted = requireOk "actor post" posted
        Assert.Equal(Revision 1, accepted.revision)
    })

module Gambol.Server.Tests.ActorCoreChangesDoorTests

open System
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

/// Seam: Actor CoreChangesDoor is the CoreDoor public door.

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private sampleRequest: Gambol.Shared.ActorStart =
    { zoomId = Graph.rootId
      focusId = Graph.rootId
      runDefinitionId = Graph.rootId
      graphIds = [ Graph.rootId ]
      eventId = EventId.zero }

let private withPersist persist body =
    task {
        let pool = CoreActorPool.create ()
        let host = CoreMailbox.host pool persist admittedCredentials
        try
            do! body host pool
        finally
            CoreMailboxApi.dispose host
    }

let private filePersist () =
    FileAgent.persist (FileAgent.create (newTempDir ()))

let private startRootActor
    (host: MailboxHost)
    (pool: CoreActorPool)
    (actor: Gambol.Shared.ActorFunction)
    =
    let placement : ActorOrderFromStart.FunctionActorPlacement =
        { request = sampleRequest
          actor = actor
          actorName = "root"
          actorNodeIdOf = fun start -> start.focusId }
    CoreMailboxApi.coreMailboxStartActorFromFunction
        host
        testCaller
        placement

[<Fact>]
let ``Actor getEventId surfaces persist error instead of Revision 0`` () =
    let persist = filePersist ()
    let filling =
        { persist with
            handlers =
                { persist.handlers with
                    graphAtEvent =
                        fun () -> Error "revision unavailable" } }
    let seen = TaskCompletionSource<Result<unit, string>>()
    withPersist filling (fun host pool -> task {
        let! started =
            startRootActor host pool (fun _ changes -> async {
                let! result = changes.graphAtEvent ()
                let reported =
                    match result with
                    | Ok _ -> Ok ()
                    | Error err -> Error err
                seen.TrySetResult reported |> ignore
            })
            |> Async.StartAsTask
        requireOk "startActor" started |> ignore
        let! observed = seen.Task.WaitAsync(TimeSpan.FromSeconds 5.0)
        Assert.Equal(Error "revision unavailable", observed)
    })

[<Fact>]
let ``Actor postChange on scheduled handle reaches persist`` () =
    let persist = filePersist ()
    let seen = TaskCompletionSource<Result<EventId, string>>()
    let childId = NodeId.New()
    let event =
        changeEvent
            ""
            EventId.zero
            (Guid.NewGuid())
            [ Op.NewNode(childId, "from-actor")
              Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ]
    withPersist persist (fun host pool -> task {
        let! started =
            startRootActor host pool (fun _ changes -> async {
                let! posted = changes.postChanges [ event ]
                match posted with
                | Error err ->
                    seen.TrySetResult(Error err) |> ignore
                | Ok () ->
                    let! atEvent = changes.graphAtEvent ()
                    let id =
                        Result.map (fun state -> state.eventId) atEvent
                    seen.TrySetResult id |> ignore
            })
            |> Async.StartAsTask
        requireOk "startActor" started |> ignore
        let! posted = seen.Task.WaitAsync(TimeSpan.FromSeconds 5.0)
        let eventId = requireOk "actor post" posted
        Assert.True(EventId.isAccepted eventId)
    })

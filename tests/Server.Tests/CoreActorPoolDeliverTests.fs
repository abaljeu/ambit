module Gambol.Server.Tests.CoreActorPoolDeliverTests

open System
open System.Threading
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

let private hangActor (session: string ref) : ActorFn =
    fun input _ ->
        async {
            session := input.sessionId
            do! Async.Sleep 60_000
        }

let private withPool body =
    task {
        let dataDir = newTempDir ()
        let pool = CoreActorPool.create ()
        pool.register (ActorName "root") (hangActor (ref ""))
        let host =
            CoreMailbox.host
                pool
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
        try
            do! body host pool
        finally
            CoreMailbox.dispose host
    }

let private startAt host focusId commandId graphIds =
    CoreMailbox.startActor
        host
        testCaller
        { zoomId = Graph.rootId
          focusId = focusId
          commandId = commandId
          graphIds = graphIds
          eventId = EventId.zero }

[<Fact>]
let ``mint deliver enqueue drop then deliver fails`` () =
    let session = ref ""
    let dataDir = newTempDir ()
    let pool = CoreActorPool.create ()
    pool.register (ActorName "root") (hangActor session)
    let host =
        CoreMailbox.host
            pool
            (FileAgent.persist (FileAgent.create dataDir))
            admittedCredentials
    try
        let started =
            CoreMailbox.startActor
                host
                testCaller
                { zoomId = Graph.rootId
                  focusId = Graph.rootId
                  commandId = Graph.rootId
                  graphIds = [ Graph.rootId ]
                  eventId = EventId.zero }
            |> Async.RunSynchronously
        requireOk "start" started
        let deadline = DateTime.UtcNow.AddSeconds 2.0
        while !session = "" && DateTime.UtcNow < deadline do
            Thread.Sleep 10
        Assert.False(String.IsNullOrEmpty !session)
        requireOk "deliver" (pool.deliver (!session, "chunk"))
        match pool.takeInbox !session with
        | Ok texts ->
            Assert.Equal<string list>([ "chunk" ], texts)
        | Error err -> Assert.Fail($"inbox: {err}")
        match pool.trySecretForFocus Graph.rootId with
        | None -> Assert.Fail("missing live secret")
        | Some secret -> pool.drop secret
        match pool.deliver (!session, "late") with
        | Error "not live" -> ()
        | other -> Assert.Fail($"drop deliver: {other}")
    finally
        CoreMailbox.dispose host

[<Fact>]
let ``second start on the same commandId is rejected`` () =
    withPool (fun host pool -> task {
        let childId = NodeId.New()
        let event =
            { id = EventId.zero
              submissionId = Guid.NewGuid()
              authority = Authority "Browser"
              commandName = ""
              body =
                EventBody.Change
                    [ Op.NewNode(childId, "focus-2")
                      Op.Replace(
                          Graph.rootId,
                          [],
                          [ ChildNode.owner childId ]) ] }
        let! posted =
            CoreMailbox.postGraphOnly host testCaller event
            |> Async.StartAsTask
        requireOk "child" posted |> ignore
        let! first =
            startAt
                host Graph.rootId Graph.rootId [ Graph.rootId; childId ]
            |> Async.StartAsTask
        requireOk "first" first
        let! second =
            startAt
                host childId Graph.rootId [ Graph.rootId; childId ]
            |> Async.StartAsTask
        match second with
        | Error msg ->
            Assert.Contains(
                "command already has a live Actor", msg)
        | Ok _ -> Assert.Fail("expected command exclusivity")
        Assert.True(
            Set.contains Graph.rootId (pool.liveFocusIds ()))
    })

[<Fact>]
let ``focus exclusivity still rejects a second live Focus`` () =
    let pool = CoreActorPool.create ()
    pool.register (ActorName "root") (hangActor (ref ""))
    requireOk
        "first"
        (pool.startActor
            { zoomId = Graph.rootId
              focusId = Graph.rootId
              commandId = Graph.rootId
              graphIds = [ Graph.rootId ]
              eventId = EventId.zero }
            (fun () -> Graph.create ()))
    |> ignore
    match
        pool.startActor
            { zoomId = Graph.rootId
              focusId = Graph.rootId
              commandId = Graph.rootId
              graphIds = [ Graph.rootId ]
              eventId = EventId.zero }
            (fun () -> Graph.create ())
    with
    | Error msg ->
        Assert.Contains("focus already has a live Actor", msg)
    | Ok _ -> Assert.Fail("expected focus exclusivity")

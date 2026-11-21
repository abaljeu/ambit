module Gambol.Server.Tests.MailboxAskTests

open System
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err -> failwith $"{label}: {err}"

let private event commandName =
    let nodeId = NodeId.New()
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = testAuthority
      commandName = commandName
      body = EventBody.Change [ Op.NewNode(nodeId, commandName) ] }

let private seededLog =
    EventLog.empty
    |> EventLog.append (event "first")
    |> EventLog.append (event "second")

[<Fact>]
let ``EventsSince Ask reads the seeded mailbox EventLog`` () = task {
    let persist = FileAgent.persist (FileAgent.create (newTempDir ()))
    let filling =
        { persist with
            handlers =
                { persist.handlers with
                    getEventsSince =
                        fun after ->
                            if after = EventId.zero then
                                Ok seededLog.events
                            else
                                Error "persistence read after seed"
                    getEventLog = fun () -> Ok seededLog } }
    let host =
        CoreMailbox.host
            (CoreActorPool.create ())
            filling
            admittedCredentials
    try
        let first =
            seededLog.events
            |> List.minBy (fun stored -> EventId.value stored.id)
        let! tail =
            CoreMailboxApi.getEventsSince host first.id
            |> Async.StartAsTask
        let! full =
            CoreMailboxApi.eventHistory host
            |> Async.StartAsTask
        Assert.Single(tail) |> ignore
        Assert.Equal("second", tail.Head.commandName)
        Assert.Equal<Ev list>(seededLog.events, full.events)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``LastEventId Ask waits for the prior order`` () = task {
    let host =
        CoreMailbox.host
            (CoreActorPool.create ())
            (FileAgent.persist (FileAgent.create (newTempDir ())))
            admittedCredentials
    try
        let! posted =
            CoreMailboxApi.postEvent host testCaller (event "ordered")
            |> Async.StartAsTask
        let stored = requireOk "postEvent" posted
        let! lastEventId =
            CoreMailboxApi.getEventId host |> Async.StartAsTask
        Assert.True(EventId.value lastEventId >= EventId.value stored.id)
    finally
        CoreMailboxApi.dispose host
}

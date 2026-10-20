module Gambol.Server.Tests.DbAgentFailureTests

open System
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private changedBody () =
    let childId = NodeId.New()
    [ {
        id = EventId.zero
        submissionId = Guid.NewGuid()
        authority = Authority "Browser"
        commandName = ""
        body = EventBody.Change
            [
                Op.NewNode(childId, "failure probe")
                Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ])
            ]
    } ]

let private freshState () : State =
    { graph = Graph.create ()
      eventId = EventId.zero }

let private host agent = admittedHostDb agent

let private getState agent = async {
    match! CoreMailboxApi.getState (host agent) with
    | Ok state -> return state
    | Error error ->
        Assert.Fail($"get state: {error}")
        return Unchecked.defaultof<_>
}

let rec private waitNotice host pred spins = task {
    match! CoreMailboxApi.getState host with
    | Error err ->
        Assert.Fail(err)
        return false
    | Ok _ ->
        let found =
            MailboxHost.loggedNotices host |> List.exists pred
        if found || spins <= 0 then
            return found
        else
            do! Task.Delay 10
            return! waitNotice host pred (spins - 1)
}

/// A throw from the live-persist step leaves the edit. The order succeeds.
[<Fact>]
let ``persistence failure leaves the edit and the mailbox survives`` () = task {
    let dataDir = newTempDir ()
    let throwingPersist : string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string> =
        fun _ _ _ _ ->
            raise (InvalidOperationException("injected persistence failure"))
    let agent =
        DbAgent.createForTestWithDependencies
            (freshState ())
            (Some dataDir)
            throwingPersist
            (fun _ -> Ok [])
    let mailbox = host agent
    let! postResult =
        CoreDoor.postEvents (admittedChanges mailbox)
            ((changedBody ()))
        |> Async.StartAsTask
    match postResult with
    | Error error -> Assert.Fail(error)
    | Ok accepted -> Assert.Equal(None, accepted.message)
    let! found =
        waitNotice
            mailbox
            (function
                | Notice.PersistFailed failure ->
                    failure.message = "injected persistence failure"
                | _ -> false)
            100
    Assert.True(found)
    let! state = getState agent |> Async.StartAsTask
    Assert.True(EventId.isAccepted state.eventId)
}

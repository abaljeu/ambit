module Gambol.Server.Tests.DbAgentFailureTests

open System
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
    match! CoreMailbox.getState (host agent) with
    | Ok state -> return state
    | Error error ->
        Assert.Fail($"get state: {error}")
        return Unchecked.defaultof<_>
}

/// A throw from the live-persist step becomes an error value. The reply is
/// that error, and the mailbox keeps serving later reads.
[<Fact>]
let ``persistence failure is returned and mailbox survives`` () = task {
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
    let! postResult =
        (admittedChanges (host agent)).postEvents
            ((changedBody ()))
        |> Async.StartAsTask
        |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
    match postResult with
    | Ok _ -> Assert.Fail("Expected persistence failure.")
    | Error error ->
        Assert.Contains("injected persistence failure", error)

    let! state =
        getState agent
        |> Async.StartAsTask
        |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
    Assert.Equal(EventId.zero, state.eventId)
}

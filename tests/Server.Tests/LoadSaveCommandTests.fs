module Gambol.Server.Tests.LoadSaveCommandTests

open System
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.HttpResults
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend
open Thoth.Json.Newtonsoft

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

let private encodeRequest request =
    EventJson.encodeLoadSaveCommandRequest request
    |> Encode.toString 0

let private request operation prePick nodeId eventId =
    { operation = operation
      prePick = prePick
      start =
        { zoomId = nodeId
          focusId = nodeId
          commandId = nodeId
          graphIds = [ Graph.rootId; nodeId ]
          eventId = eventId } }

let private requireResponse (result: IResult) =
    match box result with
    | :? ContentHttpResult as content ->
        match
            Decode.fromString
                ApiResponseSerialization.decodeLoadSaveCommandResponseDecoder
                content.ResponseContent
        with
        | Result.Ok response -> response
        | Result.Error err -> failwith err
    | other ->
        failwith $"expected JSON content, got {other.GetType().Name}"

[<Fact>]
let ``Desk request reaches pool without starting Peer Actor`` () = task {
    let host =
        CoreMailbox.host
            (CoreActorPool.create ())
            (FileAgent.persist (FileAgent.create (newTempDir ())))
            admittedCredentials
    try
        let nodeId = NodeId.New()
        let router =
            { resolvePath = fun _ _ -> Result.Ok LoadSavePath.Desk
              startCommand =
                fun path command ->
                    CoreMailbox.startLoadSaveCommand
                        host
                        testCaller
                        path
                        (PeerActorName "unregistered")
                        command }
        let body =
            request
                LoadSaveOperation.Load
                LoadSavePrePick.Desk
                nodeId
                EventId.zero
            |> encodeRequest

        let! result =
            Api.postLoadSaveCommand
                router
                (CoreMailbox.coreChanges host testCaller)
                body
            |> Async.StartAsTask

        let response = requireResponse result
        Assert.Equal(LoadSavePath.Desk, response.path)
        Assert.Equal(None, response.command)
    finally
        CoreMailbox.dispose host
}

let private seedNode host =
    let nodeId = NodeId.New()
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body =
            EventBody.Change
                [ Op.NewNode(nodeId, "load")
                  Op.Replace(
                      Graph.rootId,
                      [],
                      [ ChildNode.owner nodeId ]) ] }
    CoreMailbox.postGraphOnly host testCaller event
    |> Async.RunSynchronously
    |> function
        | Result.Ok accepted -> nodeId, accepted.eventId
        | Result.Error err -> failwith err

let private stoppingPeer : ActorFn =
    fun input changes ->
        async {
            let actorCaller =
                { authority = Authority "Actor"
                  name = ""
                  secret = input.secret }
            let! _ =
                changes.asCaller(actorCaller).actorStop ActorSucceeded
            return ()
        }

[<Fact>]
let ``Git request reaches Peer Actor through mailbox and actor pool`` () = task {
    let pool = CoreActorPool.create ()
    let peerName = PeerActorName "route-test"
    pool.registerPeer peerName stoppingPeer
    let host =
        CoreMailbox.host
            pool
            (FileAgent.persist (FileAgent.create (newTempDir ())))
            admittedCredentials
    try
        let nodeId, eventId = seedNode host
        let router =
            { resolvePath = fun _ _ -> Result.Ok LoadSavePath.Git
              startCommand =
                fun path command ->
                    Assert.Equal(LoadSavePath.Git, path)
                    Assert.Equal(
                        LoadSaveOperation.Load,
                        command.operation)
                    CoreMailbox.startLoadSaveCommand
                        host
                        testCaller
                        path
                        peerName
                        command }
        let body =
            request
                LoadSaveOperation.Load
                LoadSavePrePick.Git
                nodeId
                eventId
            |> encodeRequest

        let! result =
            Api.postLoadSaveCommand
                router
                (CoreMailbox.coreChanges host testCaller)
                body
            |> Async.StartAsTask

        let response = requireResponse result
        Assert.Equal(LoadSavePath.Git, response.path)
        Assert.True(Option.isSome response.command)
        let! history =
            CoreMailbox.eventHistory host |> Async.StartAsTask
        Assert.Contains(
            history.events,
            fun event ->
                match event.body with
                | EventBody.ActorStart start ->
                    start.focusId = nodeId
                | _ -> false)
    finally
        CoreMailbox.dispose host
}

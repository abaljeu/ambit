module Gambol.Server.Tests.CoreChangesTests

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http.HttpResults
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

/// Result-aware assertion: an Error fails the test through xUnit, not an exception.
let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private decodeChangeResponse json =
    Decode.fromString
        ApiResponseSerialization.decodeChangeSuccessResponseDecoder
        json
    |> requireOk "decode response"

let private encodePollRequest eventId want =
    Encode.toString 0 (
        ApiResponseSerialization.encodePollRequest
            { eventId = eventId; want = want })

let private addRootChild _revision text =
    let _, event = addRootChildEvent text
    { event with id = EventId.zero }

[<Fact>]
let ``typed Normal caller publishes accepted Change to Poll`` () = task {
    let dataDir = newTempDir ()
    let agent, handle = createAdmittedFile dataDir
    try
        let event = addRootChild 0 "typed caller"
        let! accepted =
            handle.postEvents [ event ]
            |> Async.StartAsTask
        let accepted = requireOk "typed post" accepted
        Assert.True(EventId.isAccepted accepted.eventId)
        Assert.Equal<Guid list>(
            [ event.submissionId ],
            accepted.events |> List.map (_.submissionId))

        let body = encodePollRequest EventId.zero [ Graph.rootId ]
        let! poll = Api.postPoll handle 10 20 body |> Async.StartAsTask
        match box poll with
        | :? ContentHttpResult as content ->
            let response = decodeChangeResponse content.ResponseContent
            Assert.Equal(accepted.eventId.Value, response.eventId.Value)
            Assert.Equal<Ev list>(accepted.events, response.events)
            Assert.True(Map.containsKey Graph.rootId response.childMap)
            Assert.NotEmpty(response.nodes)
        | other ->
            Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
    finally
        CoreMailbox.dispose agent
}

/// Thread-pool Actor: Local Graph plus full CoreChanges, off the apply mailbox.
let private runActor
    (subgraph: Graph)
    (handle: CoreChanges)
    (act: Graph -> CoreChanges -> Async<'a>)
    : Task<'a> =
    act subgraph handle |> Async.StartAsTask

let private produceFromSubgraph
    (subgraph: Graph)
    (handle: CoreChanges)
    : Async<Result<CoreChangesAccepted, string>> =
    async {
        let priorChildren =
            match Map.tryFind Graph.rootId subgraph.nodes with
            | Some _ -> Graph.children subgraph Graph.rootId
            | None -> []
        let childId = NodeId.New()
        let event =
            changeEvent
                ""
                EventId.zero
                (Guid.NewGuid())
                [ Op.NewNode(childId, "test Actor")
                  Op.Replace(
                      Graph.rootId,
                      priorChildren,
                      priorChildren @ [ ChildNode.owner childId ]) ]
        return! handle.postEvents [ event ]
    }

[<Fact>]
let ``test Actor posts Normal Change off apply mailbox and Poll sees it`` () =
    task {
        let dataDir = newTempDir ()
        let agent, handle = createAdmittedFile dataDir
        try
            let subgraph = Graph.create ()
            let! accepted =
                runActor subgraph handle produceFromSubgraph
            let accepted = requireOk "actor post" accepted
            Assert.True(EventId.isAccepted accepted.eventId)
            Assert.NotEmpty(accepted.events)

            let body = encodePollRequest EventId.zero []
            let! poll =
                Api.postPoll handle 10 20 body |> Async.StartAsTask
            match box poll with
            | :? ContentHttpResult as content ->
                let response = decodeChangeResponse content.ResponseContent
                Assert.Equal(accepted.eventId.Value, response.eventId.Value)
                Assert.Equal<Ev list>(accepted.events, response.events)
                Assert.Empty(response.nodes)
                Assert.Empty(response.childMap)
            | other ->
                Assert.Fail(
                    $"Expected ContentHttpResult, got {other.GetType().FullName}")
        finally
            CoreMailbox.dispose agent
    }

let private recordingHandle (posts: ResizeArray<Ev list>) =
    let state =
        { graph = Graph.create ()
          eventId = EventId.zero }
    let accepted events : CoreChangesAccepted =
        { eventId = EventIdFixtures.storedId 1
          events = events
          externalChanges = false
          message = None
          isReady = true }
    { getState = fun () -> async.Return(Result.Ok state)
      getEventId = fun () -> async.Return (EventId.zero)
      getEventsSince = fun _ -> async.Return []
      isReady = fun () -> true
      postEvents =
        fun events ->
            posts.Add(events)
            async.Return(Result.Ok(accepted events))
      postGraphOnly = fun _ -> async.Return(Result.Error "unused")
      actorStop = fun _ -> async.Return(Result.Error "unused")
      asCaller = fun _ -> Unchecked.defaultof<CoreChanges> }
    : CoreChanges

[<Fact>]
let ``HTTP Adapter passes typed Changes only after valid decode`` () = task {
    let posts = ResizeArray<Ev list>()
    let handle = recordingHandle posts
    let event = addRootChild 0 "adapter"
    let validBody =
        Encode.toString 0 (
            ApiResponseSerialization.encodeChangeRequest
                { events = [ event ]
                  want = [ Graph.rootId ] })

    let! result =
        Api.postEvents handle 10 20 validBody
        |> Async.StartAsTask
    let! _ =
        Api.postEvents handle 10 20 "not-json"
        |> Async.StartAsTask
    let! missingWant =
        Api.postEvents handle 10 20 """{"events":[]}"""
        |> Async.StartAsTask

    let posted = Assert.Single(posts)
    Assert.Equal<Ev list>([ event ], posted)
    match missingWant with
    | :? Microsoft.AspNetCore.Http.IStatusCodeHttpResult as status ->
        Assert.Equal(Nullable 400, status.StatusCode)
    | other ->
        Assert.Fail($"Expected status result, got {other.GetType().FullName}")
    match box result with
    | :? ContentHttpResult as content ->
        let response = decodeChangeResponse content.ResponseContent
        Assert.Equal<Ev list>([ event ], response.events)
        Assert.True(Map.containsKey Graph.rootId response.childMap)
        Assert.NotEmpty(response.nodes)
    | other ->
        Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
}

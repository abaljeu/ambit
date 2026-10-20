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
            CoreDoor.postEvents handle [ event ]
            |> Async.StartAsTask
        let accepted = requireOk "typed post" accepted
        Assert.True(EventId.isAccepted accepted)
        let! history =
            CoreMailboxApi.eventHistory agent |> Async.StartAsTask
        Assert.Equal(
            event.submissionId,
            Assert.Single(onlySubmission event.submissionId history.events)
                .submissionId)

        let body = encodePollRequest EventId.zero [ Graph.rootId ]
        let! poll = Api.postPoll handle 10 20 body |> Async.StartAsTask
        match box poll with
        | :? ContentHttpResult as content ->
            let response = decodeChangeResponse content.ResponseContent
            Assert.True(response.eventId >= accepted)
            Assert.Contains(
                response.events,
                fun item -> item.submissionId = event.submissionId)
            Assert.True(Map.containsKey Graph.rootId response.childMap)
            Assert.NotEmpty(response.nodes)
        | other ->
            Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
    finally
        CoreMailboxApi.dispose agent
}

/// Thread-pool Actor: Local Graph plus full CoreChangesDoor, off the apply mailbox.
let private runActor
    (subgraph: Graph)
    (handle: CoreChangesDoor)
    (act: Graph -> CoreChangesDoor -> Async<'a>)
    : Task<'a> =
    act subgraph handle |> Async.StartAsTask

let private produceFromSubgraph
    (subgraph: Graph)
    (handle: CoreChangesDoor)
    : Async<Result<EventId, string>> =
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
        return! CoreDoor.postEvents handle [ event ]
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
            Assert.True(EventId.isAccepted accepted)
            let! history =
                CoreMailboxApi.eventHistory agent |> Async.StartAsTask
            Assert.Contains(history.events, fun item -> item.id = accepted)

            let body = encodePollRequest EventId.zero []
            let! poll =
                Api.postPoll handle 10 20 body |> Async.StartAsTask
            match box poll with
            | :? ContentHttpResult as content ->
                let response = decodeChangeResponse content.ResponseContent
                Assert.True(response.eventId >= accepted)
                Assert.Contains(response.events, fun item -> item.id = accepted)
                Assert.Empty(response.nodes)
                Assert.Empty(response.childMap)
            | other ->
                Assert.Fail(
                    $"Expected ContentHttpResult, got {other.GetType().FullName}")
        finally
            CoreMailboxApi.dispose agent
    }

[<Fact>]
let ``HTTP Adapter passes typed Changes only after valid decode`` () = task {
    let dataDir = newTempDir ()
    let agent, handle = createAdmittedFile dataDir
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

    let! history = CoreMailboxApi.eventHistory agent |> Async.StartAsTask
    let posted =
        Assert.Single(onlySubmission event.submissionId history.events)
    Assert.Equal(event.submissionId, posted.submissionId)
    match missingWant with
    | :? Microsoft.AspNetCore.Http.IStatusCodeHttpResult as status ->
        Assert.Equal(Nullable 400, status.StatusCode)
    | other ->
        Assert.Fail($"Expected status result, got {other.GetType().FullName}")
    match box result with
    | :? ContentHttpResult as content ->
        let response = decodeChangeResponse content.ResponseContent
        Assert.Equal<Guid list>(
            [ event.submissionId ],
            response.events |> List.map _.submissionId)
        Assert.True(Map.containsKey Graph.rootId response.childMap)
        Assert.NotEmpty(response.nodes)
    | other ->
        Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
    CoreMailboxApi.dispose agent
}

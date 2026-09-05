module Gambol.Server.Tests.CoreChangesTests

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http.HttpResults
open Xunit
open Gambol.Server
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

let private addRootChild revision text =
    let childId = NodeId.New()
    { id = revision
      changeId = Guid.NewGuid()
      ops =
        [ Op.NewNode(childId, text)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }

[<Fact>]
let ``typed Normal caller publishes accepted Change to Poll`` () = task {
    let dataDir = newTempDir ()
    let agent = FileAgent.create dataDir
    try
        let handle = FileAgent.coreChanges agent
        let change = addRootChild 0 "typed caller"
        let! accepted =
            handle.postChange [ change ]
            |> Async.StartAsTask
        let accepted = requireOk "typed post" accepted
        Assert.Equal(Revision 1, accepted.revision)
        Assert.Equal<Guid list>(
            [ change.changeId ],
            accepted.changes |> List.map (_.changeId))

        let! poll = Api.getPoll handle 10 20 0 |> Async.StartAsTask
        match box poll with
        | :? ContentHttpResult as content ->
            let response = decodeChangeResponse content.ResponseContent
            Assert.Equal(accepted.revision, response.revision)
            Assert.Equal<Change list>(accepted.changes, response.changes)
        | other ->
            Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
    finally
        FileAgent.dispose agent
}

let private recordingHandle (posts: ResizeArray<Change list>) =
    let state =
        { graph = Graph.create ()
          history = History.empty
          revision = Revision 0 }
    let accepted changes : CoreChangesAccepted =
        { revision = Revision 1
          changes = changes
          externalChanges = false
          message = None
          isReady = true }
    { getState = fun () -> async.Return(Result.Ok state)
      getRevision = fun () -> async.Return state.revision
      getChangesSince = fun _ -> async.Return []
      isReady = fun () -> true
      postChange =
        fun changes ->
            posts.Add(changes)
            async.Return(Result.Ok(accepted changes))
      postGraphOnlyChange = fun _ -> async.Return(Result.Error "unused") }
    : CoreChanges

[<Fact>]
let ``HTTP Adapter passes typed Changes only after valid decode`` () = task {
    let posts = ResizeArray<Change list>()
    let handle = recordingHandle posts
    let change = addRootChild 0 "adapter"
    let validBody =
        Encode.toString 0 (
            Serialization.encodeChangeBatch
                { changes = [ change ] })

    let! _ = Api.postChange handle 10 20 validBody |> Async.StartAsTask
    let! _ = Api.postChange handle 10 20 "not-json" |> Async.StartAsTask

    let posted = Assert.Single(posts)
    Assert.Equal<Change list>([ change ], posted)
}

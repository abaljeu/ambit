module Gambol.Server.Tests.GraphOnlyChangePostTests

open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err -> failwith $"{label}: {err}"

[<Fact>]
let ``postChunks mints Ev with EventId.zero and commandName`` () =
    let posts = ResizeArray<Ev>()
    let accepted =
        CoreChanges.accepted (EventIdFixtures.storedId 1) true [] false
    let post event =
        posts.Add event
        async.Return(Ok accepted)
    GraphOnlyChangePost.postChunks
        post
        "Parse"
        [ [ Op.NewNode(NodeId.New(), "a") ]
          [ Op.NewNode(NodeId.New(), "b") ] ]
    |> Async.RunSynchronously
    |> requireOk "chunks"
    |> ignore
    Assert.Equal(2, posts.Count)
    for event in posts do
        Assert.Equal(EventId.zero, event.id)
        Assert.Equal("Parse", event.commandName)
        match event.body with
        | EventBody.Change ops -> Assert.True(ops.Length > 0)
        | _ -> failwith "expected Change Event"

module Gambol.Server.Tests.CoreCredentialsTests

open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

module Encode = Thoth.Json.Newtonsoft.Encode

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private strangerDoor () =
    let host = CoreMailbox.createFile (newTempDir ()) admittedCredentials
    let caller =
        { authority = Authority "Browser"
          name = "stranger"
          secret = Credential "stranger" }
    host, CoreDoor.create true host caller

let private readOnlyDoor () =
    let host = CoreMailbox.createFile (newTempDir ()) admittedCredentials
    host, CoreDoor.create false host testCaller

let private addRootChild text = addRootChildEvent text |> snd

[<Fact>]
let ``inactive sender is auth-refused and is not enqueued`` () = task {
    let posts = ResizeArray<Ev list>()
    let enqueue events =
        posts.Add(events)
        async.Return(Result.Error "must not enqueue")
    let event = addRootChild "refused"
    let! result =
        CoreAuth.post false enqueue [ event ] |> Async.StartAsTask
    Assert.Equal(Error CoreAuth.refuse, result)
    Assert.Equal(
        Error(CoreAdmissionError.text CoreAdmissionError.Unauthorized),
        result)
    Assert.Empty(posts)
}

[<Fact>]
let ``live credential is enqueued`` () = task {
    let posts = ResizeArray<Ev list>()
    let accepted events : CoreChangesAccepted =
        { eventId = EventIdFixtures.storedId 1
          events = events
          externalChanges = false
          isReady = true }
    let enqueue events =
        posts.Add(events)
        async.Return(Result.Ok(accepted events))
    let event = addRootChild "admitted"
    let! result =
        CoreAuth.post true enqueue [ event ] |> Async.StartAsTask
    let accepted = requireOk "admitted post" result
    Assert.Equal<Ev list>([ event ], Assert.Single(posts))
    Assert.Equal<System.Guid list>(
        [ event.submissionId ],
        accepted.events |> List.map _.submissionId)
}

[<Fact>]
let ``inactive caller is not admitted at the entry`` () =
    task {
        let host, handle = strangerDoor ()
        try
            let! admitted =
                CoreMailboxApi.isAdmitted host handle.caller
                |> Async.StartAsTask
            Assert.False(admitted)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``TCP or Database failure is not that auth refuse`` () = task {
    let unavailable =
        "Database persistence is unavailable; file fallback is read-only."
    let tcp = "connection refused"
    Assert.False(CoreAuth.isAuthRefuse unavailable)
    Assert.False(CoreAuth.isAuthRefuse tcp)
    Assert.True(CoreAuth.isAuthRefuse CoreAuth.refuse)
    let host, handle = readOnlyDoor ()
    let event = addRootChild "system"
    let body =
        Encode.toString 0 (
            ApiResponseSerialization.encodeChangeRequest
                { events = [ event ]; want = [] })
    try
        let! result =
            Api.postEvents handle 10 20 body
            |> Async.StartAsTask
        Assert.False(result.GetType().Name = "UnauthorizedHttpResult")
    finally
        CoreMailboxApi.dispose host
}

module Gambol.Server.Tests.SessionTests

open System
open System.IO
open System.IO.Pipelines
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

type private NotifyingReadStream
    (inner: Stream, entered: TaskCompletionSource<unit>) =
    inherit Stream()

    override _.CanRead = inner.CanRead
    override _.CanSeek = inner.CanSeek
    override _.CanWrite = inner.CanWrite
    override _.Length = inner.Length
    override _.Position
        with get () = inner.Position
        and set value = inner.Position <- value
    override _.Flush () = inner.Flush()
    override _.Read(buffer, offset, count) =
        entered.TrySetResult() |> ignore
        inner.Read(buffer, offset, count)
    override _.ReadAsync(buffer, offset, count, cancellationToken) =
        entered.TrySetResult() |> ignore
        inner.ReadAsync(buffer, offset, count, cancellationToken)
    override _.ReadAsync(buffer, cancellationToken) =
        entered.TrySetResult() |> ignore
        inner.ReadAsync(buffer, cancellationToken)
    override _.Seek(offset, origin) = inner.Seek(offset, origin)
    override _.SetLength(value) = inner.SetLength(value)
    override _.Write(buffer, offset, count) =
        inner.Write(buffer, offset, count)

let private requireSome label value =
    match value with
    | Some found -> found
    | None ->
        Assert.Fail(label)
        Unchecked.defaultof<_>

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error error ->
        Assert.Fail($"{label}: {error}")
        Unchecked.defaultof<_>

let private orderEvent text = {
    id = EventId.zero
    submissionId = Guid.NewGuid()
    authority = testAuthority
    commandName = text
    body = EventBody.Change [ Op.NewNode(NodeId.New(), text) ]
}

let private latePlace caller channel =
    Mailbox.placeForCaller
        { orderId = OrderId.New()
          authority = caller.authority
          commandName = Some "late"
          order =
              Order.Change(
                  [ Op.NewNode(NodeId.New(), "late") ],
                  ChangeOrigin.Edit) }
        caller
        channel

let private sessionOf host = MailboxHost.session host

let rec private waitUntilAuthorityGone attempts host caller = async {
    if not (Session.hasAuthority (sessionOf host) caller) then
        return ()
    elif attempts = 0 then
        return Assert.Fail("authority remained after the drop")
    else
        do! Async.Sleep 1
        return! waitUntilAuthorityGone (attempts - 1) host caller
}

let private devRuntime dataDir =
    CoreRuntime.create
        {
            DbStatus = DatabaseSetup.DbStatus.Absent
            DbConnectionString = ""
            DataDir = dataDir
            AuthUser = "alice"
            AuthPass = "secret"
            Actors = []
        }
        ignore

let private devPersistence dataDir runtime = {
    DataDir = dataDir
    DbStatus = DatabaseSetup.DbStatus.Absent
    Core = runtime
}

let private cookieRequest () =
    let context = DefaultHttpContext()
    context.Request.Headers.Cookie <-
        AuthToken.cookieHeaderValue "alice" "secret"
    context

[<Fact>]
let ``login opens admission without an Ask`` () =
    let session = Session.create CoreCredentials.empty
    Session.login session testCaller |> requireOk "login"
    let route =
        Session.beginAdmittedRoute session testCaller
        |> requireSome "login did not open admission"
    Session.finishAdmittedRoute route
    Assert.True(Session.admissionIsOpen session testCaller)
    Assert.True(Session.hasAuthority session testCaller)

[<Fact>]
let ``login sets the cookie only after Session holds the authority`` () = task {
    let host =
        CoreMailbox.host
            (CoreActorPool.create ())
            (FileAgent.persist (FileAgent.create (newTempDir ())))
            CoreCredentials.empty
    let expected = browserCallerFromAuth "alice" "secret"
    let cookieSet = ref false
    let auth = {
        ExpectedUser = "alice"
        ExpectedPass = "secret"
        GitToken = ""
        IsAuthenticated = fun _ -> false
        IsGitAuthenticated = fun _ -> false
        SetCookie =
            fun _ ->
                Assert.True(Session.hasAuthority (sessionOf host) expected)
                cookieSet.Value <- true
        ClearCookie = ignore
    }
    try
        let response = DefaultHttpContext().Response
        let! result =
            RouteAuthentication.loginThenSetCookie host auth response
            |> Async.StartAsTask
        requireOk "loginThenSetCookie" result
        Assert.True(cookieSet.Value)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``stop admitting refuses a later route and waits for the open one`` () = task {
    let session = Session.create admittedCredentials
    let route =
        Session.beginAdmittedRoute session testCaller
        |> requireSome "seeded caller was not admitted"
    let stopped =
        Session.stopAdmitting session testCaller
        |> requireSome "stop admitting"
    let waiting =
        Session.waitForAdmittedRoutes stopped |> Async.StartAsTask
    Assert.False(Session.admissionIsOpen session testCaller)
    Assert.True(Session.hasAuthority session testCaller)
    Assert.False(waiting.IsCompleted)
    Session.finishAdmittedRoute route
    do! waiting
    Assert.True(Session.hasAuthority session testCaller)
}

[<Fact>]
let ``duplicate login keeps the other admitted route open`` () = task {
    let session = Session.create admittedCredentials
    let first =
        Session.beginAdmittedRoute session testCaller
        |> requireSome "first admission"
    let second =
        Session.beginAdmittedRoute session testCaller
        |> requireSome "second admission"
    Session.login session testCaller |> requireOk "duplicate login"
    let stopped =
        Session.stopAdmitting session testCaller
        |> requireSome "stop admitting"
    let waiting =
        Session.waitForAdmittedRoutes stopped |> Async.StartAsTask
    Session.finishAdmittedRoute first
    Session.finishAdmittedRoute first
    Assert.False(waiting.IsCompleted)
    Session.finishAdmittedRoute second
    do! waiting
}

type private OrderGate = {
    entered: TaskCompletionSource<unit>
    release: TaskCompletionSource<unit>
}

let private newOrderGate () = {
    entered =
        TaskCompletionSource<unit>(
            TaskCreationOptions.RunContinuationsAsynchronously)
    release =
        TaskCompletionSource<unit>(
            TaskCreationOptions.RunContinuationsAsynchronously)
}

let private gatingAppend
    (inner: Ev -> Result<unit, string>)
    (gate: OrderGate)
    (event: Ev)
    =
    gate.entered.TrySetResult() |> ignore
    gate.release.Task.Wait()
    inner event

let private hostGated until (gate: OrderGate) =
    let persist = FileAgent.persist (FileAgent.create (newTempDir ()))
    let filling = {
        persist with
            until = until
            handlers = {
                persist.handlers with
                    appendEvent = gatingAppend persist.handlers.appendEvent gate
            }
    }
    CoreMailbox.host (CoreActorPool.create ()) filling admittedCredentials

let private releaseGate (gate: OrderGate) =
    gate.release.TrySetResult() |> ignore

[<Fact>]
let ``logout returns before the queued order finishes`` () = task {
    let gate = newOrderGate ()
    let host = hostGated None gate
    try
        let posting =
            CoreMailboxApi.postEvent
                host
                testCaller
                (orderEvent "before drop")
            |> Async.StartAsTask
        do! gate.entered.Task
        do! CoreMailboxApi.logout host testCaller |> Async.StartAsTask
        Assert.False(Session.admissionIsOpen (sessionOf host) testCaller)
        Assert.True(Session.hasAuthority (sessionOf host) testCaller)
        releaseGate gate
        let! posted = posting
        let stored = requireOk "postEvent" posted
        do! waitUntilAuthorityGone 1000 host testCaller |> Async.StartAsTask
        let! last =
            CoreMailboxApi.getEventId host |> Async.StartAsTask
        Assert.True(EventId.value last >= EventId.value stored.id)
    finally
        releaseGate gate
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``logout during startup drains queued orders before the authority drops`` () =
    task {
    let gate = newOrderGate ()
    let releasePrelude =
        TaskCompletionSource<Result<unit, string>>(
            TaskCreationOptions.RunContinuationsAsynchronously)
    let host =
        hostGated (Some (releasePrelude.Task |> Async.AwaitTask)) gate
    try
        let posting =
            CoreMailboxApi.postEvent
                host
                testCaller
                (orderEvent "during prelude")
            |> Async.StartImmediateAsTask
        do! CoreMailboxApi.logout host testCaller |> Async.StartAsTask
        do! Task.Delay(200)
        let! during =
            CoreMailboxApi.getEventId host |> Async.StartAsTask
        Assert.Equal(EventId.zero, during)
        Assert.False(gate.entered.Task.IsCompleted)
        Assert.False(posting.IsCompleted)
        Assert.True(Session.hasAuthority (sessionOf host) testCaller)
        releasePrelude.TrySetResult(Ok ()) |> ignore
        do! gate.entered.Task
        Assert.True(Session.hasAuthority (sessionOf host) testCaller)
        releaseGate gate
        let! posted = posting
        let stored = requireOk "prelude order" posted
        do! waitUntilAuthorityGone 1000 host testCaller |> Async.StartAsTask
        let! last =
            CoreMailboxApi.getEventId host |> Async.StartAsTask
        Assert.True(EventId.value last >= EventId.value stored.id)
    finally
        releasePrelude.TrySetResult(Ok ()) |> ignore
        releaseGate gate
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``a later login survives removal of the closed generation`` () = task {
    let session = Session.create admittedCredentials
    let first =
        Session.stopAdmitting session testCaller
        |> requireSome "first close"
    let second =
        Session.stopAdmitting session testCaller
        |> requireSome "second close"
    do! Session.waitForAdmittedRoutes first
    Session.removeAuthority first
    Session.login session testCaller |> requireOk "new login"
    do! Session.waitForAdmittedRoutes second
    Session.removeAuthority second
    Assert.True(Session.admissionIsOpen session testCaller)
    Assert.True(Session.hasAuthority session testCaller)
}

[<Fact>]
let ``login during logout lets the open route finish its wait`` () = task {
    let session = Session.create admittedCredentials
    let route =
        Session.beginAdmittedRoute session testCaller
        |> requireSome "admitted route"
    let stopped =
        Session.stopAdmitting session testCaller
        |> requireSome "stop admitting"
    let waiting =
        Session.waitForAdmittedRoutes stopped |> Async.StartAsTask
    Session.login session testCaller |> requireOk "login during logout"
    Assert.False(waiting.IsCompleted)
    Assert.True(Session.admissionIsOpen session testCaller)
    Session.finishAdmittedRoute route
    do! waiting
    Session.removeAuthority stopped
    Assert.True(Session.admissionIsOpen session testCaller)
    Assert.True(Session.hasAuthority session testCaller)
}

[<Fact>]
let ``login during a pending logout runs and a dropped generation rejects a late order`` () =
    task {
    let gate = newOrderGate ()
    let releasePrelude =
        TaskCompletionSource<Result<unit, string>>(
            TaskCreationOptions.RunContinuationsAsynchronously)
    let host =
        hostGated (Some (releasePrelude.Task |> Async.AwaitTask)) gate
    try
        do! CoreMailboxApi.logout host testCaller |> Async.StartAsTask
        Assert.False(Session.admissionIsOpen (sessionOf host) testCaller)
        Session.login (sessionOf host) testCaller
        |> requireOk "login during logout"
        Assert.True(Session.admissionIsOpen (sessionOf host) testCaller)
        let posting =
            CoreMailboxApi.postEvent
                host
                testCaller
                (orderEvent "new generation")
            |> Async.StartImmediateAsTask
        releasePrelude.TrySetResult(Ok ()) |> ignore
        do! gate.entered.Task
        Assert.True(Session.hasAuthority (sessionOf host) testCaller)
        releaseGate gate
        let! posted = posting
        let stored = requireOk "new generation order" posted
        do! CoreMailboxApi.logout host testCaller |> Async.StartAsTask
        do! waitUntilAuthorityGone 1000 host testCaller |> Async.StartAsTask
        let! late =
            MailboxHost.postMailboxAndAsyncReply
                host
                (latePlace testCaller)
        Assert.Equal(Error "authority dropped", late)
        let! last =
            CoreMailboxApi.getEventId host |> Async.StartAsTask
        Assert.True(EventId.value last >= EventId.value stored.id)
    finally
        releasePrelude.TrySetResult(Ok ()) |> ignore
        releaseGate gate
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``logout removes authority while persist reads fail`` () = task {
    let host =
        hostCanned
            (CoreActorPool.create ())
            (fun () -> Error "read failed")
            (fun _ -> [])
            (fun _ _ -> Error "unused")
    try
        do! CoreMailboxApi.logout host testCaller |> Async.StartAsTask
        do! waitUntilAuthorityGone 1000 host testCaller |> Async.StartAsTask
        Session.login (sessionOf host) testCaller |> requireOk "relogin"
        Assert.True(Session.admissionIsOpen (sessionOf host) testCaller)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``cancelled drain keeps authority until the barrier replies`` () = task {
    let session = Session.create admittedCredentials
    let route =
        Session.beginAdmittedRoute session testCaller
        |> requireSome "admitted route"
    let stopped =
        Session.stopAdmitting session testCaller
        |> requireSome "stop admitting"
    use cancellation = new CancellationTokenSource()
    let cancelled =
        TaskCompletionSource<unit>(
            TaskCreationOptions.RunContinuationsAsynchronously)
    Async.StartWithContinuations(
        Session.waitForAdmittedRoutes stopped,
        (fun () -> Assert.Fail("admitted routes drained")),
        (fun error ->
            match error with
            | :? TaskCanceledException -> cancelled.SetResult()
            | _ -> cancelled.SetException(error)),
        (fun _ -> cancelled.SetResult()),
        cancellationToken = cancellation.Token)
    Assert.False(Session.admissionIsOpen session testCaller)
    cancellation.Cancel()
    do! cancelled.Task
    Assert.True(Session.hasAuthority session testCaller)
    Session.finishAdmittedRoute route
    Session.login session testCaller |> requireOk "login during logout"
    Assert.True(Session.admissionIsOpen session testCaller)
    Session.removeAuthority stopped
    Assert.True(Session.hasAuthority session testCaller)
    Assert.True(Session.admissionIsOpen session testCaller)
}

[<Fact>]
let ``logout returns while the reconciliation route is open`` () = task {
    let dataDir = newTempDir ()
    let runtime = devRuntime dataDir
    let persistence = devPersistence dataDir runtime
    let caller = browserCallerFromAuth "alice" "secret"
    let entered = TaskCompletionSource<unit>()
    let release = TaskCompletionSource<unit>()
    let postedId = TaskCompletionSource<EventId>()
    let context = cookieRequest ()
    try
        let reconciling =
            RouteRegistration.withBrowserReconciliation
                persistence
                context.Request
                (fun handle -> async {
                    entered.SetResult()
                    do! release.Task |> Async.AwaitTask
                    let! posted =
                        CoreMailboxApi.postEvent
                            handle.host
                            handle.caller
                            (orderEvent "reconcile")
                    let stored = requireOk "reconcile post" posted
                    postedId.SetResult(stored.id)
                    return Results.Ok()
                })
            |> Async.StartAsTask
        do! entered.Task
        do! CoreMailboxApi.logout runtime.host caller |> Async.StartAsTask
        Assert.False(Session.admissionIsOpen (sessionOf runtime.host) caller)
        release.SetResult()
        let! _ = reconciling
        do!
            waitUntilAuthorityGone 1000 runtime.host caller
            |> Async.StartAsTask
        let! last =
            CoreMailboxApi.getEventId runtime.host |> Async.StartAsTask
        Assert.True(
            EventId.value last >= EventId.value postedId.Task.Result)
    finally
        CoreMailboxApi.dispose runtime.host
}

[<Fact>]
let ``admitted route order after DropAuthority is rejected`` () = task {
    let dataDir = newTempDir ()
    let runtime = devRuntime dataDir
    let persistence = devPersistence dataDir runtime
    let caller = browserCallerFromAuth "alice" "secret"
    let entered = TaskCompletionSource<unit>()
    let release = TaskCompletionSource<unit>()
    let context = cookieRequest ()
    try
        let reconciling =
            RouteRegistration.withBrowserReconciliation
                persistence
                context.Request
                (fun _ -> async {
                    entered.SetResult()
                    do! release.Task |> Async.AwaitTask
                    let! posted =
                        MailboxHost.postMailboxAndAsyncReply
                            runtime.host
                            (latePlace caller)
                    Assert.Equal(Error "authority dropped", posted)
                    return Results.Ok()
                })
            |> Async.StartAsTask
        do! entered.Task
        do! CoreMailboxApi.logout runtime.host caller |> Async.StartAsTask
        do!
            waitUntilAuthorityGone 1000 runtime.host caller
            |> Async.StartAsTask
        release.SetResult()
        let! _ = reconciling
        let! last =
            CoreMailboxApi.getEventId runtime.host |> Async.StartAsTask
        Assert.Equal(EventId.zero, last)
        let! history =
            CoreMailboxApi.eventHistory runtime.host |> Async.StartAsTask
        Assert.Empty(history.events)
    finally
        CoreMailboxApi.dispose runtime.host
}

[<Fact>]
let ``logout returns while the reconciliation route abandons`` () = task {
    let dataDir = newTempDir ()
    let runtime = devRuntime dataDir
    let persistence = devPersistence dataDir runtime
    let caller = browserCallerFromAuth "alice" "secret"
    let entered = TaskCompletionSource<unit>()
    let release = TaskCompletionSource<unit>()
    let context = cookieRequest ()
    try
        let reconciling =
            RouteRegistration.withBrowserReconciliation
                persistence
                context.Request
                (fun _ -> async {
                    entered.SetResult()
                    do! release.Task |> Async.AwaitTask
                    return Results.BadRequest()
                })
            |> Async.StartAsTask
        do! entered.Task
        do! CoreMailboxApi.logout runtime.host caller |> Async.StartAsTask
        Assert.False(Session.admissionIsOpen (sessionOf runtime.host) caller)
        release.SetResult()
        let! _ = reconciling
        do!
            waitUntilAuthorityGone 1000 runtime.host caller
            |> Async.StartAsTask
        let! last =
            CoreMailboxApi.getEventId runtime.host |> Async.StartAsTask
        Assert.Equal(EventId.zero, last)
    finally
        CoreMailboxApi.dispose runtime.host
}

[<Fact>]
let ``parse body read holds admission until the route finishes`` () = task {
    let dataDir = newTempDir ()
    let runtime = devRuntime dataDir
    let persistence = devPersistence dataDir runtime
    let caller = browserCallerFromAuth "alice" "secret"
    let entered = TaskCompletionSource<unit>()
    let pipe = Pipe()
    let context = cookieRequest ()
    context.Request.Body <-
        new NotifyingReadStream(pipe.Reader.AsStream(), entered)
    try
        let parsing =
            RouteRegistration.postParseRequest persistence context.Request
        do! entered.Task
        let session = sessionOf runtime.host
        let stopped =
            Session.stopAdmitting session caller
            |> requireSome "stop admitting"
        let waiting =
            Session.waitForAdmittedRoutes stopped |> Async.StartAsTask
        Assert.False(waiting.IsCompleted)
        let! _ =
            pipe.Writer.WriteAsync(ReadOnlyMemory([| byte '{' |])).AsTask()
        do! pipe.Writer.CompleteAsync().AsTask()
        let! _ = parsing
        do! waiting
        Assert.True(Session.hasAuthority session caller)
        do! CoreMailboxApi.logout runtime.host caller |> Async.StartAsTask
        do!
            waitUntilAuthorityGone 1000 runtime.host caller
            |> Async.StartAsTask
    finally
        CoreMailboxApi.dispose runtime.host
}

[<Fact>]
let ``save preparation holds admission until the handoff finishes`` () = task {
    let dataDir = newTempDir ()
    GitSave.runGit dataDir "init" |> requireOk "initialize repository" |> ignore
    let runtime = devRuntime dataDir
    let persistence = devPersistence dataDir runtime
    let caller = browserCallerFromAuth "alice" "secret"
    let entered = TaskCompletionSource<unit>()
    let release = TaskCompletionSource<unit>()
    let prepare () = async {
        entered.SetResult()
        do! release.Task |> Async.AwaitTask
        return Error "stop before git"
    }
    let context = cookieRequest ()
    try
        let saving =
            RouteRegistration.postSaveRequest persistence prepare context.Request
        do! entered.Task
        let session = sessionOf runtime.host
        let stopped =
            Session.stopAdmitting session caller
            |> requireSome "stop admitting"
        let waiting =
            Session.waitForAdmittedRoutes stopped |> Async.StartAsTask
        Assert.False(waiting.IsCompleted)
        release.SetResult()
        let! _ = saving
        do! waiting
        do! CoreMailboxApi.logout runtime.host caller |> Async.StartAsTask
        do!
            waitUntilAuthorityGone 1000 runtime.host caller
            |> Async.StartAsTask
    finally
        CoreMailboxApi.dispose runtime.host
}

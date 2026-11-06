module Gambol.Server.Tests.CancelByFocusTests

open System.Threading.Tasks
open Microsoft.AspNetCore.Http.HttpResults
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.AskCancelHarness
open Gambol.Server.Tests.TestBackend
open Thoth.Json.Newtonsoft

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

let private decodeUniversal json =
    Decode.fromString
        ApiResponseSerialization.decodeUniversalResponseDecoder
        json

let private actorCaller secret : Caller =
    { authority = Authority "Actor"
      name = ""
      secret = secret }

let private cancelledStops (log: EventLog) focusId =
    log.events
    |> List.filter (fun event ->
        match event.body with
        | EventBody.Notice (Notice.ActorStopped (fid, ActorCancelled))
            when fid = focusId -> true
        | _ -> false)

[<Collection("Agent ask runner")>]
type CancelByFocusTests() =

    [<Fact>]
    member _.``cancel by Focus accepts Cancelled and drops the live row``
        ()
        =
        let hangStarted, hang = hangUntilCancel ()
        withFake hang (fun () ->
            withHost (fun host pool -> task {
                let! request = startLiveAsk host pool "?ai"
                do! awaitHang hangStarted
                do! cancelFocus host request.focusId
                do! expectActorCancelled host pool request.focusId
                do! expectOwnedTexts
                        host
                        request.focusId
                        [ "?ai"; "visible-context" ]
                let! sawCancel = waitFakeCancelled 2000
                Assert.True(sawCancel)
            }))

    [<Fact>]
    member _.``Cancel before Change rejects later Agent replace``() =
        let hangStarted, hang = hangUntilCancel ()
        withFake hang (fun () ->
            withHost (fun host pool -> task {
                let! request = startLiveAsk host pool "?ai"
                do! awaitHang hangStarted
                do! cancelFocus host request.focusId
                do! expectActorCancelled host pool request.focusId
                do! Task.Delay 200
                do! expectOwnedTexts
                        host
                        request.focusId
                        [ "?ai"; "visible-context" ]
                do! expectChangeCount host 1
            }))

    [<Fact>]
    member _.``Change before Cancel keeps the accepted children``() =
        let posted, probe = postChildThenWait "kept-change"
        withHost (fun host pool -> task {
            registerActor pool "probe" probe
            let! actorNodeId = seedCommand host "?probe"
            let! request = startOnCommand host actorNodeId
            do! awaitHang posted
            do! cancelFocus host request.focusId
            do! expectActorCancelled host pool request.focusId
            do! expectOwnedTexts
                    host
                    request.focusId
                    [ "kept-change" ]
        })

    [<Fact>]
    member _.``late duplicate completion after cancel is ignored``() =
        let hangStarted, hang = hangUntilCancel ()
        withFake hang (fun () ->
            withHost (fun host pool -> task {
                let! request = startLiveAsk host pool "?ai"
                do! awaitHang hangStarted
                do! cancelFocus host request.focusId
                do! cancelFocus host request.focusId
                do! expectActorCancelled host pool request.focusId
                do! Task.Delay 200
                let! stops = actorStopCount host request.focusId
                Assert.Equal(1, stops)
            }))

    [<Fact>]
    member _.``posted Cancel cancels a hanging Focus by NodeId``() =
        let hangStarted, hang = hangUntilCancel ()
        withFake hang (fun () ->
            withHost (fun host pool -> task {
                let! request = startLiveAsk host pool "?ai"
                do! awaitHang hangStarted
                let cancel =
                    { id = EventId.zero
                      submissionId = System.Guid.NewGuid()
                      authority = Authority "Browser"
                      commandName = "Cancel"
                      body = EventBody.Cancel request.focusId }
                let! result =
                    CoreMailboxApi.postEvents host testCaller [ cancel ]
                    |> Async.StartAsTask
                let accepted =
                    match result with
                    | Ok value -> value
                    | Error err -> failwith err
                let cancelled =
                    accepted.events
                    |> List.exists (fun event ->
                        match event.body with
                        | EventBody.ActorStop(fid, ActorCancelled)
                            when fid = request.focusId -> true
                        | _ -> false)
                Assert.True(cancelled)
                do! expectActorCancelled host pool request.focusId
                let! sawCancel = waitFakeCancelled 2000
                Assert.True(sawCancel)
            }))

    [<Fact>]
    member _.``actorStop after cancel does not emit a root Focus ActorStop``
        ()
        =
        let hangStarted, hang = hangUntilCancel ()
        withFake hang (fun () ->
            withHost (fun host pool -> task {
                let! request = startLiveAsk host pool "?ai"
                do! awaitHang hangStarted
                let secret =
                    match pool.trySecretForFocus request.focusId with
                    | Some value -> value
                    | None -> failwith "expected live secret"
                do! cancelFocus host request.focusId
                do! expectActorCancelled host pool request.focusId
                let! _ =
                    CoreMailboxApi.actorStop
                        host
                        (actorCaller secret)
                        ActorSucceeded
                    |> Async.StartAsTask
                let! rootStops = actorStopCount host Graph.rootId
                Assert.Equal(0, rootStops)
                let! stops = actorStopCount host request.focusId
                Assert.Equal(1, stops)
            }))

    [<Fact>]
    member _.``cancelByFocus places Order.CancelActor`` () =
        let hangStarted, hang = hangUntilCancel ()
        withFake hang (fun () ->
            withHost (fun host pool -> task {
                let! request = startLiveAsk host pool "?ai"
                do! awaitHang hangStarted
                let! cancelled =
                    CoreMailboxApi.cancelByFocus
                        host testCaller request.focusId
                    |> Async.StartAsTask
                match cancelled with
                | Ok () -> ()
                | Error err -> Assert.Fail(err)
                Assert.False(
                    Set.contains
                        request.focusId
                        (pool.liveFocusIds ()))
                let! history =
                    CoreMailboxApi.eventHistory host
                    |> Async.StartAsTask
                Assert.NotEmpty(
                    cancelledStops history request.focusId)
            }))

    [<Fact>]
    member _.``second Cancel for the same run logs no stop`` () =
        let hangStarted, hang = hangUntilCancel ()
        withFake hang (fun () ->
            withHost (fun host pool -> task {
                let! request = startLiveAsk host pool "?ai"
                do! awaitHang hangStarted
                do! cancelFocus host request.focusId
                let! before =
                    CoreMailboxApi.eventHistory host
                    |> Async.StartAsTask
                let firstStops =
                    cancelledStops before request.focusId
                Assert.NotEmpty(firstStops)
                let! second =
                    CoreMailboxApi.cancelByFocus
                        host testCaller request.focusId
                    |> Async.StartAsTask
                match second with
                | Ok () -> ()
                | Error err -> Assert.Fail(err)
                let! after =
                    CoreMailboxApi.eventHistory host
                    |> Async.StartAsTask
                Assert.Equal<Ev list>(
                    firstStops,
                    cancelledStops after request.focusId)
            }))

    [<Fact>]
    member _.``second Cancel on the same focus replies the same`` () =
        let hangStarted, hang = hangUntilCancel ()
        withFake hang (fun () ->
            withHost (fun host pool -> task {
                let! request = startLiveAsk host pool "?ai"
                do! awaitHang hangStarted
                let place focusId =
                    MailboxHost.postPlacedOrder host testCaller {
                        orderId = OrderId.New()
                        authority = testCaller.authority
                        commandName = None
                        order = Order.CancelActor focusId }
                    |> Async.StartAsTask
                let! first = place request.focusId
                let! before =
                    CoreMailboxApi.eventHistory host
                    |> Async.StartAsTask
                Assert.NotEmpty(
                    cancelledStops before request.focusId)
                let! second = place request.focusId
                Assert.Equal(first, second)
                let! idle = place (NodeId.New())
                Assert.Equal(first, idle)
                Assert.False(
                    Set.contains
                        request.focusId
                        (pool.liveFocusIds ()))
            }))

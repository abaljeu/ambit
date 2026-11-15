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
        withHost (fun host pool -> task {
            let! runDefinitionId = seedCommand host "?probe"
            let posted, probe =
                postChildThenWait runDefinitionId "kept-change"
            let! request =
                startFromFunction host probe runDefinitionId
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
                do! cancelFocus host request.focusId
                do! expectActorCancelled host pool request.focusId
                CoreMailboxApi.postNotice
                    host
                    (Notice.ActorStopped(
                        request.focusId,
                        ActorSucceeded))
                let! rootStops = actorStopCount host Graph.rootId
                Assert.Equal(0, rootStops)
                let! stops = actorStopCount host request.focusId
                Assert.Equal(1, stops)
            }))

    [<Fact>]
    member _.``posted Cancel replies the stored stop id`` () =
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
                let post () =
                    CoreMailboxApi.postEvents
                        host testCaller [ cancel ]
                    |> Async.StartAsTask
                let! first = post ()
                let accepted =
                    match first with
                    | Ok value -> value
                    | Error err -> failwith err
                let! history =
                    CoreMailboxApi.eventHistory host
                    |> Async.StartAsTask
                let stored = Assert.Single(cancelledStops history request.focusId)
                Assert.Equal(stored.id, accepted.eventId)
                let! second = post ()
                let again =
                    match second with
                    | Ok value -> value
                    | Error err -> failwith err
                Assert.Equal(stored.id, again.eventId)
                do! expectActorCancelled host pool request.focusId
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
                    CoreMailboxApi.postEvents host testCaller [
                        { id = EventId.zero
                          submissionId = System.Guid.NewGuid()
                          authority = Authority "Browser"
                          commandName = "Cancel"
                          body =
                            EventBody.Cancel request.focusId } ]
                    |> Async.StartAsTask
                match second with
                | Ok _ -> ()
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
                let orderId = OrderId.New()
                let place orderId focusId =
                    MailboxHost.postPlacedOrder host testCaller {
                        orderId = orderId
                        authority = testCaller.authority
                        commandName = None
                        order = Order.CancelActor focusId }
                    |> Async.StartAsTask
                let! first = place orderId request.focusId
                let! before =
                    CoreMailboxApi.eventHistory host
                    |> Async.StartAsTask
                let stored =
                    Assert.Single(
                        cancelledStops before request.focusId)
                match first with
                | Ok id -> Assert.Equal(stored.id, id)
                | Error err -> Assert.Fail(err)
                let! second = place orderId request.focusId
                Assert.Equal(first, second)
                let! idle = place (OrderId.New()) (NodeId.New())
                match idle with
                | Ok id -> Assert.Equal(EventId.zero, id)
                | Error err -> Assert.Fail(err)
                Assert.False(
                    Set.contains
                        request.focusId
                        (pool.liveActorNodeIds ()))
            }))

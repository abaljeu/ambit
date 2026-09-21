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
            let! commandId = seedCommand host "?probe"
            let! request = startOnCommand host commandId
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
    member _.``Adapter postCancel cancels a hanging Focus by NodeId``() =
        let hangStarted, hang = hangUntilCancel ()
        withFake hang (fun () ->
            withHost (fun host pool -> task {
                let! request = startLiveAsk host pool "?ai"
                do! awaitHang hangStarted
                let body =
                    Encode.toString 0 (
                        EventJson.encodeCancelRequest request.focusId)
                let! result =
                    Api.postCancel
                        (fun focusId ->
                            CoreMailbox.cancelByFocus
                                host testCaller focusId)
                        body
                    |> Async.StartAsTask
                match box result with
                | :? ContentHttpResult as content ->
                    Assert.Contains("\"ok\":true", content.ResponseContent)
                | other ->
                    failwith
                        $"expected JSON content, got {other.GetType().Name}"
                do! expectActorCancelled host pool request.focusId
                let! sawCancel = waitFakeCancelled 2000
                Assert.True(sawCancel)
            }))

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
                        EventJson.encodeCancelRequest
                            { focusId = request.focusId
                              eventId = EventId.zero })
                let! result =
                    Api.postCancel
                        (fun focusId ->
                            CoreMailbox.cancelByFocus
                                host testCaller focusId)
                        (CoreMailbox.coreChanges host testCaller)
                        body
                    |> Async.StartAsTask
                match box result with
                | :? ContentHttpResult as content ->
                    match decodeUniversal content.ResponseContent with
                    | Error err -> failwith err
                    | Ok (response: UniversalResponse) ->
                        let cancelled =
                            response.events
                            |> List.exists (fun event ->
                                match event.body with
                                | EventBody.ActorStop(fid, ActorCancelled)
                                    when fid = request.focusId -> true
                                | _ -> false)
                        Assert.True(cancelled)
                | other ->
                    failwith
                        $"expected JSON content, got {other.GetType().Name}"
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
                    CoreMailbox.actorStop
                        host
                        (actorCaller secret)
                        ActorSucceeded
                    |> Async.StartAsTask
                let! rootStops = actorStopCount host Graph.rootId
                Assert.Equal(0, rootStops)
                let! stops = actorStopCount host request.focusId
                Assert.Equal(1, stops)
            }))

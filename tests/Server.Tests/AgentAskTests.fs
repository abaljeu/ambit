module Gambol.Server.Tests.AgentAskTests

open System.Threading
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend
open Gambol.Server.Tests.AskCancelHarness

[<CollectionDefinition("Agent ask runner", DisableParallelization = true)>]
type AgentAskRunnerCollection() =
    class
    end

[<Collection("Agent ask runner")>]
type AgentAskTests() =

    [<Fact>]
    member _.``Ask with fake completion replaces Focus Children through Core Change``
        ()
        =
        withFake
            (fun args ->
                Assert.Contains("visible-context", args.Prompt)
                Assert.Contains("<node", args.Prompt)
                Assert.Contains("class=\"prompt\"", args.Prompt)
                Assert.Contains(
                    "XML Zoom-rooted extract", args.Prompt)
                Assert.Contains("css class prompt", args.Prompt)
                Assert.DoesNotContain("mixed Amb", args.Prompt)
                Assert.DoesNotContain("Focus marked", args.Prompt)
                Assert.DoesNotContain("codec text", args.Prompt)
                Assert.DoesNotContain("owning-codec", args.Prompt)
                fakeReply "from-agent")
            (fun () ->
                withHost (fun host pool -> task {
                    let! seeded = seedAskTree host "?ai"
                    let! request = startAsk host seeded
                    do! expectActorSucceeded
                            host pool request.focusId
                    do! expectOwnedTexts
                            host
                            request.focusId
                            [ "from-agent" ]
                    let! started =
                        hasActorStart host request.focusId
                    Assert.True(started)
                    let! state =
                        CoreMailbox.getState host
                        |> Async.StartAsTask
                    match state with
                    | Error err -> Assert.Fail(err)
                    | Ok s ->
                        let node = s.graph.nodes.[request.focusId]
                        Assert.False(
                            CssClass.contains
                                AiExtractPack.PromptClass
                                node.cssClasses)
                }))

    [<Fact>]
    member _.``Ask ignores Command args and still replaces Focus Children``() =
        withFake
            (fun _ -> fakeReply "ignored-args-reply")
            (fun () ->
                withHost (fun host pool -> task {
                    let! seeded =
                        seedAskTree host "?ai later please"
                    let! request = startAsk host seeded
                    do! expectActorSucceeded
                            host pool request.focusId
                    do! expectOwnedTexts
                            host
                            request.focusId
                            [ "ignored-args-reply" ]
                }))

    [<Fact>]
    member _.``Ask empty success clears every Focus Child``() =
        withFake
            (fun _ -> fakeReply "")
            (fun () ->
                withHost (fun host pool -> task {
                    let! seeded = seedAskTree host "?ai"
                    let! request = startAsk host seeded
                    do! expectActorSucceeded
                            host pool request.focusId
                    do! expectOwnedTexts host request.focusId []
                }))

    [<Fact>]
    member _.``Ask extract carries Graph.focus from launch``() =
        let seen = ref None
        let probe: ActorFn =
            fun input coreChanges ->
                async {
                    seen := input.graph.focus
                    let caller =
                        { authority = Authority "Actor"
                          name = ""
                          secret = input.secret }
                    let! _ =
                        coreChanges.asCaller(caller).actorStop
                            ActorSucceeded
                    return ()
                }
        withHost (fun host pool -> task {
            registerActor pool "probe" probe
            let! commandId = seedCommand host "?probe"
            let! request = startOnCommand host commandId
            do! expectActorSucceeded host pool request.focusId
            Assert.Equal(Some commandId, !seen)
        })

    [<Fact>]
    member _.``second launch on the same Focus is refused``() =
        withFake
            (fun _ ->
                Thread.Sleep 300
                fakeReply "slow")
            (fun () ->
                withHost (fun host _ -> task {
                    let! seeded = seedAskTree host "?ai"
                    let request = askRequest seeded
                    let! first =
                        CoreMailbox.startActor
                            host testCaller request
                        |> Async.StartAsTask
                    match first with
                    | Ok () -> ()
                    | Error err ->
                        Assert.Fail($"first start: {err}")
                    let! second =
                        CoreMailbox.startActor
                            host testCaller request
                        |> Async.StartAsTask
                    match second with
                    | Error err ->
                        Assert.Contains("focus", err)
                    | Ok () ->
                        Assert.Fail("expected Focus exclusivity")
                    let! _ =
                        waitForActorStop host request.focusId 2000
                    return ()
                }))

    [<Fact>]
    member _.``credentialed Change under Focus is accepted while Ask runs``() =
        withFake
            (fun _ ->
                Thread.Sleep 200
                fakeReply "from-agent")
            (fun () ->
                withHost (fun host pool -> task {
                    let! seeded = seedAskTree host "?ai"
                    let! request = startAsk host seeded
                    do! postOwnedChild
                            host request.focusId "browser-edit"
                    do! expectActorSucceeded
                            host pool request.focusId
                }))

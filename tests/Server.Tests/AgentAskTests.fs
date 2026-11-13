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
                        CoreMailboxApi.graphAtEvent host
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
    member _.``second launch on the same Focus is refused``() =
        withFake
            (fun _ ->
                Thread.Sleep 300
                fakeReply "slow")
            (fun () ->
                withHost (fun host _ -> task {
                    let! seeded = seedAskTree host "?ai"
                    let request = askRequest seeded
                    let! first = startAiRun host request
                    match first with
                    | Ok () -> ()
                    | Error err ->
                        Assert.Fail($"first start: {err}")
                    let! second = startAiRun host request
                    match second with
                    | Error err ->
                        Assert.Contains(
                            "actor node already has a live Actor",
                            err)
                    | Ok () ->
                        Assert.Fail("expected Focus exclusivity")
                    let! _ =
                        waitForActorStop host request.focusId 2000
                    return ()
                }))

    [<Fact>]
    member _.``?ai prompt keeps an expanded child's text and omits a folded child's descendant``
        ()
        =
        withFake
            (fun args ->
                Assert.Contains("expanded-child-text", args.Prompt)
                Assert.Contains("folded-row-text", args.Prompt)
                Assert.DoesNotContain("folded-child-text", args.Prompt)
                fakeReply "from-agent")
            (fun () ->
                withHost (fun host pool -> task {
                    let zoomId = NodeId.New()
                    let commandId = NodeId.New()
                    let expandedId = NodeId.New()
                    let expandedChildId = NodeId.New()
                    let foldedId = NodeId.New()
                    let foldedChildId = NodeId.New()
                    let event =
                        { id = EventId.zero
                          submissionId = System.Guid.NewGuid()
                          authority = Authority "Browser"
                          commandName = ""
                          body =
                            EventBody.Change
                                [ Op.NewNode(zoomId, "zoom-root")
                                  Op.NewNode(commandId, "?ai")
                                  Op.NewNode(
                                      expandedId, "expanded-row-text")
                                  Op.NewNode(
                                      expandedChildId,
                                      "expanded-child-text")
                                  Op.NewNode(foldedId, "folded-row-text")
                                  Op.NewNode(
                                      foldedChildId, "folded-child-text")
                                  Op.Replace(
                                      Graph.rootId,
                                      [],
                                      [ ChildNode.owner zoomId ])
                                  Op.Replace(
                                      zoomId,
                                      [],
                                      [ ChildNode.owner commandId
                                        ChildNode.owner expandedId
                                        ChildNode.owner foldedId ])
                                  Op.Replace(
                                      expandedId,
                                      [],
                                      [ ChildNode.owner expandedChildId ])
                                  Op.Replace(
                                      foldedId,
                                      [],
                                      [ ChildNode.owner foldedChildId ]) ] }
                    let! posted =
                        CoreMailboxApi.postGraphOnly
                            host testCaller event
                        |> Async.StartAsTask
                    match posted with
                    | Error err -> Assert.Fail(err)
                    | Ok _ -> ()
                    let request =
                        { zoomId = zoomId
                          focusId = commandId
                          runDefinitionId = commandId
                          graphIds =
                            [ zoomId
                              commandId
                              expandedId
                              expandedChildId
                              foldedId ]
                          eventId = EventId.zero }
                    let! started = startAiRun host request
                    match started with
                    | Error err -> Assert.Fail(err)
                    | Ok () -> ()
                    do! expectActorSucceeded host pool commandId
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

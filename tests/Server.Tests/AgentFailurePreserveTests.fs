module Gambol.Server.Tests.AgentFailurePreserveTests

open Xunit
open Gambol.Shared
open Gambol.Server.Tests.AskCancelHarness

[<Collection("Agent ask runner")>]
type AgentFailurePreserveTests() =

    [<Fact>]
    member _.``Ask Failed preserves Focus Children and posts no Change``
        ()
        =
        let failText = "provider-body-secret"
        withFake
            (fun _ -> fakeFailed failText)
            (fun () ->
                withHost (fun host pool -> task {
                    let! seeded = seedAskTree host "?ai"
                    let! before =
                        ownedChildren host seeded.zoomId
                    Assert.Equal(2, before.Length)
                    let! request = startAsk host seeded
                    do! expectActorFailed
                            host pool request.focusId
                    let! after =
                        ownedChildren host request.focusId
                    Assert.Equal<(NodeId * string) list>(
                        before, after)
                    do! expectChangeCount host 1
                    do! expectNoNodeText host failText
                    let! started =
                        hasActorStart host request.focusId
                    Assert.True(started)
                }))

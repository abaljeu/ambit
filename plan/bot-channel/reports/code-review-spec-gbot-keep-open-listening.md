# Spec — [08 — gbot keep-open listening](../issues/08-gbot-keep-open-listening.md)

Spec: [08 — gbot keep-open listening](../issues/08-gbot-keep-open-listening.md). Range: `git diff origin/staging...HEAD`.

None.

Finding count: 0

The prior race is gone. `ActorsDeliverDoorGrokTests` sits in Collection `GrokBot actor`. `GrokBotFake.streamFake` waits while `Stream` is set and events are not readable (`streamEventsReady`), so status-handler `Finished` does not steal the Finish proof. The keep-open seam still holds: empty-text Done does not complete `streamUntilComplete` and does not Finish the Actor; a later deliver grows Focus; Cancel/drop is 404 / not live; the Cursor path is unchanged. Focused tests passed: `GrokBotOneshotTests` (13) and `AgentGrokBotStreamTests|ActorsDeliverDoorTests|CoreActorPoolDeliverTests` together (15), including `fake Grok stream adds Focus children then Finishes` with the door keep-open wait.

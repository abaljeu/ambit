# Spec re-review — [13 — Vertical proof: Browser Ask from what I see](plan/llm-connector/issues/13-vertical-proof-browser-ask.md)

Range: `origin/staging...HEAD` (`cad5a1f1`, `dfb4b7de`). Ticket Status `coded` (unchanged). Spec: ticket **1. Proof path** plus arch Story path **Agent ask from what I see**, Locked **Vertical proof timing**, **Agent Command spelling**, **CloudAgents setFake**, **Live Actor chrome**.

## Prior Poll finding

**Fixed.** Ticket: “Poll / Graph shows the new Focus Children under Focus.” `launchBrowserAsk` snapshots `pollAfter` / `graphAfterSeed` after the Browser seed Change and before `startActor`. `pollEventsSince` is `getEventsSince` after that id, so the seed is outside the Poll window. `applyPollChanges` folds only `EventBody.Change` through `Ev.apply` (ActorStart/Stop: `Ev.ops` is `None`, so `Ev.apply` is Unchanged). The proof asserts Focus owned children `[ "from-agent" ]` on that Poll graph, then the same texts on Graph `getState`. `hasReplace` is gone.

## Coverage (no gap)

1. **Browser Authority** — Ticket allows “Browser or harness.” Seed Event `authority` is Browser. Launch Caller is `testCaller` (Authority Test), same as the hello harness.
2. **CommandRequest.oneNodeStart** — `startBrowserAsk` builds `ActorStart` with that encode for `?ai` (Locked **Agent Command spelling**). `expectOneNodeStart` checks one-Node Zoom/Focus/Command.
3. **setFake Finished** — `withFake` + `fakeReply "from-agent"`; `None` in `finally` (Locked **CloudAgents setFake**).
4. **Focus Children** — Poll-applied graph and Graph state both show `from-agent`. Prompt contains `visible-context` (Included extract / “from what I see”).
5. **ActorStarted then ActorFinished** — Poll tail, chrono `ActorStart` then `ActorStop ActorSucceeded` for Focus.
6. **liveFocusIds drop** — `expectActorSucceeded` → `expectLiveGone`.
7. **Non-goals** — No live-Actor chrome; no new Client encode; no mixed-format/nested-tag pack; no Cancel or Failed path in this proof.

## (a) Missing or partial

None.

## (b) Scope creep

None of product behavior. Map/project Status notes and AskCancelHarness launch/Poll helpers serve this ticket.

## (c) Implemented but wrong

None.

Findings: (a) 0, (b) 0, (c) 0. Worst: none. Prior Poll finding is fixed.

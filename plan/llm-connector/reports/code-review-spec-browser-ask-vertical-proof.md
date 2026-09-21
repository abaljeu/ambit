# Spec — Browser Ask vertical proof

Range: `origin/staging...HEAD` (`cad5a1f1`). Spec: [13 — Vertical proof: Browser Ask from what I see](plan/llm-connector/issues/13-vertical-proof-browser-ask.md). Arch: [llm-connector architecture](plan/llm-connector/arch.md) Story path **Agent ask from what I see**, Locked **Vertical proof timing**, **Agent Command spelling**, **CloudAgents setFake**, **Live Actor chrome**.
Covered without a finding: `CommandRequest.oneNodeStart` in [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs); CloudAgents `setFake` Finished via `withFake` / `fakeReply`; Graph Focus Children via `expectOwnedTexts`; ActorStarted then ActorFinished via `lifecycleIndexes` on `ActorStart` / `ActorStop ActorSucceeded`; `liveFocusIds` drop inside `expectActorSucceeded`. Launch uses `testCaller` (`Authority "Test"`). Seed Change uses `Authority "Browser"`. Spec: "Browser or harness submits `ActorStart`". Non-goals not added: Live-Actor chrome; new Client `ActorStart` encode; mixed-format owning-codec / nested-tag pack; Cancel or Failed paths.

## (a) Missing or partial

### 1. Poll Focus Children

Spec: "Poll / Graph shows the new Focus Children under Focus." Graph is proven in [BrowserAskProofTests.fs](tests/Server.Tests/BrowserAskProofTests.fs) with `expectOwnedTexts` → `getState`. Poll does not read Focus Children. `hasReplace` only requires some `EventBody.Change`.

## (b) Scope creep

None in product behavior. The diff is harness helpers, [BrowserAskProofTests.fs](tests/Server.Tests/BrowserAskProofTests.fs), and plan Status notes.

## (c) Implemented but wrong

### 2. Poll Change may be the seed

Spec: "after success, Poll / Graph shows the new Focus Children under Focus." [BrowserAskProofTests.fs](tests/Server.Tests/BrowserAskProofTests.fs) snapshots `getEventId`, then `launchBrowserAsk` posts a seed Change before `startActor`. `pollEventsSince` includes that seed. `hasReplace` can pass if the Run Agent never posts a replace Change. Graph children still fail closed via `expectOwnedTexts`.

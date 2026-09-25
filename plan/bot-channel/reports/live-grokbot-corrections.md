# Live Grokbot corrections

Date: 2026-09-25
Tickets: [01 — CoreActorPool sessionId + deliver + commandId exclusivity](../issues/01-coreactorpool-sessionid-deliver.md), [02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig](../issues/02-inbound-actors-deliver-door.md), [03 — gbot Run Agent: wake + inbox → Focus stream](../issues/03-gbot-wake-inbox-focus.md)

## 1. Changed behavior

1. **Pool session wire** — [CoreActorPool](../../../src/Server/Core/CoreActorPool.fs) mints `sessionId` on start, stores `commandId`, rejects a second live start on that Command, and exposes `deliver` / `takeInbox`. Drop / Finish / Cancel clear the session index.
2. **Inbound door** — `POST /ambit/actors/deliver` requires `X-Ambit-Inbound-Secret` equal to `grokbot:InboundSecret` (empty configured secret fails closed). Body is `sessionId` + `text`. Unknown session is 404. Success acks and calls `GrokBotRunner.deliver`.
3. **Live stream completion** — `GrokBotAdapter.streamRun` waits on inbound `deliver` instead of returning `InvalidResponse`. Non-empty `text` is `AssistantText`. Empty `text` is oneshot `RunFinished` (empty-sentinel Done; no new inbound fields).
4. **Actor oneshot** — `?ai gbot` uses the pool-minted `sessionId` on wake. Cancel logs a failed `GrokBotRunner.cancel` instead of ignoring it.
5. **Fake alignment** — `GrokBotFake` folds inbound events as they arrive (same deliver / empty Done / cancel contract as live). Existing `setFakeStream` still works.

## 2. Checks

1. `dotnet build` CloudAgents, Server, CloudAgents.Tests, Server.Tests — passed.
2. `dotnet test tests/CloudAgents.Tests` filter `GrokBotOneshotTests` — 9 passed.
3. `dotnet test tests/Server.Tests` filter GrokBot / CoreActorPoolDeliver / ActorsDeliver / CoreMailboxDoorTests / AgentGrokBot — 37 passed.
4. Client compile gate not run — no Client/Shared Fable graph edits.
5. Full repository suite not run (subagent focused-tests limit).

## 3. Unresolved protocol blocker

1. **Hub wake header exact name** — Local specs, [secrets.md](../../../doc/reference/secrets.md), and tests name `WakeSecret` but forbid inventing `X-Ambit-Wake-Secret` / `X-Ambit-Inbound-Secret` / `Authorization` on the outbound wake. `GrokBotHttp.applyWakeAuth` still attaches no header. Confirm the Admiral hub / bot webhook header at wire time; do not guess.

## 4. Console vs server readiness

1. **Server path** — After secrets are loaded, a live `?ai gbot` can wake (ack-only), receive inbound chunks/Done through `/ambit/actors/deliver`, fold Focus, and Finish. Console Grokbot was not changed and is not required by these tickets.
2. **Are secrets enough?** — `grokbot:WakeUrl` + `grokbot:InboundSecret` (and a bot that POSTs `{ sessionId, text }` then empty `text`) are enough for the Server inbound/stream/Finish path **if the hub accepts an unauthenticated wake POST**. If the hub requires a wake auth header, secrets are not enough until that header name is locked and attached.

## 5. Out of scope left alone

1. Destination keep-alive (no Finish-on-every-reply).
2. TestActor `?test gbot`.
3. Separate Server `WakeHttp` module (library wake is the ack-only POST).
4. [06 — Done seam for response concluded](../issues/06-done-seam-response-concluded.md) stays research; this slice uses empty `text` as the oneshot Done sentinel only.

# 08 — gbot keep-open listening

**Status:** coded
Actual: 1h 45m
**Blocked by:** None — [04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md) and [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md) are `done`. Eventual [01 — CoreActorPool sessionId + deliver + commandId exclusivity](01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md) stay deferred.
**Type:** coding

## Context

Alan locked the next increment (2026-09-25): the gbot Actor as it stands keeps open and listening for new messages after the first reply. Do not Finish the Actor when the oneshot path gets empty-text Done after a reply. Keep the same `sessionId` live and keep consuming later `POST /ambit/actors/deliver` inbox texts into Focus until Cancel or drop.

[04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md) and [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md) already ship one wake, stream assistant text into Focus, and treat empty `text` on deliver as oneshot Done (`GrokBotRunner.deliver` → `RunFinished` → `streamUntilComplete` returns → `RunAgentActor` `actorStop` → pool `finish` clears `sessionId`). That Finish-early is the baseline this slice extends. Tickets [01 — CoreActorPool sessionId + deliver + commandId exclusivity](01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md) remain the eventual full keep-alive design. This is the smallest step past oneshot Finish-early, not a rewrite of [03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md).

## Chosen seam

Empty `text` on deliver still enqueues `RunFinished { Text = "" }` so the shared FocusXmlStream fold can flush the turn (`onStreamEvent` / `flushLive`). That empty-text `RunFinished` must **not** complete `streamUntilComplete`. Fake `deliverFake` must **not** mark the session `Finished` for empty Done. The Run Agent Actor stays inside `runStream`. The pool `sessionId` stays live. Later non-empty deliver texts enqueue `AssistantText` and grow Focus children on the existing inbox → FocusXmlStream path.

Finish only on Cancel / Actor drop (existing), or other already-locked close paths — not on empty Done after a successful reply. Explicit `RunFinished` with non-empty `Text` (fake-stream harness) may still terminate. Cursor `AgentRunner` stays untouched.

## What to build

### 1. Keep listening after empty Done

1. [x] Empty-text deliver does not complete `GrokBotRunner.streamUntilComplete` (live adapter and fake deliver)
2. [x] Empty-text deliver still acks (`Ok` / HTTP 200) and does not 404
3. [x] Empty-text `RunFinished` still folds (Focus flush) without `actorStop` / pool `finish`
4. [x] A later non-empty `POST /ambit/actors/deliver` (or `GrokBotRunner.deliver`) for the same `sessionId` is accepted and grows Focus children
5. [x] Cancel / drop still clears the session so later deliver is 404 / `not live`

### 2. Proof

1. [x] CloudAgents: empty Done does not complete the stream; a later inbound chunk still folds; cancel still aborts
2. [x] Run Agent Actor: after first reply + empty Done the Actor/`sessionId` stays live; second deliver grows Focus; Cancel then late deliver fails
3. [x] Pool: empty deliver keeps the live row; second deliver enqueues; drop still clears
4. [x] Cursor Cloud path unchanged

## Non-goals

1. Subsequent Ambit→bot outbound wakes while the session is live
2. Close-notify from Ambit
3. MCP or Slack
4. Changing wake / auth / `responseUrl`
5. Proxy allowlist / `proxy.php`
6. Rewriting [01 — CoreActorPool sessionId + deliver + commandId exclusivity](01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md) wholesale

## See also

[04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md), [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md), [06 — Wake response URL](06-wake-response-url.md), [map.md](../map.md), [arch.md](../arch.md), [src/CloudAgents/Internal/GrokBotAdapter.fs](../../../src/CloudAgents/Internal/GrokBotAdapter.fs), [src/CloudAgents/GrokBotFake.fs](../../../src/CloudAgents/GrokBotFake.fs), [src/Server/RunAgentActor.fs](../../../src/Server/RunAgentActor.fs)

## Comments

- 2026-09-25 — Filed. Alan lock: after the first reply, do not Finish the gbot Actor on empty-text oneshot Done. Same `sessionId` stays live and later deliver texts fold into Focus until Cancel or drop. Status `defined`.
- 2026-09-25 — Coded keep-listening on empty-text `RunFinished` in `GrokBotAdapter.streamRun` and `GrokBotFake` (flush, do not complete / do not mark `Finished`). Run Agent Actor stays in `runStream`. Status `coded`.

## Time

- 2026-09-25 1h 45m — File [08 — gbot keep-open listening](08-gbot-keep-open-listening.md), keep-open seam, CloudAgents / Run Agent / pool proofs (from chat)

# Grokbot design — merge tip

## Verdict: Mixed

Module cut is coherent. Live completion is not. Most gaps are unfinished first-slice scope. The structural problem is that Runner/Actor already orchestrate a full wake→stream→cancel loop whose only working implementation is the fake.

## Supporting points

1. **Boundaries are right.** HTTP JSON/POST stays in [GrokBotHttp](src/CloudAgents/Internal/GrokBotHttp.fs). Error mapping in [GrokBotAdapter](src/CloudAgents/Internal/GrokBotAdapter.fs). Public face [GrokBotRunner](src/CloudAgents/GrokBotRunner.fs) + types in [PublicTypes](src/CloudAgents/PublicTypes.fs). Config bind is Server-only ([GrokBotSettings](src/Server/GrokBotSettings.fs) → `actorFn`). Actor chooses `gbot` vs Cursor and writes Focus. That split should stay.

2. **Incomplete scope (not a bad module map):** no [CloudAgents.Console](src/CloudAgents.Console/Program.fs) Grok command; Adapter `streamRun` is a hard Error; Adapter `cancel` is a no-op; no Server inbound Done/stream door. Those are missing product pieces. Tests and [GrokBotFake](src/CloudAgents/GrokBotFake.fs) cover the intended loop, not the hub.

3. **Structural: auth and secrets look wired but are not.** `WakeSecret` is bound and passed into `postWake` → `applyWakeAuth`, which returns the request unchanged (no header). `InboundSecret` sits on library `GrokBotConfig` with a comment that Server deliver is out of scope, and nothing reads it. Callers will think secrets are live.

4. **Structural: fake is the stream runtime.** Runner branches on `setFake`. Actor always `wake` then `streamUntilComplete`. Live: wake can POST, then stream fails. Fake: poll, synthesize chunks, honor cancel. Completing the Done seam is not a fill-in of `streamRun`; it must replace the fake’s wait/fold contract or the Actor will keep depending on a test double.

## Smallest next step

Stop treating wake as done. Either (a) attach the hub wake header in `applyWakeAuth` and add one inbound/Done path that `streamRun` consumes (same `StreamFold` the Actor already uses), or (b) do not call live `wake` until that path exists. Console Grok can wait.

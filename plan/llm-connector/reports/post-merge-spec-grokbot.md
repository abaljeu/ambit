# Spec — post-merge Grokbot (`git diff HEAD^1...HEAD`)

**Verdict: partially works.** Fake `?ai gbot` (wake + FocusXmlStream + Finish/Cancel) is wired and tested. Merge-restored Cursor `poll` / `waitUntilComplete` / `getRunStatus` stay on the cursor runner; `RunAgentActor` still uses `streamUntilComplete` for both backends. Live Grok oneshot cannot finish.

## Findings

1. **Live stream always fails after wake.** `GrokBotAdapter.streamRun` returns `InvalidResponse` and never folds `AssistantText` / `RunFinished`. `completeGrok` then `ActorFailed`. A real hub reply never reaches Focus. Spec: “Live stream without fake returns `InvalidResponse` (Done seam Unsettled).” ([24 — CloudAgents Grok Bot oneshot stream](../issues/24-cloudagents-grokbot-oneshot.md)) Same lock on [05 — Run Agent Actor Grok Bot oneshot](../../bot-channel/issues/05-run-agent-grokbot-oneshot.md): “Live `GrokBotAdapter.streamRun` may still return `InvalidResponse` until the Done seam locks.”
2. **Live wake sends no auth header.** `GrokBotHttp.applyWakeAuth` is a no-op; `WakeSecret` is unused on the wire. A hub that requires the Admiral contract can reject the POST before any stream. Spec: “Wake auth header matches the Admiral hub / bot webhook contract — do not invent an Ambit-only header. … `applyWakeAuth` is the adapter seam (no extra header until confirmed).” ([24 — CloudAgents Grok Bot oneshot stream](../issues/24-cloudagents-grokbot-oneshot.md))

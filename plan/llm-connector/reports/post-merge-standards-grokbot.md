# Design — merged Grokbot (`HEAD^1...HEAD`)

Naming/doc scan lines dropped. Layers are clear: [GrokBotHttp](src/CloudAgents/Internal/GrokBotHttp.fs) (JSON/POST) → [GrokBotAdapter](src/CloudAgents/Internal/GrokBotAdapter.fs) (errors) → [GrokBotRunner](src/CloudAgents/GrokBotRunner.fs) (public + fake) → [RunAgentActor](src/Server/RunAgentActor.fs) (Focus fold). Settings stay in Server. Public types stay small.

## Verdict

Fit for harness/fake oneshot. Live path is not a working stream. Do not treat green tests as proof the hub can finish a run.

## Material concerns

1. **Live stream is a hard Error after a real wake.** `GrokBotAdapter.streamRun` always returns `InvalidResponse` (“stream Done seam Unsettled”). `cancel` is `Ok()` and does not notify the hub. Actor `completeGrok` still `wake`s then `streamUntilComplete`. Without `setFake`, a wake can land and the Actor then fails. Tests drive [GrokBotFake](src/CloudAgents/GrokBotFake.fs) only.

2. **Fake stream ≠ adapter contract.** Fake stores events from a second handler, synthesizes chunks from `Finished` text, polls `sessionId`, and honors cancel. Live adapter ignores `GrokBotStreamArgs` and the fold. Wiring a Done seam later will not be a small fill-in; it replaces the whole wait/fold path the Actor already assumes.

3. **Cancel + Focus apply are easy to break.** Actor `requestGrokCancel` ignores the Result. Stream fold applies adds with `Async.RunSynchronously` (`postAddNow`). That matches Cursor stream style but couples mailbox latency to HTTP/Core posts; a hang or exception in `postEvents` is not folded into `AgentError`.

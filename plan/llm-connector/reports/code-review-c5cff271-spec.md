# Code review: c5cff271 streaming agent runner (spec only)

Range: `git diff HEAD~1...HEAD`. Spec: [[plan/llm-connector/issues/17-cloudagents-console-stream.md]].

## Standards

Skipped (spec-only review requested).

## Spec

1. **Missing (against the user's summary, not ticket 17): no graph insertion.** User: "As results arrive we insert into the graph." Ticket 17 says the opposite: "Graph / AI Actor write-back is [18 — AI Actor stream]" and non-goal "AI Actor / Focus Graph writes ([18])". Nothing in the diff touches the Focus Graph. The only step toward it is the new `StreamFold<'a>` accumulator (`src/CloudAgents/PublicTypes.fs:64-67`). Decide whether ticket 18 is in scope for this review.
2. **Wrong: the Console prints the final text twice.** Spec: "print assistant deltas as they arrive; print terminal summary". The old `printFinished wroteText` skipped the text if deltas had already been printed. The new `printFinished` always prints `result.Text` under `=== Result ===` (`src/CloudAgents.Console/Program.fs:156-164`). On a live run the `result` payload repeats the text that was already streamed.
3. **Partial: `error` never reaches the fold as an event.** Spec: "map at least: … terminal `result` / `done` / `error`". On the live path, `error` and HTTP failures come back as `Error`, and the fold state is dropped (`src/CloudAgents/Internal/CursorAdapter.fs:134-135`). `RunFailed` and `RunCancelled` are only emitted by the fake. The fake error path also drops the fold state (`src/CloudAgents/AgentRunnerFake.fs:233-234`). So a consumer loses whatever it built up before a failure, which is exactly what ticket 18's incremental graph inserts will need.
4. **Wrong-looking: polling knobs in the stream API.** Spec: the stream should replace polling for completion. `StreamArgs` carries `PollIntervalMs` and `MaxWaitMs` (`src/CloudAgents/PublicTypes.fs:57-62`). The live SSE path ignores both, so a live stream has no timeout even when `MaxWaitMs` is set. Only the fake uses them, in a sleep/poll loop (`src/CloudAgents/AgentRunnerFake.fs:201-209`).
5. **Partial: the fake emits deltas in one batch.** Spec: "`setFake` can simulate a short delta sequence then terminal". `waitFakeStream` polls until the whole event list is stored, then `emitFakeStream` folds all events at once (`src/CloudAgents/AgentRunnerFake.fs:197-240`). This passes the checkbox, but it can't prove that events are delivered incrementally.
6. **Scope creep (minor):** This commit changes the public `streamUntilComplete` signature from positional args plus `onEvent` to `StreamArgs` plus `StreamFold` (`src/CloudAgents/AgentRunner.fs:118-126`). It also moves the fake into `AgentRunnerFake.fs`. Ticket 17 didn't ask for either. The commit also touches `.agents/skills/code-review/*`, which is unrelated to the ticket.

Summary: 6 spec findings; worst is the Console printing the final text twice on a live run. Graph insertion is absent, but ticket 17 defers it to ticket 18.

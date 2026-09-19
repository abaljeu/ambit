# Spec review: 10 — Cancel by Focus

Range: `git diff origin/staging...HEAD` (merge-base `f4bdf872` = `origin/staging`; tip `6230600a`). Originating spec: [10 — Cancel by Focus](plan/llm-connector/issues/10-cancel-by-focus.md) and Story path 4 in [llm-connector architecture](plan/llm-connector/arch.md).

## (a) Missing or partial

None in the in-scope slice. Story 4.1 Browser cancel and Story 4.5 Browser live projection stay open. Ticket: “do not require Browser live-Actor UI (core-creation 21/22)”. No Client or HTTP cancel decode is in the range.

## (b) Not asked for

1. **CloudAgents public helpers beyond the locked DLL face.** Arch: “Existing public API: start / poll / cancel / waitUntilComplete (keep; do not reshape for Ambit)” and `setFake: (StartArgs -> AgentResult) option -> bool`. The diff adds `AgentRunner.waitForCancel` and `fakeCancelCount`. `waitForCancel` is a test seam: a hanging `StartArgs -> AgentResult` handler can wait for cancel without a new `setFake` type. It is not a reshape of start, poll, cancel, or waitUntilComplete. `fakeCancelCount` is extra; Poll status `Cancelled` already shows “CloudAgents cancel is requested”.

## (c) Implemented but wrong

None.

## Seams

1. **Cancel vs Change.** Ticket: “Change-before-Cancel applies; Cancel-before-Change rejects; late duplicate completion after cancel is ignored.” One mailbox runs `PostEvent` and `CancelActor` in queue order. `dispatchCancelActor` writes `ActorCancelled` then `finish` (token cancel + drop). After drop, Actor admit fails, so a late Change or `actorStop` does not append a second ActorFinished.
2. **ActorFinished.** Ticket: “ActorFinished without Error or response Change.” Event body is `ActorStop(focusId, ActorCancelled)`. EventJson encodes `result` as `"cancelled"`. Run Agent returns `CompleteCancelled` and skips `postReplace`. No Undo.
3. **CloudAgents cancel and stop poll.** Arch Run Agent Actor: “On cancel token: request CloudAgents cancel and stop.” `pollUntilDone` registers `OnCancel` and checks the token, calls `AgentRunner.cancel`, and leaves the poll loop.
4. **liveFocusIds drop.** Ticket: “observed via liveFocusIds (secrets are not an observation surface).” Proof uses `pool.liveFocusIds ()`.
5. **Children preserved.** Ticket: “Focus Children unchanged aside from earlier accepted Changes.” Cancel-before-Change keeps seed Children; Change-before-Cancel keeps the earlier accepted child.
6. **waitForCancel.** See (b). Required test seam for a hanging `setFake` handler. Not a reshape of start / poll / cancel / waitUntilComplete.
7. **No Browser chrome.** Ticket: “this ticket may prove cancel through Core / harness without UI chrome.” Seam “Browser cancel ↔ Core Cancelled” stays open.

# 10 — Cancel by Focus

**Status:** coded
**Blocked by:** [[08-agent-ask-from-what-i-see.md|08 — Run Agent Actor calls CloudAgents]]
**Type:** task
Actual: 2h35m

## Context

Implements Story path **Cancel by Focus** in [[arch.md|llm-connector architecture]]. Cancel clears the live Actor without Undo and without writing Error or response Changes. Browser live-Actor chrome stays on core-creation 21/22; this ticket may prove cancel through Core / harness without UI chrome.

## What to build

While a Run Agent Actor is live for a Focus, cancel by Focus NodeId. Core orders Cancelled against Change messages (Change-before-Cancel applies; Cancel-before-Change rejects). CloudAgents cancel is requested. ActorFinished has no Error and no Change. Focus Children are preserved except for earlier accepted Changes. Live row and secret are dropped.

### 1. CoreMailbox Cancelled

1. [x] Cancel by Focus — accept Cancelled terminal keyed by Focus NodeId.
2. [x] Order vs Change — Change-before-Cancel applies; Cancel-before-Change rejects; late duplicate completion after cancel is ignored.

### 2. Run Agent Actor / CloudAgents

1. [x] Request cancel — on cancel token, call existing CloudAgents cancel and stop polling.
2. [x] Terminal without Graph write — ActorFinished without Error or response Change.

### 3. Proof

1. [x] Preserve children — Focus Children unchanged aside from earlier accepted Changes.
2. [x] Drop live — live row gone; observed via liveFocusIds (secrets are not an observation surface).
3. [x] Chrome out of scope — do not require Browser live-Actor UI (core-creation 21/22).

## See also

[[arch.md|llm-connector architecture]], [[08-agent-ask-from-what-i-see.md|08 — Agent ask from what I see]], [[plan/core-creation/issues/21-client-shows-lock-present.md|21]], [[plan/core-creation/issues/22-client-cancels-a-job.md|22]], [[06-define-command-run-agent-redesign.md|06]], [[07-lock-run-agent-architecture.md|07]]

## Comments

- 2026-09-19 — Long fact **Change before Cancel keeps the accepted children** now uses [AskCancelHarness](../../../tests/Server.Tests/AskCancelHarness.fs). Shared Ask/Cancel helpers are public only when a fact calls them; wait/filter/spin wiring is private. `postChildThenWait` is the reusable probe (one Focus-child Change, then wait on the cancel token).

## Time

- 2026-09-19 2h — CoreMailbox cancelByFocus, hanging setFake, Run Agent cancel token, CancelByFocusTests (from chat)
- 2026-09-19 35m — Split Ask/Cancel facts onto AskCancelHarness; slim Change-before-Cancel (from chat)

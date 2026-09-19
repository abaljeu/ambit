# 10 — Cancel by Focus

**Status:** defined
**Blocked by:** [[08-agent-ask-from-what-i-see.md|08 — Run Agent Actor calls CloudAgents]]
**Type:** task

## Context

Implements Story path **Cancel by Focus** in [[arch.md|llm-connector architecture]]. Cancel clears the live Actor without Undo and without writing Error or response Changes. Browser live-Actor chrome stays on core-creation 21/22; this ticket may prove cancel through Core / harness without UI chrome.

## What to build

While a Run Agent Actor is live for a Focus, cancel by Focus NodeId. Core orders Cancelled against Change messages (Change-before-Cancel applies; Cancel-before-Change rejects). CloudAgents cancel is requested. ActorFinished has no Error and no Change. Focus Children are preserved except for earlier accepted Changes. Live row and secret are dropped.

### 1. CoreMailbox Cancelled

1. [ ] Cancel by Focus — accept Cancelled terminal keyed by Focus NodeId.
2. [ ] Order vs Change — Change-before-Cancel applies; Cancel-before-Change rejects; late duplicate completion after cancel is ignored.

### 2. Run Agent Actor / CloudAgents

1. [ ] Request cancel — on cancel token, call existing CloudAgents cancel and stop polling.
2. [ ] Terminal without Graph write — ActorFinished without Error or response Change.

### 3. Proof

1. [ ] Preserve children — Focus Children unchanged aside from earlier accepted Changes.
2. [ ] Drop live — live row gone; secret revoked.
3. [ ] Chrome out of scope — do not require Browser live-Actor UI (core-creation 21/22).

## See also

[[arch.md|llm-connector architecture]], [[08-agent-ask-from-what-i-see.md|08 — Agent ask from what I see]], [[plan/core-creation/issues/21-client-shows-lock-present.md|21]], [[plan/core-creation/issues/22-client-cancels-a-job.md|22]], [[06-define-command-run-agent-redesign.md|06]], [[07-lock-run-agent-architecture.md|07]]

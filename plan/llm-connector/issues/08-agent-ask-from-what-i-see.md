# 08 — Run Agent Actor calls CloudAgents

**Status:** defined
**Blocked by:** None — can start immediately.
**Type:** task
Actual: 35m

## Context

Story path **TestActor hello** is already delivered. This ticket starts Story path **Agent ask from what I see** in [[arch.md|llm-connector architecture]]. Command text `?ai` (optional args ignored) launches the Run Agent Actor. The Actor ignores the Graph extract. It passes Focus Node text into the existing CloudAgents DLL and receives Completed text or a Failed / Cancelled outcome. No Focus-child write, no pack format, no live-Actor chrome (core-creation 21/22). Not a revival of cancelled [[05-create-cloud-agent-posts-reply-under-focus.md|05]].

## What to build

One successful `?ai` launch: the Run Agent Actor starts, calls CloudAgents with Focus text, observes Completed text or a mapped error, then ActorStarted and ActorFinished appear on the EventLog and the live Actor is dropped. Prove with a fake CloudAgents completion. Do not post a Graph Change from this Actor on this ticket.

### 1. CoreMailbox / CoreMsg / CoreActorPool

Resolve and launch the Agent Actor on the existing one-mailbox path. State / Interface / Uses: [[arch.md]] module **CoreMailbox / CoreMsg / CoreActorPool**.

1. [ ] Resolve Agent Command — Command text `?ai` with optional args selects the Run Agent Actor; args are ignored.
2. [ ] Start lifecycle — register, append ActorStarted, schedule the Run Agent Actor body; Focus exclusivity admit.
3. [ ] Ignore extract — do not serialize or walk included membership for the CloudAgents call.

### 2. Run Agent Actor

Call CloudAgents and stop. State / Interface / Uses: [[arch.md]] module **Run Agent Actor**.

1. [ ] Pass Focus text — send the Focus Node's current text as the document; system prompt may be a fixed string.
2. [ ] Map outcome — Completed text, Failed safe error, or Cancelled from existing `start` / `poll` / `cancel`.
3. [ ] No Graph write — do not replace Focus Children and do not write provider text as Graph Error.
4. [ ] No error amplification — never turn a pack or provider error into a second Agent call.

### 3. CloudAgents (Ambit fit)

Keep the DLL public API. State / Interface / Uses: [[arch.md]] module **CloudAgents**.

1. [ ] Fit locked inputs — Ambit-side mapping into existing `start` / `poll` / `cancel` (no library reshape).
2. [ ] Fake for tests — Actor tests use fake CloudAgents (narrowest shared test seam).

## See also

[[arch.md]], [[spec.md]], [[06-define-command-run-agent-redesign.md]], [[07-lock-run-agent-architecture.md]], [[11-simple-extract-format.md]], [[12-replace-focus-children-from-reply.md]]

## Comments

- 2026-09-19 — Charted from arch Story path **Agent ask from what I see**. Vertical proof ticket waits until this and sibling implement tickets are `defined` (arch lock).
- 2026-09-19 — Split: this ticket is Actor + DLL with Focus text only. Pack format and Focus-child replace moved to [11 — Simple extract format](11-simple-extract-format.md) and [12 — Replace Focus Children from reply](12-replace-focus-children-from-reply.md). Md pack cancelled. Stronger serialization tabled.

## Time

- 2026-09-19 15m — lock Md supplied-subgraph strings vs owned-subgraph file write (from chat)
- 2026-09-19 20m — split Actor+DLL from pack and replace; drop Md (from chat)

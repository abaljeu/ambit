# 08 — Agent ask from what I see

**Status:** defined
**Blocked by:** None — 11 Amb extract-walk is landed.
**Type:** task

## Context

Story path **TestActor hello** is already delivered. This ticket implements Story path **Agent ask from what I see** in [[arch.md|llm-connector architecture]]. A person Runs Command text `?ai` with optional args (args ignored). Core launches the Run Agent Actor, which packs the extract with [11 — Pack extract with Amb (supplied-fragment walk)](11-simple-extract-format.md), completes through the existing CloudAgents DLL, replaces Focus Children from the complete response, and finishes through ordinary Core Changes. No live-Actor chrome (core-creation 21/22). Not a revival of cancelled [[05-create-cloud-agent-posts-reply-under-focus.md|05]].

## What to build

One successful Ask from what I see: Browser (or harness) Runs `?ai` (+ optional ignored args), Poll shows new Focus Children from the Agent reply, ActorStarted then ActorFinished appear on the EventLog, and the live Actor is dropped. Prove with a fake CloudAgents completion where possible; live Cursor is optional extra. Concurrent Browser Changes under Focus use ordinary merge only — no Agent-specific stale rules.

### 1. CoreMailbox / CoreMsg / CoreActorPool

Resolve and launch the Agent Actor on the existing one-mailbox path. State / Interface / Uses: [[arch.md]] module **CoreMailbox / CoreMsg / CoreActorPool**.

1. [ ] Resolve Agent Command — Command text `?ai` with optional args selects the Run Agent Actor; args are ignored.
2. [ ] Authoritative extract — Core builds the extract from included membership; Focus exclusivity admit.
3. [ ] Start lifecycle — register, append ActorStarted, schedule the Run Agent Actor body.

### 2. Run Agent Actor

Orchestrate pack → complete → inject → Change. State / Interface / Uses: [[arch.md]] module **Run Agent Actor**.

1. [ ] Serialize — ask Document for the Amb extract-walk pack from [11 — Pack extract with Amb (supplied-fragment walk)](11-simple-extract-format.md).
2. [ ] Complete — fit system prompt + document + cancel into existing CloudAgents `start` / `poll` / `cancel`; map outcomes to Completed | Failed | Cancelled.
3. [ ] Inject and post — Document plans Focus-child replacement; submit ordinary Core Change; queue Succeeded.
4. [ ] No error amplification — never turn pack or provider errors into a second Agent call; never write raw provider text as Graph Error.

### 3. Document

Amb extract-walk serialize (from 11) and Reference-Paste-style replace. State / Interface / Uses: [[arch.md]] module **Document**. Focus is on the extract Graph (`Graph.focus`).

1. [ ] Use Amb extract-walk pack — consume [11 — Pack extract with Amb (supplied-fragment walk)](11-simple-extract-format.md); do not invent a second format.
2. [ ] Complete parse — structural parse of the complete response; else Plain indentation; never keep a partial structural parse.
3. [ ] Replace Focus Children — plan Ops that delete every current Focus Child and create from the response; empty success clears all children.

### 4. CloudAgents (Ambit fit)

Keep the DLL public API. State / Interface / Uses: [[arch.md]] module **CloudAgents**.

1. [ ] Fit locked inputs — Ambit-side mapping into existing `start` / `poll` / `cancel` (no library reshape).
2. [ ] `setFake` on the DLL — `setFake: (StartArgs -> AgentResult) option -> bool`. `Some f` makes start/poll/wait use `f` (no HTTP); `None` restores CursorAdapter. Returns false if refused. Process-local; clear in test finally.
3. [ ] Fake success path — Run Agent Actor tests install `setFake (Some …)` and prove Ask through ordinary Core Change (narrowest shared test seam). Live Cursor optional; until a real API key exists, live facts only cover call-reject.

### 5. Browser / Poll proof

Graph + Poll only; no live-Actor chrome.

1. [ ] Poll Focus Children — after success, Poll / Graph shows the new Focus Children.
2. [ ] Concurrent edit — a credentialed Change under Focus while the Actor runs reconciles with ordinary merge only (Story path **Credentialed Change while Agent runs** open hop).

## See also

[[arch.md|llm-connector architecture]], [[spec.md]], [[11-simple-extract-format.md|11 — Pack extract with Amb (supplied-fragment walk)]], [[06-define-command-run-agent-redesign.md|06 — Define the revised Command + Run Agent seam]], [[07-lock-run-agent-architecture.md|07 — Lock the Run Agent architecture]]

## Comments

- 2026-09-19 — Locked: CloudAgents fake/real via DLL `setFake: (StartArgs -> AgentResult) option -> bool` (arch). Not an Ambit/Core switch.
- 2026-09-19 — 11 Amb extract-walk landed on staging; pack dependency satisfied.
- 2026-09-19 — Charted from arch Story path **Agent ask from what I see**. Vertical proof ticket waits until this and sibling implement tickets are `defined` (arch lock).
- 2026-09-19 — Pack is [11 — Pack extract with Amb (supplied-fragment walk)](11-simple-extract-format.md). This ticket is blocked by 11. Mixed-format owning-codec stays tabled.

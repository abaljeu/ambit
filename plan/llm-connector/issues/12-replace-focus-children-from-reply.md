# 12 — Replace Focus Children from reply

**Status:** defined
**Blocked by:** [[08-agent-ask-from-what-i-see.md|08 — Run Agent Actor calls CloudAgents]], [[11-simple-extract-format.md|11 — Simple extract format]]
**Type:** task

## Context

Finishes Story path **Agent ask from what I see** in [[arch.md|llm-connector architecture]]. [08 — Run Agent Actor calls CloudAgents](08-agent-ask-from-what-i-see.md) already launches and completes. [11 — Simple extract format](11-simple-extract-format.md) already writes and parses the nested-tag string. This ticket packs the extract with that format, sends it through the Actor, and replaces every Focus Child from the complete reply through an ordinary Core Change. No live-Actor chrome (core-creation 21/22).

## What to build

One successful Ask from what I see: `?ai` packs the supplied extract as `<div>` / `<focus>` strings, CloudAgents returns text (fake allowed), Poll / Graph shows new Focus Children, ActorStarted then ActorFinished appear, and the live Actor is dropped. Concurrent Browser Changes under Focus use ordinary merge only — no Agent-specific stale rules. Empty success clears all Focus Children.

### 1. Run Agent Actor

State / Interface / Uses: [[arch.md]] module **Run Agent Actor**.

1. [ ] Pack extract — serialize the supplied Zoom extract with [11 — Simple extract format](11-simple-extract-format.md); stop passing Focus text alone.
2. [ ] Inject and post — parse the complete reply with that format; submit an ordinary Core Change; queue Succeeded.
3. [ ] No error amplification — never turn pack or provider errors into a second Agent call; never write raw provider text as Graph Error.

### 2. Document

State / Interface / Uses: [[arch.md]] module **Document**.

1. [ ] Replace Focus Children — plan Ops that delete every current Focus Child and create from the parsed reply; empty success clears all children.
2. [ ] Complete parse only — if the reply is not a complete simple-format tree, discard it; do not keep a partial tree.

### 3. Browser / Poll proof

Graph + Poll only; no live-Actor chrome.

1. [ ] Poll Focus Children — after success, Poll / Graph shows the new Focus Children.
2. [ ] Concurrent edit — a credentialed Change under Focus while the Actor runs reconciles with ordinary merge only (Story path **Credentialed Change while Agent runs** open hop).

## See also

[[arch.md]], [[spec.md]], [[08-agent-ask-from-what-i-see.md]], [[11-simple-extract-format.md]], [[06-define-command-run-agent-redesign.md]], [[07-lock-run-agent-architecture.md]]

## Comments

- 2026-09-19 — Replace for Amb moved into [08 — Agent ask from what I see](08-agent-ask-from-what-i-see.md). This ticket's nested-tag pack is stale; do not implement 12 separately.

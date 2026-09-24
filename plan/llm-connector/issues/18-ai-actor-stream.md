# 18 — AI Actor stream

**Status:** defined
**Blocked by:** None — [17 — CloudAgents Console stream](17-cloudagents-console-stream.md) is `coded`.
**Type:** task
Estimate: 4h

## Context

After [17](17-cloudagents-console-stream.md) exposes SSE deltas on CloudAgents, the AI Actor ([RunAgentActor](../../../src/Server/RunAgentActor.fs)) still waits for a full completion then replaces Focus children once. User wants **incremental** Focus updates from a `<>`-only XML fragment stream without add-then-edit churn.

Locked draft algorithm (chat 2026-09-20):

- Buffer an open element **off-graph** (`pending = { name, text }`).
- Text deltas append to `pending.text` only (e.g. `Te` then `xt` → `Text`).
- On the next complete tag boundary (`Start` child, `End` self, or `Empty`): **one** `addChild(parent, child, pending.text)` — no later text edits on that node.
- Partial tags (`<di`) stay in the byte buffer until `>`.
- Stream is a valid XML **fragment** under Focus (`<>` only). Final `result` may still run a complete Amb/Plain tidy parse if needed; drafts must not install a broken partial tree.

Amends “complete parse only / never keep partial structural parse” for **streaming drafts**: commit only closed elements / completed text runs via the pending rule; do not install half-open markup as siblings.

## What to build

### 1. Actor consumes stream

1. [ ] Run Agent Actor uses CloudAgents stream (from 17) instead of sleep-poll for the live path when stream is available.
2. [ ] Cancel still stops the run (token / cancel API).
3. [ ] Missing key / auth failures still use [14 — Provider-named AI errors](14-provider-named-ai-errors.md).

### 2. Incremental Focus write

1. [ ] Implement pending-buffer → single `addChild` at tag boundary (pseudocode locked in Comments / design note).
2. [ ] Direct text under an open element is not Graph-edited after `addChild`.
3. [ ] On terminal `result` / `done`: flush pending if required; optional complete parse replace of Focus children (document choice in Comments when coding).
4. [ ] Client sees growth via existing Poll / chrome ([core-creation 21](../../core-creation/issues/21-client-shows-lock-present.md)) — no new chrome ticket required unless gaps appear.

### 3. Proof

1. [ ] Fake stream (from 17) drives Actor: multiple deltas → intermediate Focus children without text-edit ops after add.
2. [ ] Cancel mid-stream still drops live chrome and preserves earlier accepted Changes ([09](09-agent-failure-preserves-children.md) / [10](10-cancel-by-focus.md) spirit).

### 4. Non-goals

1. CloudAgents.Console UX (owned by 17).
2. Follow-up turns / multi-Ask chat (Epic later).
3. Indent-outline streaming (this ticket is `<>` fragment only unless a later amend adds it).

## Design note — pending commit

```
pending = none   // { name, text } off-graph
stack = [Focus]  // graph-committed parents only

on Start(name):  commitPending(); pending = { name, text: "" }
on Text(s):      pending.text += s
on End(name):    commitPending(); pop matching child
on Empty(name):  commitPending(); addChild(stack.last, leaf, "")

commitPending():
  if pending is none: return
  addChild(stack.last, newNode(), pending.text)  // once
  push child; pending = none
```

Tag tokenizer waits for `>` on tags; text takes bytes up to next `<` into `pending.text` (not Graph).

## See also

[17 — CloudAgents Console stream](17-cloudagents-console-stream.md), [07 — Lock Run Agent architecture](07-lock-run-agent-architecture.md), [08 — Agent ask from what I see](08-agent-ask-from-what-i-see.md)

## Comments

- 2026-09-20 — Filed from chat after SSE discovery and pending-buffer lock (commit on `<`/`>` boundary, no add-then-edit). Blocked by 17. Status `defined`.

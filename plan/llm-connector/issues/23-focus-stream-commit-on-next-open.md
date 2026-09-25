# 23 — Focus stream commit on next <

**Status:** done
Actual: 1h
**Blocked by:** None — [18 — AI Actor stream](18-ai-actor-stream.md) is `done`.
**Type:** task

## Context

[18 — AI Actor stream](18-ai-actor-stream.md) locked: commit pending at tag boundaries. Spec intent (Alan 2026-09-24): **`<tag>Text<` is enough to generate a node** — the leading `<` of the next tag commits pending text. What shipped waits for a full next token (`Start` / `End` / `Empty` after `…>`) or terminal flush, so mid-stream text stays off-graph until the following tag completes.

Incomplete tag *bytes* like `<di` must still stay in `hold` until `>` (do not invent a node from a half-written tag name).

This ticket amends only the commit trigger. The “no SetText after add” rule stays.

## What to build

### 1. Commit pending on next open

1. [x] [FocusXmlStream](../../../src/Shared/documents/FocusXmlStream.fs): when `hold` (or the tokenizer) sees that text is followed by `<` starting a new tag, `commitPending` so the node exists with text so far.
2. [x] Then continue buffering the incomplete tag in `hold` until `>`.
3. [x] Incomplete tag bytes like `<di` stay in `hold` and do not invent a node.
4. [x] Once committed, later text for that element must not edit the graph. If more text arrives before the next `<`, that text is still pending of the current open element — only the boundary `<` commits.
5. [x] Flush / End / Empty / full Start stay coherent with [18 — AI Actor stream](18-ai-actor-stream.md).

### 2. Proof

1. [x] Unit test: `<tag>Text<` yields a PlannedAdd / graph child with text `Text` before the next tag’s `>` arrives.
2. [x] Similar cases: more text before `<` still pending; incomplete `<di` after commit stays in `hold`; End / flush / full Start still one add and no SetText after add.
3. [x] Existing [FocusXmlStream](../../../tests/Shared.Tests/FocusXmlStreamTests.fs) and [AgentActorStream](../../../tests/Server.Tests/AgentActorStreamTests.fs) facts stay green.

### 3. Non-goals

1. Indent-outline streaming.
2. Changing the “no SetText after add” rule.
3. Land / squash onto staging without Alan accept.

## Design note — commit on next `<`

```
on leftover hold starting with '<':
  commitPending()
  // incomplete tag bytes stay in hold until '>'
```

Tokenizer still waits for `>` before emitting `Start` / `End` / `Empty`. Text still takes bytes up to the next `<` into `pending.text`. The leading `<` of the next tag is the commit boundary; `>` of that next tag is not required.

## See also

[18 — AI Actor stream](18-ai-actor-stream.md), [FocusXmlStream](../../../src/Shared/documents/FocusXmlStream.fs)

## Comments

- 2026-09-25 — Filed: Alan lock 2026-09-24/25 — `<tag>Text<` is enough to generate a node; commit pending on the next `<`. Status `defined`.
- 2026-09-25 — Coded: tokenizer still waits for `>` on tags and emits `Text` up to the next `<`. `apply` calls `commitIfNextOpen` when leftover `hold` starts with `<`, so pending becomes one PlannedAdd before the next tag’s `>`. Incomplete `<di` stays in `hold`. Proofs: [FocusXmlStreamTests](../../../tests/Shared.Tests/FocusXmlStreamTests.fs) (`leading angle of next tag commits pending text` and siblings) and [AgentActorStreamTests](../../../tests/Server.Tests/AgentActorStreamTests.fs) (`next open angle commits Focus child before next tag closes`). Status `coded`.
- 2026-09-25 — Alan accepted as-is; squash-landed on staging. Status `done`.

## Time

- 2026-09-25 1h — Ticket + commit-on-next-`<` in FocusXmlStream + proofs (from chat)

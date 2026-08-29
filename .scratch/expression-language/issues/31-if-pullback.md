# 31 — Prefix combinator `IF` (same-input pullback)

**Context:** Yield the input Answer when the operand yields any Answer from it; otherwise miss. User spelling `IF`. Independent of text operations: `IF` is useful with today's catalog (`IF child` keeps a Node that has Children; `NOT (NOT e)` is the same emptiness inversion). Text-ops examples that need pullback to keep Nodes live in [[30-text-operations.md]]. Work on branch `w/tree2-semantics`. Do not implement product code. Do not implement [[28-outer-prefix-combinator.md]].

**What to lock:** Combinator row `IF`, same parse family as `NOT` (prefix, capitals only, compound operands need parentheses). After HITL closes spelling and type, an implementation issue can go `ready-for-agent`.

**Blocked by:** none. Not blocked by [[30-text-operations.md]]. Implementation must not start while [[28-outer-prefix-combinator.md]] still owns parse/eval of prefix combinators.

**See also:** [[../spec.md]] chapter 5–6 `NOT` and `outer`; [[30-text-operations.md]]; [[28-outer-prefix-combinator.md]].

**Status:** needs-info

## Sketch (not a spec lock)

```text
E⟦IF e⟧ x  = ⟨x⟩ when E⟦e⟧ x is nonempty, otherwise ⟨⟩
⊢ e : τ ⇒ τ'    ⊢ IF e : τ ⇒ τ
```

`outer` already pullbacks while walking Owned descendants. `IF` pullbacks in place (the input itself). `NOT` is the complementary emptiness test. `NOT (NOT e)` denotes the same function as `IF e`; `IF` is the readable spelling.

Motivating examples from [[30-text-operations.md]] (Answers are Nodes):

- `(nodes) IF (left 5 is "rapid")`
- `(nodes) IF (name right 4 is ".txt")`

Today, without text ops: `(nodes) IF child` keeps each Node that has Children.

## Open questions (HITL)

- Spelling: reserved `IF` (capitals only), parallel to `NOT`?
- Does a combinator row in [[../spec.md]] chapter 7 get `IF` next to `NOT` / `outer`?

## Out of scope

- Text catalog rows and Node→Text coercion ([[30-text-operations.md]]).
- Fused walk `outer` ([[28-outer-prefix-combinator.md]]).
- Product F# in this planning issue.

## Comments

# 15 — Answer-sequence eval core and catalog row shape

**Context:** The Expression language spec defines every term as a function from one input Answer to an ordered Answer sequence. Before path clusters, walk words, or consumers can land, Shared needs one evaluation foundation and a catalog row shape that every later ticket plugs into. Work on branch `w/expr` (cut from `selective-client-sync`).

**What to build:** Introduce the Shared answer-sequence evaluation core so later catalog rows compose by bind, `OR`, `AND`, and `NOT` without ad hoc special cases. Answer equality follows Node identity and Text string equality. A walk that needs Children of an Unloaded Node yields no Answers (a miss, never an error). The catalog is data: each row has spellings, an optional argument slot, a signature, and an Answer function hook that later tickets fill in.

**Blocked by:** None — can start immediately.

**See also:** [[.scratch/expression-language/spec.md]] chapters 2 and 6; [[.scratch/expression-language/reports/spec-abstraction-core-and-barriers.md]] Part 1.

**Status:** done

- [x] Shared tests prove bind concatenates left-to-right Answer sequences in order.
- [x] `OR` concatenates operand sequences and may repeat an Answer; `AND` keeps left-operand order with at-most-once intersection by Answer equality; `NOT` yields the input when the operand sequence is empty and otherwise yields nothing.
- [x] A catalog row type holds spellings, slot, signature, and Answer function; a minimal stub row can be registered and invoked through the core.
- [x] Evaluating through an Unloaded Node boundary yields an empty sequence, not an exception or Load side effect.

# 23 — AND, OR, NOT, and comma combinators

**Context:** Boolean-style control is sequence algebra, not a separate type. Combinators must compose with juxtaposition at the locked precedence. Work on branch `w/expr` (cut from `selective-client-sync`).

**What to build:** Parse and evaluate `OR`, comma, `AND`, and `NOT` at spec precedence: juxtaposition tightest, then `NOT`, then `AND`, then `OR` and comma. `OR` and comma concatenate Answer sequences and may repeat. `AND` keeps left-operand order with at-most-once intersection by Answer equality. `NOT` is negation-as-failure on the operand sequence from the same input. Mixed operand types across combinators is a type error. Parentheses group sub-Expressions where precedence requires them.

**Blocked by:** none.

**See also:** [[plan/expression-language/issues/04-boolean-operators-as-control.md]]; [[plan/expression-language/issues/05-how-multiple-answers-surface.md]].

**Status:** done

- [x] `#x , #y` concatenates Answers from both searches; a Node may appear twice.
- [x] `containing "the" AND named "blue"` keeps the current Node only when both pure filters succeed on the same input.
- [x] `root descendant NOT containing "draft"` keeps descendants where the inner predicate yields nothing from that Node.
- [x] `d AND b OR c` parses as `(d AND b) OR c` by precedence.

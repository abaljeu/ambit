# Boolean operators as control

Type: grilling
Status: resolved
Blocked by: none

## Question

What is the concrete `and` / `or` / `not` syntax, and how do fail and succeed work, if booleans are control rather than values?

Recommended answer (HITL confirm): conjunction is default pipeline composition (space). Explicit disjunction is the word `or` (optional `;`). Negation-as-failure is the word `not`. There is no Boolean Answer type in the first spec. Do not use `,` for conjunction; Amble already uses `,` to concatenate.

[[plan/expression-language/issues/03-first-primitive-catalog.md|First primitive catalog]] locked composition versus `AND` versus comma/`OR` versus `not`. Remaining here: what that ticket did not settle — for example precedence, and letter-case of `AND` / `OR`.

## Answer

HITL 2026-08-27. Prolog is semantically similar, not syntactically. There is no `;`. The reserved words are `AND`, `OR`, and `NOT` (all caps). Lowercase `and` / `or` / `not` is a name, not an alias. Comma `,` is `OR`. No Boolean Answer type (already locked on [[03-first-primitive-catalog.md]]).

`AND`, `OR`/comma, and `NOT` are combinators, not pipeline words. Precedence, tightest first: juxtaposition (composition), then `NOT`, then `AND`, then `OR`/comma. So `a b AND c OR d` is `((a b) AND c) OR d`. `left NOT named x` takes `named x` as the predicate.

Same-operator `AND` and `OR` are semantically associative. The parse tree of `a AND b AND c` does not matter. Mixed `d AND b OR c` is legal as `(d AND b) OR c` by precedence. It is confusing; write `(d AND b) OR c` or `d AND (b OR c)`.

`NOT` is negation-as-failure. For each left Node, if the predicate yields any Answer, drop that Node; if it yields zero Answers, keep that left Node. Succeed is the left Node; fail is 0 Answers. A compound predicate needs parentheses: `left NOT (containing "the" AND named "blue")`.

This revises [[03-first-primitive-catalog.md]] from lowercase `not` to `NOT`.

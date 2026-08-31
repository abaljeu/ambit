# Keep or drop Amble of and comma

Type: grilling
Status: resolved
Blocked by: none

## Question

Amble infix `of` and comma `,` were sugar for prefix `FunCall` ([[doc/roadmap/language-syntax-and-semantics.md]]). [[.scratch/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md]] locked juxtaposition as left-associative anchors, postfix, and infix; space only lexes; prefix `FunCall` is not the meaning. Keep `of` and `,` as sugar on that surface, or drop them from this spec?

Recommended answer (HITL confirm): drop `of` (it existed to nest prefix calls). Keep `,` only if a later catalog needs concatenate; do not treat `,` as conjunction.

[[.scratch/expression-language/issues/03-first-primitive-catalog.md|First primitive catalog]] keeps comma as `OR` / concatenation. `of` is still this ticket.

## Answer

HITL 2026-08-27. Drop `of`. Drop Amble comma-as-`FunCall` sugar. Comma stays as `OR` / concatenation, already locked on [[03-first-primitive-catalog.md|First primitive catalog]].

`sort 3,5,2` is not defined. A number is only valid as the right operand of `:` or `!`.

Reworked examples: [[doc/roadmap/language-syntax-and-semantics.md]].

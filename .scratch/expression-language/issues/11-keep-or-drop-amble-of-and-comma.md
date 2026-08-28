# Keep or drop Amble of and comma

Type: grilling
Status: open
Blocked by: none

## Question

Amble infix `of` and comma `,` were sugar for prefix `FunCall` ([[doc/roadmap/language-syntax-and-semantics.md]]). [[.scratch/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md]] locked juxtaposition as left-associative anchors, postfix, and infix; space only lexes; prefix `FunCall` is not the meaning. Keep `of` and `,` as sugar on that surface, or drop them from this spec?

Recommended answer (HITL confirm): drop `of` (it existed to nest prefix calls). Keep `,` only if a later catalog needs concatenate; do not treat `,` as conjunction.

[[.scratch/expression-language/issues/03-first-primitive-catalog.md|First primitive catalog]] keeps comma as `OR` / concatenation. `of` is still this ticket.

# Research: existing Amble and RefExpr seams the spec must not contradict

Type: research
Status: resolved
Blocked by: none

## Question

From the survey and the language/interpretation docs, which existing parse and eval seams must the spec not contradict? Include empty-match versus error, Amble juxtaposition, `text` eval, Find AND, and `**` Owned-only versus other steps following Ref.

## Answer

Empty miss is fail-to-answer, not silent success; parse errors stay distinct. Pipeline space must desugar into the same operators as Amble juxtaposition. Amble `,` stays concatenate. `text` already evaluates; Find AND is not the pipeline. `**` is Owned-only and stops at Directory/Workspace; other path steps follow Ref. Findings: [[plan/expression-language/reports/amble-refexpr-seams.md]].

# OUTER spec lock

Design A is locked in [[plan/expression-language/spec.md]]. First spelling was lowercase `outer`; user later locked `OUTER` (capitals, same class as `NOT`). No product code in this lock report. Project stage stays `active`. [[doc/]] is unchanged.

## Files changed

- [[plan/expression-language/spec.md]] — chapters 3 (reserved `OUTER`), 4 (grammar), 5 (type rule), 6 (evaluation), 7 (combinator row), 9 (new-surface list), 10 (defer sugar and Ref analog), 11 (`root OUTER containing "blue"`)
- [[plan/expression-language/reports/tree2-semantics.md]] — spelling and fusion closed; `tree2` remains history only
- [[plan/expression-language/issues/28-outer-prefix-combinator.md]] — implementation issue, Status `ready-for-agent`
- [[plan/expression-language/git.md]] — project branch `w/tree2-semantics`
- [[CONTEXT.md]] — glossary entry for `OUTER`

## WORK.md mutations (for the parent)

- `remove` [[plan/expression-language/reports/tree2-semantics.md]] — next: lock prefix combinator `outer` (fused Owned walk; canonical prune-during-accept algorithm) into [[plan/expression-language/spec.md]]; do not replace `tree`; do not implement a post-pass prune
- `add` [[plan/expression-language/issues/28-outer-prefix-combinator.md]] — implement `outer` per spec (fused Owned walk); do not replace `tree`; do not post-pass prune

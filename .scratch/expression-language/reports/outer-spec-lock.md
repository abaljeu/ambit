# outer spec lock

Design A is locked in [[.scratch/expression-language/spec.md]] with spelling `outer`. User confirmation: "Let's go with outer" and "plan A". No product code. Project stage stays `active`. [[doc/]] is unchanged.

## Files changed

- [[.scratch/expression-language/spec.md]] — chapters 3 (reserved `outer`), 4 (grammar), 5 (type rule), 6 (evaluation), 7 (combinator row), 9 (new-surface list), 10 (defer sugar and Ref analog), 11 (`root outer containing "blue"`)
- [[.scratch/expression-language/reports/tree2-semantics.md]] — spelling and fusion closed; `tree2` remains history only
- [[.scratch/expression-language/issues/28-outer-prefix-combinator.md]] — implementation issue, Status `ready-for-agent`
- [[.scratch/expression-language/git.md]] — project branch `w/tree2-semantics`
- [[CONTEXT.md]] — glossary entry for `outer`

## WORK.md mutations (for the parent)

- `remove` [[.scratch/expression-language/reports/tree2-semantics.md]] — next: lock prefix combinator `outer` (fused Owned walk; canonical prune-during-accept algorithm) into [[.scratch/expression-language/spec.md]]; do not replace `tree`; do not implement a post-pass prune
- `add` [[.scratch/expression-language/issues/28-outer-prefix-combinator.md]] — implement `outer` per spec (fused Owned walk); do not replace `tree`; do not post-pass prune

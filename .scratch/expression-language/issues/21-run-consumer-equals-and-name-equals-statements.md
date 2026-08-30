# 21 — Run consumer: `=` and `Name=` statements

**Context:** Run materialises Expression Answers as Children of the current Node. Only two statement forms are valid; other lines are ignored. Work on branch `w/expr` (cut from `selective-client-sync`).

**What to build:** Wire Run to accept `= Expression` and `Name=Expression` (optional whitespace around `=`). Apply the Expression to the current Node as initial Answer. Node Answers become Ref Children; Text Answers become new Owned Nodes, in Answer order, unfolding when Children are written. `Name=` also renames the current Node to Name. Parse error, type error, and zero Answers all write one blueletter Child `No matches found`. Reject `#ident = …` and other non-Name left-hand forms.

**Blocked by:** [[.scratch/expression-language/issues/19-spaced-pipeline-lexer-walk-words-and-juxtaposition.md]], [[.scratch/expression-language/issues/20-pure-filter-catalog-rows.md]].

**See also:** [[.scratch/expression-language/issues/07-statements-in-this-spec.md]]; [[.scratch/expression-language/spec.md]] chapter 8.

**Status:** done

- [x] `= root descendant named "blue"` on a matching graph writes Ref Children for each Node Answer in order.
- [x] `todo=root descendant named "blue"` renames the current Node to `todo` and materialises as `=`.
- [x] A line with no leading `=` (bare Expression) does nothing in Run.
- [x] Parse error, type error, and zero Answers each produce one blueletter Child `No matches found`.

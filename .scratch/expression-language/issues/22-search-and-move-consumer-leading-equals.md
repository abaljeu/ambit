# 22 — Search and Move consumer: leading `=`

**Context:** Search and Move dialogs can run an Expression against zoomRoot when the line begins with `=`. Without a leading `=`, the line stays today's word search outside this spec. Work on branch `w/expr` (cut from `selective-client-sync`).

**What to build:** When Search or Move receives a line starting with `=`, parse and evaluate the following Expression with zoomRoot as the initial Answer. Require a `Node ⇒ Node` result type. Present Answers in the scrolling picker; the user picks one Node and Zoom (Search) or relocates (Move) to it. Parse error, type error, and zero Answers show no hits (same merged outcome shape). Lines without a leading `=` keep legacy word-search behavior unchanged.

**Blocked by:** [[.scratch/expression-language/issues/19-spaced-pipeline-lexer-walk-words-and-juxtaposition.md]], [[.scratch/expression-language/issues/20-pure-filter-catalog-rows.md]].

**See also:** [[.scratch/expression-language/spec.md]] chapter 8; [[.scratch/expression-language/issues/06-top-level-context-node-versus-text.md]].

**Status:** done

- [x] `= root descendant containing "the"` in Search lists matching Nodes under zoomRoot; picking one Zooms to it.
- [x] Move `= Expression` relocates to the picked Node with the same Answer set as Search.
- [x] A `Node ⇒ Text` Expression shows no hits (type error merged into empty display).
- [x] A line without leading `=` still runs today's word search, not Expression evaluation.

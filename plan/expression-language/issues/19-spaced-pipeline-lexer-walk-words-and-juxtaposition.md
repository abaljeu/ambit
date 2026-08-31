# 19 — Spaced pipeline lexer, walk words, and juxtaposition

**Context:** Layer-one lexing splits spaced segments into path clusters, reserved words, numbers, and catalog Names. Walk words and juxtaposition compose full Expressions left-to-right on top of cluster evaluation. Work on branch `w/expr` (cut from `selective-client-sync`).

**What to build:** Implement layer-one segmentation and left-associative juxtaposition (monadic bind) for spaced pipeline Expressions. Register and evaluate the anchor and walk catalog rows: `root`, `child`, `descendant`, `tree`, `containing`, and `wsroot`. A standalone Name must resolve to a catalog row or parse as unknown. Juxtaposition concatenates Answer sequences in order (`root descendant containing "the"`). Reserve `AND`, `OR`, and `NOT` as spaced words only in capitals.

**Blocked by:** [[plan/expression-language/issues/15-answer-sequence-eval-core-and-catalog-row-shape.md]], [[plan/expression-language/issues/18-content-search-path-step-evaluation.md]].

**See also:** [[plan/expression-language/spec.md]] chapters 3 and 4; [[plan/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md]].

**Status:** done

- [x] `root descendant containing "the"` evaluates as bind: ROOT, then descendant closure, then Header substring filter.
- [x] `child` equals `:*`; `descendant` follows Ref; `tree` and `**` match the Owned-only closure row.
- [x] `// ws` is a parse error (spaced `ws` is a symbol, not a `/` argument); `//ws` is valid.
- [x] Unknown standalone words (not in the catalog) are parse errors.

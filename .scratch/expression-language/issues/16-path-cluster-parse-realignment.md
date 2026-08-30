# 16 — Path-cluster parse realignment

**Context:** Path clusters are the space-free segments that spell structural and content search steps. Today's RefExpr parser does not match the locked two-layer lexing and cluster grammar. Work on branch `w/expr` (cut from `selective-client-sync`).

**What to build:** Realign path-cluster parsing with the spec so clusters desugar and bind arguments uniformly. `//` is shorthand for `root /`, not its own row. `/` and `#` each require a name argument (cluster NamePattern or trailing quoted string). `:` and `!` require a signed integer or `*`. A cluster-leading name or a name after `^`, `.`, or `**` binds to an implicit `/`. Spaced quoted arguments work (`x / "a b"`). Bare `/`, bare `//`, bare `#`, and other unfilled required slots are parse errors with the same reporting shape as a missing `containing` string.

**Blocked by:** None — can start immediately.

**See also:** [[.scratch/expression-language/spec.md]] chapters 3 and 4; [[.scratch/expression-language/issues/02-path-references-as-pipeline-terms.md]].

**Status:** done

- [x] `//ws` and `root /ws` parse to the same cluster steps as `root / "ws"`; bare `//` and bare `/` are parse errors.
- [x] `a/b/c` parses as implicit `/ "a"`, then `/ "b"`, then `/ "c"` from the omitted left input.
- [x] `:*` and `!*` parse as child-all and sibling-all steps; `3` alone is a parse error.
- [x] `x / "filename with spaces"` and `x # "a b"` accept spaced quoted name arguments.

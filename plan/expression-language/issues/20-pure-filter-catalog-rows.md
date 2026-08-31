# 20 — Pure filter catalog rows

**Context:** Pure filters test the current input Node without walking Children. They compose with search and walk words through juxtaposition and later through `AND`. Work on branch `w/expr` (cut from `selective-client-sync`).

**What to build:** Add the pure filter catalog rows and evaluate them as subsequence tests on the input Answer. `named` takes a quoted name and yields the input when it is a Normal Node whose name matches the glob; otherwise yield nothing. `ws`, `dir`, `file`, and `normal` each keep the input when its Node classification matches. `class` takes a quoted token and keeps the input when that token is in the Node cssClasses list (exact, case-sensitive). These rows do not search Children; `#` remains the content-search row.

**Blocked by:** [[plan/expression-language/issues/19-spaced-pipeline-lexer-walk-words-and-juxtaposition.md]].

**See also:** [[plan/expression-language/spec.md]] chapter 7 filter rows; [[plan/expression-language/issues/03-first-primitive-catalog.md]].

**Status:** done

- [x] `named "blue"` on a matching Normal Node yields that Node; on a non-match or non-Normal Node yields nothing without walking Children.
- [x] `root ws` equals `root`; `x / "d" dir` keeps only Directory Nodes named `d` among structural matches.
- [x] `class "h1"` keeps a Node only when `h1` is an exact cssClasses member.
- [x] `containing "the" AND named "blue"` (once combinators exist) intersects on the same input; `named` does not replace `#` search semantics.

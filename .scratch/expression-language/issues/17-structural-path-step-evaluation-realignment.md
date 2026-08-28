# 17 — Structural path-step evaluation realignment

**Context:** Structural path steps (`/`, `**`, `^`, `.`, `:`, `!`) decide which Nodes a cluster yields before content search or pipeline words run. Today's walk rules diverge from the locked spec on Owned descent, tree scope, and glob metacharacters. Work on branch `w/expr` (cut from `selective-client-sync`).

**What to build:** Evaluate structural cluster steps per the catalog structural rows. `/` (including implicit `/` and the `//` desugar) searches Workspace Nodes, Directory Nodes, and File Nodes by Owned recursive descent strictly below the input, without entering Children of a Directory Node or Workspace Node. `**` and `tree` yield transitively Owned Nodes depth-first; they do not follow Ref and do not stop at directory or workspace boundaries. `^`, `.`, and `wsroot` walk up the Owned chain per their rows. `:` and `!` index Children and siblings per spec. Glob matching uses `*` only; retire `?` as a wildcard.

**Blocked by:** [[.scratch/expression-language/issues/16-path-cluster-parse-realignment.md]].

**See also:** [[.scratch/expression-language/spec.md]] chapter 7 structural search row; chapter 9 divergences 1, 3, 10, 13, and 15.

**Status:** done

- [x] `root / "ws" / "x"` and `//ws/x` yield the same structural Answers under ROOT in a fixture graph.
- [x] `**` from a Node matches `tree` and does not follow Ref Children; it differs from `descendant` when Refs are present.
- [x] Structural `/` does not recurse into Children of a Directory Node or Workspace Node; chaining reaches deeper names (`//ws/x`).
- [x] Glob patterns match with `*` only; a name containing `?` is literal, not a one-character wildcard.

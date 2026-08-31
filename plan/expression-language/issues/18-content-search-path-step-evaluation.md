# 18 — Content-search path-step evaluation (`#`)

**Context:** Content search is separate from structural `/` search. The `#` cluster step searches strictly below each input for named Normal Nodes through both Owned and Ref Children, with walls and deduplication rules that structural search does not use. Work on branch `w/expr` (cut from `selective-client-sync`).

**What to build:** Implement the `#` catalog row as content search below each input Node. Traverse Children (Owned and Ref) depth-first in Children order. Visit each Node at most once per input by Node identity; first reach wins. Unnamed Normal Nodes are transparent. Named Normal Nodes match when their name fits the glob and wall the search (do not enter their Children). Do not enter Children of File Nodes, Directory Nodes, or Workspace Nodes. Chained `#` steps search below each Answer from the prior step (`a#b#c`).

**Blocked by:** [[.scratch/expression-language/issues/17-structural-path-step-evaluation-realignment.md]].

**See also:** [[.scratch/expression-language/spec.md]] chapter 7 content search row; [[.scratch/expression-language/issues/12-owned-versus-ref-walk-for-descendant.md]].

**Status:** done

- [x] `d#e` and `/ "d" # "e"` yield the same Normal Nodes named `e` strictly below each structural `d` match.
- [x] A named Normal Node that matches stops descent; a named non-match is a wall; unnamed Normal Nodes are walked through.
- [x] Ref Children are followed; duplicate Node identity within one `#` search from one input appears at most once.
- [x] `a#b#c` applies each `#` search below the Answers of the prior step in order.

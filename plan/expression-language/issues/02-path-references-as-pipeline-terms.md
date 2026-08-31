# Path references as pipeline terms

Type: grilling
Status: resolved
Blocked by: none

## Question

How do `/`, `//`, `#tag`, `^`, `.`, `*`, and `**` appear inside the pipeline? Are they generators, literals, or mixed syntax?

Recommended answer (HITL confirm): a path RefExpr remains a term and generator that yields Nodes. Words are relations over Node sets. Example: `// descendant containing "the"`. A path-only expression stays valid.

See the Answer on [[.scratch/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md]]: the lexer stays; `#todo` is two tokens (`#` then `todo`) and `(left) #todo` means `(left) tagged "todo"`.

## Answer

HITL 2026-08-27. Path symbols are pipeline terms (postfix and infix functions), not a second language. A path-only Expression is juxtaposition of these functions. One rule: every path operator is one function of its left-hand Nodes. There is no variant that searches up from an initial context and down on later steps. When the left side is omitted, the left-hand Node is the current Node (same as [[.scratch/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md|Pipeline versus Amble juxtaposition]]).

Every path operator is a Prolog-style predicate. It finds 0, 1, or many Answers. Failure is zero Answers, not an error. Example: `x!-249053534` finds 0 Answers (out-of-range sibling index). The same holds for out-of-range `:n` and a name, `#`, or `/` search that matches nothing. This is the empty-miss rule in [[doc/roadmap/reference-expression-interpretation.md]] and the fail-to-answer rule from [[.scratch/expression-language/issues/09-research-prolog-control-mapped.md|Research: Prolog control mapped to this language]]. Index, glob, and `!n` are not exceptions.

**Anchors and up**

- `//` — ROOT (a value).
- `^` — from each left Node, walk up to the nearest structural container (File Node, Directory Node, or Workspace Node). Intent unchanged.
- `.` — from each left Node, walk up to the nearest Directory Node or Workspace Node.

**`//name` and `/`**

- `//name` — search from ROOT for `name` with the path-search ruling in [[doc/roadmap/reference-expression-interpretation.md]] (so `//workspacename` is that Workspace Node).
- `/` as a postfix filter: keep Directory Node and Workspace Node. Do not drop a Workspace Node. Drop File Node and Normal Node. `/` is not a prefix. Bare `/` is undefined.
- After a kept Workspace Node or Directory Node, a following name is documented path search: `//ws/x` searches for File Node `x`, or Directory Node `x` with `x/`. `/x` is not tag search.

**`#` (tag / Normal names)**

- No bare `#` (the old nearest named ancestor is gone). `#x` always searches down from the left.
- `#x` finds Normal Nodes named `x`. Unnamed Nodes are transparent. A named descendant is a wall: search does not enter it. The left Node itself is not a wall.
- If Focus is named `todo` or is a Child of `todo`, `#blue` finds `blue` under `todo`. `^#blue` searches from the structural container, hits `todo`, and yields no Answer. `^#todo` is that `todo`. `^#todo#blue` finds `blue`.
- `/x` and `#x` are two name searches: path File Node / Directory Node versus Normal / tag.

**Glob**

- `*` is a standard glob on a name. `tagged "re*ed"` and `#re*ed` are the same.

**`**`**

- Same as `tree`: transitively Owned Nodes; acyclic; does not follow Ref; no Directory/Workspace stop. Not the word `descendant`. Walk words: [[12-owned-versus-ref-walk-for-descendant.md|Owned versus Ref walk for descendant]].

**`:` and `!`**

- `x:n` — the Child of `x` at index `n`. `x!n` — the sibling of `x` at offset `n`. `!0` is `x`.
- `x:` and `x!` are not defined (drop old “all Children” and “identity”).
- `x:*` — every Child of `x`. `x!*` — every sibling of `x` including `x` (the parent’s Children). Omitted `x` is allowed (`:*`, `!*`).
- A number is only valid as the right operand of `:` or `!`. Anywhere else is a type error.

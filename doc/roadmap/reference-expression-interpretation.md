# Reference expression interpretation

## Content

**Content** is the tree of `normal` nodes owned beneath a `workspace`, `directory`, or `file`
node. Structural children (`workspace`, `directory`, `file`) are outside content.

## Interpretation

All interpretation is relative to the **context** of a node: its ancestors in the ownership tree
(a concept, not an operator).

Let `w`, `d`, `f`, `t`, `x` be string variables that resolve respectively to:

- a workspace node
- a directory node
- a file node
- a tagged (i.e. named) normal node
- any node

These are F# string expressions; a function maps them to a flat list of `Node`.

**Match** — each step may match multiple nodes. The result is the flat list of all combinations
(e.g. two `x` and two `y` under `/x/y` → four nodes).

Named search follows **owned** children only; `Ref` edges are excluded.

## Anchors

From the context node, walking **up the ownership chain** (including self):

- `/` — nearest `workspace` ancestor (ROOT when no other `workspace` in the chain).
- `.` — nearest `directory` or `workspace` ancestor (current directory).
- `^` — nearest `file`, `directory`, or `workspace` ancestor (current structural container).
- `#` — nearest named `normal` ancestor (current tagged node).

`#` alone is an anchor; `#name` is a tagged search step (see **Tagged nodes**).

Other anchors:

- `//` — always ROOT.
- `@w:` — workspace named `w`.
- **Current view root** — client view-root node (`siteMap.rootId`); supplied at interpretation time,
  not from ownership ancestry.

## Path steps

From base `x`:

- `x + "name/"` — a `directory` matching `name`.
- `x + "name"` — a `file` matching `name`.

Locate `name` by recursive descent through **owned** children from `x`. Recursion does not
enter children of `directory` or `workspace` nodes.

## Tagged nodes

From base `x`:

- `x + "#name"` — a `normal` node whose `name` matches `name` (unnamed normals do not match).

Search **within content** only: recursive descent through owned `normal` nodes under the
relevant `workspace`, `directory`, or `file`. Recursion does not enter children of `file`,
`directory`, or `workspace` nodes.

Composition (each step uses the prior step’s matches as `x`):

- `x + "#name" + "name"` — a `file` named `name` under a tagged node.
- `x + "#name" + "#name"` — a tagged node under another tagged node.

## Wildcards

In a name or `#tag` pattern, `*` matches zero or more characters (including none). Matching is
case-insensitive, with usual single-segment glob / `fnmatch` semantics.

`**` is a multi-level wildcard with usual glob semantics: it matches zero or more descent levels
within the current search scope (path steps or content, per the step kind).

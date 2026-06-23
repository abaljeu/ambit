# Reference expression interpretation

Reference expressions resolve owned nodes from a context node. They are a small path language, not the full command or expression language. Surrounding language forms such as `text Ref` and `children Ref` are described in [[doc/roadmap/language-syntax-and-semantics.md]].

## Content

**Content** is the tree of `normal` nodes owned beneath a `workspace`, `directory`, or `file` node. Structural children (`workspace`, `directory`, `file`) are outside content.

Named search follows **owned** children only; `Ref` edges are excluded.

## Grammar

```ebnf
RefExpr     ::= (Anchor | ε) (Sep? Step)*

Sep         ::= "/"

Anchor      ::= "//"
              | "/"
              | "."
              | "^"
              | "#"

Step        ::= "**"
              | TagStep
              | DirStep
              | FileStep
              | IndexStep
              | ChildStep

TagStep     ::= "#" NamePattern
DirStep     ::= NamePattern "/"
FileStep    ::= NamePattern
IndexStep   ::= "!"
              | "!" SignedInt
ChildStep   ::= ":"
              | ":" SignedInt

SignedInt   ::= ["+" | "-"] Digit+

NamePattern ::= NamePart+
NamePart    ::= character except "/", "#", "^"
              | "*"
```

`//` is tokenized before `/`. `**` is tokenized before `*` inside a name pattern.

Quoted path segments are not part of reference-expression syntax. Bracket postfix (`[n]`) and filters are not part of reference-expression syntax.

## Interpretation

All interpretation is relative to the **context** node and its ancestors in the ownership tree.

Each step may match multiple nodes. The result is the flat list of all combinations. For example, if two `x` nodes each contain two `y` nodes, `/x/y` resolves to four nodes.

## Anchors

From the context node, walking **up the ownership chain** including self:

- `/` resolves to the nearest `workspace` ancestor, or ROOT when no other `workspace` is in the chain.
- `.` resolves to the nearest `directory` or `workspace` ancestor, the current directory.
- `^` resolves to the nearest `file`, `directory`, or `workspace` ancestor, the current structural container.
- `#` resolves to the nearest named `normal` ancestor, the current tagged node.
Other anchors:

- `//` resolves to ROOT.

Named workspace access is ordinary ROOT-relative path lookup. A standard workspace node name starts with `@`, so `//@workspaceName/...` resolves beneath that workspace.

The current view root is not represented in `PathExpr`. If the client needs a view-root-relative reference, it is an external interpretation concern supplied outside this parser.

## Path steps

From base `x`:

- `x + "name/"` resolves directories matching `name`.
- `x + "name"` resolves files matching `name`.

Locate `name` by recursive descent through **owned** children from `x`. Recursion does not enter children of `directory` or `workspace` nodes.

## Tagged nodes

From base `x`:

- `x + "#name"` resolves `normal` nodes whose `name` matches `name`. Unnamed normals do not match.

Search **within content** only: recursive descent through owned `normal` nodes under the relevant `workspace`, `directory`, or `file`. Recursion does not enter children of `file`, `directory`, or `workspace` nodes.

Composition uses the prior step's matches as the next base:

- `x + "abc" + "#xyz"` search each x match for file `abc`; each match find normal node named xyz.
- `x + "#def" + "#ghi"` find normal node #def in each node in x.  Find ghi under def.
- `x + "#abc" + "/def"` searches each x for normal nodes named `abc`; each match searches for files named `def`.

## Indexing

From base `x`:

- `x + "!"` resolves to `x`.
- `x + "!nn"` resolves to the sibling of `x` that is `nn` steps away in parent child order. Positive `nn` moves toward later siblings; negative `nn` moves toward earlier siblings.
- `x + ":"` resolves to all owned children of `x`.
- `x + ":nn"` resolves to the owned child of `x` at zero-based index `nn` in parent child order.

Out-of-range sibling offsets and child indices resolve to nothing for that base. If every base fails, the step yields an empty result; this is not a parse or resolution error.

`!`, `!nn`, `:`, and `:nn` are path steps, not anchors.

## Wildcards

In a path or tag name pattern, `*` matches zero or more characters in a single segment. Matching is case-insensitive.

`**` is a multi-level wildcard. It matches zero or more descent levels within the current search scope: path steps search structure, and tag steps search content.

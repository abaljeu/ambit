# Language syntax

This is the draft syntax for the small functional language that hosts reference expressions. Reference-expression parsing and interpretation are specified separately in [[doc/roadmap/reference-expression-interpretation.md]].

## Expressions

Reference expressions are first-class expression values. They can be passed to functions directly.

```ebnf
Expr        ::= RefExpr
              | Name
              | String
              | FunctionApplication
              | "(" Expr ")"

FunctionApplication ::= Name Expr+
```

Function application is prefix-style and whitespace-separated.

## Reference functions

These functions operate on the node list resolved by a reference expression:

- `text Ref` returns text from each resolved node.
- `name Ref` returns names from resolved nodes that have names.
- `children Ref` returns direct owned children of each resolved node.

`Ref` is any expression that resolves to nodes, usually a reference expression such as `#todo`, `^/notes.md`, or `//@workspaceName/src/`.

## Examples

```text
text #todo
name ^/notes.md
children //@workspaceName/src/
```

Property-like access such as `Ref.text`, `Ref.name`, and `Ref.children` is not reference-expression syntax. Filtering and indexing are also outside the current reference-expression syntax.

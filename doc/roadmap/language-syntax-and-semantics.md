# Syntax and Semantics

The language described below has 3 contextual uses:
1) Interactive search queries to find nodes
2) File persistence, to capture links to other nodes.
These deal strictly with reference expressions.

3) Executable nodes in the graph, which when executed compute subnodes.

## Language syntax

This is the draft syntax for the small functional language that hosts reference expressions. Reference-expression parsing and interpretation are specified separately in [[doc/roadmap/reference-expression-interpretation.md]].

### Expressions

Expressions are first-class expression values. They can be passed to functions directly.

```ebnf
Expr        ::= RefExpr
            | String
            | Number
            | FunCall
            | "(" Expr ")"
FunCall     ::= Function "of" Expr
            | Function Expr+
```
### Statements

```ebnf
Statement   ::= Assignment
            | Expr
            | Command

Assignment ::= Name "=" RefExpr
Command ::= ">" CmdLine
```

### Shell commands

Shell commands run external programs on the server. The line after `>` mixes literal argv tokens with embedded expressions. Reference-expression syntax is in [[doc/roadmap/reference-expression-interpretation.md]].

```ebnf
CmdLine     ::= Stage ("|" WS Stage)*
Stage       ::= StagePart (WS StagePart)*
StagePart   ::= ShellWord | Redir
ShellWord   ::= RefExpr | String | BareWord | "(" Expr ")"
Redir       ::= "<" Expr
            | ">" Expr
            | ">>" Expr
BareWord    ::= BareStart BareChar*
BareStart   ::= character except WS, quote, "(", "<", ">", "|", "#", "/", "^", "."
BareChar    ::= character except WS, quote, "<", ">", "|"
```

**Disambiguation.** `RefExpr` tokens expand before spawn; `BareWord` and quoted `String` tokens do not. `( Expr )` evaluates before spawn and may use functions such as `text Ref`. An unquoted ref-like token that fails to parse or resolve is an error, not a literal fallback.

**Redirections and pipes.** `< RefExpr` supplies stdin from resolved node text. `> Expr` and `>> Expr` write or append stdout to resolved file nodes. `< ( Expr )` is valid when the expression yields a path string; string content belongs in argv, not stdin. `|` connects the stdout of one stage to the stdin of the next.

## Semantics

### Expressions

RefExpr computes a list of nodes, according to [[doc/roadmap/reference-expression-interpretation.md]]

Other expressions may resolve to strings, numbers, or lists of them.

### Assignment
An assigment is functionally interpreted the same as the expression.
Additionally it sets the name of the node

### Shell commands

Evaluation resolves each embedded `RefExpr` or `( Expr )` in argv before spawn. File nodes become filesystem paths; string expressions become argv text. Redirection expressions are resolved first; resolution failure aborts the command with a diagnostic and no spawn.

| Form | Resolves to |
|------|-------------|
| file ref in argv | disk path passed as one argv element |
| `( Expr )` in argv | evaluated string passed as one argv element |
| `< RefExpr` | UTF-8 stdin from resolved node text |
| `< ( Expr )` | stdin from a path string (unusual) |
| `> Expr` | stdout written to file node |
| `>> Expr` | stdout appended to file node |
| `Stage \| Stage` | left stdout piped to right stdin |

When a ref resolves to multiple nodes, the command runs once per target (cartesian product with other multi refs) unless a step restricts to one node (`!0`, `:0`, etc.).

Parse and resolution errors follow the command result contract in [[doc/roadmap/reference-expressions.md]]: no silent empty success.

### Functions
## Reference functions

These functions operate on the node list resolved by a reference expression:

- `text Ref` returns text from each resolved node.
- `name Ref` returns names from resolved nodes that have names.
- `children Ref` returns direct children, including refs, of each resolved node.

`Function "of" Expr` is equivalent to applying the function on the left to the value of `Expr` on the right. E.g. `name of children ./folder/` applies `name` to the nodes returned by `children ./folder/`.

`Ref` is any expression that resolves to nodes, usually a reference expression such as `#todo`, `^/notes.md`, or `//@workspaceName/src/`.

## Examples

```text
text #todo
name ^/notes.md
children //@workspaceName/src/
name of children ./folder/
> python //@ws/rugby.py < #rugbydata
> tool --data (text #rugbydata)
> python ./rugby.py --verbose
> tool "/arg=x" --option p > ^/out.log
> python ./step1.py < #rugbydata | python ./step2.py > ^/result.txt
```

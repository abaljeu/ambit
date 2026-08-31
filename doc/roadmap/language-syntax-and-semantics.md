# Syntax and Semantics

This file descripts the mostly functional Amble language.  
Refer to [[src/Shared/AmbleTypes.fs]]

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
Expr        ::= Number | RefExpr | String | "(" Expr ")" | FunCall | ">" CmdLine

FunCall     ::= Function Juxtapose+
            | Juxtapose "," InfixExpr

InfixExpr   ::= Juxtapose
            | Juxtapose "," InfixExpr

Juxtapose   ::= Function Juxtapose+ | Primary
Primary     ::= Number | RefExpr | String | "(" Expr ")"
Number      ::= SignedInt | SignedFloat
SignedInt   ::= ["+" | "-"] Digit+
SignedFloat ::= SignedInt "." Digit+
Name        ::= NameChar+
NameChar    ::= letter | digit | "@" | "." | "-" | "_" | "?" | "*"
Function    ::= Name
```

Juxtaposition is left-associative postfix (`#todo text`). The `FunCall` production above is the old prefix shape and does not match these examples. See [[plan/expression-language/spec-draft.md]].

`Name` and `NameChar` match `RefExprParse.isNameChar` / `RefExpr.readName` (including `.`). Numbers are signed integers or floats with a single `.` fractional part; no `e` / `E` exponent notation.

**Number vs RefExpr.** `Primary` tries `Number` before `RefExpr`. A valid number literal never parses as a reference expression: e.g. `123`, `-3`, and `1.5` are numbers, not bare file steps. If digits are followed immediately by ref continuation characters such as `/`, `#`, `:`, or `!`, the whole primary is parsed as `RefExpr` instead (e.g. `123/foo`). `.5` is not a float; lexically it is one ref name token (`.5`), not `CurrentDir`. `.amb` is likewise one name token, not `CurrentDir`.

**Anchored references.** Only Amble requires every `RefExpr` to begin with an explicit anchor (`//`, `/`, `^`, `#`, `!`, or `!nn`) or a current-directory base (`.` alone or `./…`; see [[doc/roadmap/reference-expression-interpretation.md]]). Search and persistence do not mandate either; implicit context (e.g. `:0`, `.amb`, a bare name) is valid there and is a parse error in Amble.

**Comma.** `,` is `OR`. It concatenates Answer streams. It binds less tightly than juxtaposition. `#list , (#list sort)` concatenates the `#list` stream with the sorted `#list` stream. `of` is not in this language. `sort 3,5,2` is not defined.

### Statements

```ebnf
Statement   ::= Assignment | Expr

Assignment ::= Name "=" Expr
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

These functions take a Node stream on the left (postfix):

- `text` returns text from each Node.
- `name` returns names from named Nodes.
- `child` returns Children (Owned and Ref) of each Node.
- `sort` sorts Nodes by their text. `sort 3,5,2` is not defined.
- `,` is `OR`: it concatenates two Node streams.

`of` is not in this language. Prefix `text Ref` is a type error; the form is `Ref text`.

`Ref` is any Expression that yields Nodes, usually a path such as `#todo`, `^/notest.md`, or `//workspaceName/src/`.

## Examples

```text
#todo text
^/notest.md
//workspaceName/src/ child
./folder/ child name
#list , (#list sort)
> python //ws/rugby.py < #rugbydata
> tool --data (text #rugbydata)
> python ./rugby.py --verbose
> tool "/arg=x" --option p > ^/out.log
> python ./step1.py < #rugbydata | python ./step2.py > ^/result.txt
```

`#list , (#list sort)`: comma is `OR`. `#list` applies to the current Node and yields Nodes. The second `#list` does the same. `sort` sorts those Nodes by their text. The result is the list appended to itself; the second half is in order. `sort 3,5,2` is not defined.

# Reference Expressions

Status: Partially superseded
Authority: Reference **interpretation** semantics — [[doc/roadmap/reference-expression-interpretation.md]].
See also: [[doc/roadmap/revising-workspace-file-model.md]], [[doc/roadmap/workspace-file-model.md]],
[[doc/roadmap/workspace-stage-plan.md]], [[doc/reference/style.md]]

Expression/command language beyond reference resolution remains draft here.

Scope note: current stage implementation scope is defined separately in
[[doc/roadmap/workspace-stage-plan.md]].

## Interpretation

See [[doc/roadmap/reference-expression-interpretation.md]] for anchors, path steps, tagged nodes,
wildcards, match semantics, property access, filtering, and content.

## Expression Syntax

Surface syntax for reference expressions. Whitespace around `/` is optional (`a/b` and `a / b` are
equivalent). Interpretation semantics: [[doc/roadmap/reference-expression-interpretation.md]].

```ebnf
Expression ::= RefExpr
             | Primary

RefExpr      ::= ( Anchor | ε ) ( Sep? Step )* Postfix*

Sep          ::= "/"

Anchor       ::= "//"
               | "/"
               | "."
               | "^"
               | "@" identifier ":"
               | "#" AnchorEnd          (* tagged anchor; see note *)

AnchorEnd    ::= ε                       (* # alone: not followed by NamePattern *)

Step         ::= "**"
               | TagStep
               | DirStep
               | FileStep

TagStep      ::= "#" NamePattern
DirStep      ::= NamePattern "/"
FileStep     ::= NamePattern FileEnd

FileEnd      ::= ε                       (* not immediately followed by "/" *)

NamePattern  ::= string
               | Identifier             (* may contain * wildcards *)

Postfix      ::= Property
               | Filter

Property     ::= "." "text"
               | "." "name"
               | "." "children"

Filter       ::= "[" integer "]"
               | "[" "text" "~=" Pattern "]"
               | "[" "kind" "=" KindName "]"

Pattern      ::= string
               | Identifier             (* wildcard semantics *)

KindName     ::= identifier

Primary      ::= identifier
               | identifier "(" Args ")"
               | string
               | "(" Expression ")"

Args         ::= Expression ( "," Expression )*
               | ε
```

**Notes**

- `ε` as `( Anchor | ε )` — when no `Anchor` is given, the context node is the base.
- `#` **anchor** (`AnchorEnd`) — `#` not followed by a `NamePattern` (e.g. `#`, `#/foo`,
  `#.text`). `#` followed immediately by a name starts a `TagStep` (e.g. `#blue`).
- `//` is tokenized before `/`.
- `**` is tokenized before `*` within steps.
- `DirStep` trailing `/` distinguishes directories from files (`dir/` vs `file`).
- `Postfix` applies to the flat list produced by the preceding chain.

### Examples

| Expression | Meaning |
|------------|---------|
| `@ws:src/utils.fs` | workspace `ws`, directory `src/`, file `utils.fs` |
| `/proj/docs/` | workspace in context, directory `proj/`, directory `docs/` |
| `.` | current directory anchor |
| `^` | current structural container (`file`, `directory`, or `workspace`) |
| `#` | current tagged (`named`) normal ancestor |
| `#todo` | tagged nodes named `todo` from context base |
| `#todo/notes.md` | file `notes.md` under tagged `todo` |
| `#a/#b` | tagged `b` under tagged `a` |
| `^/**/*.md` | files matching `*.md` at any depth under `^` |
| `/x/y[0]` | first match of workspace-relative path `/x/y` |
| `^/src[text ~= *test*]` | nodes under `^/src` whose text matches `*test*` |

## Statement Syntax
(incomplete)
Assignment ::= "=" Expression
             | "#" identifier "=" Expression

Statement  ::= Assignment | Command

## Usage

These expressions will be employed in model language. The result of an expression will be an array
of nodes, or sometimes one. The language apart from these references can also be used to compute
other datatypes of info.
Examples below are tentative ideas.

`[[ref]]` could establish a link to the reference.
`>ls ref` could resolve ref to something the shell likes and execute `ls`.
`=ref` resolves `ref` to a list of nodes, puts those nodes as children of the current node
`#x=ref` is the same and names the current node `x`.
`ref` or `ref anything` is not valid.  #x above only accepts an identifier.

Assignment semantics note: assignments always target the current node context.

## Error Handling Semantics

The expression language must not fail silently.

### Parse Errors (syntax)

If input cannot be parsed as a valid expression or statement:

- evaluation does not run
- no graph mutation is applied
- the client surfaces a syntax diagnostic at the command/input location

Examples:

- unclosed string
- unexpected token
- incomplete step chain

### Resolution Errors (undefined references)

If parsing succeeds but any required reference cannot be resolved:

- evaluation is marked unresolved/failed
- no graph mutation is applied for mutation statements
- the client surfaces which reference segment failed to resolve

Examples:

- unknown workspace label in `@workspace:`
- missing member under a resolved namespace base
- unresolved tag or member selector in a context where at least one target is required

### Command Result Contract

Execution returns an explicit result kind:

- success with resolved targets/value
- syntax error with diagnostic payload
- resolution error with diagnostic payload

The language runtime must return one of these explicitly; it must not collapse failures into
an empty success result.

### UI Requirement

When syntax or resolution errors occur, the user must receive immediate visible feedback.
Bias to squiggle indicators and similar lightweight feedback.
No-op without feedback is invalid behavior for this language.

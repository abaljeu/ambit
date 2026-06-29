# Reference Expressions

Status: Partially superseded
Authority: Reference **interpretation** semantics — [[doc/roadmap/reference-expression-interpretation.md]].
See also: [[doc/roadmap/revising-workspace-file-model.md]], [[doc/roadmap/workspace-file-model.md]],
[[doc/current/workspace-stage-plan.md]], [[doc/reference/style.md]]

Expression/command language beyond reference resolution remains draft in [[doc/roadmap/language-syntax.md]].

Scope note: current stage implementation scope is defined separately in
[[doc/current/workspace-stage-plan.md]].

## Interpretation

See [[doc/roadmap/reference-expression-interpretation.md]] for anchors, path steps, tagged nodes, wildcards, match semantics, and content.

## Expression Syntax

This document no longer duplicates the active grammar. Current reference-expression syntax is in [[doc/roadmap/reference-expression-interpretation.md]]. Surrounding language syntax, including function application forms such as `text Ref`, `children Ref`, and `name Ref`, is in [[doc/roadmap/language-syntax.md]].

### Examples

| Expression | Meaning |
|------------|---------|
| `//@workspaceName/src/utils.fs` | workspace `@workspaceName`, directory `src/`, file `utils.fs` |
| `/proj/docs/` | workspace in context, directory `proj/`, directory `docs/` |
| `.` | current directory (`.` alone or `./…`; not an anchor) |
| `^` | current structural container (`file`, `directory`, or `workspace`) |
| `#` | current tagged (`named`) normal ancestor |
| `#todo` | tagged nodes named `todo` from context base |
| `#todo/notes.md` | file `notes.md` under tagged `todo` |
| `#a/#b` | tagged `b` under tagged `a` |
| `^/**/*.md` | files matching `*.md` at any depth under `^` |

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

- unknown workspace node in `//@workspaceName`
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

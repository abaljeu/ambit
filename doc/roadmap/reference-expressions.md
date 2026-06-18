# Reference Expressions

Status: Not implemented (target design)
Authority: Design intent only; current behavior is defined in [[doc/arch.md]] and [[doc/api.md]].
See also: [[doc/roadmap/workspace-file-model.md]], [[doc/roadmap/future-merge-sync.md]], [[doc/reference/style.md]]

We will have an expression/command language.  This file defines basic reference expressions that will typically resolve to an array of nodes.

Scope note: this is a target-scope language design document.
Current stage implementation scope is defined separately in
[[doc/roadmap/workspace-stage-plan.md]], which is intentionally narrower.

## Namespace Semantics

Authority: [[doc/roadmap/revising-workspace-file-model]], [[doc/roadmap/workspace-file-model.md]].

References resolve from the **context** of a node. Context is special-node ancestry along the owner
chain (`workspace`, `directory`, `file`; `normal` nodes are skipped).

| Base | Resolves to |
|------|-------------|
| `@label:` | Named workspace root |
| `/` | Root of the current workspace namespace |
| `./` | Current directory (directory context) |
| `^` | Nearest owning special node (`workspace`, `directory`, or `file`) |

Member lookup uses `dir / member` steps within the namespace established by the base. Directory
members are not globally addressable without first naming the directory. `^name` finds `name` as a
direct member under the owning special node.

Member-name matching is case-insensitive and uses standard operating-system-style wildcard matching,
including `*` and `?`.

## Model Elements

Workspace, directory, and file identity rules are defined in [[doc/roadmap/workspace-file-model.md]].

Expressions run from a node context. A node usually sits inside a file-owned subtree. A file node
owns a subtree of the graph. Workspace, directory, and file nodes are first-class special nodes in
the ownership tree — not a separate path-to-node table.

Within a namespace scope, nodes may carry tags (see `#` steps below). Not every node is tagged.
Tags may repeat within or across different files.

## Expression Syntax

`/` between steps is namespace member lookup, not filesystem path syntax. Whitespace around `/` is
optional (`dir/member` and `dir / member` parse the same).

```ebnf

Expression ::= RefExpr

RefExpr    ::= Base                    (* Evaluates to the base node itself *)
             | Base Step               (* e.g., ^ #blue or @ws1:dir_k *)
             | RefExpr "/" Step        (* e.g., @ws1:dir_k / file.md *)
             | Primary

Base       ::= "/"                     (* Workspace namespace root *)
             | "^"                     (* Owning special node: workspace, directory, or file *)
             | "."                     (* Current directory; ./ is the explicit form *)
             | "@" identifier ":"      (* Named workspace root *)

Primary    ::= identifier              (* Function or command invocation *)
             | identifier "(" Args ")" (* Function with arguments *)
             | string                  (* Constant text *)
             | "(" Expression ")"

Step       ::= identifier              (* Member name without spaces; may contain * ? *)
             | string                  (* Member name with spaces or punctuation *)
             | "#" identifier         (* Tag selector, e.g. #blue *)
             | "*"                     (* Single-level wildcard over direct members *)
             | "**"                    (* Multi-level wildcard, standard glob semantics *)

Args       ::= Expression ( "," Expression )* 
             | empty
```

## Statement Syntax
(incomplete)
Assignment ::= "=" Expression
             | "#" identifier "=" Expression

Statement  ::= Assignment | Command


### How examples would parse now:

*   **`@ws1:dir_k / file.md`**
    1. `Base` → `@ws1:` (workspace namespace root)
    2. `Base Step` → `@ws1:dir_k` (member `dir_k` under workspace)
    3. `RefExpr "/" Step` → `(@ws1:dir_k) / file.md` (member `file.md` under directory)
   *(Note: `file.md` may be tokenized as an identifier or as a string token.)*

*   **`@ws1:"My Folder" / "File Name.md"`**
   1. `Base` → `@ws1:`
   2. `Base Step` → `@ws1:"My Folder"`
   3. `RefExpr "/" Step` → `(@ws1:"My Folder") / "File Name.md"`

*   **`@bobby:src / *.fs`**
   1. `Base` → `@bobby:`
   2. `Base Step` → `@bobby:src`
   3. `RefExpr "/" Step` → `(@bobby:src) / *.fs`
   4. Final step matches member names case-insensitively using wildcard semantics

*   **`./ / index.html / ** / blue`**
    1. `Base` → `.` (current directory namespace)
    2. `RefExpr "/" Step` → `./ / index.html` (member `index.html` under current directory)
    3. `RefExpr "/" Step` → `(. / index.html) / ** / blue` (descendants, then member `blue`)

*   **`^ / ** / #blue`**
   1. `Base` → `^` (owning special node — workspace, directory, or file)
   2. `RefExpr "/" Step` → `^ / ** / #blue` (descendants tagged `blue`)


## Usage

These expressions will be employed in model language.  The result of an expression will be an array of nodes, or sometimes one.  The language apart from these references can also be used to compute other datatypes of info.
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



### What's missing / What's next?

1. **Relative navigation from the "Current Node":** 
   You mentioned omitting this for now since we're using "self-definition." If you ever need to query a sibling or parent of the *node currently being defined*, you might introduce a new base (like `_` or `~`) or a relative prefix (like `..`). 
2. **Filtering / Predicates:** 
   We previously discussed indexing `[0]` and content filtering `[content ~= "text"]`. Do you want to re-introduce `[` `]` into the `Step` definition now, or keep it strictly functional (e.g., `filter(#//blue, "text")`)? 
3. **Property Access:** 
   How do we want to extract `.content` or `.children`? Is it an operator, or a function like `content(#blue)`?
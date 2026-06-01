# Reference Expressions

Status: Not implemented (target design)
Authority: Design intent only; current behavior is defined in [[doc/arch.md]] and [[doc/api.md]].
See also: [[doc/roadmap/workspace-file-model.md]], [[doc/roadmap/future-merge-sync.md]], [[doc/reference/style.md]]

We will have an expression/command language.  This file defines basic reference expressions that will typically resolve to an array of nodes.

Scope note: this is a target-scope language design document.
Current stage implementation scope is defined separately in
[[doc/roadmap/workspace-stage-plan.md]], which is intentionally narrower.

## Model Elements

The workspace, directory, and file identity rules used here are defined in
[[doc/roadmap/workspace-file-model.md]].

These expressions will be executed from the context of a node.  A node will usually belong to a file.  A file node owns a subtree of the graph.  A workspace maps a directory in the OS to a workspace node in the graph.  The subdirectories and files may be instantiated into the graph as nodes.  

Workspace, file and directory nodes are unique.  A database table will capture the mapping of path to node.

Within a file subtree, nodes can have tags, referred to as names below.  Not every node is tagged.  Tags may repeat within or across different files.

Path matching for workspace, directory, and file resolution is case-insensitive and uses standard
operating-system-style wildcard matching, including `*` and `?`.

## Expression Syntax
```ebnf

Expression ::= PathExpr

PathExpr   ::= Base                    (* Evaluates to the base node itself *)
             | Base Step               (* e.g., ^ #blue or @ws1:dir_k *)
             | PathExpr "/" Step       (* e.g., @ws1:dir_k / file.md *)
             | Primary

Base       ::= "/"                     (* Root node of current workspace *)
             | "^"                     (* Root node of current file *)
             | "."                     (* Directory node of current file *)
             | "@" identifier ":"      (* Root node of specified workspace *)

Primary    ::= identifier              (* Function or command invocation *)
             | identifier "(" Args ")" (* Function with arguments *)
             | string                  (* Constant text *)
             | "(" Expression ")"

Step       ::= identifier              (* Name/path segment without spaces; may contain * ? *)
             | string                  (* Name/path segment with spaces or punctuation *)
             | "#" identifier         (* Tag selector, e.g. #blue *)
             | "*"                     (* Single-level wildcard *)
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

*   **`@ws1:dir_k/file.md`**
    1. `Base` → `@ws1:`
    2. `Base Step` → `@ws1:dir_k`
    3. `PathExpr "/" Step` → `(@ws1:dir_k) / file.md` 
   *(Note: `file.md` may be tokenized as an identifier or as a string token.)*

*   **`@ws1:"My Folder"/"File Name.md"`**
   1. `Base` → `@ws1:`
   2. `Base Step` → `@ws1:"My Folder"`
   3. `PathExpr "/" Step` → `(@ws1:"My Folder") / "File Name.md"`

*   **`@bobby:src/*.fs`**
   1. `Base` → `@bobby:`
   2. `Base Step` → `@bobby:src`
   3. `PathExpr "/" Step` → `(@bobby:src) / *.fs`
   4. Final step matches file names case-insensitively using wildcard semantics

*   **`. / index.html / ** / blue`**
    1. `Base` → `.` (Current file's directory node)
    2. `PathExpr "/" Step` → `. / index.html` (Finds the index file in the directory)
    3. `PathExpr "/" Step` → `(. / index.html) / ** / blue` (Expands to descendants, then matches `blue`)

*   **`^ / ** / #blue`**
   1. `Base` → `^` (Current file root)
   2. `PathExpr "/" Step` → `^ / ** / #blue` (Descendants tagged `blue`)


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
- missing path segment under a resolved base
- unresolved tag/path selector in a context where at least one target is required

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
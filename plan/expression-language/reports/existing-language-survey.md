# Existing language survey

Facts only. No wayfinder map. No implementation.

## 1. Git and project files

Current branch is `w/broken`. The worker did not create `w/expression-language`. [[.cursor/skills/project-work/SKILL.md]] requires stay on an existing `w/*` branch.

Contents of [[plan/expression-language/git.md]]:

- **Project branch:** `w/broken`
- **Cut from:** `w/broken` (already on a project branch; stayed)
- **Notes:** Wayfinder charting; destination still unnamed.

[[plan/expression-language/project.md]] Stage is `charting`. [[plan/index.md]] was regenerated from every live `plan/*/project.md`. The overview skill also required a missing [[plan/download-no-parse-fix/project.md]]; that file was created as `charting`.

## 2. Path and reference syntax: specified versus implemented

Authority for interpretation is [[doc/roadmap/reference-expression-interpretation.md]]. [[doc/roadmap/reference-expressions.md]] is marked Partially superseded. It still points at [[doc/roadmap/language-syntax.md]], which is not in the repo. The live language draft is [[doc/roadmap/language-syntax-and-semantics.md]]. Baseline code notes are in [[doc/current/workspace-graph.md]] (stale on assignment and command parse).

The grammar in the interpretation doc:

```ebnf
RefExpr     ::= (RefBase | ε) (Sep? Step)*
RefBase     ::= Anchor | CurrentDir
Anchor      ::= "//" | "/" | "^" | "#" | "!" | "!" SignedInt
CurrentDir  ::= "." | "." Sep
Step        ::= "**" | TagStep | DirStep | FileStep | IndexStep | ChildStep
```

| Form | Specified | Implemented |
| --- | --- | --- |
| `/` nearest Workspace, else ROOT | yes | yes. `ExprAnchor.WorkspaceRoot`. |
| `//` ROOT | yes | yes. `ExprAnchor.GlobalRoot`. Named Workspace is an ordinary `DirStep` under ROOT, e.g. `//bobby/src/`. |
| `^` nearest File, Directory, or Workspace | yes | yes. `ExprAnchor.Structural`. |
| `#` nearest named Normal ancestor | yes | yes. `ExprAnchor.Tagged`. `#todo` is not this anchor; it is `Context` plus `TagStep`. |
| `.` / `./` current Directory or Workspace | yes | yes. `ExprAnchor.CurrentDir`. `.amb` and `.5` are one `FileStep` name, not CurrentDir. |
| implicit context (no prefix) | yes for search; Amble requires an explicit base | yes. Search and `RefExpr.parse` accept `:0`, `.amb`, bare names. `AmbleParse.parsePrimary` rejects a leading name without an anchor (`"reference requires an explicit anchor"`). |
| `!` / `!nn` as Anchor | yes | no separate anchor type. Leading `!` parses as `Context` plus `IndexStep`. Match of `!` is the context Node. Match of `!nn` is a sibling offset. Tests lock this. |
| `:` / `:nn` ChildStep | yes | yes. `:` is all Children. `:nn` is zero-based index. Counts Ref appearances. |
| `*` in a name | yes, one segment, case-insensitive | yes. `globMatch` also treats `?` as one character. The interpretation doc does not specify `?`. |
| `**` multi-level | yes, within current search scope | yes as `MultiWild`. Walk uses Owned Children only. Walk stops at Directory and Workspace. A `**` start at ROOT therefore does not enter Workspace Children. |
| quoted segments | interpretation doc: not part of syntax | implemented. `RefExprParse` quote loop. Tests accept `/"open #1"/"todo!"`. `/` inside quotes still splits steps. |
| `[n]` postfix / filters | not in syntax | tokenize error `"postfix not supported yet"`. |
| Named search Owned-only; Ref edges excluded | yes | **not** for `DirStep`, `FileStep`, `TagStep`, `ChildStep`, `IndexStep`. Those walks use all Children. Tests `match_ path step follows ref children` and `match_ tag step follows ref children` lock Ref follows. `**` is the Owned-only walk. |
| Path recursion does not enter Directory or Workspace Children | yes | **not** for `DirStep`/`FileStep`. `pathSearchFrom` recurses through all Children. That is why `//bobby/src/*` can match nested File Nodes. |

`RefExpr.match_` returns an empty list on miss. That is not a parse error. The command-result contract in [[doc/roadmap/reference-expressions.md]] forbids silent empty success for the surrounding language. Run treats an empty eval result as failure (redletter Children). Search does not.

No `PathExpr` persistence codec was found. Document Ref lines in [[src/Shared/documents/AmbDocument.fs]] (`parseRefTarget`) address Nodes by identity, not by this path language.

## 3. What the language draft says about function application

[[doc/roadmap/language-syntax.md]] does not exist. The text that [[doc/roadmap/reference-expressions.md]] attributes to it lives in [[doc/roadmap/language-syntax-and-semantics.md]].

Draft EBNF: juxtaposition is prefix `FunCall`. `text Ref`, `name Ref`, and `children Ref` are listed functions on a Node list. Infix `of` and `,` desugar to `FunCall` with equal precedence, looser than juxtaposition, right-associative. Examples: `text #todo`, `name ^/notes.md`, `children //workspaceName/src/`, `name of children ./folder/`, `#list , sort #list`.

`Ref` in those examples is any expression that resolves to Nodes, usually a `RefExpr`.

Amble parse implements this AST in [[src/Shared/AmbleParse.fs]]. Tests in [[tests/Shared.Tests/AmbleTests.fs]] cover `text`, `name`, `of`/`children`, comma, `sort`, numbers, assignment `Name = Expr`, and `> …` shell lines.

Eval does not implement `name`, `children`, `,`, `of`, `sort`, `Cmd`, `Str`, `Num`, or `Paren`. Only `AmbleExpr.Ref` and `FunCall("text", [Ref])` evaluate. See [[src/Shared/AmbleEval.fs]].

[[doc/roadmap/workspace-file-model.md]] still says surrounding functions and assignment are not implemented. That is stale for parse and for `text` plus assignment at Run. It remains true for `name` and `children` eval.

## 4. Pipeline, filter, and boolean combinators

Query pipeline of the form `root descendant containing "the" tagged "blue"` is **not** specified and **not** implemented.

What exists:

- **Path step chains.** Sequential `applyStep` over a Node list. Example: `^/**/#blue`. This is the only implemented set-transform pipeline, and it is path syntax, not words.
- **Find AND.** [[src/Shared/ViewModelSearch.fs]] splits the query on whitespace. A Node must satisfy **every** part. Each part is a case-insensitive substring of text or name, **or** a successful `RefExpr` match for that part. This is intersection on one Node, not a left-to-right pipeline over a set. File Find in [[src/Shared/ViewModelFileSearch.fs]] uses the same part rule, scoped to artifact Nodes.
- **Shell `|`.** In [[doc/roadmap/language-syntax-and-semantics.md]] and `AmbleParse.parseCmdLine`, `|` connects process stdout to stdin. It is not a Graph query combinator. Eval of `Cmd` is not implemented.
- **Comma `,`.** Concatenate two values in the draft. Parse only.
- **No `and` / `or` / `not` operators.** A word `and` would parse as a generic `FunCall` if arguments follow. There is no boolean combinator semantics.
- **No filter postfix.** Interpretation doc forbids `[n]` and filters in `RefExpr`. Parser rejects `[` `]`.

[[doc/current/workspace-graph.md]] still lists filters as not implemented.

## 5. Concrete code pointers

Parse and types:

- [[src/Shared/RefExprTypes.fs]] — `ExprAnchor`, `ExprStep`, `PathExpr`, `RefContext`
- [[src/Shared/RefExprParse.fs]] — `RefExprParse.parse`, `tokenize`, `parseAnchor`, `parseStep`, `format`
- [[src/Shared/RefExpr.fs]] — facade `RefExpr.parse`, `match_`, `refContext`
- [[src/Shared/AmbleTypes.fs]] — `AmbleExpr` (`Ref`, `FunCall`, `Cmd`, …), `AmbleStatement`
- [[src/Shared/AmbleParse.fs]] — `AmbleParse.parse` / `parseStatement`, `parseJuxtapose`, `parseCmdLine`

Eval and Run:

- [[src/Shared/RefExprMatch.fs]] — `RefExprMatch.refContext`, `match_`, `applyStep`, `pathSearchFrom`, `tagSearchFrom`, `pathScopeDescendants`
- [[src/Shared/AmbleEval.fs]] — `evalRefExpr`, `evalExpr`, `evalStatement`, `evalText`
- [[src/Shared/AmbleRun.fs]] — `AmbleRun.run` (parse → eval → rename plus replace Children, or redletter error Children)
- [[src/Shared/Amble.fs]] — `Amble.parse`, `Amble.run`
- [[src/Client/UpdateAmbleRun.fs]] — `runAmbleOp` (command id `Exec`, display name Run)

Callers of `RefExpr` match besides Amble:

- [[src/Shared/ViewModelSearch.fs]] — `buildPartFilter`, `startSearch` (Find dialog, [[src/Client/SearchDialog.fs]])
- [[src/Shared/ViewModelFileSearch.fs]] — `buildPartFilter`, `startFind`

Tests: [[tests/Shared.Tests/RefExprTests.fs]], [[tests/Shared.Tests/RefExprTestTree.fs]], [[tests/Shared.Tests/AmbleTests.fs]], [[tests/Shared.Tests/AmbleRunTests.fs]].

Executable today:

- `RefExpr.parse` + `RefExpr.match_` for path/tag/index/child/`**` queries (Find and Amble).
- Amble Run on a Normal Node: anchored `RefExpr` replaces Children with Ref appearances; `text #name` creates Owned Children from Node text; `name = RefExpr` also `SetName`; parse/eval/empty failure writes redletter Children from the focus line.
- Special focus: Run is a no-op.

Draft-only (parse AST, no eval): `name`, `children`, `of`, `,`, `sort`, numbers as values, `Paren`, shell `> … | …`.

Draft-only (docs, little or no code): statement forms `= Expression` and `#ident = Expression` in [[doc/roadmap/reference-expressions.md]]; view-root-relative path; boolean query combinators; word pipeline functions.

## 6. Gaps versus `root descendant containing "the" tagged "blue"`

That line is not valid Amble and not a valid `RefExpr`. Juxtaposition means prefix `FunCall`, so the first word would have to be a function. `root` is not a function. `descendant`, `containing`, and `tagged` are not functions.

Closest existing pieces, and why they do not compose into that query:

- **ROOT.** `//` is the ROOT anchor. `/` is the nearest Workspace. There is no `root` word.
- **Descendants.** `**` is a path step, not a function. It does not enter Directory or Workspace, so it cannot list descendants of ROOT. `DirStep`/`FileStep` recursion can find File and Directory Nodes at depth, but it is name-glob search, not a general descendant operator over Normal Nodes.
- **Containing `"the"`.** No path step and no Amble function filters by text. Find substring is per-Node AND with other whitespace parts. A Node must itself contain `"the"`; the filter does not apply to descendants of a prior set.
- **Tagged `"blue"`.** `#blue` is a `TagStep` from current bases. It matches named Normal Nodes. It is not a function `tagged` on a prior pipeline result, though a path chain such as `^/#blue` is sequential from a base.

What would be new: word-level pipeline (or equivalent combinators) that thread a Node set through named operators; a descendant operator that can start from ROOT or a Workspace; a text-containment filter on that set; a tag filter as a function rather than only `#name` path syntax; boolean combinators if AND/OR/NOT are required beyond Find's same-Node AND.

Related docs that affect syntax or eval: [[doc/roadmap/amble-run.md]] (Run wiring; later slices still list `name` / `children` / `,` / shell); [[doc/current/workspace-graph.md]] (search merges RefExpr hits with text); [[doc/roadmap/workspace-file-model.md]] (stale "not implemented" list). [[doc/reference/style.md]] does not define this language.

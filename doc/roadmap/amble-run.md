# Amble Run

Status: In progress
Authority: Executable Amble on the focus line — complements [[doc/roadmap/language-syntax-and-semantics.md]].
See also: [[src/Shared/AmbleTypes.fs]], [[src/Shared/AmbleParse.fs]], [[src/Shared/DocumentPathMove.fs]] (`NodeRenameOps`).

## Goal

Add a **Run** command that reads the **focus line**, parses it as Amble, interprets it, and applies graph changes. Parsing already lives in `AmbleParse`; this work adds evaluation and client wiring.

```mermaid
%%{init: {'themeVariables': {'fontSize': '20px'}}}%%
flowchart LR
    focusLine[FocusLine] --> parse[Amble.parse]
    parse --> eval[AmbleEval.evalStatement]
    eval --> pair["name option, Node list"]
    pair --> run[AmbleRun.run]
    run --> ops[Op list]
    ops --> apply[applyAndPost]
```

**Focus line:** when editing, live text from the edit input; otherwise `graph.nodes.[focusId].text` (same rule as Jump to Target in [[src/Client/Commands.fs]]).

## Slice 1 (current)

### Shared: `AmbleRun.fs`

Orchestration entry:

```fsharp
AmbleRun.run : focusNodeId: NodeId -> graph: Graph -> line: string -> Result<Op list, string>
```

Pipeline: `Amble.parse line` → `AmbleEval.evalStatement` → `AmbleRun.run` maps eval result to ops.

### Shared: `AmbleEval.fs`

Semantic evaluation only — no `Op` types.

```fsharp
evalStatement : NodeId -> Graph -> AmbleStatement -> Result<string option * Node list, string>
evalExpr : NodeId -> Graph -> AmbleExpr -> Result<Node list, string>  // placeholder: Ok []
```

| Statement | Eval result |
|-----------|-------------|
| `Assign(name, expr)` | `Ok (Some name, nodes)` — `nodes` from `evalExpr` (placeholder `[]`) |
| `ExprStmt expr` | `Ok (None, nodes)` — same placeholder |

### Shared: `AmbleRun.run` op mapping

| `name` | Ops |
|--------|-----|
| `None` | `Ok []` |
| `Some name` | `NodeRenameOps.planRenameNode graph focusNodeId name` → ops |

Evaluated `Node list` is ignored for now; later slices will derive ops from it.

### Special nodes: NoOp

If the focus node has `kind = Special _`, **Run is a no-op**: return `Ok []` without parsing. No error, no graph change. Applies to workspace, directory, file, and workspaces nodes.

Normal nodes proceed through parse → eval.

### Client: Run command

Full-stack wiring (user confirmed):

- Add `Run` to `CommandId` and [[src/Shared/CommandEntry.fs]] (`SelectionOrEditing`, category TBD — likely Primary).
- [[src/Client/UpdateAmbleRun.fs]]: `runAmbleOp` — resolve focus id and focus line, call `AmbleRun.run`, apply ops via `applyAndPost` (same pattern as [[src/Client/UpdateRename.fs]] / [[src/Client/UpdateFileSearch.fs]]).
- Register in [[src/Client/Commands.fs]] command registry.
- Keybinding: pick during implementation (e.g. `Ctrl+Enter`); not fixed in this doc.

Parse or eval errors: no graph change (mirror rename failure — return model unchanged).

### Project files

Add to [[src/Shared/Gambol.Shared.fsproj]] after `Amble.fs`:

- `AmbleEval.fs`
- `AmbleRun.fs`

Expose from [[src/Shared/Amble.fs]]: `let run = AmbleRun.run` (optional facade).

Client: `UpdateAmbleRun.fs` in [[src/Client/Gambol.Client.fsproj]].

### Tests

[[tests/Shared.Tests/AmbleRunTests.fs]]:

- `Assign` on a normal node → `Op.SetName` with correct old/new names.
- Assign when name unchanged → empty ops.
- Special-node focus (e.g. file node from [[tests/Shared.Tests/RefExprTestTree.fs]]) → `Ok []` without parsing invalid empty line.
- `ExprStmt` → `Ok []` (eval succeeds; node list placeholder unused).
- Parse error propagates (e.g. trailing garbage).

Update [[tests/Shared.Tests/CommandEntryTests.fs]] command count when `Run` is added.

## Later slices

- Evaluate `ExprStmt` (refs, `text` / `name` / `children`, infix `,`).
- Shell `> …` commands ([[doc/roadmap/language-syntax-and-semantics.md]] shell section).
- RHS evaluation on assignment (functional result + name side effect).
- Diagnostics / user-visible error messages on Run failure.
- Whether Run should commit or clear focus-line text after success.

# Workspace `.amb` Text Format

Status: Draft
Authority: [[src/Shared/Snapshot.fs]] documents a pre-workspace baseline; workspace `.amb` rules below are target design.
See also: [[doc/roadmap/workspace-text-outline-conversion.md]], [[doc/roadmap/reference-expressions.md]]

This document defines the native `.amb` line grammar and how it maps to outline nodes. Import/export workflow and the generic conversion contract live in [[doc/roadmap/workspace-text-outline-conversion.md]].

## Pre-workspace baseline (Snapshot.fs)

`Snapshot.write` / `Snapshot.read` implement an interim format for whole-graph snapshots. Workspace `.amb` **replaces** its identity scheme; backward compatibility is not required.

- Tab indentation sets parentage.
- `#n1` Owner lines and `-> #n1` Ref lines use ephemeral short ids reassigned on each write.
- Plain lines mint fresh `NodeId`s on read.
- Metadata prefix `{.class1 .class2}rest` on line bodies.

## Line grammar

One line per outline row. Tab depth sets parentage; the file root is implicit.

```
<line>      ::= <indent> <content>
<indent>    ::= TAB*
<content>   ::= <ref-line> | <owner-line> | <plain-line>
<ref-line>  ::= "-> " <ref-target>
<owner-line>::= "#" <stable-id> [" " <name-token>] <body>
<plain-line>::= <body>
<body>      ::= [<meta>] <text>
<meta>      ::= "{" <class-list> "}"
<stable-id> ::= <guid> | "WORKSPACES" | "TRASH"
<name-token>::= identifier satisfying [[src/Shared/Filename.fs]] Ok rules (without leading #)
<ref-target>::= "#" <name-token>
              | <workspace-relative-path> "#" <name-token>
<guid>      ::= canonical `NodeId` string form (full GUID)
```

Line breaks: `\n` or `\r\n`; normalize on read.

**Owner line.** Replaces baseline `#n1`. The stable id is authoritative for import reconciliation. The optional `name-token` is the node's readable `name` field; required when the node is a reference target within or across files. `body` is node text after optional metadata prefix.

**Ref line.** Replaces baseline `-> #n1`. Resolves by stable `NodeId` via the target's `name` in the current file subtree, or by cross-file path + name. Import must not rely on line order or tab depth alone.

**Plain line.** Content without a stable id. Import mints a new `NodeId`. Export uses owner lines for all persisted subtree nodes; plain lines appear only from external edits before reconciliation.

**Metadata prefix.** Unchanged from baseline: `{.class1 .class2}rest` on line bodies maps to `cssClasses` + text.

## Text to outline

| Line kind | Import rule | Export counterpart |
|-----------|-------------|-------------------|
| Owner | Match `stable-id` to a node in the current file subtree; update text, classes, and children stack. Unknown id in subtree: report or mint per policy (TBD). | **Owner line** |
| Ref | Resolve `ref-target` to `NodeId` (within-file by `name`, cross-file by path + `name` against graph). Emit Ref edge under current parent. | **Ref line** |
| Plain | Mint new `NodeId`; Owner edge; push onto stack. | **Owner line** (once persisted) |
| Tab depth | Pop stack to depth; attach under stack head. | Same depth on write |
| Metadata | Parse `{...}` prefix into classes + text. | Same prefix on write |

Special nodes: `WORKSPACES` and `TRASH` ids map to fixed canonical nodes as today.

## Outline to text

| Outline state | Export rule | Import counterpart |
|---------------|-------------|-------------------|
| Owned node in file subtree | `#<NodeId> [<name>] <body>` at tab depth from parentage; include `name-token` when node has a readable name or is referenceable. | **Owner line** |
| Ref edge | `-> #<name>` within file, or `-> <path>#<name>` cross-file. | **Ref line** |
| Hierarchy | Tab depth = child depth under owner parentage. | Tab depth |
| Classes | `{.class...}` prefix when non-empty or text starts with `{`. | Metadata prefix |

Workspace export projects one file subtree only (not the whole graph).

## References (within and cross-file)

Readable anchor is `#name`. Cross-file form is `relative/path.amb#name`, aligned with [[doc/roadmap/reference-expressions.md]] path steps where applicable.

- Bind by stable `NodeId`; editing file B must not force rewriting file A when A holds a cross-file reference into B.
- Ambit ensures `name` is unique among reference targets within a file when a reference is created.
- Each import rule above names its export counterpart.

## Carried forward from baseline

- Tab indentation sets parentage; root is implicit.
- Metadata prefix `{.class1 .class2}rest` on line bodies.
- Line break normalization on read.

## Not in Snapshot.fs

- Stable file-scoped and cross-file identity as above.
- Subtree scope — workspace export projects one file subtree only.
- Unsupported-structure reporting — workspace import reports what cannot round-trip.
- Delta export (`file_next = f_out(file_prev, op)`) on top of this grammar.

## Open Questions

- Whether unknown stable id on import is an error or triggers mint-with-warning.
- Exact cross-file path encoding when the target workspace label differs from the current file's workspace.
- Delta export rules on top of this grammar (minimal diff vs full rewrite).

## Verification Targets

- Tab depth, Owner/Ref line kinds, and metadata prefix round-trip under workspace rules.
- `read` then `write` is stable when the outline is unchanged (modulo readable ref projection choices).
- Reorder and text edit preserve identity via stable ids on import.
- Cross-file reference in file A stays valid without rewriting A when file B is edited elsewhere.

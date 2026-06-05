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

## Workspace target

### Identity (replaces `#sid`)

- **Owner lines** carry the node's stable `NodeId` (not `n1`, `n2`, …). import matches by this id against the current file subtree.
- **Ref lines** point at a target by stable `NodeId` within the file.
- **New nodes** in an externally edited file get ids minted on import when no stable id is present.
- Canonical special nodes (`WORKSPACES`, `TRASH`) may retain fixed symbolic ids as today.

Structural alignment alone is not sufficient for import reconciliation.

### References (within and cross-file)

This format owns how a reference to a node is captured **readably** and matched **persistently** through edits — internal Ref lines and cross-file pointers alike. Requirements from [[doc/roadmap/workspace-text-outline-conversion.md]]:

- Bind by stable `NodeId`; a peer file edit must not force unrelated files to be rewritten.
- Pair export and import rules; each import rule names its export counterpart.
- Reconcile with [[doc/roadmap/reference-expressions.md]] where the target is addressable that way.

Exact line grammar is open below.

### Carried forward from baseline

- Tab indentation sets parentage; root is implicit.
- Metadata prefix `{.class1 .class2}rest` on line bodies.
- Line breaks: `\n` or `\r\n`; normalize on read.

### Not in Snapshot.fs

- Stable file-scoped and cross-file identity as above.
- Subtree scope — workspace export projects one file subtree only.
- Unsupported-structure reporting — workspace import reports what cannot round-trip.
- Delta export (`file_next = f_out(file_prev, op)`) on top of this grammar.

## Open Questions

- Owner/Ref line syntax using stable `NodeId` (replacing `#n1` / `-> #n1`).
- Readable, persistent reference surface form for internal and cross-file Ref lines.
- Delta export rules on top of this grammar.

## Verification Targets

- Tab depth, Owner/Ref line kinds, and metadata prefix round-trip under workspace rules.
- `read` then `write` is stable when the outline is unchanged (modulo readable ref projection choices).
- Reorder and text edit preserve identity via stable ids on import.
- Cross-file reference in file A stays valid without rewriting A when file B is edited elsewhere.

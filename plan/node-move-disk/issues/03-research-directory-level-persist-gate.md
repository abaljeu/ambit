# 03 — Research: directory-level persist gate for moves

**Type:** research
**Status:** defined
**Blocked by:** None

## Context

Today's gate is `WorkspaceGit.withWorkTreeGate` ([src/Server/WorkspaceGit.fs](../../../src/Server/WorkspaceGit.fs)). It is one `SemaphoreSlim` per normalized Workspace root. The git pull (`WorkspaceGit.pullTracked`, wrapped by `commitInGate` in `GithubTransportActor`), git Save (`WorkspaceGit.saveTracked`), and the persist writes (`DocumentPersistWrite.writeArtifactText`, `writeDocument`, `writeAllDocuments`) all take it. `DocumentPersistPath.withWorkTreeGates` takes several roots in sorted order. The path-move persist in `DocumentPersistChange` gates the Workspace roots of both the pre graph and the post graph before `executePathMoves`. Shared `GitRun` holds its own separate per-folder lock around each git command.

The gate is Workspace-wide only. [core-refinement architecture](../../core-refinement/arch.md) §6 and [Parse and persist](../../../doc/current/parse-persist.md) mark it obsolescent and plan a workspace lock plus per-member persist locks. Those locks are not built.

[Node move and disk architecture](../arch.md) **Move write gate** needs a lock below Workspace scope: while a git move of a Directory runs, no write is allowed under that Directory until the move completes.

## Goal

Come up with a solution that supports both of these:

- A git pull locks a Workspace.
- A Directory move locks a Directory.

In both cases, the lock prevents any of its descendants from being persisted.

## Research

1. Key — How is a gate keyed by Directory path prefix, or by walking the Directory's members? What does each need when a path is itself moving?
2. Planned locks — How does this lock relate to the planned workspace lock and per-member persist locks in core-refinement §6?
3. `withWorkTreeGate` — Should it be replaced or coexist with the new lock?
4. Deadlock — What are the deadlock risks with nested gates, such as a Directory gate inside a Workspace gate, a move that holds both a source and a destination, and the separate `GitRun` lock?

## Outputs

A written answer to each research question, with a recommended solution. No code changes.

## Notes

- 2026-11-09 — Alan asked for this research ticket, on the same branch after the arch. Do not implement anything.

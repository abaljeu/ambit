# 09 — Git Load reconcile discovers top level only

**Type:** coding
**Status:** coded
**Blocked by:** None — can start immediately
**Binding arch:** [Parse thread architecture](../arch.md)
**Trackers:** architecture, project spec

## What to build

Git Load reconcile after the pull discovers files in the workspace top directory only. [DocumentPersistPath.discoverArtifactRelatives](../../../src/Server/DocumentPersistPath.fs) takes a search option. [GithubTransportActor](../../../src/Server/GithubTransportActor.fs) passes top-directory-only on its [LazyLoadReconciliationServer.reconcileWorkspace](../../../src/Server/LazyLoadReconciliationServer.fs) call. Cold bootstrap [DocumentPersistChange.readAllDocuments](../../../src/Server/DocumentPersistChange.fs) and every other caller keep all-directories.

### 1. Top directory

1. [x] Git Load — Reconcile after the pull does not discover a nested file.
2. [x] Other callers — Cold bootstrap and every other discover caller keep all-directories.

## Finish-up

1. [x] Piece answers — none changed.
2. [x] Basis and reference docs — [Parse thread architecture](../arch.md) item 18 **Pull handoff stays the pull sentence** and [Parse thread spec](../spec.md) item 18 **Pull handoff stays the pull sentence** already keep the immediate-members sentence. This ticket does not change those docs.

## See also

[02 — Git Load: Unparsed then Parse stack](../../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md)

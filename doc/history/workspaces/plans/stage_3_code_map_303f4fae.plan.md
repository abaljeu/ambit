---
name: Stage 3 Code Map
overview: Complete Stage 3 — shared workspace-label → workspace-root mapping. Graph/SQL/change-log done; remaining work is Snapshot.write path emission and one doc fix.
todos:
  - id: snapshot-write-paths
    content: "Snapshot.write: emit workspace/directory/file as paths via NodeDesktopPath"
    status: pending
  - id: snapshot-write-tests
    content: SnapshotTests for path write output (write-only; no read round-trip)
    status: pending
  - id: doc-stage-plan-s3
    content: Fix workspace-stage-plan.md §3 — shared persistence, not desktop-local
    status: pending
  - id: doc-file-model-s3
    content: Mark Stage 3 [x] in workspace-file-model.md
    status: pending
isProject: false
---

# Complete Stage 3

**Goal:** shared persistence stores workspace-label → workspace-root mapping only.

**Done:** `Special Workspace` nodes under `Workspaces` (`Node.name` = label); persisted via `GraphProjection` + `nodes.kind`/`nodes.name` and change-log JSON. Lookup via `RefExpr.namedWorkspacesFromGraph` / `FilePathResolve.findOwnerChild`.

**Remaining:**

1. **`Snapshot.write`** ([src/Shared/Snapshot.fs](src/Shared/Snapshot.fs)) — for `Special Workspace` / `Directory` / `File`, use `NodeDesktopPath.pathForNodeId` as line body instead of `#sid` + `node.text`. Write-only; round-trip not required.
2. **Tests** ([tests/Shared.Tests/SnapshotTests.fs](tests/Shared.Tests/SnapshotTests.fs)) — assert `@label:` / path segments in write output.
3. **Docs** — fix [doc/roadmap/workspace-stage-plan.md](doc/roadmap/workspace-stage-plan.md) §3 (currently wrongly points at desktop-local mapping); mark Stage 3 `[x]` in [doc/roadmap/workspace-file-model.md](doc/roadmap/workspace-file-model.md).

**Out of scope:** directory/file path mapping (Stage 7), `Snapshot.read` round-trip.

**Later (not Stage 3):** startup duplicate-label validation; optional cached label index on `Graph`.

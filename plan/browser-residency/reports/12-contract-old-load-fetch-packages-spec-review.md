# Spec review — 12 — Contract old Load Fetch packages

Range: `origin/staging...HEAD` (`e4a78a02` Contract old Load Fetch packages). Authority: [12 — Contract old Load Fetch packages](../issues/12-contract-old-load-fetch-packages.md). Support: [Browser residency architecture](../arch.md), [06 — Dual-run vs migrate explicit Load Fetch](../issues/06-dual-run-vs-migrate-explicit-load.md).

## 1. Missing or partial

1. **Legacy packagesForTarget remains** — [12 — Contract old Load Fetch packages](../issues/12-contract-old-load-fetch-packages.md) What to build: "Remove the legacy Load Fetch package API after every migration ticket is done." Section **3. Contract proof**: "Prove no legacy symbol or caller remains." [06 — Dual-run vs migrate explicit Load Fetch](../issues/06-dual-run-vs-migrate-explicit-load.md) Answer **3. Remove legacy code** requires removal of `packagesForTargets`, `installPackages`, "and remaining callers." The named plural helper, `installPackages`, `LoadResponse.packages`, and `packageChildMap` are gone. [ResidentProjection](../../../src/Shared/ResidentProjection.fs) still exports `packagesForTarget`, which returns a complete owning-Workspace Node list. [LoadCaptureTests](../../../tests/Shared.Tests/LoadCaptureTests.fs) still asserts "owning Workspace subgraph." Production `postLoad` and `captureLoadResponse` call `wantAnswer`. The leftover helper is unused by those doors and is still the old package API.

## 2. Scope creep

None.

## 3. Implementation that looks wrong

None.

# 12 — Contract old Load Fetch packages — independent Spec review

Range: `git diff origin/staging...HEAD`. Axis: Spec only.

## Findings

1. **must-fix — leftover Load Fetch package helper.** Spec [12 — Contract old Load Fetch packages](../issues/12-contract-old-load-fetch-packages.md): "Remove the legacy Load Fetch package API after every migration ticket is done." Contract proof: "Prove no legacy symbol or caller remains." Map: "The legacy `packages` API is removed; there is no dual-run or compatibility path." Decision [06 — Dual-run vs migrate explicit Load Fetch](../issues/06-dual-run-vs-migrate-explicit-load.md): remove `packagesForTargets`, `installPackages`, "and remaining callers." [ResidentProjection](src/Shared/ResidentProjection.fs) still exports `packagesForTarget`, which returns the owning-Workspace subgraph Node list. Production Load uses `wantAnswerForTargets` / `wantAnswer`. Callers remain in [LoadCaptureTests](tests/Shared.Tests/LoadCaptureTests.fs). Named deletes (`LoadResponse.packages`, `packageChildMap`, `packagesForTargets`, `installPackages`) are gone; this singular helper is still a package path under `src/` and `tests/`. Checkmarks 31.4 / 3.2.3 are honest for those four names and dishonest for the proof heading.

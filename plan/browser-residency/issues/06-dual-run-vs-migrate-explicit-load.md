# 06 — Dual-run vs migrate explicit Load Fetch

**Type:** grilling
**Status:** done
**Blocked by:** [01 — Lock Sync want + edges/Nodes package shape](01-lock-sync-want-package-shape.md)
**Actual:** 5m

## 1. Question

When does the user-facing Load command stop using the old Fetch path, and may it dual-run both paths for a while?

Auto growth and bootstrap use the edges-plus-Nodes package. They are not the Load command. Explicit Load may dual-run the old Fetch path. Hollow-click Load may remain wired to that command. This ticket decides when the old path dies.

Lock:

1. **Dual-run window** — Which Load invocations keep the old Fetch packages, and which use the new edges-plus-Nodes answer?
2. **Death of the old path** — What must be true before Load Fetch uses only the new package? Is there a single cut, or a per-door migrate?
3. **App vs Browser** — If the App still needs the old Fetch path after the Browser migrates, is that in scope here or a [transport-layer](plan/transport-layer/project.md) note?

## Answer

1. **No dual-run** — No Load invocation keeps the legacy `packages` response in the destination. Browser and App explicit Fetch use the current edges-plus-Nodes answer.
2. **One residency API** — Poll, post-Event, bootstrap, and Load Fetch converge on `nodes` plus `childMap` and `installWantAnswer`. There is no interoperation between legacy and current versions.
3. **Remove legacy code** — [12 — Contract old Load Fetch packages](12-contract-old-load-fetch-packages.md) removes `LoadResponse.packages`, `packageChildMap`, `packagesForTargets`, `installPackages`, and remaining callers after migration tickets 08–11 are done.
4. **Load command remains** — Upload and Parse remain stages of the user-facing Load command. Only the legacy Fetch package API is removed.

## Time

- 2026-09-26 5m — affirmed one current Load Fetch package and no legacy API (from chat)

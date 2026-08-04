# 20 — Restore saved zoom Workspace during bootstrap

**What to build:** Restore a saved zoom target during initial bootstrap by adding at most its complete owning Workspace to complete ROOT, with deterministic fallback for duplicate or stale targets and no residency caused by fold restoration.

**Blocked by:** 19 — Bootstrap fresh sessions with complete ROOT.

**Status:** ready-for-agent

- [ ] With no saved zoom target, bootstrap installs complete ROOT and no additional Workspace.
- [ ] A valid saved target owned by ROOT restores from the ROOT package without requesting or installing ROOT twice.
- [ ] A valid saved target outside ROOT adds exactly one complete owning Workspace, captured and installed atomically with ROOT and the response revision.
- [ ] A stale or missing saved target installs only complete ROOT and selects the normal default in-ROOT view.
- [ ] Saved folds are restored only for resident nodes and neither fold restoration nor saved nonresident fold entries request additional content.
- [ ] The first rendered view reflects the resolved zoom and folds after the complete bootstrap state is installed.

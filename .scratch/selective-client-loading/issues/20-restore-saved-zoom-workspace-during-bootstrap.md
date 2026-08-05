# 20 — Restore saved zoom Workspace during bootstrap

**Context:** On refresh the client may have a saved zoom node. That target can widen the initial `/state` Graph without changing the response shape.

**What to build:** When restoring a valid saved zoom target outside ROOT, include at most one extra complete owning Workspace in the same `/state` Graph as ROOT, at one response revision, with deterministic fallback for duplicate or stale targets and no residency caused by fold restoration.

**Blocked by:** 19 — Bootstrap fresh sessions with complete ROOT.

**See also:** [[.scratch/selective-client-loading/spec.md]] (saved zoom Workspace and fold restoration); [[.scratch/selective-client-loading/issues/14-simplify-selective-loading.md]] (startup installs ROOT plus at most one saved-zoom Workspace).

**Status:** ready-for-agent

- [ ] With no saved zoom target, `/state` installs complete ROOT and no additional Workspace.
- [ ] A valid saved target owned by ROOT restores from the ROOT closure without requesting or installing ROOT twice.
- [ ] A valid saved target outside ROOT adds exactly one complete owning Workspace into the same `/state` Graph, captured and installed atomically with ROOT at one response revision.
- [ ] A stale or missing saved target installs only complete ROOT and selects the normal default in-ROOT view.
- [ ] Saved folds are restored only for resident nodes and neither fold restoration nor saved nonresident fold entries request additional content.
- [ ] The first rendered view reflects the resolved zoom and folds after that `/state` Graph is installed.

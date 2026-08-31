# 20 — Restore saved zoom Workspace during bootstrap

**Context:** Browser refresh may carry a saved zoom target. Selective loading still serves startup as `GET /{file}/state` → `StateResponse` with a scoped `graph`, not a Change list. A valid target outside ROOT may widen that same `/state` Graph by at most one complete owning Workspace.

**What to build:** Extend the ticket-19 `/state` Graph so a valid saved zoom target outside ROOT adds at most one complete owning Workspace into the same response (ROOT ± one Workspace) at one revision. Nested Workspace headers in that Workspace stay resident with Unloaded empty children; Ref headers reachable from owned nodes are included without their children. `/state` does not use SiteMap. Keep deterministic fallbacks when there is no target, the target is already in ROOT, or the target is stale/missing. Saved folds never request extra residency. The Browser installs the complete `/state` Graph before first render.

**Blocked by:** 19 — Bootstrap fresh sessions with complete ROOT.

**See also:** [[.scratch/selective-client-loading/spec.md]] (saved zoom Workspace and fold restoration); [[.scratch/selective-client-loading/issues/14-simplify-selective-loading.md]] (startup installs ROOT plus at most one saved-zoom Workspace).

**Status:** agent-done

- [x] With no saved zoom target, `/state` installs complete ROOT and no additional Workspace.
- [x] A valid saved target owned by ROOT restores from the ROOT closure without requesting or installing ROOT twice.
- [x] A valid saved target outside ROOT adds exactly one complete owning Workspace into the same `/state` Graph, captured and installed atomically with ROOT at one response revision.
- [x] Nested Workspace headers in that added Workspace appear as resident Headers with Unloaded empty children; Ref headers reachable from its owned nodes are resident with children omitted.
- [x] A stale or missing saved target installs only complete ROOT and selects the normal default in-ROOT view.
- [x] Saved folds are restored only for resident nodes and neither fold restoration nor saved nonresident fold entries request additional content.
- [x] The first Graph render waits until that `/state` Graph is installed, so the resolved zoom and folds are not shown against a partial package.

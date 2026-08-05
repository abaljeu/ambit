# 19 — Bootstrap fresh sessions with complete ROOT

**Context:** Fresh Browser sessions previously received the complete Server Graph via `/state`. Selective loading keeps `StateResponse` with a scoped `graph`, not a Change list.

**What to build:** Serve `/state` as a scoped Graph at one response revision: the resident ROOT Workspace closure only. Nested Workspace headers stay resident with Unloaded (empty) children; Ref headers reachable from owned ROOT nodes are included without their children. The Browser installs that graph atomically before first render. Do not elevate SYSTEM/TRASH as special bootstrap concerns. `/state` does not use SiteMap.

**Blocked by:** 18 — Synchronize a resident projection safely.

**See also:** [[.scratch/selective-client-loading/spec.md]] (ROOT `/state` scope); [[.scratch/selective-client-loading/issues/14-simplify-selective-loading.md]] (Residency and graph model); [[.scratch/selective-client-loading/undo-spec.md]] (`/state` installs graph; Poll/Load catch-up remain Changes).

**Status:** ready-for-agent

- [ ] `/state` returns `StateResponse` with a scoped `graph` and one response revision; it does not return or apply a Change tail.
- [ ] The graph map contains only the resident ROOT closure, not the full canonical Server Graph.
- [ ] Nested named Workspaces appear as resident Headers with Unloaded empty children rather than their Workspace contents.
- [ ] Ref headers reachable from owned ROOT nodes are resident; their children are omitted.
- [ ] The Browser does not request, transfer, or install the complete canonical Graph during fresh startup.
- [ ] The first Graph render waits until that `/state` graph is installed, so no partial ROOT view is exposed.

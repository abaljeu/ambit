# 19 — Bootstrap fresh sessions with complete ROOT

**Context:** When the client initializes, previously it would receive the complete graph from the server via /state.  Now we want only part of the graph.
**What to build:** Start each fresh webpage session from one coherent complete ROOT Workspace package instead of the full canonical graph, while retaining the core subtrees and headers needed for the first usable view.

**Blocked by:** 18 — Synchronize a resident projection safely.

**See also:** [[.scratch/selective-client-loading/spec.md]] (ROOT bootstrap scope); [[.scratch/selective-client-loading/issues/14-simplify-selective-loading.md]] (Residency and graph model).

**Status:** ready-for-agent

- [ ] A fresh-session bootstrap atomically captures and installs complete ROOT with any ordered HistoryAction tail at one response revision.
- [ ] The ROOT package contains every canonical ROOT child plus the complete SYSTEM and TRASH subtrees.
- [ ] Nested named Workspaces appear as resident headers with Unloaded children rather than bringing their Workspace contents into the session.
- [ ] External and Ref targets required by loaded ROOT content appear as ordinary resident headers with canonical owners and without implicit child loading.
- [ ] The client does not request, transfer, or install the complete canonical graph during fresh startup.
- [ ] The first graph render waits until the bootstrap package and its catch-up are installed, so no partial ROOT view is exposed.

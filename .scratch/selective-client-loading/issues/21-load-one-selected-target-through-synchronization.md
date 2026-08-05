# 21 — Load one selected target through synchronization
This and 22 belong together.

**Context:** The Load command will be expanded so after parse considerations are done, it finishes by fetching selected nodes and surrounding context (if they are not already loaded).  These nodes become added to the client state with their children now Loaded.
**What to build:** Run Load for one selected target through the existing ordered source workflow and global synchronization path, adding its complete owning Workspace only when its children are Unloaded.

**Blocked by:** 16 — Rename Upload to Load; 19 — Bootstrap fresh sessions with complete ROOT.

**See also:** [[.scratch/selective-client-loading/spec.md]] (single-target Load through single-flight sync); [[.scratch/selective-client-loading/issues/14-simplify-selective-loading.md]] (User surface and Load; Synchronization and projected correctness).

**Status:** ready-for-agent

- [ ] Load retains the existing source filters and ordered desktop push, parse, and reconciliation stages before resolving the selected target's residency intent.
- [ ] Loading a target with Unloaded children requests and installs its complete owning Workspace from the fully resident authoritative server.
- [ ] Loading a target with Loaded children requests no Workspace subgraph and still receives the normal ordered Change catch-up.
- [ ] The ordered Change tail and any Workspace subgraphs (Graph form at R) are captured at one response revision and install as one coherent client transition.
- [ ] Poll, submit, and Load remain globally single-flight; a Load requested while busy queues and is released by the existing synchronization planner.
- [ ] Existing optimistic conflict behavior remains in force, including the established full reload outcome when pending local actions prevent response application.

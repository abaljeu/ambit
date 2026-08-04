# 21 — Load one selected target through synchronization

**What to build:** Run Load for one selected target through the existing ordered source workflow and global synchronization path, adding its complete owning Workspace only when its children are Unloaded.

**Blocked by:** 16 — Rename Upload to Load; 19 — Bootstrap fresh sessions with complete ROOT.

**Status:** ready-for-agent

- [ ] Load retains the existing source filters and ordered desktop push, parse, and reconciliation stages before resolving the selected target's residency intent.
- [ ] Loading a target with Unloaded children requests and installs its complete owning Workspace from the fully resident authoritative server.
- [ ] Loading a target with Loaded children requests no Workspace package and still receives the normal ordered HistoryAction catch-up.
- [ ] The HistoryAction tail and any Workspace package are captured at one response revision and install as one coherent client transition.
- [ ] Poll, submit, and Load remain globally single-flight; a Load requested while busy queues and is released by the existing synchronization planner.
- [ ] Existing optimistic conflict behavior remains in force, including the established full reload outcome when pending local actions prevent response application.

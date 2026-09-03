# 21 — Load one selected target through synchronization
This issue and issue 22 form one pair.

**Context:** The Load command will get one new step. First, Load runs its current parse steps. Then Load fetches the selected target. If the target's owning Workspace is not loaded, Load also fetches that Workspace. The client adds the Workspace to its state. The client marks the new children as Loaded.
**What to build:** Send one selected target through the current Load steps and the current synchronization path. Add the target's complete owning Workspace only when its children are Unloaded.

**Blocked by:** 16 — Rename Upload to Load; 19 — Bootstrap fresh sessions with complete ROOT.

**See also:** [[plan/selective-client-loading/spec.md]] (single-target Load through single-flight sync); [[plan/selective-client-loading/issues/14-simplify-selective-loading.md]] (User surface and Load; Synchronization and projected correctness); [[src/Shared/WorkspaceUploadStructure.fs]]; [[tests/Shared.Tests/WorkspaceUploadStructureTests.fs]].

**Status:** ready-for-agent

- [ ] Load keeps its current source filters and its current push and parse steps, including how parsing reconciles source content into the Graph. Load runs these steps before it checks if the target needs its Workspace.
- [ ] When a target has Unloaded children, Load fetches the target's complete owning Workspace from the server. Load then installs that Workspace on the client.
- [ ] When a target has Loaded children, Load does not fetch a Workspace subgraph. Load still receives the normal ordered Change catch-up.
- [ ] The server captures the ordered Change tail and any Workspace subgraphs at one response revision. The client installs them together as one transition.
- [ ] Poll, submit, and Load run one at a time across the app. If a user requests Load while the app is busy, the app queues the request. The current synchronization planner releases the request later.
- [ ] The app keeps its current conflict behavior. If pending local actions block the response, the app does a full reload.
- [ ] HITL: Load of an Unloaded named Workspace after stub-skip fix (inventory → push → `/load` with packages; no `/changes` name conflict).

## Comments

- 2026-09-02: Parked from WORK.md. Added HITL and artifact links.

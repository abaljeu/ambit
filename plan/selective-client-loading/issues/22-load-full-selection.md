# 22 — Load full selection

**Context:** A user can select several targets at once. Loading one Workspace can be slow; loading several at once could be extremely slow. Load must check every selected target's owning Workspace before it runs any stage. If the targets do not all belong to the same Workspace, Load must refuse the whole request instead of processing part of it.

**What to build:** Extend Load to carry the user's whole selection, not just one target, and deduplicate that selection's one owning Workspace's package across every target that needs it. Before running any Load stage, check that every selected target belongs to one Workspace; if more than one Workspace is involved, refuse the request and change nothing.

**Blocked by:** 21 — Load one selected target through synchronization.

**See also:** [[plan/selective-client-loading/spec.md]] (Workspace-selection refusal and package deduplication); [[plan/selective-client-loading/issues/14-simplify-selective-loading.md]] (selected-target resolution and Workspace packages).

**Status:** ready-for-agent

- [ ] One Load call carries every selected target, not just one. Load marks each target as Loaded or Unloaded.
- [ ] When every selected target belongs to one Workspace, Load runs its current source steps in their current order for the whole selection.
- [ ] The server sends that one owning Workspace at most once, even when several selected targets need it.
- [ ] Loaded targets receive the normal Change catch-up. Load does not send the shared Workspace twice when a Loaded target and an Unloaded target share it.
- [ ] The server sends the one Workspace package and the ordered Change tail at one response revision. The client installs both together.
- [ ] After a successful Load, the client keeps every Workspace it already had. The client also has the newly requested Workspace in full.
- [ ] Load checks each target's owning Workspace before running any stage. If any two selected targets have different owning Workspaces, Load refuses the whole request and runs no stage.
- [ ] A refused Load changes nothing: no source stages run, no Change catch-up applies, and no Workspace installs.

## Comments

- 2026-09-02: Parked from WORK.md. Parent: [[plan/selective-client-loading/spec.md]].

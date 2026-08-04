# 22 — Load full selection across Workspaces

**What to build:** Make one Load process the user's complete selection across owning Workspaces, preserving source-stage order while deduplicating the authoritative Workspace packages needed by Unloaded targets.

**Blocked by:** 21 — Load one selected target through synchronization.

**Status:** ready-for-agent

- [ ] One Load carries every selected target and distinguishes which targets have Loaded versus Unloaded children.
- [ ] Mixed selections run the existing source synchronization stages in their established order for every eligible target.
- [ ] The server resolves each Unloaded target to its canonical owning Workspace and returns each required Workspace package at most once.
- [ ] Loaded targets receive catch-up without causing redundant Workspace packages, including when they share a Workspace with an Unloaded target.
- [ ] Selections spanning several Workspaces install all deduplicated packages and the ordered HistoryAction tail at one response revision.
- [ ] The resulting resident projection retains all previously loaded Workspaces and contains the complete newly requested Workspace closures.

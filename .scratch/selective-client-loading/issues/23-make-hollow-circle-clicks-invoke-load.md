# 23 — Make hollow-circle clicks invoke Load

**What to build:** Let users invoke the full-selection Load command from the hollow-circle affordance while keeping Unloaded residency distinct from Unparsed source state and loaded empty nodes visually distinct.

**Blocked by:** 22 — Load full selection across Workspaces.

**Status:** ready-for-agent

- [ ] Unloaded and Unparsed occurrences remain distinct states but each presents the hollow-circle affordance and invokes the same Load command.
- [ ] A Loaded node with an authoritative empty child list is distinguishable from an Unloaded node and is not presented as unloaded.
- [ ] Clicking the hollow circle on an occurrence outside the current selection first makes that occurrence the sole selection and then loads it.
- [ ] Clicking the hollow circle on an already selected occurrence preserves the entire current selection and loads all selected targets.
- [ ] Hollow-circle Load uses the global QueuedLoad and Loading synchronization behavior and introduces no per-node loading state.
- [ ] No other click or rendering transition implicitly obtains resident content.

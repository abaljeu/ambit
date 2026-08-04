# 17 — Represent unloaded child lists end to end

**What to build:** Represent whether each resident Node header has an authoritative Loaded child list or an Unloaded child list, preserving canonical owner identity and making the distinction survive graph construction, comparison, encoding, persistence projection, and index rebuilding.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] A child list has exactly Unloaded or Loaded status, Unloaded is valid only with no resident children, and existing current and full graphs default to Loaded.
- [ ] A resident header with Unloaded children is observably distinct from both a Loaded empty leaf and a Loaded parent.
- [ ] Encoding, decoding, equality, graph construction, and persistence projection preserve child-list status and reject or avoid the invalid Unloaded-with-children state.
- [ ] Only Loaded child lists contribute parent, owner-parent, traversal, and other edge-derived indexes.
- [ ] Every resident header supplies its plain canonical NodeId owner; rebuilding a projection whose owner edge or owner list is Unloaded preserves that owner instead of replacing it with ROOT.
- [ ] The owner-parent index contains only authoritative Owner edges from Loaded lists, ROOT self-owns, and no Unknown owner wrapper, case, or sentinel is introduced.

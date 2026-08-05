# 23 — Make hollow-circle clicks invoke Load

**Context:** Unloaded residency and Unparsed source state are different facts. Both show a hollow circle next to the Node. Unparsed also keeps its current secondary visual indicator. A Node that is Loaded and Parsed keeps its current look and behavior. Users click the hollow circle to run Load. Load already carries the full selection after issue 22.

**What to build:** Let a hollow-circle click run the Load command. Keep Unloaded distinct from Unparsed. Keep the current Unparsed secondary indicator. Keep the current look and behavior for Nodes that are Loaded and Parsed. Use the current QueuedLoad and Loading path. Do not add per-Node loading state. Do not let other clicks or render steps load content.

**Blocked by:** 22 — Load full selection across Workspaces.

**See also:** [[.scratch/selective-client-loading/spec.md]] (hollow-circle Load affordance); [[.scratch/selective-client-loading/issues/14-simplify-selective-loading.md]] (Unloaded vs Unparsed; full-selection hollow-circle click).

**Status:** ready-for-agent

- [ ] Unloaded and Unparsed each show the hollow circle. Each runs the same Load command.
- [ ] Unparsed also keeps its current secondary visual indicator.
- [ ] A Node that is Loaded and Parsed keeps its current look and behavior.
- [ ] A hollow-circle click on an occurrence that is not selected first makes that occurrence the only selection. Then Load runs.
- [ ] A hollow-circle click on an occurrence that is already selected keeps the full current selection. Then Load runs for every selected target.
- [ ] Hollow-circle Load uses global QueuedLoad and Loading. It does not add per-Node loading state.
- [ ] No other click or render step loads resident content.

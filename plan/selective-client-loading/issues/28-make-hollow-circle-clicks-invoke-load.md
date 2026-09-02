# 28 — Make hollow-circle clicks invoke Load

**Context:** Unloaded residency and Unparsed source state are different facts. Issue 23 introduces the hollow-circle indicator for both. Users click the hollow circle to run Load. Load already carries the full selection after issue 22.

**What to build:** Let a hollow-circle click run the Load command. Depend on the hollow-circle presentation created by issue 23. Use the current QueuedLoad and Loading path. Do not add per-Node loading state. Do not let other clicks or render steps load content.

**Blocked by:** 22 — Load full selection; 23 — Introduce hollow-circle presentation.

**See also:** [[plan/selective-client-loading/spec.md]] (hollow-circle Load control); [[plan/selective-client-loading/issues/14-simplify-selective-loading.md]] (full-selection hollow-circle click); [[plan/selective-client-loading/issues/23-introduce-hollow-circle-presentation.md]] (introduces hollow-circle presentation).

**Status:** ready-for-agent

- [ ] Unloaded and Unparsed hollow circles each run the same Load command.
- [ ] A hollow-circle click on an occurrence that is not selected first makes that occurrence the only selection. Then Load runs.
- [ ] A hollow-circle click on an occurrence that is already selected keeps the full current selection. Then Load runs for every selected target.
- [ ] Hollow-circle Load uses global QueuedLoad and Loading. It does not add per-Node loading state.
- [ ] No other click or render step loads resident content.

## Comments

- Split from the former combined issue 23: presentation is [[23-introduce-hollow-circle-presentation.md]]; this ticket owns click→Load only.
- 2026-09-02: Parked from WORK.md Blocked. Blocked by already recorded.

# 18 — Apply catch-up HistoryActions under Loaded rules

**Context:** When changes happen server-side, client polling receives those changes and applies them.  The rules for applying the change set is a bit different where absent or unloaded nodes exist.

**What to build:** Apply ordered canonical Change, Undo, and Redo catch-up to the client's graph without inventing Loaded child lists, dropping History, or exposing a half-applied synchronization response.

**Blocked by:** 15 — Introduce HistoryAction messaging; 17 — Represent unloaded child lists end to end.

**See also:** [[.scratch/selective-client-loading/spec.md]] (projected catch-up and atomic install); [[.scratch/selective-client-loading/issues/14-simplify-selective-loading.md]] (Synchronization and projected correctness).

**Status:** ready-for-agent

- [ ] Ordered Change, Undo, and Redo HistoryActions apply structural effects only to Loaded child lists and apply nonstructural facts to resident Node headers.
- [ ] An action concerning an absent header has no projected graph effect, while every received revision and complete HistoryAction remains in projected History.
- [ ] Incremental structural effects never promote an Unloaded list; only receipt of an authoritative complete child list, including an empty list, marks that list Loaded.
- [ ] Installing a complete list and applying its ordered catch-up produce loaded-only derived indexes while preserving canonical owner identity for resident headers.
- [ ] Applying a synchronization response is atomic: either its full valid projection and History transition is visible or no part of it is.
- [ ] The same transition behavior on a fully resident graph preserves existing full-graph Change, Undo, and Redo results.

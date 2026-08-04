# 18 — Synchronize a resident projection safely

**What to build:** Apply ordered canonical HistoryAction tails to a client's resident projection without manufacturing partial child lists, losing History, or exposing a partially applied response.

**Blocked by:** 15 — Introduce HistoryAction messaging; 17 — Represent unloaded child lists end to end.

**Status:** ready-for-agent

- [ ] Ordered Change, Undo, and Redo HistoryActions apply structural effects only to Loaded child lists and apply nonstructural facts to resident Node headers.
- [ ] An action concerning an absent header has no projected graph effect, while every received revision and complete HistoryAction remains in projected History.
- [ ] Incremental structural effects never promote an Unloaded list; only receipt of an authoritative complete child list, including an empty list, marks that list Loaded.
- [ ] Installing a complete list and applying its ordered catch-up produce loaded-only derived indexes while preserving canonical owner identity for resident headers.
- [ ] Applying a synchronization response is atomic: either its full valid projection and History transition is visible or no part of it is.
- [ ] The same transition behavior on a fully resident graph preserves existing full-graph Change, Undo, and Redo results.

# 18 — Synchronize a resident projection safely

**Context:** When the Server Graph changes, Browser Poll receives ordered Changes and applies them. With Absent Headers and Unloaded child lists in the resident projection, structural application follows Loaded rules.

**What to build:** Apply ordered Changes from Poll to the Browser Graph under Loaded rules: structural Ops only on Loaded child lists, non-structural facts on resident Headers, clear local History before applying any Changes returned by Poll, and install each Sync response atomically.

**Blocked by:** 15 — Introduce ChangeRequest submission; 17 — Represent unloaded child lists end to end.

**See also:** [[plan/selective-client-loading/spec.md]] (projected catch-up and atomic install); [[plan/selective-client-loading/issues/14-simplify-selective-loading.md]] (Synchronization and projected correctness); [[plan/selective-client-loading/undo-spec.md]] (clear local History on Poll Changes).

**Status:** agent-done

- [x] Ordered Changes from Poll apply structural Ops only to Loaded child lists and apply non-structural facts to resident Headers.
- [x] An Action that concerns an Absent Header has no projected Graph effect; the Browser still consumes every received Revision so Sync does not stall.
- [x] Incremental structural Ops never promote an Unloaded child list; only receipt of an authoritative complete child list, including an empty list, marks that list Loaded.
- [x] Installing a complete child list and applying its ordered Poll Changes rebuild loaded-only derived indexes while preserving canonical owner identity for resident Headers.
- [x] Applying a Sync response is atomic: either its full valid projection and History transition is visible, or no part of it is.
- [x] When Poll returns one or more Changes, clear local History before applying them; empty Polls and acknowledgements of this Browser's own Actions preserve local History.
- [x] The same transition on a fully resident Graph preserves existing full-Graph Change, Undo, and Redo results.

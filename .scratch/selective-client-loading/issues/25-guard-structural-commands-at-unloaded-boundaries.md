# 25 — Guard structural commands at unloaded boundaries

**What to build:** Apply one all-or-nothing precommit residency rule to every local command that plans structural Change effects, except MoveSelected, so no command partially edits an Unloaded child list while nonstructural editing remains available.

**Blocked by:** 17 — Represent unloaded child lists end to end.

**See also:** [[.scratch/selective-client-loading/spec.md]] (shared structural pre-commit guard); [[.scratch/selective-client-loading/issues/14-simplify-selective-loading.md]] (Structural commands).

**Status:** ready-for-agent

- [ ] Every local Change-planning command other than MoveSelected is rejected before commit when any planned operation would modify an Unloaded child list.
- [ ] A rejected plan is a silent no-op: graph state, History, selection, synchronization queue, and command effects remain unchanged.
- [ ] A plan containing both valid Loaded-list operations and one Unloaded-list operation commits none of its operations.
- [ ] Add Child, Paste, ordinary structural moves, delete-related plans, and other structural command categories exhibit the same all-or-nothing boundary behavior through the common guard.
- [ ] Nonstructural changes to a resident Node header remain allowed when that header's children are Unloaded.
- [ ] Existing ROOT, SYSTEM, and TRASH ordinary delete, permanent delete, and Undo behavior remains available because their required lists are Loaded.

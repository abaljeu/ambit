# 25 — Guard structural commands at unloaded boundaries

**Context:** Local commands can plan Changes that edit child lists. An Unloaded child list must not receive any edit. This includes MoveSelected. Header edits on a resident Node remain allowed when its children are Unloaded. ROOT is Loaded, so delete paths that need ROOT and TRASH stay available. Ticket 26 covers the Move dialog UI rule.

**What to build:** Add one Shared pre-commit guard for every local command that plans a Change, including MoveSelected. If any planned operation would change an Unloaded child list, commit nothing. Make that rejection a silent no-op. Keep nonstructural header edits available on Unloaded Nodes.

**Blocked by:** 17 — Represent unloaded child lists end to end.

**See also:** [[plan/selective-client-loading/spec.md]] (shared structural pre-commit guard); [[plan/selective-client-loading/issues/14-simplify-selective-loading.md]] (Structural commands).

**Status:** ready-for-agent

- [ ] Every local Change-planning command, including MoveSelected, is blocked before commit when any planned operation would change an Unloaded child list.
- [ ] A blocked plan is a silent no-op. Graph, History, selection, sync queue, and command effects do not change.
- [ ] If a plan has both Loaded-list operations and one Unloaded-list operation, the guard commits none of them.
- [ ] Add Child, Paste, MoveSelected, ordinary structural moves, delete plans, and other structural commands all use this one guard.
- [ ] Nonstructural edits to a resident Node header remain allowed when that Node's children are Unloaded.
- [ ] Ordinary delete, permanent delete, and Undo stay available because ROOT is Loaded, including the lists they need such as TRASH.

## Comments

- 2026-09-02: Parked from WORK.md. Parent: [[plan/selective-client-loading/spec.md]]. Guard all structural Change plans, including MoveSelected, from Unloaded child lists.

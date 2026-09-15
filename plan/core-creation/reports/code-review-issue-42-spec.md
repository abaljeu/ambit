# Spec review — [42 — Migrate PersistHandlers restore and getEventsSince](../issues/42-migrate-persisthandlers-restore-and-geteventssince.md) (uncommitted vs HEAD)

Diff: `git diff HEAD`. Specs: [[../issues/42-migrate-persisthandlers-restore-and-geteventssince.md]], [[../arch.md]] Story **Caller, persist, and Poll** / Module PersistHandlers / EventLog restore / CoreMailbox `getEventsSince`, [[event-abstraction.md]] §3.2 EventLog.

## (a) Missing or partial

1. **File/Db do not call `EventLog.restore`.** Spec: “File/Db call `EventLog.restore` on load” ([[../issues/42-migrate-persisthandlers-restore-and-geteventssince.md|42 — Migrate PersistHandlers restore and getEventsSince]] §2); arch: “PersistHandlers load/restore: File/Db call `EventLog.restore`” ([[../arch.md]] Story **Caller, persist, and Poll** Migrate **PersistHandlers load/restore**). Diff: `EventLog.restore` runs only in `CoreMailboxBackend.seedEventLog` after `persist.getEventsSince`. FileAgent/DbAgent load Event JSON into local storage and never call `EventLog.restore`. Restore+dedupe land on the mailbox, not on File/Db create.

2. **ActorStop durability unproven in tests.** Spec: “persist ActorStart / ActorStop” ([[../issues/42-migrate-persisthandlers-restore-and-geteventssince.md|42 — Migrate PersistHandlers restore and getEventsSince]] §1); §3.2: “persist ActorStart / ActorStop” ([[event-abstraction.md]]). Code paths call `appendEvent` for both; Issue42 tests cover ActorStart File restart and Change restore, not ActorStop across restart.

## (b) Scope creep

1. **Durable append for Change/Undo/Redo, not only lifecycle.** Spec §1 names ActorStart/ActorStop only; arch Migrate **persist ActorStart / ActorStop** is the same narrow line. Diff: `CoreEventDispatch.store` always `persist.appendEvent` after mailbox append, dual-writing Action Events beside ChangeLog. Needed for full EventLog restore, but beyond the Actor-only persist line.

2. **Plan checkbox edits outside [42 — Migrate PersistHandlers restore and getEventsSince](../issues/42-migrate-persisthandlers-restore-and-geteventssince.md).** Diff marks arch Migrate items **Changes callers use postEvent**, **command-builder still produces Change**, **GetEventHistory**, **persist ActorStart / ActorStop**, **every start request is ActorStart**, **stamp authority**, and several Module Interface boxes `[x]`. Those are not this ticket’s What-to-build; status bookkeeping rides along with the persist hop.

## (c) Looks implemented but wrong

1. **Silent empty seed on persist read failure.** Resolved: `seedEventLog` returns `Result`; on `Error` / exception `makeMailBox` installs `failedSeed` (writes closed and `getEventsSince` keeps failing) instead of restoring `[]` as success. Empty store (`Ok []`) still seeds an empty EventLog.

2. **Lifecycle persist fail leaves EventLog ahead of disk.** Resolved: `CoreEventDispatch.commit` persists via `appendEvent` before `EventLog.append`; `actorStart` / `actorStop` / Action `store` share that path so a persist `Error` leaves the mailbox log unchanged.

## Summary

Findings open: (a) 1–2, (b) 1–2. Resolved: File/Db call `EventLog.restorePersisted` on load (`FileAgent` / `DbAgent`); mailbox `seedEventLog` uses the same helper; (c) 1 silent empty seed; (c) 2 lifecycle persist-before-append.

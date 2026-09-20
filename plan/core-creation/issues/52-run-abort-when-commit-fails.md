# 52 — Run must not launch when edit commit fails; no orphan live

**Status:** defined
**Type:** coding
**Blocked by:** None — [51 — Browser Run Focus vs Command](51-browser-run-focus-vs-command.md) is `done`.
Estimate: 2h

## Context

Alan: Run while editing showed `Run: old text does not match.` Retry after commit showed `focus already has a live Actor`, but **no live chrome**. Live must not remain if launch failed (or must not launch if the preceding commit failed).

### Client (confirmed)

`execRunOp` always `commitIfEditing` then, on `CommandRequest.tryStart` Ok, appends `SubmitCommand` even when commit failed. `commitTextEdit` on CAS failure sets `lastCmdResult` via `withMoveError` and returns no effects; `withDiagnostic` on Run stamps the **Run** command name onto that error (`Run: old text does not match`). Submit still fires → server can admit a live Actor.

### Server (confirmed)

`dispatchStartActor` calls `pool.startActor` (**putLive**) before `CoreEventDispatch.actorStart`. On Event append failure it replies `Error` without dropping the live row → orphan live; client never gets ActorStart Events → no chrome; next Run hits `focus already has a live Actor`.

## What to build

### 1. Client Run

1. [ ] If `commitIfEditing` was required (mode was Editing) and commit failed (error result / no successful text apply when text needed commit), **do not** `SubmitCommand` / Amble Run. Keep the commit error visible.
2. [ ] Proof: Run while Editing with a SetText that fails CAS does not POST `/command`.

### 2. Server start

1. [ ] If `putLive` succeeds and durable `ActorStart` (or schedule) fails, **drop** the live row (same as failed start — no live Focus). Reply Error.
2. [ ] Prefer: do not putLive until ActorStart Event is durable, or transactional admit — whichever matches existing mailbox style with least risk.
3. [ ] Proof: forced ActorStart Event failure leaves Focus out of `liveFocusIds`.

### 3. Non-goals

1. Fixing root causes of `old text does not match` while editing (separate).
2. Stream / Focus≠Command encode (51 done).
3. Undo/global history.

## See also

[21 — Client shows live Actor](21-client-shows-lock-present.md), [Commands.fs](../../../src/Client/Commands.fs) `execRunOp`, [CoreMailboxBackend.fs](../../../src/Server/Core/CoreMailboxBackend.fs) `dispatchStartActor`

## Comments

- 2026-09-20 — Filed from chat repro: Run while editing → old text; retry → live without chrome. Status `defined`.

## Time

- 2026-09-20 — Ticket (from chat)

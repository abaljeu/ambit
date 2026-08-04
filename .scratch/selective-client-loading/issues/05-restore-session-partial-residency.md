# Restore sessions under partial residency

Type: grilling
Status: resolved
Blocked by: 03, 04

## Question

What navigation and interaction intent should session state persist and restore, how should restoration request missing residency without silently changing that intent, and what should happen when remembered node IDs or scopes no longer exist or their loads fail?

## Answer

- Session state remains a best-effort tab-session preference. It persists the zoom-root ID, the ID of the Workspace that owns that zoom root, and expanded node IDs. Selection, modes, overlays, caret, scroll, residency, and other interaction state remain ephemeral.
- “Current node” means the zoom root, never a selected or focused occurrence. The saved Workspace ID is bootstrap data derived from that zoom root when saving, not independent navigation intent.
- Before first render, restoration sends the ticket-03 initial no-revision `Workspace` batch targeting ROOT and the saved Workspace ID. The targets are deduplicated when that Workspace is ROOT.
- After the batch installs, restoration uses the saved zoom root only when it belongs to the returned Workspace closure. A missing Workspace or zoom root, or a stale ownership pairing, silently falls back to the normal default startup state, preserving current behavior.
- Saved folds are presentation hints, not loading requirements. Restoration reapplies an expansion only when that node’s child list is already resident and silently skips every other saved expansion.
- Missing or malformed session data keeps the normal default. A transport, HTTP, or decode failure uses the existing boot-error and retry behavior without clearing saved state; later loads catch up through the latest atomically read server revision as decided in [Define synchronization and revision correctness](09-define-sync-revision-correctness.md).
- Refresh begins a new residency session while restoring these tab-session preferences. Pending-edit residency and replay dependencies remain with ticket 08.

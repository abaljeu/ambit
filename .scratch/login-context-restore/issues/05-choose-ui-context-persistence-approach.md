# Choose UI-context persistence approach

Type: grilling
Status: resolved
Blocked by: 02

## Question

Normal Refresh restores UI context (e.g. Zoom, folds) via tab `sessionStorage` in [[src/Client/SessionState.fs]]. After iOS unloads Safari from memory with tabs still open, the active tab often cold-reloads (new Session). What persistence approach should restore that same UI context after that reload?

Grill toward a locked approach — medium and scope of “same as Refresh,” not the full selective-loading residency design. Home Screen/PWA storage quirks are out of scope unless the destination is redrawn.

## Comments

- Ruled out of scope: human — nothing needs be done for UI-context restore on this effort. Destination is auth-only.

## Answer

Out of scope for this map. After iOS memory-unload, only the login redirect is in scope. Zoom, folds, and other Refresh UI restore are not part of this destination.

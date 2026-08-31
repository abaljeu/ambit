# What storage survives Safari tab discard

Type: research
Status: closed

## Question

For Safari on Apple (regular tab, not Home Screen/PWA): what storage survives a harsh tab discard / process reclaim versus a normal Refresh, and how does that map to Gambol’s current persistence?

Compare at least:

- Cookies (HttpOnly and non-HttpOnly)
- `sessionStorage` (where [[src/Client/SessionState.fs]] keeps zoom/folds)
- `localStorage`
- IndexedDB / Cache Storage (if relevant to WebKit tab lifecycle)

Cite primary WebKit/Safari sources. State clearly which of today’s auth and UI-context stores would still be present after the destination’s “harsh restart,” and which would be empty the way a brand-new tab is.

## Comments

- Findings: [[plan/login-context-restore/research/02-what-storage-survives-safari-tab-discard.md]]
- Closed on `w/login-context-restore` (common project branch; no `research/*` checkout).

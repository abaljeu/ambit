# HttpOnly cookie options for Safari durability

Type: research
Status: closed

## Question

As a **sidenote investigation** (not a locked decision): which cookie and session-cookie design choices are known to improve or worsen durable first-party auth across Safari tab discard / reclaim?

Cover with primary sources:

- HttpOnly vs readable cookies for a site-password gate like `gambol_auth`
- Session cookie vs persistent Expires/Max-Age under Safari/WebKit
- SameSite (Strict vs Lax), Secure, Path, and partitioned-cookie interactions with ITP
- Any Apple-documented guidance for “stay logged in” on return to a first-party tab after idle

Record options and tradeoffs only — do not recommend a product decision here; that belongs to auth-persistence grilling.

## Comments

- Findings: [[plan/login-context-restore/research/03-httponly-cookie-options-for-safari-durability.md]]
- Closed on `w/login-context-restore` (common project branch; no `research/*` checkout).

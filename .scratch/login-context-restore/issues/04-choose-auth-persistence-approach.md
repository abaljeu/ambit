# Choose auth persistence approach

Type: grilling
Status: resolved
Blocked by: 01, 02, 03

## Question

Given how Safari actually treats our long-lived `gambol_auth` cookie on harsh restart, and which stores survive, what approach should Gambol take so the user returns already authenticated on `/ambit` (no login form) for as long as today’s cookie longevity already allows?

Grill toward a locked approach — not implementation detail. Candidates may include (non-exhaustive; human decides): relying on cookie-attribute fixes alone, a different cookie lifetime/SameSite posture, client-assisted re-auth without a visible form, or accepting a narrower survival window. Desktop AuthStore is out of scope as the primary path.

## Comments

- Round 1: Q1 **A unless it fails** (cookie attributes only: keep HttpOnly + long Expires, SameSite Strict → Lax; no client credential store unless Lax fails HITL). Q2 **B** (provisional — do not close until Safari HITL).
- Round 2: Q3 **A** (SameSite Strict → Lax only; not None). Q4 — `http://` local is not a use case; Secure is in play. Q5–Q6 still open.
- Round 3: Q5 **D** (fallback after we see failure mode). Q6 rejected kill-Safari: expected to lose context. Real event is iPad/iPhone unloading Safari from memory with tabs still open; on reactivate the active tab reloads and login redirect is definitive. Other UI forgetfulness unknown (ticket 05).
- Round 4: Q6 **A** — exact unload procedure does not matter; if the still-open tab cold-reloads, that is the test. Pass = no login form. Q7 **A** — map Destination updated; kill/quit Safari out of scope.
- Round 5: Q8 — one Gambol site; cross-site CSRF not a concern; Lax OK. Q9 — do not gate charting on HITL; write an experimenting step into the plan; implementation includes experiment to find the solution. Q2 B superseded.
- Implement landed: `setAuthCookie` now Lax + Secure + HttpOnly (same long expiry). HITL experiment still on the human after deploy — see [[map.md]] Notes.

## Answer

First try: keep HttpOnly + long expiry; change SameSite Strict → Lax; set Secure. One Gambol site, so Lax CSRF from “other sites” is not a concern.

Do not add a client credential store unless the experiment fails. If it fails, inspect cookie gone vs present-but-not-sent, then choose the fallback.

HITL is an **implementation experiment**, not a close-gate on this ticket: after the Server cookie change, on iPad/iPhone, if a still-open tab cold-reloads after memory unload, pass = `/ambit` with no login form. Exact unload procedure does not matter. Kill/quit Safari is out of scope.

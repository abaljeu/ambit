# Choose auth persistence approach

Type: grilling
Status: open
Blocked by: 01, 02, 03

## Question

Given how Safari actually treats our long-lived `gambol_auth` cookie on harsh restart, and which stores survive, what approach should Gambol take so the user returns already authenticated on `/ambit` (no login form) for as long as today’s cookie longevity already allows?

Grill toward a locked approach — not implementation detail. Candidates may include (non-exhaustive; human decides): relying on cookie-attribute fixes alone, a different cookie lifetime/SameSite posture, client-assisted re-auth without a visible form, or accepting a narrower survival window. Desktop AuthStore is out of scope as the primary path.

## Comments

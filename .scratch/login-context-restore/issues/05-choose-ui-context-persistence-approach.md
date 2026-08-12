# Choose UI-context persistence approach

Type: grilling
Status: open
Blocked by: 02

## Question

Normal Refresh restores UI context (e.g. zoom, folds) via tab `sessionStorage` in [[src/Client/SessionState.fs]]. Harsh Safari restart often behaves like a colder load. What persistence approach should restore that same UI context after the destination’s harsh restart?

Grill toward a locked approach — medium and scope of “same as Refresh,” not the full selective-loading residency design. Home Screen/PWA storage quirks are out of scope unless the destination is redrawn.

## Comments

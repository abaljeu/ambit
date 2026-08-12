# Safari login and UI-context restore

Labels: wayfinder:map

## Destination

After Safari on Apple (regular Safari tab) restarts an inactive Gambol page — a full reload harsher than Refresh that often demands a new login — the user returns already authenticated (e.g. on `/ambit`, no login form) **and** with UI context restored (e.g. zoom) the way a normal Refresh does via the tab’s persistent storage. Longevity: as long as the existing auth cookie already allows. Success vibe: switch apps, come back after idle (e.g. 30+ min), enter a note — no relogin/renavigate.

## Notes

- **Why it matters:** Relogin + renavigate breaks the note-taking workflow.
- **Surface:** Regular Safari tab on Apple — not Desktop-first; not Home Screen/PWA unless the destination is later redrawn.
- **“Remember me” UI** is not the problem; ignore cosmetic remember-me controls.
- **Prior facts:** Server sets long-lived HttpOnly `gambol_auth` cookie ([[src/Server/AuthToken.fs]], [[src/Server/RouteRegistration.fs]]); Desktop has Windows AuthStore restore ([[src/Desktop/AuthStore.fs]]) — orthogonal here; Client restores zoom/folds in `sessionStorage` ([[src/Client/SessionState.fs]]), not auth. Charting notes: [[tmp/wayfinder-login-restore-chart.md]].
- **Sidenote (idea, not locked):** HttpOnly session cookies / cookie-attribute choices may help durable Safari auth — investigate via research; do not treat as a locked decision.
- **Skills:** wayfinder, grilling, domain-modeling, research; implement-fsharp-feature only after the way is clear.
- **HITL:** Parent owns grilling questions to the human. Do not delegate grilling questions via subagent summaries. Research is AFK/delegable.
- Plan by default: charting produces decisions/spec clarity, not shipped behavior, unless Notes later override.

## Decisions so far

## Not yet specified

- Whether cookie-attribute / SameSite / partition fixes alone restore auth, or client-side credential re-presentation is also needed — hangs on research + auth grilling.
- Exact UI-context inventory beyond “what Refresh already restores” (folds, bootstrap widen, workspace residency cues) once persistence medium is chosen.
- HITL verification recipe on real Safari (idle duration, discard vs process kill vs device reboot).
- How this effort hands off to `/to-spec` or implementation slices once approaches are locked.
- Interaction details with selective-client-loading bootstrap restore ([[.scratch/selective-client-loading/]]) once auth survives the harsh restart.

## Out of scope

- Desktop-first / Windows AuthStore as the primary fix path for this destination.
- Home Screen web app / PWA install surface (unless destination redrawn).
- Changing or adding a “Remember me” UI control.
- Multi-user account system; auth remains the site password gate.
- macOS / iOS native App host (none in-tree).

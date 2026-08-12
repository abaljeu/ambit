# Safari login restore

Labels: wayfinder:map

## Destination

After iOS unloads Safari from memory while tabs stay open (iPad/iPhone, regular Safari tab), the active tab often cold-reloads. The user returns already authenticated (e.g. on `/ambit`, no login form) **and** with the previously active Workspace Loaded and Zoom restored (Refresh-parity). Longevity: as long as the existing auth cookie already allows. Success vibe: switch apps, come back after idle, enter a note — no relogin/renavigate. Kill/quit Safari is not this event.

## Notes

- **Why it matters:** Relogin breaks the note-taking workflow.
- **Way:** auth landed (Lax + Secure); HITL login succeeded. Ticket 06 code landed (session then localStorage). HITL cold-init remaining.
- **Surface:** Regular Safari tab on iPad/iPhone — not Desktop-first; not Home Screen/PWA unless the destination is later redrawn. Event: iOS unloads Safari from memory, tabs stay open, active tab cold-reloads.
- **“Remember me” UI** is not the problem; ignore cosmetic remember-me controls.
- **Prior facts:** Server sets long-lived HttpOnly `gambol_auth` cookie ([[src/Server/AuthToken.fs]], [[src/Server/RouteRegistration.fs]]); Desktop has Windows AuthStore restore ([[src/Desktop/AuthStore.fs]]) — orthogonal here; Client restores zoom/folds via `gambol-session-v1` in sessionStorage then localStorage ([[src/Client/SessionState.fs]]), not auth. Charting notes: [[tmp/wayfinder-login-restore-chart.md]].
- **Implementation experiment (auth):** After Server sets SameSite Lax + Secure (HttpOnly, same long expiry), HITL on iPad/iPhone: if a still-open tab cold-reloads after memory unload, pass = `/ambit` with no login form. Exact unload procedure does not matter. If it fails, inspect cookie gone vs present-but-not-sent, then choose fallback ([[.scratch/login-context-restore/issues/04-choose-auth-persistence-approach.md]]).
- **Branch:** Working area slug `login-context-restore` ⇒ stay on `w/login-context-restore` only (`w/<slug>`). No `research/*` or other branch names for this project's research/findings.
- **Parallel research:** OK only with disjoint finding files (e.g. `.scratch/login-context-restore/research/01-….md`). Do not contend on `map.md`, `WORK.md`, or the same ticket body concurrently — serialize map/ticket closure updates.
- **Skills:** wayfinder, grilling, domain-modeling, research; implement-fsharp-feature only after the way is clear.
- **HITL:** Parent owns grilling questions to the human. Do not delegate grilling questions via subagent summaries. Research is AFK/delegable.
- Plan by default: charting produces decisions/spec clarity, not shipped behavior, unless Notes later override.

## Decisions so far

- Research 01–03 closed on `w/login-context-restore` with disjoint findings under [[.scratch/login-context-restore/research/]].
- Auth drop after harsh Safari restart is poorly explained by ITP’s 7-day caps for this HttpOnly cookie; strongest primary lead is SameSite=Strict failing after tab/window recovery ([[.scratch/login-context-restore/research/01-why-safari-harsh-restart-drops-auth.md]], WebKit bug 200345).
- UI context in `sessionStorage` does not survive tab-session destruction; auth cookie and UI storage are different failure modes on the same event ([[.scratch/login-context-restore/research/02-what-storage-survives-safari-tab-discard.md]]).
- Cookie-attribute options/tradeoffs recorded without locking a product choice ([[.scratch/login-context-restore/research/03-httponly-cookie-options-for-safari-durability.md]]).
- [[.scratch/login-context-restore/issues/04-choose-auth-persistence-approach.md]] — first try Lax + Secure + HttpOnly; experiment during implement; fallback only if that fails. HITL login succeeded.
- [[.scratch/login-context-restore/issues/05-choose-ui-context-persistence-approach.md]] — closed out of scope; destination later redrawn — see ticket 06.
- [[.scratch/login-context-restore/issues/06-restore-active-workspace-on-cold-init.md]] — Refresh-parity Workspace + Zoom; same snapshot; sessionStorage then localStorage fallback.

## Not yet specified

- HITL after deploy: memory-unload cold reload Loads previously active Workspace and restores Zoom (ticket 06).

## Out of scope

- Desktop-first / Windows AuthStore as the primary fix path for this destination.
- Home Screen web app / PWA install surface (unless destination redrawn).
- Changing or adding a “Remember me” UI control.
- Multi-user account system; auth remains the site password gate.
- macOS / iOS native App host (none in-tree).
- Kill/quit Safari (user ends the app). That Session is expected to be gone; not the destination event.
- (superseded) blanket UI-context out of scope — destination redrawn; Workspace restore is in scope via ticket 06. Ticket 05 stays closed as the old scope call.

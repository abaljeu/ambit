# Implement auth cookie (Lax + Secure)

Branch: `w/login-context-restore`. Tree left dirty. No commit. No push.

## What changed

[[src/Server/RouteRegistration.fs]] `setAuthCookie`: keep HttpOnly and 10-year Expires; SameSite Strict → Lax; set Secure true. Cookie name still `gambol_auth` in [[src/Server/AuthToken.fs]] (unchanged).

`clearAuthCookie` now Delete with the same HttpOnly + Secure + Lax options so logout can clear the cookie after Secure is set. No client credential store. No Desktop AuthStore. No Zoom/folds/UI context.

Ticket comment pointer: [[issues/04-choose-auth-persistence-approach.md]] — implement landed; HITL still human after deploy.

## Test / seam status

No pre-agreed seam. Searched Server/Shared tests for cookie attributes / Set-Cookie / `setAuthCookie` / SameSite / Secure / HttpOnly: none. [[tests/Server.Tests/AuthTokenTests.fs]] only asserts `cookieHeaderValue` starts with `gambol_auth=` (name=value, not attributes). Did not invent a new harness. No focused tests run.

## Typecheck

`dotnet build src/Server -c Debug` — succeeded, 0 warning, 0 error (after set + clear).

## Code review vs HEAD

Range: uncommitted `git diff HEAD` on this cookie work. `measure-fs-size.py --diff HEAD`: no over-limit bindings, no added long lines. Review done inline (diff is two CookieOptions sites); no extra sub-agents.

### Standards

No hard violations of [[.cursor/rules/fsharp-source.mdc]] (40-line bindings, 100-char lines, 4-space indent, match existing CookieOptions style). Smell baseline: possible Duplicated Code (HttpOnly/Secure/Lax repeated on set vs clear) — judgement call; left inline, no helper. No Speculative Generality.

Stale doc not in this diff: [[doc/api.md]] still says `gambol_auth` is HttpOnly, SameSite=Strict.

### Spec

Sources: [[map.md]] Destination + Notes experiment; [[issues/04-choose-auth-persistence-approach.md]] Answer.

Implemented: HttpOnly + long expiry kept; SameSite Lax; Secure set; no client store; Desktop AuthStore untouched; UI context out of scope.

Companion not in the Answer text: matching `clearAuthCookie` Delete options. Needed so logout still clears a Secure+Lax cookie. Not a new feature.

HITL experiment not run here (agent cannot). Map Notes already hold the recipe.

Summary: Standards 0 hard / 1 smell (duplicated CookieOptions flags). Spec 0 missing / 1 justified companion (Delete match) / 0 wrong.

## Suggested commit message

```
Set gambol_auth SameSite Lax and Secure so Safari tab recovery still sends the cookie.

```

Include [[src/Server/RouteRegistration.fs]]. Optionally the ticket comment pointer. Do not commit unless asked.

## HITL experiment (human after deploy)

Recipe (also on [[map.md]] Notes): After Server sets SameSite Lax + Secure (HttpOnly, same long expiry), on iPad/iPhone: if a still-open tab cold-reloads after memory unload, pass = `/ambit` with no login form. Exact unload procedure does not matter. Kill/quit Safari is out of scope. If it fails, inspect cookie gone vs present-but-not-sent, then choose fallback on [[issues/04-choose-auth-persistence-approach.md]].

## WORK.md mutations (for parent)

- `remove` Active: [[.scratch/login-context-restore/issues/04-choose-auth-persistence-approach.md]] — implement SameSite Lax + Secure on `gambol_auth` (code landed)
- `add` Pending: [[.scratch/login-context-restore/map.md]] — HITL iPad/iPhone still-open tab cold-reload after memory unload; pass = `/ambit` no login form (ticket: [[.scratch/login-context-restore/issues/04-choose-auth-persistence-approach.md]])
- `add` Pending (optional): [[doc/api.md]] — cookie line still says SameSite=Strict; update to Lax + Secure + HttpOnly

The project is now `done` after the later context-restore HITL passed on 2026-08-15; [[.scratch/index.md]] was regenerated.

## Blockers

None.

# Implement issue 20 — Client presents credential

Date: 2026-09-06

Issue: [[../issues/20-client-presents-credential.md]]. First UI ticket in this Project (earliest Client/Browser issue; [[../issues/21-client-shows-lock-present.md]] and [[../issues/22-client-cancels-a-job.md]] come later). Project Stage stays `active`. Branch: `dev`. No commit.

## What to build

The live Browser presents a credential on every message. A missing or inactive credential is the same auth refuse as an inactive Actor sender. Locked source: the existing `gambol_auth` cookie at the Adapter ([[../issues/10-define-actor-cancellation-and-output-admission.md]]).

## What changed

- [[src/Client/JsInterop.fs]] — `postJson` sends `credentials: 'same-origin'` so the session cookie goes with every Browser POST (changes, load, save, and desktop JSON posts that use this helper). GET poll/state/capabilities already sent it.
- [[tests/Server.Tests/BrowserCredentialTests.fs]] — without a live cookie, state, poll, changes, and load are HTTP 401 (`CoreAuth.refuse`). With the live cookie, those calls are not 401.

## Focused verification

- `dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~BrowserCredentialTests"` — 2 passed.
- `./scripts/client.sh build` — Fable and esbuild succeeded.

## Leftover tree

[[../issues/21-client-shows-lock-present.md]] still waits on this ticket (now done) plus [[../issues/16-track-running-job.md]]. [[../issues/17-cancel-a-job.md]] was not started. Core still does not store the Browser cookie in the Actor credential set; Adapter cookie check remains the Browser source.

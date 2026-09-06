# Implement issue 14 — Server tracks credentials

Date: 2026-09-06

Issue: [[../issues/14-server-tracks-credentials.md]]. Project Stage stays `active`. Branch: `dev`. No commit.

## Readiness

Verdict for issue 14: **all-clear**. Stage is `active`, not grilling. Ticket was `ready-for-agent` with no blockers. Issues 09–12 are resolved. Issue 01 is done. The locked refuse family is in [[../issues/10-define-actor-cancellation-and-output-admission.md]].

14++ (15–22) was not re-assessed. Next ticket if they continue is [[../issues/15-launch-actor-and-hold-span.md]].

## Answer

Issue 14 was ready, so it was implemented. Core now owns a process-lifetime credential set. `CoreAuth.post` refuses an inactive sender with `Unauthorized` and does not call enqueue. The HTTP Adapter maps that same refuse to `Results.Unauthorized()`, which is the Adapter cookie-fail result. A Database-unavailable or TCP Error stays a different family.

## What changed

- [[src/Server/Core/CoreCredentials.fs]] — `Credential`, mailbox-backed `CoreCredentials`, and `CoreAuth.admit` / `CoreAuth.post`.
- [[src/Server/Core/CoreRuntime.fs]] — `create` holds `credentials` for the Server process.
- [[src/Server/RouteRegistration.fs]] — `PersistenceContext` keeps that set alive.
- [[src/Server/Api.fs]] — `agentErrorResult` maps `CoreAuth.refuse` to HTTP 401.
- [[tests/Server.Tests/CoreCredentialsTests.fs]] — five focused facts.

`CoreChanges.postChange` is still the inner apply mailbox. Browser POST still uses the Adapter cookie, then that handle. [[../issues/20-client-presents-credential.md]] is the ticket that presents a Browser credential on every message.

## Focused verification

- `dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~CoreCredentialsTests|FullyQualifiedName~CoreChangesTests"` — 8 passed, 0 failed.

No Shared or Browser file changed, so the Browser compile gate was not required.

## Out of scope

Launch, cancel, finish, shutdown, Browser credential presentation, lock-present UI, and issue 13 mirror removal. Those stay 15–22 and 13.

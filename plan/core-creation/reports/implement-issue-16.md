# Implement issue 16 — Track running job

Date: 2026-09-06

Issue: [[../issues/16-track-running-job.md]]. Project Stage stays `active`. Branch: `dev`. No commit.

## What to build

Query by the launch public number succeeds while the Actor is registered. Query does not return a job result or job Error. The public number is the query key, not the cancel argument ([[../issues/17-cancel-a-job.md]]). The number lasts until delete-actor ([[../issues/18-finish-and-drop.md]]).

## What changed

- [[src/Server/Core/CoreActorPool.fs]] — `query` looks up the pool map by `PublicNumber`. Ok is the retained launch identity (`LaunchRequest`: name, Revision, span). It does not return a send credential, a completed-job result, or a job Error. Unknown number is `CoreActorPool.unknownJob`.
- [[tests/Server.Tests/CoreActorPoolTests.fs]] — three facts: identify by public number; no job result or job Error after the Actor function returns; two jobs distinguished by number, unused number fails.

HTTP Command query and Browser UI stay later. Shared was not edited.

## Focused verification

- `dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~CoreActorPoolTests"` — 8 passed, 0 failed.

No Shared or Browser file changed, so the Browser compile gate was not required.

## Leftover tree

Query still succeeds after the Actor `Async` returns, because delete-actor is [[../issues/18-finish-and-drop.md]]. Cancel by NodeId is [[../issues/17-cancel-a-job.md]]. HTTP Command query is later. First UI ticket [[../issues/20-client-presents-credential.md]] was implemented next in this session.

# 27 — Field test the Load trial sequence

**Status:** `defined`
**Type:** task
**Blocked by:** [25 — Pace server functions](25-pace-server-functions.md)
**Trial step:** 7. Next: [26 — Revisit WebDAV parse](26-revisit-webdav-parse.md).

## Context

Trial step 7. Alan runs this checklist on the deployed Azure app `amble` (Free plan). A found problem becomes a new ticket. Many of those tickets sit outside this Project. This ticket is done when Alan signs off.

Desktop_mapped is the WebDAV branch inside the Load actor and inside a Save actor. It waits until the Git case and the Neither case in [23 — Run the Load actor for Git and Neither](../../actor-as-client/issues/23-run-the-load-actor-for-git-and-neither/23-run-the-load-actor-for-git-and-neither-ticket.md) are straight.

WebDAV needs its own design effort before it is built. That design also considers the Grok Bot AI response channel. `GrokBotSettings.deliverResponseUrl` in `src/Server/GrokBotSettings.fs` names `POST /ambit/actors/deliver`, `RouteRegistration` maps that route, and `RunAgentActor` in `src/Server/RunAgentActor.fs` sends the address as `ResponseUrl`. One part of the design is the desktop sending material to the server. `loadActorFunction` and `saveActorFunction` in `src/Server/GithubTransportActor.fs` pull and push when a person runs Load or Save, and the design has to place a desktop send beside that request style.

## Checklist

1. [ ] Load a Workspace that has a git remote.
2. [ ] Load a Workspace that has no git remote.
3. [ ] Load a Directory.
4. [ ] Load a File.
5. [ ] Load a multi-selection.
6. [ ] git Save.
7. [ ] desk Save.
8. [ ] Each Load queues only its targets.
9. [ ] The Browser does not request `/ambit/workspace/reconciliation/directory`.
10. [ ] The run has no HTTP 502 proxy timeout.
11. [ ] Cancel removes the chip.
12. [ ] Azure CPU stays under 60% for a short window.
13. [ ] Azure CPU stays under 5% as a daily average.
14. [ ] `http-responses.log` and `eventlog.xml` show no restart.
15. [ ] Record each found problem as a new ticket.
16. [ ] Alan signs off.

# 27 — Field test the Load trial sequence

**Status:** `defined`
**Type:** task
**Blocked by:** [25 — Pace server functions](25-pace-server-functions.md)
**Trial step:** 7. Next: [26 — Revisit WebDAV parse](26-revisit-webdav-parse.md).

## Context

Trial step 7. Alan runs this checklist on the deployed Azure app `amble` (Free plan). A found problem becomes a new ticket. Many of those tickets sit outside this Project. This ticket is done when Alan signs off.

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

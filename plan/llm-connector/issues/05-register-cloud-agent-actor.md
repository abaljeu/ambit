# 05 — Register cloud-agent Actor

**Status:** ready-for-agent
**Blocked by:** none (CloudAgents library on `ready`; Core finish-and-drop landed)

## Context

Actor definitions stay outside Core. Composition registers an `ActorFn` under ActorName `cloud-agent`. The Actor uses [[src/CloudAgents/]] in-process (no stdin/stdout, no HTTP to our console).

## What to build

- Register `cloud-agent` at Server startup (composition / host wiring, not inside Core).
- `ActorFn` receives subgraph + send Credential + CoreChanges.
- Build prompt: fixed Ambit system prompt (module constant for v1) + user message (Focus Header / `?` remainder when present; for Server-only tests a message from Focus text is enough) + Md pack of the launch subgraph.
- Call CloudAgents runner **no-repo**; read `CURSOR_API_KEY` via CloudAgents config.
- On success: convert result text to graph and `postChange` Owned children of Focus.
- Rely on Core finish-drop when the Actor returns (enqueue finish marker → drop). No cancel in this ticket.

- [ ] `cloud-agent` is registered outside Core at startup.
- [ ] Actor calls CloudAgents with no repo and posts Owned children of Focus via `postChange`.
- [ ] API key is only read through CloudAgents config; Core never sees it.
- [ ] Fixed Ambit system prompt is included in the outbound prompt.

## See also

[[../reports/grill-run-agent-actor-2026-09-08.md]], [[../reports/first-agent-cursor-cloud-agents.md]], [[06-api-actors-create-to-core-launch.md]]

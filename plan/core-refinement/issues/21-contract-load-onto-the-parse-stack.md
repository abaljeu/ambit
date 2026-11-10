# 21 — Contract Load onto the Parse stack

**Status:** `done`
**Type:** coding
**Blocked by:** None
**Trackers:** architecture, user manual

## Context

Web desk Load of a Workspace Node, Directory Node, or File Node already posts through mailbox Load. A client hop remains for desktop-mapped Load and for the git Load after-step. The git Load after-step is already done. Desktop-mapped Load is still open.

## What to build

The remaining client hop retires. Load of a Workspace Node, Directory Node, or File Node uses the Parse stack.

### Contract

1. [x] **Mailbox Load stays** — Web desk Load of a Workspace Node, Directory Node, or File Node posts through mailbox Load.
2. [x] **Client hop retires** — The remaining client hop for desktop-mapped Load and for the git Load after-step is gone.
3. [x] **Stack is the Load path** — Those Loads use the Parse stack.

### Obligations

1. [x] **Parse and persist claim** — The Parse and persist claim shows Load on the Parse stack and shows the client hop as retired.
2. [x] **Load test** — A test shows desktop-mapped Load and the git Load after-step use the Parse stack and do not use the old client hop.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## Amendments

1. **Hold the stack** — The first desktop-mapped Load command sets `holdStack`. Desk Load does not push while that flag is set. After the mapped bytes are stored, a second Load posts the subject with `holdStack` clear. That second Load uses the Parse stack. Its quiet plan does not start another desktop push.
2. **Git Load after-step** — That Browser after-step was already retired. This ticket's test shows the Browser does not run a second reconcile or parse. Server git Load still pushes that Workspace Node onto the Parse stack.
3. **Parse routes remain** — `POST /ambit/file/parse` and the directory reconcile route stay. Load does not call them.

## Time

- 2026-10-16 2h — contract desktop-mapped Load and the git Load after-step onto the Parse stack (from chat)

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement map](plan/core-refinement/map.md)
- [core-refinement](plan/core-refinement/project.md)
- [26 — Revisit WebDAV parse](../../github-transport/issues/26-revisit-webdav-parse.md)

## Open design

Desktop_mapped is the WebDAV branch inside the Load actor and inside a Save actor. It waits until the Git case and the Neither case in [23 — Run the Load actor for Git and Neither](../../actor-as-client/issues/23-run-the-load-actor-for-git-and-neither/23-run-the-load-actor-for-git-and-neither-ticket.md) are straight.

WebDAV needs its own design effort before it is built. That design also considers the Grok Bot AI response channel. `GrokBotSettings.deliverResponseUrl` in `src/Server/GrokBotSettings.fs` names `POST /ambit/actors/deliver`, `RouteRegistration` maps that route, and `RunAgentActor` in `src/Server/RunAgentActor.fs` sends the address as `ResponseUrl`. One part of the design is the desktop sending material to the server. `loadActorFunction` and `saveActorFunction` in `src/Server/GithubTransportActor.fs` pull and push when a person runs Load or Save, and the design has to place a desktop send beside that request style.
- [22 — Drop the Browser git Load after-step](plan/github-transport/issues/22-drop-browser-git-load-after-step.md)
- [Parse and persist](doc/current/parse-persist.md)

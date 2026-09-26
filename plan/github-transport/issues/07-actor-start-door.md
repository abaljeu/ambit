# 07 — Actor start door

**Type:** grilling
**Status:** done
Blocked by: None
Actual: 5m

## 1. Question

- [x] How does Command Load or Save start the GitHub Peer Actor?

## 2. Answer

Locked 2026-09-26 (Alan, Github Sync room).

Not Run. Not a `?git` / Run entrée.

Door: Load (or Save) Command → load/save command request → mailbox → message to actor pool → pool invokes the GitHub Peer Actor → it acts.

The Actor only cares about Focus (which Workspace / work tree).

Same wiring for Save as for Load.

Map gist: [[../map.md]] Decisions so far item 12.

## Comments

- 2026-09-26: Alan locked in chat. Status `done`. Mailbox → actor pool. Focus names the work tree.

## Time

- 2026-09-26 5m — recorded lock from chat

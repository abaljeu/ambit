# Grill: Run Agent Actor (Server slice)

Date: 2026-09-08  
Project: [[../project.md]]  
Related: [[first-agent-cursor-cloud-agents.md]]

## Shared understanding

Build the **Server** Run Agent Actor that uses Core’s actor pool (register / launch / mailbox / finish-drop) while **Actor definitions stay outside Core**. Standalone [[src/CloudAgents/]] is the in-process library. **No Browser / Client / `?` recognition in this session.**

## Locked decisions

1. **Home:** `plan/llm-connector/` owns Actor + Create path. CloudAgents remains the library only.
2. **ActorName:** `cloud-agent` — Server composition calls `register` at startup.
3. **v1 behavior:** no-repo CloudAgents; poll full result; write Owned children of Focus; no cancel, no stream, no `life`/repo attach.
4. **HTTP:** `Api.fs` handles POST → typed message → message handler → Core. Not Core HTTP.
5. **Launch mapping:** Browser/API body uses nodelist + focus + root (+ revision, actor). Map to Core launch (registered name + revision + span per **Define the Core Command launch contract**) **at the edge**. Do not revise that Core contract’s locked text this slice.
6. **Secrets:** `CURSOR_API_KEY` via CloudAgents config / module read — never Core, Graph, or cookie.
7. **Prompt:** Ambit-specific **system prompt** (fixed string in Actor module for v1) + user message + Md pack.
8. **Write-back:** result text → Md→graph → `postChange` Owned children of Focus.
9. **Lock:** Focus only (extract/pack may be larger).
10. **Out of session:** Client/`?` UI; cancel; streaming; repo attach.

## Implementation tickets

- [[../issues/05-register-cloud-agent-actor.md]]
- [[../issues/06-api-actors-create-to-core-launch.md]]

## Next

After tickets: implement Server slice; Client later.

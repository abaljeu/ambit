# Summary goal

## New Summary

Summary: On App refresh after a prior Session, the Browser shows the Graph from a local IndexedDB snapshot plus stored Changes, then does a Poll, so the user does not wait for `/state` while a blank screen or Loading... is visible.

## Why it matches wait-what and CONTEXT

[[.agents/skills/wait-what/SKILL.md]] asks for a re-pitch: a little context, ASD-STE100, and terms from [[CONTEXT.md]]. The old Summary listed ticket progress. The new line states the user goal from [[plan/client-start-time/research.md]] and [[cache-first-boot-via-poll.md]]: after a prior Session, App refresh must show the Graph without a long blank screen or Loading... wait. The context is the chosen path: local IndexedDB snapshot plus stored Changes, then a Poll, so first show does not wait for `/state`.

CONTEXT terms used: App, Session, Browser, Graph, Change, Poll. IndexedDB is the store named in the design; it is not a glossary synonym. The line says stored Changes, not Change log, because CONTEXT reserves Change log as an avoid-word for History. Loading... is the `#amb-document` placeholder from research. The line does not name Load or Workspace Node: those bound snapshot scope (no Load-only Workspace Nodes), not the user-visible goal.

STE100: one sentence, present tense, `so` for purpose, `does not wait` for the user outcome. No ticket counts, no HITL status.

## What changed

In [[plan/client-start-time/project.md]]: replaced the status dump with the goal line above. Set `Updated: 2026-09-02`. Left `Stage: active`. No other body text.

# 28 — Parsed when clear

**Status:** `coded`
**Type:** coding
**Actual:** 15m
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

A File Node carries `noServerFile`. Parsed with `noServerFile` is not a state. The parse thread adds `InMsg` `ParseFinished` when it finishes that File Node. Core loop applies `ParseFinished`. The architecture says that apply sets the File Node Parsed only when `noServerFile` is clear. This ticket owns that rule. A Directory Node and a Workspace Node do not carry `noServerFile`.

## What to build

Core loop sets a File Node Parsed from `ParseFinished` only when `noServerFile` is clear. When `noServerFile` is set, that File Node stays Unparsed.

### Parsed when clear

1. [x] **Clear flag** — `ParseFinished` sets a File Node Parsed only when `noServerFile` is clear.
2. [x] **Set flag** — When `noServerFile` is set, `ParseFinished` leaves that File Node Unparsed.
3. [x] **Field write** — The Parsed write is `GraphMutate.setParseState`. PersistState stays unchanged.

### Obligations

1. [x] **Workspace graph claim** — The Workspace graph claim says Parsed with `noServerFile` is not a state, and `ParseFinished` sets a File Node Parsed only when `noServerFile` is clear.
2. [x] **Parse and persist claim** — The Parse and persist claim states that same File Node rule.
3. [x] **Mailbox claim** — The Mailbox claim for Core loop states that same File Node rule.
4. [x] **ParseFinished test** — A test applies `ParseFinished` to a File Node with `noServerFile` clear and that File Node becomes Parsed. A test applies `ParseFinished` to a File Node with `noServerFile` set and that File Node stays Unparsed.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## Amendments

1. **None** — The implementation matches this ticket. No rule changed during the build.

## Time

- 2026-10-16 15m — `ParseFinished` leaves a `noServerFile` File Node Unparsed (from chat)

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement](plan/core-refinement/project.md)
- [Workspace graph](doc/current/graph.md)
- [Parse and persist](doc/current/parse-persist.md)
- [Mailbox](doc/current/mailbox.md)

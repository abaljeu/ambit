# 08 — Reject UX

**Type:** grilling
**Status:** done
Blocked by: None
Actual: 5m

## 1. Question

- [x] How does a person see a rejected non-FF push or a failed pull?

## 2. Answer

Locked 2026-09-26 (Alan, Github Sync room).

Conflict: error message naming at least one file path.

Other failures: matching short error message.

Basically reflect what git reports, condensed.

Same reject wording for Load and Save.

That wording covers network, credentials, a missing remote, and a non-fast-forward push. It does not mean pull is fast-forward.

Map gist: [[../map.md]] Decisions so far item 13.

## Comments

- 2026-09-26: Alan locked in chat. Status `done`. Condensed git report. Conflict names a path.
- 2026-10-14: Alan, Github Sync. The shared Load and Save wording is the reject text. Pull is not fast-forward.

## Time

- 2026-09-26 5m — recorded lock from chat

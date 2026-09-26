# 03 — When pull and push fire

**Type:** grilling
**Status:** done
Blocked by: 02
Actual: 5m

## 1. Question

- [x] When does the Server Actor pull, and when does it push, in round-trip v1?

The Destination locks pull and push on the Server Actor. It does not lock a person Command, a schedule, a post-Persist trigger, or a post-Download trigger.

Grill after [02 — Actor command surface](02-actor-command-surface.md) names the Commands. Do not implement.

## 2. Answer

Locked 2026-09-26 (Alan, chat).

**No automatic** pull or push in v1 (no schedule, post-Persist, or post-Download git).

Person Load/Save (and explicit git*/desk*) only.

When a remote exists, plain Load/Save prefer **git first** (git Load/Save).

All three Load forms **transfer files only** on this chart. WebDAV Upload/Download path **remains**.

Parse autonomy and Graph sync autonomy are **independent of this Project**. Do not adapt Load one way or the other for parse or graph. Out of scope / non-adaptation on [[../map.md]].

Map gist: [[../map.md]] Decisions so far item 9.

## Comments

- 2026-09-26: Alan locked in chat. Status `done`. Person-started only. Load forms transfer files only. Parse and Graph sync stay independent.

## Time

- 2026-09-26 5m — recorded lock from chat

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

WebDAV Upload/Download path **remains**.

**Amend (2026-09-26):** Independent / autonomous Parse is **not** in place. All three Load forms keep today’s Load → Parse pipeline (Parse and graph push as desk Load already does). “Do not adapt for parse autonomy” means do not redesign around a future autonomous Parse; that rearchitecture stays [[plan/parse-actor/project.md]]. Do not design git Load as file-transfer-only.

Map gist: [[../map.md]] Decisions so far item 9.

## Comments

- 2026-09-26: Alan locked in chat. Status `done`. Person-started only. WebDAV remains. git first when a remote exists.
- 2026-09-26: Amend — keep today’s Load → Parse coupling; do not redesign around a future autonomous Parse.

## Time

- 2026-09-26 5m — recorded lock from chat

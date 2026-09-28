# 03 — When pull and push fire

**Type:** grilling
**Status:** done
Blocked by: 02
Actual: 10m

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

**Amend (2026-09-26, superseded 2026-09-28):** Independent / autonomous Parse was not in place; 2026-09-26 kept today’s Load → Parse pipeline. **Current truth (2026-09-28):** after files land, Load marks Unparsed and pushes onto the one Parse actor ([17 — Git Load: Unparsed then Parse stack](17-post-pull-cascade-and-gate-handoff.md), [18 — One Parse actor stack](18-parse-actor-stack-and-file-lock-ownership.md)). Nobody starts an Actor after pull. Parse home stays [[plan/parse-actor/project.md]]. Do not design git Load as file-transfer-only.

Map gist: [[../map.md]] Decisions so far item 9.

## Comments

- 2026-09-26: Alan locked in chat. Status `done`. Person-started only. WebDAV remains. git first when a remote exists.
- 2026-09-26: Amend — keep today’s Load → Parse coupling; do not redesign around a future autonomous Parse.
- 2026-09-28: That amend is superseded. Current truth is Unparsed → push onto the one Parse actor ([17 — Git Load: Unparsed then Parse stack](17-post-pull-cascade-and-gate-handoff.md), [18 — One Parse actor stack](18-parse-actor-stack-and-file-lock-ownership.md)).

## Time

- 2026-09-26 5m — recorded lock from chat
- 2026-09-28 5m — annotated 2026-09-26 Load → Parse amend as superseded by Unparsed → push onto Parse (#151 / [17 — Git Load: Unparsed then Parse stack](17-post-pull-cascade-and-gate-handoff.md))

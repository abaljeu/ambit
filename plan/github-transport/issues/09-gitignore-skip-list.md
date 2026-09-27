# 09 — Skip list is .gitignore

**Type:** grilling
**Status:** done
Blocked by: None
Actual: 5m

## 1. Question

- [x] Where does the skip list for the GitHub remote live, and is there an Ambit-specific `.amb` config key?

## 2. Answer

Locked 2026-09-26 (Alan, Github Sync room).

No special Ambit config key.

Skip list is whatever `.gitignore` already says; the person edits that file.

When `.amb` (or any other path) is listed there, git skips it on the remote. That is not a hard Ambit default and not a separate class from other ignore rules. When those notes are excluded from the remote, offsite backup stays Ambit Server DataDir (WebDAV Upload/Download + Server git / daily save).

Map gist: [[../map.md]] Decisions so far item 14.

## Comments

- 2026-09-26: Alan locked in chat. Status `done`. Plain `.gitignore`. No Ambit skip key.

## Time

- 2026-09-26 5m — recorded lock from chat

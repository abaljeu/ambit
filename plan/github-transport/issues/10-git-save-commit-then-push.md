# 10 — git Save is commit then push

**Type:** grilling
**Status:** done
Blocked by: None
Actual: 5m

## 1. Question

- [x] How does git Save meet the current Server Persist / GitSave commit path?

## 2. Answer

Locked 2026-09-26 (Alan, Github Sync).

git Save is `git commit` of the work-tree edits, then push.

Graph→file Persist already happens independently. git Save does not own or replace that path.

Do not invent a merge of Persist into git Save. Commit (git) then push is the composition.

git Load commits its merge as part of the pull. That merge commit is not a new Save. Commit-before-pull already happens. This lock adds no commit step before the merge.

Map gist: [[../map.md]] Decisions so far item 15.

## Comments

- 2026-09-26: Alan locked in chat. Status `done`. Commit then push. Persist stays separate.
- 2026-10-14: Alan, Github Sync. git Save stays commit then push. The Load merge commit is part of the pull, not a second Save.

## Time

- 2026-09-26 5m — recorded lock from chat

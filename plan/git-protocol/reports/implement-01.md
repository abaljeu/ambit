# Implement 01 — Align merge-to-live with git-protocol

Issue 01 is **done**. [[plan/git-protocol/issues/01-align-merge-to-live-with-git-protocol.md]] has `Status: done` and all three checkboxes checked. The [[plan/git-protocol/project.md]] Stage stays `active`. No commit. Sibling issues were not closed.

## What changed

- [[.cursor/skills/update-matt-skills/scripts/merge-to-live.sh]] — workplace check is exact `dev`; other HEAD names exit 1. Header comments no longer name `w/*`.
- [[.cursor/skills/update-matt-skills/SKILL.md]] — removed the `w/*` / **diverging** exception. Merge preconditions and step 4 require a clean `dev` tip. Do-not now forbids merging vendor onto `dev`.
- [[plan/done/update-matt-skills/forks/implement/SKILL.md]] and [[plan/done/update-matt-skills/forks/request-refactor-plan/SKILL.md]] — history banner at the top. Snapshot body left in place.
- [[plan/done/update-matt-skills/forks/MANIFEST.md]] — the fork set is marked history, not live workplace procedure.
- [[plan/git-protocol/issues/01-align-merge-to-live-with-git-protocol.md]] — status and checkboxes.

## Acceptance criteria

1. The merge script accepts `dev` and refuses other workplaces. `bash -n` on the script passed. A dry check of the same predicate (`[ "$branch" != "dev" ]`) accepted `dev` and refused `ready`, `master`, `w/foo`, `selective-client-sync`, `update/mattpocock-skills`, `vendor/mattpocock-skills`, and `HEAD`. The live script was not run; it would `git checkout -B` and merge.
2. Skill preconditions and step 4 match that script: merge starts on clean `dev`, then the script creates `update/mattpocock-skills`. No `w/*` remains under [[.cursor/skills/update-matt-skills/]].
3. The two fork skills that still named `w/*` as the workplace are marked history. Git protocol already keeps `w/` names as history; the issue allowed update or a history mark. Snapshot wording was not rewritten to teach `dev`.

## Left unchanged

- Destination branch `update/mattpocock-skills` and vendor branch `vendor/mattpocock-skills` — the issue asked only that the workplace be `dev`.
- Other update-matt-skills scripts (`pull-skills-tree.sh`, `flatten-skills.sh`, `commit-vendor.sh`).
- Other fork skills under [[plan/done/update-matt-skills/forks/]] that do not teach `w/*` as the workplace.
- Live copies under [[.agents/skills/]].
- Sibling git-protocol issues 02–04.
- Project Stage (`active`).

## Not done in this pass

- No commit.
- No remotes or push.
- No issue 04 (named UX scripts) and no other git-protocol issues.
- No F# / Shared.Tests work.

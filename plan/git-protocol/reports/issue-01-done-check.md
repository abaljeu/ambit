# Issue 01 done check

**Verdict:** not done

This issue is [[plan/git-protocol/issues/01-align-merge-to-live-with-git-protocol.md]]. It is about the update-matt-skills merge-to-live path, not repo-root [[scripts/merge.sh]].

## Status metadata vs reality

The issue has `Status: ready-for-agent`. Sibling issues 02–04 use the same open triage role. All three checkboxes in 01 are unchecked. The comment on 2026-09-02 says it was filed unclaimed from WORK.md.

No report under [[plan/git-protocol/reports/]] claims 01 is done. [[plan/git-protocol/reports/instruction-pointers.md]] still lists this gap as pending: the merge script expects a `w/*` tip, and done forks still describe `w/*`.

The issue file's status matches reality. The work is open. It is not marked closed or done, and the work is not complete.

## Criterion checklist

| # | Criterion | Result |
| --- | --- | --- |
| 1 | [[.cursor/skills/update-matt-skills/scripts/merge-to-live.sh]] accepts `dev` (and refuses other workplaces) instead of requiring `w/*` | Fail |
| 2 | [[.cursor/skills/update-matt-skills/SKILL.md]] preconditions match that script (git-protocol: run from `dev`) | Fail |
| 3 | Fork files under [[plan/done/update-matt-skills/forks/]] that still teach `w/*` as the live workplace are updated or clearly marked as history | Fail |

## Evidence

### 1. merge-to-live.sh still requires `w/*`

The script header still says it starts from a live `w/*` tip and then creates `update/mattpocock-skills`. The branch check is still `w/*` only. Any other HEAD, including `dev`, prints `Checkout a live w/* branch first` and exits 1.

Git protocol ([[.cursor/skills/git-protocol/SKILL.md]]) names three places: `dev`, `ready`, `master`. It says do not add `w/` branches. An agent cannot run this update from `dev`.

The destination branch is still `update/mattpocock-skills`. That part of the old path is unchanged. The issue asked only that the workplace be `dev`, not that the update branch go away.

### 2. Skill preconditions still match the old `w/*` script

[[.cursor/skills/update-matt-skills/SKILL.md]] points at git-protocol, then immediately says it still uses `vendor/mattpocock-skills` and a merge script that expects a clean `w/*` tip — **diverging; needs human**.

Preconditions for merge: current branch is a clean live `w/*` (script then switches to `update/mattpocock-skills`). Step 4 still tells the operator to check out a clean live `w/*` tip first.

Those preconditions match the live script. They do not match git-protocol. Criterion 2 is not met until both accept `dev`.

### 3. Done forks still teach `w/*` as the workplace

[[plan/done/update-matt-skills/forks/MANIFEST.md]] is a snapshot list. It does not mark the fork skills as history.

Two fork skills still teach `w/*` as the live workplace:

- [[plan/done/update-matt-skills/forks/implement/SKILL.md]] — agent-done only on the current project branch (`w/*`); if not on `w/*`, offer to create `w/<slug>`.
- [[plan/done/update-matt-skills/forks/request-refactor-plan/SKILL.md]] — commits only on an unlocked project branch (`w/*`); later implementing on `w/*`.

Those files are not updated and not marked as history. Git protocol says `selective-client-sync` and the `w/` names remain as history and must not be resumed as the workplace.

## What would count as done

- The merge script accepts HEAD `dev` and refuses other workplaces.
- The skill preconditions and step 4 say the same.
- The fork files that teach `w/*` as the live workplace are corrected or marked history.

None of that is in the tree today.

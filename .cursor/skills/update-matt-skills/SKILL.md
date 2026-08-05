---
name: update-matt-skills
description: Update mattpocock/skills under .agents/skills/ and merge prior local forks into the new upstream files. User types /update-matt-skills.
disable-model-invocation: true
---

# Update Matt Skills

Refresh managed Matt skills, then **merge previous edits into the new files**. The lockfile is inventory only — it does not preserve forks.

Scratch workspace: `.scratch/matt-skills-merge/` (safe to delete after the run is accepted).

## Preconditions

Stop and ask if any fail:

- Current branch is `w/*` (see [[.cursor/rules/environment.mdc]]).
- Working tree is clean for `.agents/skills/` and `skills-lock.json` (commit or stash first).
- `skills-lock.json` exists at the repo root.

## Steps

### 1. Snapshot ours

Copy `.agents/skills/` → `.scratch/matt-skills-merge/ours/`.

Record `PRE=$(git rev-parse HEAD)`.

**Done when:** scratch `ours/` exists and matches the current skill tree.

### 2. Recover base (previous upstream)

Run `npx skills experimental_install -y` so on-disk skills match the **current lock** (last installed upstream).

Copy `.agents/skills/` → `.scratch/matt-skills-merge/base/`.

If `experimental_install` fails, stop and ask — do not update without a base (2-way guesswork).

**Done when:** scratch `base/` exists and differs from `ours/` only where we had local forks.

### 3. Pull new upstream

Run `npx skills update -p -y`.

On-disk `.agents/skills/` and `skills-lock.json` are now **theirs** (new upstream). Keep this lock.

Copy `.agents/skills/` → `.scratch/matt-skills-merge/theirs/` for reference.

**Done when:** lock hashes changed where upstream moved, and `theirs/` is snapshotted.

### 4. Merge forks into new files

For each skill directory name under the union of `ours/`, `base/`, and `theirs/`:

| Situation | Action |
| --- | --- |
| In `ours` and `base`, trees equal | Keep **theirs** (already on disk). No local fork. |
| In `ours` and `base`, trees differ | **Fork** — merge into `.agents/skills/<name>/` (below). |
| Only in `theirs` | New upstream skill — keep. |
| Only in `ours` (gone from lock/theirs) | Ask before deleting; default leave a note, do not silently delete. |

For each **forked** skill, for each file path in the union of ours/base/theirs for that skill:

- Missing in ours → keep theirs (or add new upstream file).
- Missing in theirs → ask (upstream removed the file).
- Present in all three → run a 3-way merge into the on-disk path:

```bash
git merge-file -p \
  .scratch/matt-skills-merge/ours/<skill>/<file> \
  .scratch/matt-skills-merge/base/<skill>/<file> \
  .scratch/matt-skills-merge/theirs/<skill>/<file> \
  > .agents/skills/<skill>/<file>
```

If `git merge-file` exits non-zero, conflict markers are in the file — resolve like [[.agents/skills/resolving-merge-conflicts/SKILL.md]]: keep upstream structure, re-apply local intent, do not invent behaviour.

**Done when:** every forked skill is merged or has an explicit unresolved conflict list for the user; pristine skills remain pure theirs.

### 5. Hand back

Show a short report:

- Skills updated with no local fork
- Skills merged (names)
- Conflicts still open (paths)
- Removals / new skills needing a decision

Do **not** commit unless the user asks. Leave `.scratch/matt-skills-merge/` until they confirm; then delete it.

**Done when:** user has the report and a usable tree (or a clear conflict list).

## Do not

- Edit Matt skills “in place” as the way to customize long-term without expecting this merge on the next update.
- Treat `skills-lock.json` as something to merge hunk-by-hunk — accept the post-update lock.
- Run remotes/`gh` beyond what `npx skills` needs for this flow.
- Touch `.cursor/skills/` (Gambol-local); this flow only manages `.agents/skills/` entries from the lock.

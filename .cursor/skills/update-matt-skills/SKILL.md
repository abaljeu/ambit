---
name: update-matt-skills
description: Pull mattpocock skills tree onto vendor, flatten to .agents/skills, commit, merge to live w/*. User types /update-matt-skills.
disable-model-invocation: true
---

# Update Matt Skills

Same-branch model: operator skill lives here under `.cursor/skills`. Branch `vendor/mattpocock-skills` holds:

| Path | Role |
| --- | --- |
| `skills/` | Bucketed tree from `skills-source` |
| `.agents/skills/` | Flat install agents use |
| `.cursor/skills/update-matt-skills/` | This skill + scripts |

Run scripts **in place** from the repo root (no temp copy, no worktrees).

Flat target: `.agents/skills`. No `skills-lock.json`, no `npx skills`. No SHA bookkeeping in commit messages or reports. No push of vendor / no origin remotes unless the user asks.

## Preconditions

Stop and ask if any fail:

- Working tree is clean.
- Remote `skills-source` exists (local path to upstream clone).
- For pull / flatten / vendor commit: current branch is `vendor/mattpocock-skills`.
- For merge: current branch is live `w/*` and is clean.

## Ordinary update

### 1. Pull tree (on vendor)

```bash
bash .cursor/skills/update-matt-skills/scripts/pull-skills-tree.sh
```

Fetches `skills-source` and replaces `skills/` from `skills-source/main`.

**Done when:** `skills/` matches source main.

### 2. Flatten (on vendor)

```bash
bash .cursor/skills/update-matt-skills/scripts/flatten-skills.sh
```

Deletes `.agents/skills` on vendor only, then copies each non-deprecated `skills/**/SKILL.md` parent dir to `.agents/skills/<name>/`. Rejects duplicate basenames. Does not touch `skills/` or this cursor skill.

**Done when:** flat tree exists under `.agents/skills` with no duplicates and no deprecated entries.

### 3. Commit on vendor

```bash
bash .cursor/skills/update-matt-skills/scripts/commit-vendor.sh
```

Stages `skills/` and `.agents/skills/`, commits with message `Update projected skills` when there are changes.

**Done when:** vendor HEAD has the new tree + flat, or the script reports nothing to commit.

### 4. Merge to live

On a clean live `w/*` branch:

```bash
bash .cursor/skills/update-matt-skills/scripts/merge-to-live.sh
```

First-time bootstrap only:

```bash
bash .cursor/skills/update-matt-skills/scripts/merge-to-live.sh --bootstrap
```

(`--bootstrap` adds `--allow-unrelated-histories`.)

**Done when:** merge commit exists on live (resolve conflicts if any).

### 5. Hand back

Short report: skill counts under `skills/` and `.agents/skills/`, whether anything needed conflict resolution. No SHAs. Do **not** commit further unless the user asks. Forks re-apply from `.scratch/update-matt-skills/forks/` is a separate follow-up when needed.

## Do not

- Use `mktemp` worktrees or copy this skill elsewhere to run it.
- Push `vendor/mattpocock-skills` or touch origin remotes unless asked.
- Put SHAs in commit messages or scratch notes.
- Delete `skills/` or `.cursor/skills/update-matt-skills/` during flatten.
- Run `npx skills` or maintain `skills-lock.json`.

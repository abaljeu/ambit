# Resolve merge conflict

**Date:** 2026-08-30  
**Operation:** Merge `origin/master` into `dev` (completed)

## State at start

- Branch: `dev`
- In-progress merge: `Merge remote-tracking branch 'origin/master' into dev`
- MERGE_HEAD: `27ecd8f` — "Update README with project status information" (tip of `origin/master` history for this merge)
- Conflicted files: `README.md` (both modified)

## Resolution

### README.md

**Intent traced:**

| Side | Primary source | Intent |
|------|----------------|--------|
| HEAD (`dev`) | `5db017c` — "Reconcile public README and doc with cPanel proxy; drop legacy deploy files." | Single landing + developer README: Status first, full architecture (Desktop, Npgsql), `/ambit` on port 5215, npm/bundle, Persistence, Desktop, cPanel proxy links. Documented in [[plan/git-protocol/reports/public-face-merge.md]]. |
| `origin/master` | `27ecd8f` — status blurb + simpler README | Public-facing status text and older run instructions (port 5115, no `/ambit`, thinner architecture table). |

**Choice:** Keep HEAD (`dev`) in full. The merge goal is to bring `origin/master` into the work line; README content on `dev` already incorporated the public Status section and corrected stale facts from master. Master's side added no unique facts worth keeping; retaining it would regress ports, paths, and sections.

## Completion

- `git add README.md`
- Merge commit: `0c8d846` — "Merge remote-tracking branch 'origin/master' into dev"
- Verified: `git status` — clean working tree on `dev`

## User action

None required. Merge is complete.

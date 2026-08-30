# dev commits (git-protocol split)

Created on `dev` with a clean working tree after both commits.

## Commit 1 — git protocol

| Field | Value |
|-------|-------|
| SHA | `85b8d40` |
| Subject | Document dev/ready/master flow, merge.sh, and git-master/git-share skills. |

**Included:** `.cursor/rules/environment.mdc`, `.cursor/rules/gambol.mdc`, `.cursor/skills/git-protocol/SKILL.md`, `.cursor/skills/git-master/`, `.cursor/skills/git-share/`, `scripts/merge.sh`, `CONTEXT.md`, `WORK.md`.

## Commit 2 — public face reconciliation

| Field | Value |
|-------|-------|
| SHA | `5db017c` |
| Subject | Reconcile public README and doc with cPanel proxy; drop legacy deploy files. |

**Included:** `README.md`, `doc/README.md`, `doc/reference/cpanel-transparent-proxy.md`, deletion of `dockerfile` and `.htaccess`, `.scratch/git-protocol/reports/public-face-merge.md`, `.scratch/git-protocol/reports/public-face-reconciliation.md`.

`CONTEXT.md` and `WORK.md` were entirely git-protocol-related; both went in commit 1.

## Remaining for the human

1. **Fetch** `origin` (remote; manual approval).
2. **Merge** `origin/master` into `dev` with `--no-ff`. Only [[README.md]] should conflict; keep the reconciled work-line version. `dockerfile` and `.htaccess` resolve clean (deleted on both sides).
3. Run **`bash ./scripts/merge.sh ready`** to bring `dev` into `ready` (`--no-ff`).
4. **One-time stage-setting merge** onto `master` (not a squash):

```bash
git switch master
git merge --no-ff ready -m "<protocol begins>"
git switch dev
```

5. **Later** squashes onto `master` go through **`bash ./scripts/merge.sh master -m "<message>"`** per [[.cursor/skills/git-master/SKILL.md]]. Publishing `master` fast-forwards to `origin` once step 2 is in the ancestry.

No push was performed by the agent.

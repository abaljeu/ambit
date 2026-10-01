# Code review: git skill cluster (clean tip / HEAD contents)

Range: `.agents/skills/{git-protocol,git-share,git-master,cloud-agent-git,resolving-merge-conflicts,git-guardrails-claude-code}` — uncommitted diff empty; tip under review is current file contents. Report path `plan/skills-review/reports/code-review-git-skills.md` under Project [[plan/skills-review/]].

## Standards

### Documented-standard violations

**writing-for-agents/SKILL.md Recipe §3** — each step needs a checkable Done. No `Done when` / `Done:` on steps in:
- resolving-merge-conflicts/SKILL.md (1–5)
- git-guardrails-claude-code/SKILL.md (1–5)
- git-share/SKILL.md (Before editing / Publish / Agent workplaces)
- git-master/SKILL.md (Squash / Tag / Publish)
- cloud-agent-git/LAND.md §3 Review

**writing-for-agents/SKILL.md Gate One normative home** — place/push/human-only rules restated outside git-protocol:
- git-share lead + Still human-only / gated (dev local, master human-only, push gates)
- git-protocol Sharing (repeats Places pointers)
- git-guardrails What Gets Blocked (restates block-dangerous-git.sh patterns)

**markdown-writing.md** — no consecutive blank lines: git-protocol/SKILL.md after Merges (blank lines 39–40).

### Smells (judgement)

**Overcomplication** — git-protocol Workplace mostly restates Places/Merges/Sharing; drop it and keep only the `selective-client-sync` / `w/` caveat (or move that under Places).

**Overcomplication** — git-share Still human-only / gated repeats the lead + git-protocol; replace with pointers.

**Overcomplication** — cloud-agent-git Work one-liner vs steps 1–3:
> `You are free to work outside of github on your own branch. Pull on ready. Squash onto staging then push.`
Delete the one-liner; the numbered Work path is enough.

**Duplicated Code** — guardrails blocked list in SKILL.md and the shell `DANGEROUS_PATTERNS` array; keep the script as home, point from the skill.

## Spec

no spec available

Standards: 7 findings (worst: missing checkable Done across multiple skills). Spec: 0 (no spec available).

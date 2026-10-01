# Code review — Session handoff

Range: `.agents/skills/handoff`, `.agents/skills/claude-handoff`. Working tree clean for these paths (`git diff HEAD --` empty); tip is current file contents. scan: none.

## Standards

**Missing ordered steps / checkable Done** — writing-for-agents/SKILL.md Recipe §3 (and §4 Done). Both [handoff/SKILL.md](.agents/skills/handoff/SKILL.md) and [claude-handoff/SKILL.md](.agents/skills/claude-handoff/SKILL.md) are unordered paragraphs with no Done. They are compose-then-deliver procedures, not all-reference reviews. Opening paragraphs fuse compose and deliver.

**One normative home / shared composition** — SKILL.md Gate **One normative home**; SKILL-MECHANICS **External Reference**; GLOSSARY **Duplication**. Both user-invoked skills restate the same composition rules (suggested skills; cite artifacts; redact; tailor from arguments) instead of one External Reference. Delivery (temp file vs `claude --bg`) is out of this finding.

**One normative home (`--name`)** — SKILL.md Gate. claude-handoff puts `--name` in the command and again in “Always pass `-n`/`--name`…”.

**Why is never a step** — SKILL.md Recipe §3. claude-handoff: “— it sets the display name shown in the job list, session picker, and terminal title.” Redact add-on “— the summary becomes the agent's prompt” already said in paragraph 1.

**Prompt the positive** — SKILL.md Gate. “Instead of saving it, launch…” names the sibling delivery. The launch command is the positive.

**Cache / Overcomplication (judgement)** — GLOSSARY **Cache**; SMELLS **Overcomplication**. “the user manages it with `claude agents`” restates CLI help; after the command, claude-handoff repeats the flag, explains UI chrome, and dumps cwd/return/`claude agents`.

Not standing behind **Duplicated Code** for the two skills as wholes: delivery is a real difference.

No openai.yaml findings. No scan-limit findings.

## Spec

no spec available

Standards: 6 findings (worst: missing checkable Done / unordered steps on both skills). Spec: 0 (no spec available).

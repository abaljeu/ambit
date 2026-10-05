---
name: pr
description: "PR body after implement and code-review, before a human lands the change. Use when writing or updating a pull request body, or when opening a pull request."
---

Write the pull request body. Run this after [[.agents/skills/implement/SKILL.md]] and [[.agents/skills/code-review/SKILL.md]], and before a human lands the change. Neighbours: [[.agents/skills/implement/SKILL.md]], [[.agents/skills/code-review/SKILL.md]], [[.agents/skills/git-protocol/SKILL.md]], [[.agents/skills/project-work/SKILL.md]]. The later human invoke is [[.agents/skills/retro/SKILL.md]]. Clash notes: [[ADAPTATIONS.md]].

## Process

### 1. Confirm the place

Read the diff. Read the local spec or ticket when one exists ([[.agents/skills/project-work/SKILL.md]]). Git steps stay in [[.agents/skills/git-protocol/SKILL.md]]. A cloud agent also follows [[.agents/skills/cloud-agent-git/SKILL.md]]. This skill writes the body. It does not merge. On GitHub `abaljeu/ambit`, the day-to-day land is a squash onto `staging` after Alan approves that land. Read domain words from [[GLOSSARY.md]]. Do not rename [[GLOSSARY.md]] or `CONTEXT.md`. Done: you have the diff, the original request, and the skills this run used, and you have not merged.

### 2. Draft the three sections

Start at Summary. Use this template.

```markdown
## Summary
<smallest visual>
## Evidence
- **Before:** <failing test, command output, or other hard artifact>
- **After:** <the same check, now passing>
## Merge Danger
**Door:** <one-way or two-way>
**Blast radius:** <short scope>
**Review time:** <skim or slow read, and where>
```

Summary is a picture. Pick the smallest visual that makes the change clear. Use a second visual only when the first hides the point. The menu is pseudocode, a call tree, a file tree, Mermaid, or a diff sketch. Mermaid sets the font with `%%{init: {'themeVariables': {'fontSize': '20px'}}}%%`. A paragraph is not the Summary. Put a short line beside the visual only when the visual needs a name. The menu is credited in [[ADAPTATIONS.md]].

```text
pseudocode:
  on save
    if unchanged
      return cache
call tree:
  submit
    persist
    launch
file tree:
  src/
  ├── parse/
  └── write/
diff sketch:
  submit
+   expand
    launch
```

Evidence shows a before and an after. The before is a test, command, or screenshot that failed. The after is that same check passing. Quote the artifact. "I read the code" is not Evidence. When the artifact is missing, say it is missing. Do not claim the change is shown.

Merge Danger states the door. A two-way door returns to the start when the commit is reverted. A one-way door changes the world outside the repo. A dropped column is a one-way door. A sent message is a one-way door. The blast radius is the scope in a few words. One button is a small radius. Every caller of a shared function is a wide radius. Tell Alan where to spend review time. A two-way door with a small radius is a skim. A one-way door is a slow read.

Done: Summary is one visual with at most a few short lines beside it. Evidence quotes a before artifact and an after artifact, or it names the missing artifact. Merge Danger states the door, the blast radius, and where Alan spends review time.

### 3. Put the body on the pull request

Open or update the pull request with that body. A cloud agent opens a draft toward `staging` ([[.agents/skills/cloud-agent-git/SKILL.md]]). Desktop does not push unless Alan asked ([[.agents/skills/git-protocol/SKILL.md]], [[.agents/skills/git-share/SKILL.md]]). Done: the open pull request carries the three sections, or the body is ready and the pull request is the next action in this same turn.

### 4. Run the Jev match-check

Jev is the MCP connector. Jev is not a teammate agent. The connector Alan names is `user-jev`. The tool is `jev_classify`. The live namespace may appear as `jev`. Read the tool schema, then call it.

Send the original request, what the agent changed, and which skills applied. Ask whether the work matches the complete request. The label is `yes`, `no`, or `unclear`.

Report the label and the percentiles to Alan. Percentiles are the per-option probabilities, the confidence, or both, when the tool returns them. The label alone is not the report. When the tool returns no percentiles, report that absence. Do not invent numbers.

A `no` result is a gap. An `unclear` result is a gap. A `yes` that is not strictly the highest probability is a gap. Missing percentiles are a gap. Surface the gap. Do not squash-land until Alan accepts the gap or the doc or code gap is fixed.

When the connector fails, report the failure to Alan. Do not record that failure as `yes`. Done: Alan has the label and the percentiles, or Alan has the connector failure and the missing percentiles. A gap blocks squash-land.

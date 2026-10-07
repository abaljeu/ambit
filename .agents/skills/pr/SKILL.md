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

Jev is the MCP connector. Jev is not a teammate agent. The connector Alan names is `user-jev`. The live namespace may appear as `jev`. The tool is `jev_decide`. Read the tool schema, then call it.

Build one `jev_decide` call from the live Spec and the live Standards for this diff. Every question is a choice. Name each facet with words. Give each criteria key a one-line description. Spec keys are `met` (the section holds), `partial` (some of it holds), and `missed` (it does not hold). Standards keys are `clean` (the cluster holds), `nit` (a small miss), and `blocker` (the cluster fails). The land check is this facet set. A single `jev_classify` yes/no/unclear call is not the land gate.

**Spec facets.** Read the ticket, arch, or spec for this pull request. Add one choice question for each real section that applies to the diff. Include each ticket checklist subsection that applies. Add a Spec bucket from [[.agents/skills/code-review/SKILL.md]] only when that bucket fires. One bucket is missing or partial requirements (a). One bucket is scope creep (b). One bucket is wrong implementation (c). One bucket is a global type or interface the diff adds or changes that the spec did not name (d).

**Standards facets.** Use the mechanical standards scan from [[.agents/skills/code-review/SKILL.md]]. Add one choice question for each rule or smell cluster that had a chance to apply. Take clusters from that scan. Take clusters from each file under [[.agents/rules/]] the diff can hit. Take clusters from [[.agents/skills/code-review/SMELLS.md]] when a smell cluster had a chance to apply. Files that often apply are [[.agents/rules/fsharp-source.md]], [[.agents/rules/refer-by-name.md]], [[.agents/rules/core-api.md]], [[.agents/rules/core-agent-behavior.md]], and [[.agents/rules/markdown-writing.md]]. Skip a rule that had no chance to apply.

Put the original request, the diff, and the skills this run used in the call state. Put one question per facet in the call. The connector may reject the call because the question map is too large. Send further `jev_decide` calls with the same state until every facet has a row. Keep every facet. Do not merge facets into one yes/no question.

**Facet table.** Put a facet table in the pull request body after Merge Danger. This table replaces a single yes/no/unclear block. One row per facet. Each row shows the facet name, the axis, the winning label, and the percentiles. Percentiles are the per-option probabilities, the confidence, or both, when the tool returns them. The label alone is not the row. When a facet has no percentiles, write that absence in the row. Do not invent numbers. Do not add an overall yes/no line.

<!-- Trial rule from Alan 2026-10-07. -->

**Land bar.** After the facet table, score each Spec facet. Score each Standards facet that applies. For a Spec facet, multiply confidence by the probability of `met`. For a Standards facet, multiply confidence by the probability of `clean`. When any product is below 0.5, run [[.agents/skills/code-review/SKILL.md]] in full before you recommend land. The minimum run is the Spec axis of that skill. Do not squash-land while any product is below 0.5, unless Alan has accepted that review.

A `missed` facet is a gap. A `blocker` facet is a gap. A `partial` facet or a `nit` facet that Alan should see is a gap. Surface each gap in the table. Do not squash-land until Alan accepts the gap or the gap is fixed.

A connector failure is not a pass. Report the failure to Alan and leave the labels unset. Done: the pull request body has one row per facet with the winning label and the percentiles, or Alan has the connector failure. A gap blocks squash-land. You have applied the land bar in this step.

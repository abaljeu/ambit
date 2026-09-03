# Summary goal rewrite

## New Summary

Summary: When one Node has a large Children list in the SiteMap, make Selection, Focus, and delete among the Children stay fast in the Browser.

## Why it matches wait-what and CONTEXT

[[.agents/skills/wait-what/SKILL.md]] asks for a re-pitch: a little context, ASD-STE100, and ubiquitous language from [[CONTEXT.md]]. The old Summary listed work already done (`planPatchDOM`, `childIndex`, a cost note). A reader who missed the destination would not know the goal.

The new line leads with context (one Node, a large Children list in the SiteMap), then states the goal (Selection, Focus, and delete among the Children stay fast in the Browser). It uses Node, Children, Selection, Focus, SiteMap, and Browser. It does not say tree for the Graph. It does not use visible as the name for Included context; SiteMap under Zoom and Fold is the index of what the user sees.

STE100: one sentence, simple verbs (`has`, `make`, `stay`), no implementation inventory.

The destination in [[investigation.md]] and [[delete-children-cost.md]] is the same: cursor movement among hundreds of sibling Children is too slow, and delete among a large sibling set is also costly. The goal is usable interaction, not a list of patches already shipped.

## What changed

In [[project.md]]: replaced the implementation-inventory Summary with the goal line above; set `Updated: 2026-09-02`; left `Stage: active` and the `Artifacts:` line unchanged. No other project files. Did not regenerate [[plan/index.md]].

---
name: handoff
description: Compact the current conversation into a handoff document for another agent to pick up.
argument-hint: "What will the next session be used for?"
disable-model-invocation: true
---

1. Compose the handoff summary per [[SUMMARY.md]].
   Done: the summary includes every required part of SUMMARY.md and matches any user-passed focus.
2. Save the summary to the temporary directory of the user's OS (outside the workspace).
   Done: the file exists on that path and is not under the workspace.

---
name: claude-handoff
description: Hand the current conversation off to a fresh background agent that picks up the work immediately.
argument-hint: "What will the next session be used for?"
disable-model-invocation: true
---

1. Compose the handoff summary per [[.agents/skills/handoff/SUMMARY.md]].
   Done: the summary includes every required part of that file and matches any user-passed focus.
2. Launch `claude --bg --name "<descriptive name>" "<handoff summary>"` from the current working directory.
   Done: the command completed with that `--name` and the summary as the prompt.

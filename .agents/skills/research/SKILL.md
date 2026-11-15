---
name: research
description: Investigate a question against high-trust primary sources and capture the findings as a Markdown file in the repo. Use when the user wants a topic researched, docs or API facts gathered, or reading legwork delegated to a background agent.
---

Spin up a **background agent** to do the research, so you keep working while it reads.

Its job:

1. Investigate the question against **primary sources** — official docs, source code, specs, first-party APIs — not a secondary write-up of them. Follow every claim back to the source that owns it.
2. Write the findings to a single Markdown file, citing each claim's source.
3. Save it where the repo already keeps such notes; match the existing convention, and if there is none, put it somewhere sensible and say where.

## SVN diff capture

When capturing SVN diffs for human review (e.g. auth-login / `OD_2026` history):

- Run from repo root; do **not** `cd` into directories — use paths relative to cwd or absolute from repo root.
- Do **not** pipe through `head`, `tail`, or other filters.
- Write the full diff to a file: `.agents/scripts/svndiff.sh -r REV1:REV2 path/to/file > output-file` (see [[.agents/scripts/explore.md]]).
- Place artifacts under `plan/<effort>/research/diffs/` with descriptive names (e.g. `session.cpp-r650-r680.diff`).
- Link diff file paths from the research report; do not paste diff bodies inline.

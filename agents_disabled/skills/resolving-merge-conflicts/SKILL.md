---
name: resolving-merge-conflicts
description: "Use when you need to resolve an in-progress git merge/rebase conflict."
---

Git: follow [[.agents/skills/git-protocol/SKILL.md]]. Exception: while a merge or rebase is in progress, the human owns start, continue, abort, and checkout; this skill applies file resolutions only.

1. **See the current state** of the merge/rebase. Check git history (read-only), and the conflicting files. Done when you know the merge/rebase state and which files conflict.

2. **Find the primary sources** for each conflict. Understand deeply why each change was made, and what the original intent was. Read the commit messages and local specs/issues under `plan/` per [[doc/agents/issue-tracker.md]]. Done when you can name the intent of each side for every conflict.

3. **Resolve each hunk in the working tree.** Preserve both intents where possible. Where incompatible, pick the one matching the merge's stated goal and note the trade-off. Do **not** invent new behaviour. Edit conflicted files only; leave `git add` / continue / abort to the human unless they give **manual approval**. Done when every conflicted file is resolved in the working tree and no new behaviour was invented.

4. Discover the project's **automated checks** and run them — typically typecheck, then tests. Fix anything the merge broke in source files. Done when those checks pass, or source fixes for merge breakage are in the tree.

5. **Hand back to the human** to stage, continue, or finish the merge/rebase. Summarize what you resolved and any remaining trade-offs. Done when the human has that summary and owns stage/continue/abort.

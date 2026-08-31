---
name: resolving-merge-conflicts
description: "Use when you need to resolve an in-progress git merge/rebase conflict."
---

Human owns the merge/rebase state (start, continue, abort, checkout). Agent proposes and applies **file** resolutions only. Do not run merge/rebase/commit git mutations — see [[.cursor/rules/environment.mdc]].

1. **See the current state** of the merge/rebase. Check git history (read-only), and the conflicting files.

2. **Find the primary sources** for each conflict. Understand deeply why each change was made, and what the original intent was. Read the commit messages and local specs/issues under `plan/` per [[doc/agents/issue-tracker.md]].

3. **Resolve each hunk in the working tree.** Preserve both intents where possible. Where incompatible, pick the one matching the merge's stated goal and note the trade-off. Do **not** invent new behaviour. Edit conflicted files only; leave `git add` / continue / abort to the human unless they give **manual approval**.

4. Discover the project's **automated checks** and run them — typically typecheck, then tests. Fix anything the merge broke in source files.

5. **Hand back to the human** to stage, continue, or finish the merge/rebase. Summarize what you resolved and any remaining trade-offs.

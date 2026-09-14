# Status

This file is the single source of truth for the **Status** list on every ticket. Glossary names: [[CONTEXT.md]]. **Stage** is [[project-status.md]]. Tracker operations: [[issue-tracker.md]].

`**Status:**` is a ticket field only. Projects, Epics, Chapters, and the Roadmap do not carry Status.

| Status | Meaning |
| --- | --- |
| `needs-info` | Get more information |
| `ready-to-implement` | Implement this unblocked, fully specified ticket |
| `blocked` | Wait on a named dependency |
| `coded` | Review the implementation |
| `done` | Review approved this ticket |
| `cancelled` | Reject or abandon this ticket |

Each value names the next action, or a closed end. Use `ready-to-implement` when a ticket has no blockers and needs no more information. Closed is `done` only. Implement writes `coded`. Review approval writes `done`. Git **agent-done** is not `coded` and not `done`. Do not write `ready-for-agent` or `ready-for-human` on new tickets. Existing tickets may still carry those values; do not rewrite them. `cancelled` is reject or abandon of a ticket, not Stage `dead`. Keep `blocked` even when no live ticket uses it. Do not use `needs-triage`, `wontfix`, `open`, `resolved`, `claimed`, `closed`, `agent-done`, or `in-progress`. Do not use `dead` on a ticket; `dead` is a Stage.

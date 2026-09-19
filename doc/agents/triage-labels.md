# Status

This file is the single source of truth for the **Status** list on every ticket. Glossary names: [[CONTEXT.md]]. **Stage** is [[project-status.md]]. Tracker operations: [[issue-tracker.md]].

`**Status:**` is a ticket field only. Projects, Epics, Chapters, and the Roadmap do not carry Status.

| Status | Meaning |
| --- | --- |
| `needs-info` | Get more information |
| `defined` | Spec complete. Implement when every Blocked-by ticket is `done`; otherwise hold for those deps without Status `blocked` |
| `blocked` | Wait on an external or non-ticket dependency |
| `coded` | Review the implementation |
| `done` | Review approved this ticket |
| `cancelled` | Reject or abandon this ticket |

Each value names the next action, or a closed end. Use `defined` when the spec is complete. Do not use `blocked` when the only delay is linked `Blocked by:` tickets; hold for those deps with Status `defined`. `Blocked by:` is dependency order; Status is spec readiness. An unblocked `defined` ticket is the implement frontier. Closed is `done` only. Implement writes `coded`. Review approval writes `done`. Git **agent-done** is not `coded` and not `done`. Do not write `ready-for-agent`, `ready-for-human`, or `ready-to-implement` on new tickets. Existing tickets may still carry those values; do not rewrite them. `cancelled` is reject or abandon of a ticket, not Stage `dead`. Keep `blocked` even when no live ticket uses it. Do not use `needs-triage`, `wontfix`, `open`, `resolved`, `claimed`, `closed`, `agent-done`, or `in-progress`. Do not use `dead` on a ticket; `dead` is a Stage.

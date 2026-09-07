# Status

This file is the single source of truth for the **Status** list on every ticket. Glossary names: [[CONTEXT.md]]. **Stage** is [[project-status.md]]. Tracker operations: [[issue-tracker.md]].

`**Status:**` is a ticket field only. Projects, Epics, Chapters, and the Roadmap do not carry Status.

| Status | Meaning |
| --- | --- |
| `ready-for-agent` | Fully specified and ready for an AFK agent |
| `ready-for-human` | Human implementation or judgment is required |
| `needs-info` | Waiting for more information |
| `blocked` | Waiting on a named dependency |
| `done` | Work on this ticket is delivered |
| `cancelled` | Reject or abandon this ticket |

Takeable tickets are `ready-for-agent` or `ready-for-human`. Closed is `done` only. In-flight work keeps a takeable value until `done`. `cancelled` is reject or abandon of a ticket, not Stage `dead`. Keep `blocked` even when no live ticket uses it. Do not use `needs-triage`, `wontfix`, `open`, `resolved`, `claimed`, `closed`, `agent-done`, or `in-progress`. Do not use `dead` on a ticket; `dead` is a Stage.

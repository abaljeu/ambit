# Status

This file is the single source of truth for the **Status** list on every ticket. Glossary names: [[CONTEXT.md]]. **Stage** is [[project-status.md]]. Tracker operations: [[issue-tracker.md]].

`Status:` is a ticket field only. Projects, Epics, Chapters, and the Roadmap do not carry Status.

| Status | Meaning |
| --- | --- |
| `needs-triage` | Maintainer evaluation is needed |
| `needs-info` | Waiting for more information |
| `ready-for-agent` | Fully specified and ready for an AFK agent |
| `ready-for-human` | Human implementation or judgment is required |
| `blocked` | Waiting on a named dependency |
| `done` | Work on this ticket is delivered |
| `wontfix` | Will not be actioned |

Takeable tickets are `ready-for-agent` or `ready-for-human`. In-flight work keeps that value until `done`. Do not use `open`, `claimed`, or `resolved`. Do not use `dead` on a ticket; `dead` is a Stage. `wontfix` is the ticket refuse.

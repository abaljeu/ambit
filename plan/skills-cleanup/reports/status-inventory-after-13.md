# Status inventory after issue 13

Re-run of the search class in [[plan/skills-cleanup/reports/status-reconsider-inventory.md]]. Live tickets = `plan/**/issues/*.md` (not `plan/done/`, not `plan/oldplans/`). Historical counts in that file stay.

Search: `^\*\*Status:\*\*` (first token after the label) and unbolded `^Status:`.

## Deduped first token (live tickets)

199 issue files have a Status field. Form: **199** `**Status:**`, **0** `Status:`.

| Value | Count | Form |
| --- | --- | --- |
| `done` | 119 | `**Status:**` |
| `ready-for-agent` | 61 | `**Status:**` |
| `ready-for-human` | 17 | `**Status:**` |
| `needs-info` | 2 | `**Status:**` |
| `blocked` | 0 | — |
| `cancelled` | 0 | — |

Forbidden first tokens on live tickets (`open`, `resolved`, `claimed`, `closed`, `agent-done`, `in-progress`, `wontfix`, `needs-triage`): **none**. Unbolded `^Type:` on live tickets: **none**.

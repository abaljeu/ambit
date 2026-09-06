# Lock: ticket Status set v2

Earlier locks stay. Stage stays `chart`. This replaces the reconsidered Status list. Live ticket lines and canonical docs are not edited in this pass.

**Status (tickets only):** `ready-for-agent` | `ready-for-human` | `needs-info` | `blocked` | `done` | `cancelled`.

Takeable = `ready-for-agent` or `ready-for-human`. Closed = `done` only. `cancelled` is reject/abandon (not Stage `dead`). `blocked` stays (zero live tickets is fine).

**Drop:** `needs-triage` (triage deleted), `wontfix` (replaced by `cancelled`), `open`, `resolved`, `claimed`, `closed`, `agent-done`, `in-progress`.

Later obedience (do not do in this pass):

- Write this set into [[doc/agents/triage-labels.md]] and [[doc/agents/issue-tracker.md]].
- Migrate live tickets: `open` → takeable (`ready-for-agent` unless human); `resolved` / `closed` / `agent-done` → `done`; any `wontfix` → `cancelled`.
- Bold field labels are locked in [[plan/skills-cleanup/reports/lock-bold-properties-spec-md.md]] (`**Status:**`, not unbolded `Status:`).

Inventory counts stay in [[plan/skills-cleanup/reports/status-reconsider-inventory.md]].

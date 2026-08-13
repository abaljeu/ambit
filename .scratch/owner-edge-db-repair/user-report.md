# Are startup ownership corrections user-visible?

**User-visible? No** — successful corrections are only logged on the server console via `eprintfn`. The browser operator does not see counts, Node ids, or a repair summary.

## Spec ask

[[.scratch/owner-edge-db-repair/spec.md]] requires: “Log correction counts and affected Node ids.” That is satisfied by server logging, not by a client toast/banner.

## What the server logs (success)

In [[src/Server/DbAgent.fs]] after `executeMaintenance` succeeds:

```text
DbAgent: projection repair deleted={n} ownershipUpdates={n} insertNodes={n} insertChildren={n} ordinalShifts={n} affected=[{id}, {id}, ...]
```

Counts and `affectedNodeIds` come from `ProjectionOwnershipRepair.LogFacts` (returned on `ProjectionMaintenanceResult.logFacts`). Format includes all five counters plus bracketed Guid list.

`DbAgentStartup.fs` only schedules sweep → apply → `setReady`; it does not surface facts to the API.

## What the client shows

- [[src/Client/StatusView.fs]]: while `not model.syncInfo.isServerReady` → `"Starting up…"`. After ready → normal sync labels (`synced` / `idle` / Saving… etc.). No repair copy.
- State / poll JSON: `ready` (`isReady`) bool only (`encodeStateJson` / poll). No correction payload.
- No toast/banner path for maintenance results (no client matches for toast/banner around this).

## Fail-closed vs successful corrections

| Outcome | Operator visibility |
| --- | --- |
| Successful repair (any counts, including zero) | Server stderr only; client clears “Starting up…” when `isReady` becomes true |
| Maintenance failure | `isReady` never completes; client can stay on “Starting up…”. Mutations (`PostChange`) get `Error` with `"Startup projection sweep failed: …"` — that string can reach the client as a mutation failure, not as a dedicated startup banner |

## Tests

No tests assert the `eprintfn` line or that correction facts are exposed to the client. Planner/maintenance tests cover plan/`logFacts` shape in Shared/Server code paths, not UI reporting.

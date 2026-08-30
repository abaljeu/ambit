# Follow-up — §10 drop `index` from `Op.Replace`

**Date:** 2026-08-22
**Issue:** [[../issues/14-drop-replace-index-wire-migration.md]]
**Prior work:** [[14-drop-replace-index-wire-migration-build.md]]

## Issue / board

- [[../../../WORK.md]] — no §10 or drop-index entries in Active, Pending, or Blocked; no board mutation (work done before issue file).
- Issue **14** — [[../issues/14-drop-replace-index-wire-migration.md]] — anchors §10 wire/type migration distinct from issue 13 producer migration.

## Doc edits

| Artifact | Change |
| --- | --- |
| [[wire-full-list-replace-contract.md]] | `index` field row → **Dropped**; Op type, JSON rows, producer migration, and tracker updated for post-§10 state |
| [[../project.md]] | Issues 01–14; issue 14 listed |

## Tests (prior agent)

| Suite | Result |
| --- | --- |
| `Shared.Tests` | 1350 passed |
| `StateEndpointTests` | 66 passed |

## Follow-up (2026-08-22)

- Legacy `"index"` decode shim removed from [[../../../src/Shared/Serialization.fs]] — decode requires `parentId`, `oldChildren`, `newChildren` only.
- JSON field rename `oldChildren`/`newChildren` → `oldList`/`newList` closed as **not planned** — see [[14-drop-replace-index-wire-migration-no-legacy-shim.md]].

## Commits

None (per instruction).

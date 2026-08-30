# Decision — drop legacy Replace `"index"` shim; retain `oldChildren` naming

**Date:** 2026-08-22
**Branch:** `w/event-sourced-ops`
**Issue:** [[../issues/14-drop-replace-index-wire-migration.md]]
**Prior:** [[14-drop-replace-index-wire-migration-build.md]], [[wire-full-list-replace-contract.md]]

## User decisions

1. **Remove legacy log shim** — `Serialization.decodeOp` for `Replace` no longer reads optional `"index"`. Wire decode requires current shape: `type`, `parentId`, `oldChildren`, `newChildren`.
2. **Do not rename** `oldChildren` / `newChildren` to `oldList` / `newList` on JSON — closed as not planned; `oldChildren` remains semantically accurate for wire fields.

## Code change

| File | Change |
| --- | --- |
| [[../../../src/Shared/Serialization.fs]] | Removed `_legacyIndex` optional read from `Replace` decode branch |
| [[../../../tests/Shared.Tests/SerializationTests.fs]] | `Op.Replace round-trip` asserts encoded JSON omits `"index"` |

## Doc updates

| Artifact | Change |
| --- | --- |
| [[../details/replace-amendment.md]] §1, §10 | Wire shape locked; shim removed; rename closed |
| [[wire-full-list-replace-contract.md]] | Index row, JSON fields, tracker |
| [[14-drop-replace-index-wire-migration-build.md]] | Legacy shim section amended |
| [[14-drop-replace-index-wire-migration-follow-up.md]] | Follow-up entry |

## Grep — legacy references

| Pattern | Result |
| --- | --- |
| `_legacyIndex` / optional `"index"` decode | Removed from [[../../../src/Shared/Serialization.fs]] only |
| Span upgrade at decode | Not implemented (unchanged) |
| `oldList`/`newList` JSON rename in specs | Closed in §10 and wire contract; Actor-contract prose in [[../details/replace-amendment.md]] still uses `oldList`/`newList` as semantic names |

## Tests

| Filter | Result |
| --- | --- |
| `SerializationTests` | 34 passed |
| `HistoryTests` + `ClientHistoryTests` + `SerializationTests` (combined filter) | 97 passed |

## Commits

None (per instruction).

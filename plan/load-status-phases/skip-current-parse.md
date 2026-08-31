# Skip Current Directory File parse on rediscovery

Stage note: option 3 from [[slow-parsing.md]] implemented on `w/owner-edge-db-repair`. No commit (per request).

## Decision

On web directory Load / reconcile rediscovery (`Added`), do **not** re-read or re-parse Directory File (`.amb`) when `ensureChild` / `planAddedInfo` emitted **no create ops** and the directory stub is already **`Current`**.

Still parse when the path is new (create ops) or the stub is `Unparsed` / `NoServerFile`. Modified / renamed paths still parse.

Accepted tradeoff: silent DataDir hand-edits while the stub stays `Current` are not detected until a later mtime/`Modified` path.

## Files changed

| File | Change |
|------|--------|
| [[src/Shared/dotnet/LazyLoadReconciliationApply.fs]] | `skipParseAddedDirInfo`; `parseDirInfoInfos` skips Current rediscovery |
| [[src/Shared/dotnet/LazyLoadReconciliationReport.fs]] | `finalizeAdded` skips `parseDirInfoIfPresent` when createOps empty + Current |
| [[src/Server/LazyLoadReconciliationServer.fs]] | `readDirInfoArtifacts` takes graph; skips disk read for Added + Current `.amb` |
| [[tests/Shared.Tests/LazyLoadReconciliationTests.fs]] | Poison-text skip, Unparsed/new still parse, 80-dir timing <100ms |

## Tests

```text
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~LazyLoadReconciliationTests"
→ Passed: 37

dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~LazyLoadReconciliationServerTests"
→ Passed: 20
```

New Shared cases:

- `rediscovered Current Directory Files skip parse on Added` — divergent artifact text ignored
- `rediscovered Unparsed Directory File still parses on Added`
- `new Directory File Added still parses outline`
- `second Added rediscovery of Current dirs stays under 100ms`

## Leftovers

- Options 4–6 in [[slow-parsing.md]] (mtime gate, stop blanket-`Added`, inventory ledger) not done.
- Options 1–2 discarded per user (still serialize/export).
- No dedicated Server test that asserts zero `.amb` reads on Current rediscovery (Shared + existing Server suite cover behavior).
- Full-suite / HITL Load on a large workspace not run here.

## WORK.md mutations (for parent)

- `remove` [[plan/load-status-phases/slow-parsing.md]] — option 3 delivered ([[skip-current-parse.md]])
- `add` [[plan/load-status-phases/slow-parsing.md]] — optional follow-ups: mtime gate / stop blanket-Added / inventory ledger (options 4–6), only if still desired

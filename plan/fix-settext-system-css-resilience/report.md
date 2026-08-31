# Fix: SetText SYSTEM user.css resilience

> Old/New commentary revised per user (File Node / Normal-vs-Unparsed stub wording; scrubbed “file body” / “File-shaped” / “no artifact text”).

## Old vs New (basic story)

**Old (wrong):** Cold bootstrap left `SYSTEM/user.css` as a **Normal** node with empty children — a **File Node was not made**. **SetText** targeting that File Node therefore failed (`List.exactlyOne` found no content child; even `POST /ambit/file/parse` said `file not found or not a File document` until the node was a `Special File`).

**New:** When the on-disk file exists but stays unread at cold load, seed an **Unparsed File** stub (Kind File, relative path known, text not read yet). Same File Node later holds parsed content after Parse. SetText after Parse succeeds.

## Root cause

Cold bootstrap (`DocumentPersistence.readAllDocuments`) became **directory-file-outline only** in `a3a23c9` (non–Directory File paths unloaded until Parse). Omitting those paths from the assemble map also stopped `DocumentAssembly` from **seedStub**’ing them, so `SYSTEM/user.css` stayed **Normal** with empty children — not a File Node.

Dirty `Model.fs` / `UpdateOps.fs` were unrelated (clean vs HEAD).

## Hypotheses (ranked)

1. Marker-only cold load left `user.css` as Normal (no File stub) — **confirmed**.
2. Test should Parse before SetText — **confirmed** (file text still unread by design until Parse).
3. Restore SYSTEM allowlist cold-load of file text — rejected (intentional selective-load change).
4. State encoding stripped children — falsified (node never became File).
5. Dirty Model/UpdateOps — falsified (no diff).

## Fix

1. **Product:** `assembleFromArtifactsBounded` takes stub-only disk paths; cold load seeds **Unparsed `Special File`** stubs without reading the file's text. `readAllDocuments` passes discovered non-directory-file relatives as those stubs.
2. **Test:** Parse via `/ambit/file/parse`, then SetText at the post-parse revision.

## Files changed

- `src/Shared/dotnet/DocumentAssembly.fs`
- `src/Server/DocumentPersistence.fs`
- `tests/Shared.Tests/DocumentAssemblyTests.fs`
- `tests/Server.Tests/ChangeEndpointResilienceTests.fs`
- `plan/fix-settext-system-css-resilience/` (git.md, project.md, this report)

## Verification

```bash
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~DocumentAssemblyTests" --no-restore
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~ChangeEndpointResilienceTests" --no-restore
```

Result: **20** Shared DocumentAssemblyTests passed; **2** ChangeEndpointResilienceTests passed.

## Suggested WORK.md mutations (for parent)

- None required for this failure (no prior board entry).
- Optional `add` under Pending only if tracking residual cold-stub coverage elsewhere: link this report — not needed once parent verifies green.

---

# Follow-up: DocumentPersistenceTests cold-load assertions

## Root cause of the 2 failures

Stub seeding from the fix above is **intentional and correct**. After write → `readAllDocuments`, on-disk files that exist but stay unread (e.g. `home/doc/readme.txt`) become **Unparsed `Special File`** stubs with empty children.

The two failing tests still asserted the **pre-stub** contract (`NodeKind.Normal`):

1. `readAllDocuments round trips nested workspace tree` via `assertNestedWorkspaceLoad` (line 126 / 744)
2. `readAllDocuments cold load keeps file outline without plain body` (line 878)

Coherent cold-load contract (unchanged product code):

| On-disk file | Cold node |
| --- | --- |
| Present (exists, unread / stub-only) | `Special File` + `Unparsed`, no children |
| Missing | `Normal` outline from marker (see `preserves owner handle when artifact is missing`) |

## Hypotheses (ranked)

1. Tests outdated vs intentional Unparsed File stub contract — **confirmed** (Expected Normal / Actual Special File).
2. Stub seeding too aggressive — **falsified**; narrowing would regress ChangeEndpointResilience / SYSTEM `user.css`.
3. Missing-file path wrongly upgraded to File — **falsified**; that sibling test still expects Normal and stayed green.

## Fix

Update assertions only (no product change): expect `Special File` + `Unparsed` + empty children when the on-disk file exists but stays unread.

## Files changed

- `tests/Server.Tests/DocumentPersistenceTests.fs` (`assertNestedWorkspaceLoad` + cold-load outline test)

## Verification

```bash
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~DocumentPersistenceTests" --no-restore
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~ChangeEndpointResilienceTests" --no-restore
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~DocumentAssemblyTests" --no-restore
```

Result: **54** DocumentPersistenceTests passed; **2** ChangeEndpointResilienceTests passed; **20** DocumentAssemblyTests passed.

## Relationship to prior stub-seeding fix

Same contract: cold bootstrap seeds Unparsed File stubs for discovered non–Directory File paths. Prior work fixed resilience/assembly; this follow-up aligns DocumentPersistenceTests that still expected Normal outline nodes.

## Suggested WORK.md mutations (for parent)

- None required (no board entry for this residual).
- If parent added a Pending/Active item for these two failures: **`remove`** after verifying green.

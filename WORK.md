# Work Board

Live actionable work only. Empty sections mean nothing is known pending there. Git history is the audit trail; completed items are deleted, not archived.

## Legend

Each entry is one actionable item: a link to the durable source or target, a concise expected outcome, and optional owner or blocker detail.

Entry format:

```
- [[path/to/artifact]] — expected outcome (owner: root-agent-id)
```

Mutations for delegated workers to return to their parent: `add`, `move`, `block`, `remove`.

## Active

Work currently being executed.

## Pending

Work ready to start but not yet claimed.

- [[src/Shared/dotnet/OutlineLcs.fs]] — root cause of the `pairIdenticalMovesWrite` `IndexOutOfRangeException`: `diffTexts` joins lines with `"\n"` then lets DiffPlex's `LineChunker` re-split, so a node `.text` containing an embedded `\r`/`\n` desyncs DiffPlex's chunk indices from the true list indices. Plan: replace `LineChunker.Instance` with a custom `IChunker` that splits on a collision-probed sentinel instead of `\n`, so chunk count always equals list length. Add red test first in `tests/Shared.Tests/OutlineReconcileTests.fs` (`writePlan` with an edited-line `.text` containing `"\n"`). Also noted (separate, not this bug): `remap` in `OutlineDocumentWarm.fs:254-258` never remaps `WKeep`'s `ei` from `editRest`-local to `edited`-global space — silently wrong downstream `ei`, doesn't crash, worth a follow-up (owner: unclaimed)

## Blocked

Work that cannot proceed until a named dependency or decision is resolved.

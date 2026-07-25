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

- [[src/Shared/documents/OutlineDocumentWarm.fs]] — `remap` (lines ~254-258) never remaps `WKeep`'s `ei` from `editRest`-local to `edited`-global space — silently wrong downstream `ei` field (doesn't crash, but is incorrect wherever read). User requested this be corrected (owner: root-agent-db-exception-boundary)

## Pending

Work ready to start but not yet claimed.

## Blocked

Work that cannot proceed until a named dependency or decision is resolved.

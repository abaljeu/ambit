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

- [[src/Server/Api.fs]] — empty-body HTTP 500 on `/state`: find why Problem detail is missing; ensure non-empty body with detail (owner: empty-body-500)

## Pending

Work ready to start but not yet claimed.

- [[doc/reference/dev-debug-workflow.md]] — document watch: prefer `/ambit?debug=1`; after esbuild rebuild hard-reload (Ack on CodeOutdated does not unblock)
- [[src/Shared/SyncLogic.fs]] — decide whether to ignore page-stamp drift when deploy stamp matches during Fable/esbuild watch
- [[src/Client/Program.fs]] — optional hardening: `fetchTextNoCacheWithFail` for `/ambit/state` (not the primary hang)

## Blocked

Work that cannot proceed until a named dependency or decision is resolved.

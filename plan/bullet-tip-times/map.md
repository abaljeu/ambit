# Bullet tip — time display

## Destination

Resolve every non-obvious requirement for displaying **time facts** on the Bullet hover tip, so
`/to-spec` can specify the time-display portion without re-deriving clocks, sources, availability,
de-duplication, or timezone rules. The rest of the tip (identity, cssClass, workspace path, vehicle)
stays with the parent grill.

## Notes

- Spun out of [[plan/node-bullet-tooltip/grill-notes.md]] round 3 because the time model got
  complex enough to deserve its own decision store.
- Evidence: [[tmp/node-marker-tip-facts.md]] and [[plan/node-bullet-tooltip/grill-notes-round-3.md]].
- Parent decisions already locked: tip is an always-on inspector (privacy exposure accepted);
  each line self-gates and is omitted entirely when absent (no label, no `N/A`); vehicle is native
  `title` with `\n` lines; source is the client-local **sync ledger** (`VM.workspaceSyncFacts`),
  not the active-only `/file-status` slot; no fetch on hover.
- Full requirement table: [[plan/bullet-tip-times/time-requirements.md]].

## Status

**Parked** by the user (2026-08-08): time is a separate effort, not being addressed yet.
T-Q1…T-Q4 remain the open frontier here; recommendations stand but nothing is confirmed.

## Decisions so far

- Source file/server times from the ledger facts (instant, per-node, mapped desktop sessions);
  degrade to Update Time only elsewhere. (R3-Q2 ➡️, pending final confirm.)
- User's fourth "sync time" clock has no distinct timestamp; represent `lastOp` as a word, not a
  clock. (R3-Q3(4) ➡️ (b), pending final confirm.)

## Not yet specified

- De-dup equality tolerance (exact ticks vs second granularity).
- Timezone render precision (to the second / minute) and format string.
- Whether a genuine last-sync-operation *timestamp* is wanted (would be new persistence; ledger
  stores no op time today).

## Out of scope

- Identity fields (Guid), cssClass, workspace `//label/relative` path, and vehicle — parent grill.
- Any fetch-on-hover or new server/app endpoint for per-node file times.
- Implementation.

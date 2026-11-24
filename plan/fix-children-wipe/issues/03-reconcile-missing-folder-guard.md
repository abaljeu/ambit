# 03 — Guard DirectoryReconcile against a missing folder

**Status**: pending confirmation from 01
Blockers: [01 — Run the event-log queries](01-run-event-log-queries.md) — this ticket does not start before the event-log results confirm Rank 1 on [[../project.md]].

## Scope

`src/Server/DocumentPersistPath.fs` `discoverInDirectory` returns `Ok []` for a folder that does not exist. `src/Shared/dotnet/DirectoryReconcile.fs` `planFromEntries` reads that empty listing as "nothing survived" and moves every owned File/Directory child to TRASH. A missing or unreadable folder must be a Load error that reconcile refuses to act on, not an empty listing. The same guard makes name matching against the listing case-sensitive, and reuses the ids already recorded in a folder's own `.amb` instead of minting new ones.

## Done

[ ] `discoverInDirectory` returns a distinct `Error` case for a folder that does not exist or cannot be read, separate from `Ok []` for a folder that exists and is empty.
[ ] `planFromEntries` refuses to trash owned children when given that `Error`, and the Load that triggered it reports a failure instead of reconciling silently.
[ ] The listing-to-`knownNamed` name compare is case-sensitive.
[ ] A folder holding only a `.amb` file reuses the ids recorded in that `.amb` instead of minting new ones.
[ ] A test reconciles a Workspace or Directory whose folder is missing and asserts the owned children survive and the Load reports a failure.

## Stop

[ ] Stop before ticket 01's results confirm this is Rank 1 on `project.md`; this ticket does not start before that confirmation.
[ ] Stop if the fix also changes how a genuinely empty, existing folder reconciles; that case stays `Ok []`.
[ ] Stop if the change reaches outside `DocumentPersistPath.fs` and `DirectoryReconcile.fs` plus their direct callers and tests.

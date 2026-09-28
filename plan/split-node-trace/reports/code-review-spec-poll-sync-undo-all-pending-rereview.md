# Spec re-review — Poll/sync undo-all-pending (PR 162, tip 6770efde)

No Spec findings.

Prior Must-fix (rewind every pending Graph op on any non-empty Event or Want, including CommandDone Actor tails via `applyServerTail`; `withAppliedSync` wrote the rewound Graph and left `SyncInfo.pending`) is **closed**.

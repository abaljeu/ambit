# Ready the initial Core Changes increment

Type: grilling
Status: resolved
Blocked by: 03, 04, 05
Actual: 50m

## Question

Which Browser, Server-producer, file-authority, database, and mirror runtime Change paths must migrate through Core Changes; which named startup or repair writers may remain temporary exceptions until ACID apply; and what focused acceptance evidence proves [[01-generalized-server-actor-produce-path.md|Generalized Server Actor produce path]] can start without changing current acknowledgement, persistence, timeout, or mirror behavior?

## Answer

- Core Changes retains two operations. Normal serves Browser POST and future Server Actors. Graph-only is reserved for Parse; lazy-load and git reconciliation invoke Parse.
- After initialization reaches mutation-ready state, Core is the sole Server authority that accepts, publishes, and persists runtime Changes. Graph, Node, and State values are immutable, so other modules can hold Graph snapshots for Query and planning without gaining write authority. Existing initialization, repair, database/file reconciliation, and Graph-to-file/file-to-Graph protocols remain unchanged outside Core Changes as named temporary exceptions until ACID apply is redesigned. Do not invent new Graph/file functions.
- When the database is available, the Server always selects Database persistence. Production code ignores `Persistence:Mode`. When the database is unavailable, the Server rejects Changes but accepts Graph-data and file queries.
- Runtime mirror deletion is the separate, independent [[13-delete-runtime-mirror-and-remove-production-persistence-mode.md]]. It does not block [[01-generalized-server-actor-produce-path.md]], which must not migrate or expand the unused mirror path.
- Keep the current eight-second Server Change-processing timeout. Replace it later with an asynchronous, user-cancellable Actor after Actor support exists. Late completion of an abandoned task is not required behavior.

Focused acceptance evidence adds only a few tests for typed Core API seams. In particular, a harness acting as a Server Actor calls the Normal typed Core Changes operation without HTTP and proves that the accepted Change is visible through Poll. Reuse existing behavior tests for acknowledgement, Reject, persistence, timeout, Parse, and Poll; do not duplicate those tests.

## Time

- 2026-09-05 45m — grilled and resolved the initial Core Changes increment
- 2026-09-05 5m — wrote the implementation-ready plan for resolved issues 03–06

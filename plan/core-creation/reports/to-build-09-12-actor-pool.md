# To-build: Actor pool (09–12)

This increment is the production Core Actor pool on existing Core Changes. [[../issues/02-core-actor-pool.md]] delivers launch, cancel, finish, and shutdown. [[../issues/09-define-core-command-launch-contract.md|09]]–[[../issues/12-define-actor-pool-shutdown-behavior.md|12]] lock those contracts. Actors run off the apply mailbox. Crash isolation stays out. Map: [[../map.md]].

## To build

- Registry: composition registers Actor names. Never-reused public number → Actor. Query by number works until delete-actor applies, then fails.
- Credentials: send credential is Actor initial state only; never return it to the Command caller. Core retains number→Actor, credential, span, Revision, and name.
- Launch Command: registered name, Revision, parent NodeId, non-empty start/endd span (shape of [[src/Shared/ViewModel.fs]] SiteNodeRange; parent is NodeId). Caret and no-selection are forbidden. Extract the parent Node, then child occurrences; pass that subgraph plus credential. Return the number.
- Overlap: refuse launch that shares any NodeId with a live span.
- lock-present: write immediately on each live Node in the span. Not History. Clients see it through state, Fetch, or Query. Omit the field from SQL create, update, and select. Fresh read or new process is lock off.
- Cancel Command: NodeId; find the job by span membership. Signal CancellationToken. Remove the credential from the active set. Cancel is not Undo.
- Admission: Post sender id must match an active source. Actor sender is the send credential. Browser sender is the `gambol_auth` cookie at the Adapter after page open. Inactive → one auth refuse (Unauthorized / HTTP 401), do not enqueue. Already in the mailbox → apply.
- Finish: no job result. When the Actor Async/Task stops for any reason, enqueue Core-only delete-actor (not a Change; no Actor sender). FIFO applies earlier Actor Changes first. delete-actor drops the registry entry, number, and credential, and writes lock off the Nodes.
- Database down: a mutating Post that gets a TCP or transport error Rejects that Change and marks Database down. Siblings apply. No mailbox-clear. Next mutating Post or launch is the probe: success applies from the live Graph; TCP fail Rejects that one and stays down. Launch Rejects while down. Running Actors may continue; they cannot persist Changes. Query, state, Poll, and Graph reads stay admitted. That refuse is the same system-error Reject as [[../issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]] / readOnly.
- Host StopAsync: refuse new Posts with that system-error Reject; apply remaining mailbox including delete-actor; cancel Actors with CancellationToken; exit when the mailbox is idle or the host default ShutdownTimeout fires. delete-actor while Database is down still drops the in-memory registry and skips persist of lock-off.

## Do not build

- Completed or aborted job result, or a job Error to the Command caller.
- Crash isolation, or extra terminal job facts before process exit.
- A mailbox-clear API, or drop of already-enqueued siblings on TCP fail.
- Graph-wide persist strip, post-load lock-clear, or SQL that includes lock-present.
- A second refuse kind: auth refuse and system error stay separate families, not two Actor-only Errors.
- Parse, shell, or Agent Command cases; Actor definitions; Browser chrome; advisory soft-lock policy.
- ShutdownTimeout as a named Core number.

## Depends

- [[../issues/01-generalized-server-actor-produce-path.md]] and issues 03–06 are delivered: typed Core Changes and a test Actor on `postChange`. Build the pool on that path.
- [[../issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]] is parallel: startup readOnly / Database-unavailable Reject is the same system error as 12.
- [[../issues/02-core-actor-pool.md]] is the delivery ticket. 09–12 stay resolved decision tickets.

# Define the load snapshot protocol

Type: grilling
Status: resolved
Blocked by: 01, 02

## Question

What exact client-server request, response, and application contract should realize monotonic authoritative direct-child loading—including request granularity and batching, node/header payload, currentness or version evidence, unchanged and idempotent responses, concurrent requests, and stale-response handling—while the server graph remains fully resident?

## Answer

- Add `POST /{agent}/load-snapshot` with request `{ revision, mode, targets }`. One `Direct | ArtifactClosure | Workspace` mode applies to the entire `NodeId` target batch.
- Initial bootstrap is the only request that omits `revision`: it sends one `Workspace` batch targeting ROOT and the remembered Workspace ID (deduplicated when they are equal), and the returned revision establishes the client baseline.
- `Direct` snapshots each target’s complete direct-child list. `ArtifactClosure` resolves each target's nearest artifact on its canonical Owner chain and follows Owner edges within that artifact, stopping after the header of a nested Workspace, Directory, or File artifact. `Workspace` follows Owner edges through Directory and File artifacts and stops after the header of a nested Workspace. No mode follows Ref edges.
- The server reads its fully resident graph and revision atomically. An initial request returns that current revision; every later request returns the catch-up transaction from its requested base revision plus a snapshot at the resulting revision, as decided in [Define synchronization and revision correctness](09-define-sync-revision-correctness.md).
- A malformed target rejects the whole batch. A target absent at the response revision returns catch-up without requested snapshot facts; snapshot batching has no partial success.
- A successful response contains its base and resulting revisions, catch-up changes, authoritative headers or tombstones required by catch-up, and requested snapshot child lists. Headers are deduplicated by node ID; child lists are deduplicated by parent ID and preserve authoritative child order and Owner/Ref tags.
- Every returned list parent and listed child has a header. A header contains `id`, `text`, `name`, `cssClasses`, required `ownerId`, `kind`, `documentState`, and `updateTime`, but no children. Its `ownerId` becomes `Known ownerId` and may name a non-resident owner header.
- `Loaded []` represents a loaded leaf. Header presence alone never implies that node’s child list is loaded.
- Repeating an unchanged request returns the same complete package. Requests and application are idempotent; there are no list validators or `NotModified` response.
- Concurrent batches are allowed. Request identity, progress, and continuations remain client-local and are not echoed on the wire.
- Loads dispatch only while no graph-changing local submission is pending and capture a client-local mutation epoch. A response applies only when its base revision still matches the current client revision and the epoch is unchanged. Otherwise it is discarded under the synchronization recovery rules; applying a successful response never reruns a client-wide loading planner.
- A valid response is a trusted authoritative server projection. Application atomically applies its catch-up, overwrites returned snapshot header facts and child lists, preserves residency for header-only nodes, marks every returned list `Loaded`, derives occurrence indexes only from loaded lists, and derives owner indexes from known owners.
- Same-revision semantic disagreement is a coding invariant violation, not a recoverable client/server conflict. Decode or transport failure leaves the graph unchanged.
- If the initiating UI intent disappears while loading, a still-current response may nevertheless be installed because residency is monotonic; any continuation is then reconsidered separately.

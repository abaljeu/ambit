# Future merge and sync

See also: [[on-demand-graph-residency]], [[postgres-roadmap]], [[doc/current/sync-mvp]]

## Objective

Provide **eventual consistency** of the application model across clients and the single database server: offline or lagging clients may submit edits that are merged **deterministically** at the master, with optional **conflict marker nodes** so users can finish resolution when automation stops. The **complete model** lives in PostgreSQL; webservers and clients hold **partial replicas** (whole documents they have loaded, not slices within a document). The server warm cache is also partial under [[on-demand-graph-residency]]; merge authority loads and pins the operation's document dependency closure before validation.

## Decisions

- **Merge authority:** The database server applies submitted batches against the current head and emits the canonical result (new revision, tail of ops, or full snapshot—wire details TBD). No other actor performs authoritative merge.

- **Local webservers:** They do **not** merge. They bridge OS access and forward or queue edits toward the master like any other client edge.

- **Replication unit:** A device holds a **set of whole documents** in memory. Document identity is the document-root NodeId unless a later requirement demands a separate ID; references may point across documents. Edits are **node-level**, not restricted to document-sized blobs.

- **Conflict boundary:** Keep one global ordered change sequence for audit and catch-up, but stop using exact global revision equality as the conflict boundary for unrelated workspaces. Submissions carry affected document base versions; cross-document changes check all touched documents atomically. Detail: [[on-demand-graph-residency]]. Current MVP still uses global revision equality ([[doc/current/sync-mvp]]).

- **Conflict handling:** Merge is a **complete computation** over tame concurrency (bounded outcomes, e.g. parallel branches for doubly-edited nodes). The merge may **create nodes** (e.g. conflict markers) for the user to clear manually.

- **Delete vs concurrent edit:** **Edit wins**—encode this explicitly in merge rules and in how deletes are represented (e.g. tombstone vs hard remove) so every peer converges the same way once server ops are applied.

- **Typical lag pattern:** The server may have advanced by many edits while a client holds a **small pending batch** from an **earlier base**. Convergence is **rebase-style**: interpret that batch against the **current** head, not against stale state.

- **Auditability:** Prefer a **human-readable append log** at the master unless transport and tooling already give **reliable sync** and **inspectable history** without it.

## Methods

- **Ordered append log** at the master (e.g. Postgres) as the durable source of ordered facts; merge reads current materialized state plus new batch and appends resulting ops (including conflict artifacts).

- **Client UX:** **Optimistic** local application of pending edits is allowed; the **canonical** model is whatever the server returns after accept. Clients reconcile pending queues to server acks.

- **Catch-up bandwidth:** Prefer **per-document packages and projection patches** (plus descriptor version advances for unloaded documents) so lagging clients are not required to download the entire server history or warm cache to converge; merge still runs **once** at the master for submitted batches. Detail: [[on-demand-graph-residency]].

- **Cross-document edits:** When one logical change touches multiple documents, log **one** operation with enough payload for downstream **projections** (per document read models, search indexes) to update every affected document—same spirit as a global write log with document-shaped **read** views.

- **Simultaneous editing:** Treated as **safety** for rare overlap, not as real-time collaborative editing; merge rules and tests focus on correctness and bounded conflict shapes, not on sub-second shared cursors.

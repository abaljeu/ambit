Below is a partial description of a persistence mechanism I want to implement on my Azure server using Postgres. The problem I need you to help me solve: I don't have Postgres. I only have the most basic Azure node with a shared space for client data and server code. I am the only client, so this much works but how do i add a Postgres database and set up that program to achieve the below.

========
Keep the document-oriented loading model. This is a classic CQRS + Event Sourcing pattern: the write side is global; the read side is partitioned.

Here's how to make it work correctly.

1. The Global Operations Log (Write Side)
This is the only table that receives writes. Every edit, link creation/deletion, node deletion, or document split is one row here.

Table: operations_log (Global, Append-Only)

sql
CREATE TABLE operations_log (
    global_seq_id BIGSERIAL PRIMARY KEY,  -- Strict total order
    operation_type VARCHAR(50),            -- 'UpdateNode', 'DeleteNode', 'CreateLink', 'SplitDocument'
    payload JSONB,                         -- { "nodeId": "xyz", "newText": "..." }
    timestamp TIMESTAMPTZ DEFAULT NOW()
);
Critical: When an operation affects multiple documents (e.g., deleting a node in A that B references), you log it once here. The payload must contain enough context for downstream processors to update all affected documents.

Example: Deleting Node X in Document A:

json
{
  "nodeId": "docA.nodeX",
  "cascadingDeletes": ["docB.linkToX", "docC.linkToX"]
}
2. The Document-Oriented Read Model (Query Side)
The server listens to the operations_log and projects it into document-shaped snapshots. This is where your memory model comes from.

Table: documents (Read Model, Rebuildable)

sql
CREATE TABLE documents (
    id VARCHAR PRIMARY KEY,
    content JSONB,              -- The full document with nodes & links
    adjacent_doc_ids JSONB,     -- Which documents this one links to
    last_updated_seq_id BIGINT, -- Up to which operation is this snapshot current?
    search_vector TSVECTOR      -- Full-text index for search
);
How it's updated: A background worker (or a trigger, or your server code) reads operations_log from last_updated_seq_id + 1 to the current maximum, applies those operations to the relevant documents.content, and updates adjacent_doc_ids and search_vector.

3. Handling Cross-Document References in F#
When you load Document A on the client, you need to know that Node X in A links to Node Y in Document B. But if Document B is unloaded, you can't dereference "Node Y" to show its name.

Solution: Store Link Metadata in Document A's Content

When the server projects the operations into Document A's content JSON, it denormalizes the link text:

fsharp
// Inside Document A's content
{
  "nodeId": "nodeX",
  "links": [
    { "targetDocId": "docB", "targetNodeId": "nodeY", "targetPreview": "Q1 Budget" }
  ]
}
The targetPreview is the text or title of Node Y from Document B, copied at the time the link was created. This lets Document A render a meaningful link even if Document B is unloaded.

Stale Previews: If Node Y's title changes, the operations_log must record an UpdateLinkPreview operation that updates all documents that reference it (the projection worker handles this).

4. The Client Loading Model (Unchanged but Corrected)
Your F# client still loads/unloads whole documents. The document is still the unit of caching. The key difference is that the payload in a document is a projected snapshot of the global event log, not a separate log.

fsharp
type Model = {
    Documents: Map<DocumentId, DocumentState>
    VisibleDocIds: Set<DocumentId>
}
Loading: SELECT content FROM documents WHERE id IN (...)
Eviction: Same 2-minute timeout.

5. Synchronizing Edits
When the client edits a node in Document A:

Optimistic update: Client instantly updates its local DocumentState.Loaded map.
Send operation: Client sends { "type": "UpdateNode", "nodeId": "docA.nodeX", "newText": "..." } to the server.
Server appends: Server inserts into operations_log and assigns a global_seq_id.
Broadcast: Server pushes the new global_seq_id to all connected clients (WebSocket or Server-Sent Events).
Clients update: Other clients receive the seq_id, fetch the operation, and re-apply it to their local documents map if they have that document loaded. If not, they do nothing (efficient!).
6. Document Splitting
When a user splits Document A into A' and B:

Client sends operation_type: "SplitDocument" with the split criteria.
Server appends to operations_log.
Projection worker:
Creates a new row in documents for B.
Updates the content of A to remove the split-out nodes.
Updates adjacent_doc_ids for both A and B.
Clients: The next time they load A or B, they get the new state.
Summary: The Two-Tier Architecture
Layer	Responsibility	Implementation
Global Operations Log	Source of truth, order, cross-document atomicity	Single PostgreSQL table: operations_log
Document Projections	Fast reads, search, loading	documents table, rebuilt from the log
Client Cache	Instant UI, offline capability	F# Map + IndexedDB caching documents rows
This is the same pattern used by Figma (for design files), Notion (for pages), and Linear (for issues). The event log is global; everything else is a cached view tailored for fast loading.

I'm thinking about building a memory management system that behaves somewhat like a garbage collector for my web app. But it's not sweeping unreferenced memory; rather memory that is not actively being used. More specifics: I have a directed graph model with a ton of nodes. Currently all nodes are loaded into the client but that doesn't have to be that way, and the client has a display that shows a connected subset of the graph. Which subset may change based on user action. Generally it will be nodes adjacent to the current displayed set, and I want the app to usually be instant response time when changing the display, and when editing the model through that display. Both display and model are implemented with a immutable F# map of id to node content, and the content objects have the links and other data. id is not persisted now, but maybe it should become so to achieve this scheme.

How should I approach automatic unloading and loading. Feel free to ask questions pertinent to a good decision


Thought Process
expand
This is a fascinating problem—essentially a graph-aware cache management system with an immutable data model. Before diving into architecture, I need to clarify a few things to recommend the right approach:

Key Questions:
Graph & Scale:

Size? Roughly how many nodes? (1k? 100k? 1M+?) Average node size in memory?
Dynamics? Is the graph static, or are nodes/edges added/removed during the session?
Cycles? Are there cycles in the graph? (affects prefetching strategies)
Density? Is it sparse (avg degree < 10) or dense (hundreds of edges per node)?
Usage Patterns:
5. Display Size? How many nodes are typically visible at once?
6. Navigation Pattern? Do users "explore" locally (adjacent nodes), "jump" far away, or follow predictable paths (e.g., tree traversal)?
7. Edit Frequency? How often are displayed nodes edited vs. just viewed? Are edits local or cascading?
8. "Instant" threshold? What latency is acceptable for a cache miss? (<50ms? <200ms?)

Technical Constraints:
9. Persistence Target? Where would unloaded nodes live? (IndexedDB, server API, both?)
10. IDs? You mention IDs aren't persisted—are they currently stable across sessions? If not, how are edges stored (by reference or ID)?
11. Network? Is this purely client-side, or are you syncing with a backend? Any offline requirements?
12. Concurrency? Can edits happen while loading/unloading? Any multi-user collaboration?

Provisional Architecture (assuming typical web app constraints)
Assuming: sparse graph, 10k-100k nodes, local exploration, server-backed persistence.

1. Model Layer Changes
Your intuition about persisting IDs is critical—you must have stable, persistent IDs to reference unloaded nodes.

fsharp
type PersistedNodeId = string // Must be stable across sessions

type NodeState =
    | Loaded of NodeContent
    | Loading of Async<PersistedNodeId, NodeContent> // In-flight request
    | Unloaded of neighbors: Set<PersistedNodeId> // Ghost node with edge list

type Model = {
    Nodes: Map<PersistedNodeId, NodeState>
    DisplaySet: Set<PersistedNodeId> // What's on screen
    WorkingSet: Set<PersistedNodeId> // DisplaySet + prefetch buffer
}
2. Loading Strategy (Prefetching)
Don't just load neighbors—load "neighborhood shells":

Shell 0: DisplaySet (always loaded)
Shell 1: Immediate neighbors (preload aggressively)
Shell 2+: Load with lower priority using requestIdleCallback
Smart Prefetching:

fsharp
// On navigation from A -> B
let newDisplay = getDisplayNodes B
let prefetch = 
    newDisplay 
    |> Set.collect (fun id -> getNeighbors model id |> Set.filter (isUnloaded model))
    |> Set.filter (fun id -> isFrequentlyAccessed model id || isOnCommonPath model id)
Priorities:

P0: DisplaySet (synchronous, must be loaded)
P1: Shell 1 neighbors (load immediately)
P2: Historical prediction (load in idle time)
P3: Everything else (load on demand)
3. Eviction Strategy (Unloading)
Don't use LRU—use Working Set + Access Patterns:

fsharp
let shouldEvict nodeId model =
    let isInDisplay = Set.contains nodeId model.DisplaySet
    let isInShell1 = Set.contains nodeId (getShell1 model)
    let lastAccess = getLastAccessTime model nodeId
    let accessFreq = getAccessFrequency model nodeId
    
    not isInDisplay && not isInShell1 && 
    (lastAccess < DateTime.Now.AddMinutes(-5) || accessFreq < 0.1)
Evict in batches during idle time. Keep ghost nodes (Unloaded state) with only edge lists to maintain graph structure without payload.

4. Persistence Layer
IndexedDB is your friend. Store nodes keyed by ID:

NodeRecord: { id: string; content: serialized; neighborIds: string[] }
Use separate object stores for nodes and metadata (access logs)
Serialization: Use F# ISerializable or a fast JSON converter. For binary data, consider MessagePack.

5. Making it Feel "Instant"
Optimistic UI: Assume load will succeed, render immediately with spinner for content
Snapshotting: Keep a serialized "pack" of the current DisplaySet + Shell 1 in memory for instant hydration
Background sync: Use Web Workers for I/O to avoid blocking UI thread
Edge Caching: Cache the graph topology (edges) separately from node payloads—it's smaller and needed for prefetching
6. Immutable Updates
Your immutable map is perfect. Updates become:

fsharp
let loadNode id model =
    let state = Map.tryFind id model.Nodes
    match state with
    | Some (Unloaded neighbors) -> 
        { model with Nodes = Map.add id (Loading asyncLoad) model.Nodes }, 
        Cmd.OfAsync.perform loadFromIndexedDB id NodeLoaded
    | _ -> model, Cmd.none
Next steps: Answer the questions above, and I'll refine this into a concrete implementation plan with code snippets for your specific case.

Kimi K2
Dealing with 100,000 nodes, potentially more. Mostly a broad tree structure but with some cycles. Nodes will be about 100 to 1000 bytes of data. This volume could potentially live on a client, but it's stretching a bit. Regarding display, there's the currently expanded subtree which can be any size and at times into the thousands, and then there's potentially an "onscreen" filter because there's never more than 100 that can actually be viewed without scrolling. Edits are persisted to the server by a stream of replayable operations. They are local.

Text Search is a frequent operation, and that should prioritize the local neighborhood but allow non-local finds. Those can be slower coming.

I have been thinking about storage tech but at our scale I'm not sure it's a critical factor. What do you think? The client communicates with a persistent server which stores the data somehow in addition to keeping it in memory.


Thought Process
expand
At 100,000 nodes with 100–1000 bytes per node, your total data payload is around 10MB to 100MB.

You are right: this is in the "uncanny valley" of web app data. It's technically small enough to fit into a modern browser's RAM without crashing, but parsing 100MB of JSON on startup will freeze the main thread, and downloading it on a 3G/4G connection or slow WiFi will destroy your "instant" requirement.

Given your specifics (expanded subtrees, viewport filtering, local edits via replayable operations, and text search), here is how you should architect this.

1. The Core Revelation: Split Topology from Payload
Do not treat a "Node" as a single block of memory. Split it into two F# maps.

Keeping the entire topology (IDs and relationships) in RAM for 100,000 nodes will only consume about 2–5MB. This is trivial. It's the payloads (the 100-1000 bytes of content) that consume 100MB+ and need garbage collection.

fsharp
type NodeId = string

// 1. Topology is ALWAYS fully loaded (Tiny memory footprint)
type NodeTopology = {
    Parents: Set<NodeId>
    Children: Set<NodeId>
    // Other edge types for cycles
}

// 2. Payload is paged in and out (The bulk of the memory)
type NodePayload = {
    TextContent: string
    Metadata: Map<string, string>
    // etc.
}

type Model = {
    Topology: Map<NodeId, NodeTopology>
    Payloads: Map<NodeId, NodePayload> // Only contains loaded nodes
    ExpandedNodes: Set<NodeId>         // The logical subtree currently open
    OnScreenNodes: Set<NodeId>         // The 100 nodes in the viewport
}
2. The "Graph-Aware" Mark and Sweep
Since you want automatic unloading that behaves like a garbage collector, you can write a literal Mark and Sweep algorithm that runs over your immutable data structures during idle time (e.g., every 5 seconds, or via requestIdleCallback).

Instead of tracing memory pointers, you trace graph distance starting from your "GC Roots".

GC Roots: Your ExpandedNodes (and especially OnScreenNodes).
Marking Phase: Traverse the Topology map starting from ExpandedNodes. Mark everything at Depth 0 (Expanded), Depth 1 (Immediate Children/Parents), and maybe Depth 2.
Sweeping Phase: Any ID in the Payloads map that was not marked gets removed (Map.remove).
fsharp
let collectGarbage (model: Model) =
    // 1. Mark: Find all nodes within distance D of Expanded nodes
    let rec getNeighborhood currentIds depth =
        if depth = 0 then currentIds
        else
            let neighbors = 
                currentIds 
                |> Set.map (fun id -> getNeighbors model.Topology id) 
                |> Set.unionMany
            Set.union currentIds (getNeighborhood neighbors (depth - 1))

    let keepAliveSet = getNeighborhood model.ExpandedNodes 2 // Keep 2 levels deep
    
    // 2. Sweep: Filter the immutable map
    let newPayloads = 
        model.Payloads 
        |> Map.filter (fun id _ -> Set.contains id keepAliveSet)
        
    { model with Payloads = newPayloads }
3. Display, Virtualization, and Prefetching
Because you have thousands of nodes "expanded" but only ~100 visible, you need DOM virtualization (like react-window or a custom IntersectionObserver setup).

Loading/Prefetching Rule:
Whenever the user scrolls (changing OnScreenNodes) or clicks to expand a node, check the Payloads map. If a node is missing, dispatch an async fetch. Because you know the Topology, you can eagerly fetch the payloads for the children of a node before the user expands it.

4. Solving the Text Search Problem
Search is the main reason developers mistakenly keep everything in memory. Because you are unloading payloads, you can't do a full String.Contains across the 100k nodes synchronously.

The Hybrid Search Strategy:

Instant Local Search: Run your search against the model.Payloads map. Because this contains the expanded tree and its neighbors, it immediately yields highly relevant local results.
Async Global Search: Simultaneously, send the query to your persistent server. The server runs a fast index search (Elasticsearch, Postgres Full Text, etc.) and returns a list of NodeIds and text snippets.
Hydration: When the user clicks a global search result, you already have the Topology in RAM. You can instantly calculate the shortest path to that node, expand the tree to that point, and request the payloads for that path.
5. Regarding Storage Tech & Edits
Your approach to edits (a stream of replayable operations) is perfect. When a user edits a node, you apply it to the local Payloads map and send the operation to the server. If the server is the source of truth, you actually don't need IndexedDB on the client for this to work well.

Do you need IndexedDB?

Without IndexedDB: Initial load fetches the 2MB topology, plus the payloads for the root nodes. Very fast. Scrolling/expanding makes API calls to the server. If your server latency is <50ms, this will feel instant, and memory stays perfectly managed.
With IndexedDB (Level 2 Cache): If you want to support offline mode, or if your server latency is >100ms, save fetched payloads to IndexedDB. When your Payloads map misses, check IndexedDB before hitting the server.
Summary of Recommendations for your F# App:
Persist IDs immediately. They are the backbone of the topology and the replayable edit log.
Separate Topology and Payload. 100k edges is tiny; keep them all in RAM so navigation logic is synchronous.
Use Graph-Distance GC. Write a pure function that trims the F# Map based on distance from the active viewport/expanded sets.
Rely on Server for Global Search. Let the client search what's in RAM, let the server search what's out of RAM.
Gemini 3.1 Pro
how should I implement persistence? what factors need be considered? Postgres is my default thought, beyond that i have no thoughts.


Thought Process
expand
Stick with Postgres. It is the absolute best choice for this.

At 100,000 nodes, you do not need a specialized Graph Database (like Neo4j), and you don't need a dedicated search engine (like Elasticsearch). Postgres can handle your topology, your JSON payloads, your replayable event stream, and your full-text search all in one place, with blazing speed.

Here are the key factors you need to consider, and exactly how Postgres solves them.

Factor 1: The "Replayable Operations" (Event Sourcing)
Since your client edits are a stream of replayable operations, you are essentially describing Event Sourcing.

When the server receives an edit, it shouldn't just overwrite the row. You should store the operation in an append-only log. This gives you undo/redo, audit trails, and easy client syncing.

Table: operations_log
global_seq_id (BigSerial, Primary Key)
node_id (String/UUID)
operation_type (e.g., 'UpdateText', 'AddChild')
payload (JSONB - the delta of the change)
created_at (Timestamp)
How it works: The client sends operations. The server inserts them here, which gives them a strict, guaranteed order (global_seq_id). Clients can ask the server: "Give me all operations since sequence #45210" to stay in sync.

Factor 2: The "Read Model" (Snapshots for fast querying)
Replaying 500,000 operations every time a client connects is too slow. You need a "Read Model" (or snapshot) that stays constantly updated based on the operations log.

Whenever an operation is saved, a background process (or database trigger, or your server code) applies that change to your actual graph tables.

Table: nodes (The Payload)

id (String/UUID, Primary Key)
content (JSONB - the 100-1000 byte data)
search_vector (TSVector - for text search)
Table: edges (The Topology)

parent_id (String/UUID, indexed)
child_id (String/UUID, indexed)
(Primary Key is the combination of both)
Factor 3: Bootstrapping the Client (The Topology)
Remember the "Split Topology from Payload" strategy? When your F# client boots up, it needs the topology immediately to render the tree map.

With the edges table, this is incredibly fast. You just run:
SELECT parent_id, child_id FROM edges;

For 100,000 nodes (and assuming ~120,000 edges since it's mostly a tree), this query will return in a few milliseconds and send a tiny ~2MB JSON array or CSV over the wire. The client parses this instantly and knows the entire shape of the graph.

Factor 4: Full-Text Search
Postgres has built-in Full Text Search that is insanely fast for datasets under a few million rows.

You create a tsvector column that automatically indexes the text inside your JSONB content column.

sql
-- Create an index for lightning-fast search
CREATE INDEX idx_nodes_search ON nodes USING GIN (search_vector);
When the user searches, you query:

sql
SELECT id FROM nodes WHERE search_vector @@ plainto_tsquery('english', 'user search term');
Because the client already has the topology loaded in memory, the server only needs to return a list of ids. The client can then instantly highlight where those IDs exist in the tree, and fire off async requests to load the payloads for the top 10 results.

Factor 5: Concurrency and Conflict Resolution
Since you have local edits, what happens if Client A and Client B edit the same node while offline, and both sync to the server?

Because you are using an operations_log, you have a few easy ways to handle this:

Last-Write-Wins: Just apply the operations in the order they reach the server.
Optimistic Concurrency: Include a version_number with the edit. If the server's node version is newer than the client's, the server rejects the edit and tells the client to reload that node.
CRDTs (Conflict-free Replicated Data Types): If you are using Yjs or Automerge on the client, the payload in your database is just a binary blob representing the CRDT state, and you let the math resolve the merges.
Summary Architecture
Client connects: Fetches SELECT * FROM edges (loads Topology).
Client renders: Identifies the expanded nodes, fetches SELECT * FROM nodes WHERE id IN (...) (loads initial Payloads).
User edits: Client instantly updates local F# map, and sends an Operation to the server API.
Server processes edit: Saves to operations_log, applies the change to nodes/edges, and updates the search index.
User searches: Client searches locally loaded payloads instantly, while server does a GIN-indexed Postgres search across all nodes.
Postgres is robust, handles JSON beautifully, and eliminates the need for 3 different pieces of infrastructure.

Gemini 3.1 Pro
suppose we change to 1 million nodes. Does anything significantly change?


Thought Process
expand
Yes, moving from 100,000 to 1,000,000 nodes introduces a major architectural shift—but specifically for the client, not the database.

Postgres will handle 1,000,000 rows without breaking a sweat (it routinely handles billions). The database architecture I described remains exactly the same.

However, the client-side strategy of "Keep the entire Topology in RAM" completely breaks down. Here is exactly what changes and how you must adapt.

1. The Math: Why the Topology Strategy Fails
At 100k nodes, your topology (IDs and edges) was ~2MB.
At 1,000,000 nodes, assuming UUIDs or long string IDs:

The raw JSON array of edges over the network will be 50MB to 100MB.
Parsing 100MB of JSON on the browser's main thread will freeze the UI for several seconds.
Inserting 1 million items into an F# Immutable Map (an AVL tree under the hood) creates millions of object references. The memory footprint of the Map structure alone will swell to 150MB+, and garbage collection pauses will become noticeable.
2. The Big Change: Paging the Topology
You can no longer download the whole graph structure on boot. You must now treat Topology the exact same way we treated Payloads: you page it in and out based on user navigation.

Boot sequence: The client asks the server only for the root nodes and their immediate children.
Expansion: When a user clicks to expand a subtree, the client requests the topology for that specific subtree, followed by the payloads.
Your F# model changes slightly. A node reference now has three states:

fsharp
type NodeState =
    | FullyLoaded of Topology * Payload
    | TopologyOnly of Topology // We know it exists, and we know its children, but no data
    | Ghost // We know the ID exists (because a parent referenced it), but nothing else
3. Search Changes: "Path-Aware" Search
In the 100k model, search was easy: the server returned an ID, and because the client had the whole topology, it instantly knew where that ID lived in the tree and could draw a line to it.

At 1M nodes, if the server returns ID: 999, the client might have no idea where 999 is. It doesn't have the parent edges in memory.

The Solution:
When the server does a full-text search, it must return the path from the root to the matching node, not just the ID.

Fortunately, Postgres is uniquely built for this. Because your graph is mostly a tree, you can use a Recursive CTE (Common Table Expression) or the Postgres ltree extension.
When a user searches "Revenue 2024", Postgres returns:
[ "root_id", "finance_node_id", "q1_node_id", "revenue_2024_id" ]

The client receives this array, injects those edges into the local Topology map, and can instantly expand the tree down to the search result.

4. Client-Side Caching Becomes Mandatory (IndexedDB)
At 100k nodes, we said IndexedDB was optional because server API calls for payloads are fast. At 1M nodes, because you are now fetching both topology and payloads constantly as the user navigates, relying purely on the network will ruin the "instant" feel.

You must use IndexedDB as a Level 2 cache.

User expands a node.
Client checks local F# Map (RAM). Miss.
Client checks IndexedDB (Disk). Hit? Load into RAM.
IndexedDB Miss? Fetch from Postgres via Server API.
5. F# Garbage Collection Tuning
Because F# immutable Maps recreate the path to the root on every update (O(log N)), inserting thousands of nodes during a mass-expand or search operation will create a lot of short-lived objects. In JS/WASM environments, this triggers heavy browser GC.

To mitigate this:

Batch updates: Don't update the F# model one node at a time. If an API call returns 500 nodes, merge them into the Map in a single bulk operation.
Consider a flat structure for the viewport: While the global cache is an F# Map, the data structure driving the actual OnScreenNodes view might need to be flattened into a standard array to ensure rendering the UI runs at 60 frames per second.
Summary of the 1 Million Node Shift:
Postgres stays the same. (But use Recursive CTEs or ltree for queries).
Topology is no longer fully loaded. It is fetched on demand, just like payloads.
Search must return paths. The server must tell the client how to find a search result in the tree.
IndexedDB is required. To maintain the instant feel while navigating 1M nodes, local disk caching is no longer optional.
Gemini 3.1 Pro
I'm imagining now a document oriented model, where there are links between documents, but most are within their document. A document typically maxes out at 1000 nodes; many will be smaller, a few larger, but a user could split a large document into pieces. If you load some documents, and include adjacent documents the total won't approach the full load.


Thought Process
expand
This is a dramatic and positive simplification. You've essentially shifted from a random-access graph to a "Graph of Documents" model, where each document is a small, self-contained subgraph. This is the right mental model for a scalable web app—it mirrors how tools like Notion, Roam Research, or modern CAD systems work.

Here is exactly how this changes your architecture, and what you must now prioritize.

1. The New Memory Model: Documents as GC Roots
Your "garbage collector" no longer runs on individual nodes. It runs on documents.

fsharp
type DocumentId = string

type DocumentState =
    | Loaded of DocumentContent
    | Loading // In-flight network request
    | Unloaded // Not in memory; we only have its metadata

type DocumentContent = {
    Nodes: Map<NodeId, Node>
    AdjacentDocIds: Set<DocumentId> // Links to other documents
}

type Model = {
    Documents: Map<DocumentId, DocumentState>
    VisibleDocIds: Set<DocumentId> // Documents currently displayed
}
Loading Rule: When a document is in VisibleDocIds, load it and its adjacent documents (e.g., depth 1). This creates a "working set" of typically <10 documents = <10,000 nodes. Trivial memory.

Eviction Rule: When a document has been out of VisibleDocIds for some time (e.g., 2 minutes), unload it.

2. Persistence: Split into Two Postgres Tables
Your "replayable operations" now have two layers:

Table: document_operations_log (Event Sourcing)

seq_id (BigSerial)
document_id (String)
operation (JSONB - the edit)
This is the only table that receives writes. When a user edits a node inside "Document A", you append one row here. This is append-only, so writes are lightning fast (sequential disk I/O).

Table: documents (Read Model)

id (String, Primary Key)
content (JSONB - the entire serialized document, 1000 nodes max)
adjacent_doc_ids (JSONB array of strings)
search_vector (TSVector - full text index of all node text in this document)
When the server processes a new document_operations_log entry, it rebuilds the entire document by replaying its operations and overwrites the row in documents. Because documents are small (max 1000 nodes), this replay is sub-millisecond.

3. Text Search: Two-Tier Instant Response
Tier 1 - Instant Local: Search only the Loaded documents. Because you have at most 10,000 nodes in RAM, this is synchronous and instant. Results are hyper-relevant to what the user is looking at.

Tier 2 - Global Document Search: The server searches across the documents table using the search_vector GIN index. It returns matching document_ids, not node IDs. The client then loads those documents (if not already loaded) and re-runs the local search inside them to highlight the exact node.

This is critical: Do not try to return node-level search results across the entire corpus. Return document matches, then search inside the document.

4. How Adjacent Documents Save You
Suppose a user is viewing "Project Plan" (Doc A). It links to "Budget Spreadsheet" (Doc B) and "Meeting Notes" (Doc C). You load all three. If the user clicks a link inside Doc C to "Q1 Goals" (Doc D), you:

Instantly add D to VisibleDocIds
Load D from Postgres (and its adjacent docs)
Unload A after a timeout (since it's no longer visible or adjacent)
The graph of documents is much smaller—maybe 1000 documents instead of 1,000,000 nodes. You could even keep the entire document adjacency graph (the documents.adjacent_doc_ids column) in memory on the client without issue.

5. The Real Challenge: Cross-Document References
If Node X in Document A links to Node Y in Document B, your operations must store the reference as DocumentB.NodeY, not just NodeY. When you load Document A, you must also load Document B to resolve that link. This is your "adjacent" heuristic.

Consider: How do you handle a link to a node in an unloaded document?

Option A: Show it as a "ghost" link. Clicking it loads the target document.
Option B: When loading a document, lazily fetch just the target node name from adjacent documents to render the link text, without loading the entire document.
6. Summary: What This Model Buys You
Problem	1M Node Flat Graph	Document-Oriented
Initial Load	50-100MB topology	2-5MB adjacency graph
Memory Pressure	GC on 1M objects	GC on 10k objects
Search	Complex, needs paths	Simple: local + global
Persistence	Complex node-level ops	Simple document-level ops
Offline/Cache	Mandatory IndexedDB	Optional (small working set)
One final question before you commit: When a user splits a document, what happens to the replayable operations? Do they:

Get partitioned between the two new documents?
Stay in the original document's log, and you just copy a snapshot to the new document?
Your answer determines how you handle document splitting in the document_operations_log table.

Kimi K2
I had not anticipated a model of having separate operations per document. Not clear how that can work. They do need to be synchronized because deleting a node from A affects references to that node in B.


Thought Process
expand
You're absolutely right—that was a flawed suggestion. If operations in Document A can affect Document B (like deleting a node that B references), you cannot partition the operations log by document. You need a single, global, ordered event stream that is the source of truth for the entire graph.

But you can keep the document-oriented loading model. This is a classic CQRS + Event Sourcing pattern: the write side is global; the read side is partitioned.

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
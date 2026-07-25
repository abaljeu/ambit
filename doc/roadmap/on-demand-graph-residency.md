# On-demand graph residency

Category: Workspace scale
Status: Planned — prerequisite incremental PostgreSQL projection is done; residency slices not started
See also: [[lazy-load]], [[workspace-scale-file-and-db-management]], [[postgres-roadmap]], [[future-merge-sync]], [[doc/current/persistence-model]], [[doc/current/sync-mvp]], [[src/Shared/DocumentPartition.fs]]

Authority for document-scoped server/client residency, bootstrap/load APIs, per-document versions, projection patches, hybrid search, and passive reclamation. Supersedes the earlier commitment to keep all topology resident ([[postgres-roadmap]] §5). Current full-graph bootstrap and sync remain authoritative in [[doc/current/persistence-model]] and [[doc/current/sync-mvp]] until each slice lands.

## What it gives you

- Server startup reads schema, graph revision, and root metadata only; cost is independent of total node count.
- Client startup receives ROOT's document plus restored navigation needs, never the server's entire warm cache.
- Missing children are explicit state (`Unknown | Loaded`), never interpreted as an empty list or a missing node.
- Each document has an independently checked version while the global change sequence remains available for audit and catch-up.
- Search over unloaded workspaces queries PostgreSQL without hydrating the server cache.

## What it avoids for now

- Change-scoped [[src/Server/DocumentPersistence.fs]] so disk projection no longer inspects every document (separate workstream). Before switching the server to partial residency, make its call contract partial-safe so unloaded documents are never interpreted as absent or deleted.
- Incremental in-memory maintenance of `parentByChild`, `ownerParentByChild`, and derived owner fields ([[src/Shared/GraphBuild.fs]], [[src/Shared/GraphMutate.fs]], [[src/Shared/History.fs]]) unless loaded-closure rebuild cost becomes material. Residency may rebuild indexes over a bounded loaded document closure initially.
- Server weighted eviction until measurement shows need; database-backed search must bypass cache admission so scans cannot evict interactive documents.
- IndexedDB / offline startup cache unless offline startup becomes a requirement.
- Annotation migration when files change (still later scale work under [[lazy-load]] / [[workspace-scale-file-and-db-management]]).

## Established baseline and boundaries

- Incremental PostgreSQL graph projection is complete in [[src/Server/DatabaseProjection.fs]] and [[src/Server/DbAgent.fs]]: accepted operations append their change and mutate only planned node/child rows in the same transaction. Treat this as an established prerequisite, not a residency delivery item.
- Full-graph `Graph.fromNodes` index/owner rebuilding remains unsolved as a separate optimization from residency correctness.
- [[src/Server/DocumentPersistence.fs]] still walks and serializes every Current document before content-diff skips. Change-scoped disk projection remains a separate workstream.

## Residency model

- Use existing Special Workspace/Directory/File nodes as document roots; use the root NodeId as the document identity unless a later requirement demands a separate ID. Membership follows Owner edges and stops at nested document roots, matching [[src/Shared/DocumentPartition.fs]].
- A nested document root is a lightweight boundary header in its parent's package; its complete children belong to its own document. `DocumentState.Current/Unparsed` remains source freshness and must stay separate from residency.
- Represent authoritative child completeness as `Unknown | Loaded`; keep transient `Loading` request/cache status outside the graph data. Graph queries and planners return `NeedsDocuments` when their required closure is absent rather than treating it as empty or invalid.
- Server cache admission is whole-document and lazy-on-touch. Loading a document for a client also warms it for the client's likely edit; coalesce concurrent loads, retain client-interest leases briefly, prefetch one boundary hop at low priority, and initially avoid server eviction.
- Client keeps whole loaded documents, preloads documents rooted at rendered Special nodes plus viewport/navigation lookahead, and retains only boundary headers and document descriptors outside that set. Do not keep global topology resident.

## Minimal state / API / ops

### Durable metadata and SQL loaders

- Add durable document metadata sufficient for bounded reads: document root/version, node-to-document membership, owner parent, and workspace scope. Backfill it from the existing projection and maintain it through the existing operation-derived SQL planner.
- Add SQL queries that load one complete document plus nested-root boundary headers and that load bootstrap descriptors without reading all node rows. A document query must stop at nested Special roots.

### Operation path

- Before server apply, resolve the operation's document dependency closure, load and pin it, run pure Shared validation, commit the existing incremental projection changes and all touched document-version increments atomically, then update or invalidate cached documents.
- Move global invariants such as artifact-name uniqueness and backlink/occurrence lookup to indexed SQL checks before validation stops depending on a fully resident graph.

### API and synchronization

- Replace full `GET /state` bootstrap with a document/bootstrap endpoint that returns graph sequence, document versions, requested complete document packages, and boundary headers. Batch requests and support known-version/unchanged responses.
- Keep one global ordered change sequence for audit and catch-up, but stop using exact global revision equality as the conflict boundary for unrelated workspaces. Submissions carry affected document base versions; cross-document changes check all touched documents atomically and later feed server-authoritative rebase.
- Have the server emit canonical per-document projection patches (`upsert node`, `replace children`, `remove node`, version change) plus affected document IDs. Resident clients apply a matching patch; unloaded clients advance only the descriptor version; a version gap invalidates and refetches that document. Pending edits and in-flight operations pin their documents.

### Search

- Preserve current synchronous loaded-document search and focus-first ordering from [[src/Shared/ViewModelSearch.fs]].
- Add asynchronous server search scoped by current document/workspace by default, with explicit broader scope and result caps. Preserve current case-insensitive substring semantics with an appropriate PostgreSQL text index; handle RefExpr/path lookup through owner/document metadata rather than cache traversal.
- Return node header, document root, document version, and owner breadcrumb. Selecting a remote hit hydrates its document and framing path before navigation.
- Treat unparsed source-file content as a separate search phase: return file/line hits and parse on selection, because stable inner node IDs do not exist before parsing.

### Passive reclamation

- After correctness without eviction, add a byte-budgeted document LRU/clock on the client. Pin rendered/expanded documents, viewport lookahead, zoom ingress, selection/edit state, pending/undo dependencies, clipboard dependencies, search navigation, and in-flight requests.
- Eviction removes document bodies and child lists but retains boundary headers, versions, fold/session intent, and stale markers. Evict only clean acknowledged state.

## Implementation steps

1. Durable document descriptors, membership, and versions plus a scoped SQL loader in [[src/Server/Database.fs]] and [[src/Server/DatabaseProjection.fs]]. Prove one document load stops at nested Special roots, version increments share the accepted-change transaction, and descriptor/bootstrap query cost is independent of total node count.
2. Shared explicit-residency model and `NeedsDocuments` outcomes in [[src/Shared/Model.fs]], [[src/Shared/GraphQuery.fs]], and [[src/Shared/History.fs]]. Port operation families incrementally, starting with render/edit/Replace, and prove unknown children cannot be mistaken for empty children.
3. Server document cache and bootstrap/load APIs through [[src/Server/Api.fs]] and [[src/Server/RouteRegistration.fs]]. Switch startup from full projection loading only after partial-safe disk persistence is guaranteed; prove concurrent requests coalesce and a client-loaded document remains warm for its edit.
4. Per-document base-version checks and projection patches in sync. Prove unrelated workspace changes neither block an edit nor force hydration, and cross-document changes remain atomic.
5. Client on-demand load/prefetch in [[src/Client/Program.fs]], [[src/Client/App.fs]], and [[src/Shared/ViewModelSiteMap.fs]]. Prove restored zoom/folds load only their frontier and commands wait/retry on missing dependencies.
6. Hybrid workspace-scoped search and search-result hydration. Preserve existing search tests and add unloaded-workspace/result-cap/version-race cases.
7. Client passive reclamation under a measured byte budget, followed only if needed by server eviction.

## Tests

- SQL loader: one document stops at nested Special roots; nested roots appear as boundary headers only.
- Descriptor/bootstrap queries stay O(documents touched), not O(all nodes).
- Document version increments commit in the same transaction as accepted projection changes.
- Shared graph queries treat `Unknown` children as `NeedsDocuments`, never as empty.
- Concurrent document loads coalesce; client-touch keeps the document warm for the follow-on edit.
- Unrelated workspace edits do not conflict on global revision alone and do not force hydration.
- Cross-document ops require all touched document base versions and apply atomically.
- Client restore of zoom/folds loads only the needed document frontier.
- Search: loaded-document path unchanged; unloaded workspace hits via SQL with caps; selecting a remote hit hydrates before navigate; unparsed file hits stay file/line until parse.

## Separate follow-up tracks

- Change-scope [[src/Server/DocumentPersistence.fs]] using accepted-operation impact so disk projection no longer inspects every document; optimize full `readAllDocuments` only if file bootstrap remains a supported authority path.
- Incrementally maintain `parentByChild`, `ownerParentByChild`, and derived owner fields if loaded-closure rebuild cost becomes material.

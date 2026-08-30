# Define search across selective residency

Type: grilling
Status: resolved
Blocked by: 03, 04

## Question

What searchable universe and traversal should Find use when the client graph is partial, what may a result reveal about unloaded nodes or scopes, and what exact loading and framing must selecting such a result complete before navigation?

## Answer

- Find remains a client-resident search. It preserves the current query, matching, paging, zoom-first then ROOT breadth-first traversal, and node-identity deduplication, but traverses children only from `Loaded` lists and never loads or queries the server merely to discover more results.
- The first occurrence that discovers a node wins deduplication. A result preserves that discovery occurrence path, including `Ref` ingress, and committed selection frames through it rather than reconstructing the node's canonical owner path.
- Every result keeps its current name and text and adds a non-interactive node-state indicator: hollow circle for `Unknown` children, solid circle for `Loaded []`, and collapsed chevron for a non-empty `Loaded` list. It reveals no facts about unseen descendants.
- Merely highlighting a result has no side effects. Committing a result whose child list is already `Loaded` immediately applies the existing parent-versus-leaf framing through its discovery occurrence.
- Committing a result whose child list is `Unknown` exits Find, requests `ArtifactClosure` for that node, and leaves the current frame visible. The server resolves its canonical owning artifact; a current successful load resumes the same framing intent through the preserved discovery occurrence and re-evaluates whether the node is a parent or leaf.
- Load failure abandons navigation, leaves the frame unchanged, and uses normal command-result feedback. Snapshot installation and stale-intent handling otherwise follow the established load protocol and navigation semantics.

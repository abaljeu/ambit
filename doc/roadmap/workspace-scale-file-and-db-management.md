# Workspace scale file and db management

See also: [[doc/roadmap/workspace-scale-import.md]], [[doc/roadmap/git-sync-gateway.md]], [[doc/roadmap/workspace-file-persistence.md]], [[doc/roadmap/workspace-file-model.md]]

This document is the **umbrella vision** for repo-scale outliner behavior (lazy materialization, residency, queries, identity). **Rollout** is sliced in [[doc/roadmap/workspace-scale-import.md]]; do not treat everything here as slice 1. This file was created from discussions without looking at gambol sources — terms and details may need adaptation. `Repo` maps directly onto this project's concept of `workspace`.

## Rollout

Committed sequencing (authoritative detail in linked docs):

1. **Stage 7 `DataDir`** — `{DataDir}/{label}/` live-save and path moves — **done** ([[doc/current/workspace-stage-plan.md]] §7).
2. **Slice 1** — repo tree in outline, expand-to-parse, autosave, local `git commit`, stale on external change ([[doc/roadmap/workspace-scale-import.md]] § Slice 1).
3. **Slice 2** — desktop clone sync via git gateway: pull (server JIT commit first), push (server clean, fast-forward only), client-side merge ([[doc/roadmap/git-sync-gateway.md]]).
4. **Later** — sections below marked *deferred*: server lazy DB residency, client file LRU, query model, annotation migration, git object model in the outline.

Slice 1 can use disk + graph path nodes without the full DB materialization model in this doc. Long-term, repo metadata and parsed nodes live in PostgreSQL as described here.

## Core goal

You want **transparent repo browsing/editing** inside the outliner:

- Git repo appears as an outline tree.
- Files can be expanded into parsed outline structure.
- Editing outline nodes writes immediately back to source files.
- Commit is manual at repo root; server **JIT commit** only before serving pull (slice 2).
- Git handles merge and conflict resolution on the **client**; the server git gateway does not merge.
- The outliner preserves node identity, links, annotations, and roundtripping as much as possible.

---

## Main architectural decision

Use a **lazy materialization model**.

The repo exists in the database, but most of it does not need to be parsed into full outline nodes until touched.

### Universal materialization states

These apply conceptually to both files and nodes.

1. **Stub**
   - Known to exist.
   - Minimal metadata only.
   - No full content.
   - No materialized children.

2. **Loaded**
   - Raw content available.
   - Not necessarily parsed into outline children.

3. **Parsed**
   - Content has been parsed.
   - Durable child nodes exist in the database / graph.
   - Node IDs are real and stable.

Plus an orthogonal flag:

4. **Stale**
   - Materialized outline data may no longer match the source file.
   - This is not a separate state.
   - A parsed file can be parsed-but-stale.
   - A loaded file can be loaded-but-stale.

---

## Repo representation

A repo root is a special directory node.

Under it:

- Directories are represented structurally.
- Files are represented as file nodes.
- `.git` is skipped.
- Gitignored files are not auto-imported.
- File path, mtime, hash, size, and format hint are tracked.

The repo boundary matters for:

- Commit command.
- Pull / push (git sync between server `DataDir` and desktop clone).
- Gitignore scope.
- Activation/residency (*deferred* beyond slice 1).
- Search/query scoping (*deferred*).

The file boundary matters for:

- Loading.
- Parsing.
- Serialization.
- Autosave.
- Client/server residency.
- LRU unloading.

---

## Database scale expectation

Millions of rows in Postgres are acceptable.

The issue is less total row count and more:

- avoiding whole-tree loads,
- avoiding unbounded recursive traversal,
- avoiding full-table scans for common queries,
- avoiding reparse churn on every small edit,
- keeping server/client working graphs bounded.

So the design assumes:

- parent/child indexed access,
- one-level expansion as the normal UI path,
- recursive subtree traversal only for explicit operations,
- search/query caps,
- stale marking instead of immediate reparsing.

---

## Server-side memory management

*Deferred beyond slice 1 — policy target for repo-at-scale.*

Current server loads the whole DB into memory.

Repo-scale version should change to:

- core outline may remain always resident for now,
- repos become lazy-loaded,
- server loads repo/file/node data on first touch,
- no aggressive eviction needed initially,
- daily reboot acts as a simple cleanup/reset,
- low-GB memory is acceptable if the touched working set remains modest.

So:

> Server policy: lazy-on-touch, no LRU for v1.

---

## Client-side residency memory management

*Deferred beyond slice 1 — policy target for repo-at-scale.*

Client has same logical data model as server, but stricter memory constraints.

Client policy:

- Load nodes/files on demand.
- Use file as the main LRU unit.
- Evict parsed file contents/subtrees when not recently viewed.
- Keep lightweight node stubs for visible links, backlinks, query results, or recently referenced nodes.

So a file can be evicted, while specific node stubs inside it remain known enough to display a title/path/preview and rehydrate on click.

> Client policy: file-level LRU, with node stubs retained for references.

---

## Editing model

Autosave is already present.

Therefore:

- editing an outline node immediately serializes back to its source file;
- the source file is the active working-tree artifact;
- commit is not save;
- commit is just `git commit` over the repo tree.

This keeps repo use transparent:

- user edits outline,
- file updates,
- git sees changed file,
- user commits manually.

---

## Commit and sync model

Authoritative wire protocol: [[doc/roadmap/git-sync-gateway.md]]. Summary:

**Commit cadence**

1. **Manual** `git commit` at repo root (desktop or server UI) — autosave is not commit.
2. **Server JIT commit** before serving pull — flush graph to disk, then `git commit` if the work tree is dirty (`gambol: autosave before pull`).
3. **Desktop commit** before push — user commits locally, then `git push origin`.

**Pull (server → desktop)**

1. Gateway flushes `DocumentPersistence` for the workspace.
2. JIT commit on server if needed.
3. Desktop runs `git pull origin`; merge/rebase and conflicts are handled locally with git.
4. Changed files are marked **stale** in the outliner; reparse on expand or explicit action.

**Push (desktop → server)**

1. Desktop pushes only after local commit.
2. Server **rejects** if its work tree is dirty or the push is not **fast-forward**.
3. No server-side merge — client must pull, merge locally, and push again.

Multi-machine file coherence therefore collapses into the existing “external file changed” problem (stale flags), not graph merge at the server.

HTTP change batches ([[doc/current/sync-mvp.md]]) remain for live editing; git pull/push is explicit coarse sync between machines (slice 2).

---

## Stale handling

Stale means:

> “The durable node tree may not match the current file content.”

Sources of staleness:

- external editor changes a file,
- import operation changes files,
- git merge/pull/sync changes files,
- branch operations someday,
- automation writes files.

For now:

- do not eagerly reconcile all stale files,
- mark affected file node/subtree stale,
- display stale indicator,
- reparse when user expands/views/queries that file.

This defers hard annotation/link migration until necessary.

---

## Parsing and roundtripping

Current parser model:

- newline breaks,
- tab/indent detection,
- markdown heading/block parsing,
- CSS block/property parsing,
- future statement/block parsing.

A parsed file produces durable nodes.

Each node can track:

- node ID,
- file path / file ID,
- character range,
- parser/format info,
- serialized layout details where needed.

Serializer uses the parsed structure plus captured layout details to roundtrip the source file.

---

## Query model

*Deferred beyond slice 1.*

Queries must return **node IDs**.

Because of that:

- DB-graph hits can return existing node IDs directly.
- File-level scan hits need to hydrate enough to produce node IDs.

Since partial parse may not be viable, the chosen approach is:

> Query scans file content first. For files with hits, parse the whole file, then return matching node IDs.

With a result cap, e.g.:

- 25,
- 50,
- 100 max.

This bounds the worst case.

Flow:

1. Query starts within a scope, probably repo or subtree.
2. For current parsed/non-stale nodes, query graph directly.
3. For stale/unparsed files, scan raw file content.
4. If a file has hits, parse that file.
5. Return node IDs.
6. Stop at cap.

This means a broad query may load content for many files but only parses files that actually contribute results.

---

## Node identity and annotations

Node IDs are fixed and meaningful.

The system supports:

- linking to nodes,
- editing linked nodes,
- database annotations pointing at document parts,
- Obsidian-like cross-linking patterns.

Durable sub-file nodes are therefore important.

The hard future issue:

> If file F changes, node N may still exist, but the text/range it represented may have moved or changed.

For now:

- node identity remains fixed,
- stale flags indicate possible mismatch,
- historical data can be used later,
- reconciliation/migration of annotations can be deferred,
- files with important annotations may eventually need pinning or higher-priority reconciliation.

No need to solve full semantic node correspondence yet.

---

## Git object model

Not part of v1.

Current reflection is:

- repo as directory tree,
- files as nodes,
- content as parsed outline structure.

Future possibility:

- expose commits,
- blobs,
- trees,
- branches,
- diffs,
- object graph.

But for now, `.git` is skipped and git is treated as external machinery for commit/merge.

---

## Current recommended skeleton

The design I would now write down as the working plan:

```text
Repo root
  ├─ directory nodes
  ├─ file nodes: stub / loaded / parsed
  │    └─ parsed child nodes: durable, linkable, annotatable
  └─ .git skipped
```

Each file has:

```text
path
mtime
hash
size
format hint
materialization state
stale flag
gitignore/import status
```

Each parsed node has:

```text
node_id
parent_id
file_id
path/range
display text
content/layout data
stale inherited from file/subtree
```

Policies:

```text
Server:
  lazy load on touch
  no eviction initially
  daily reboot clears memory

Client:
  load by file on demand
  LRU unload by file
  retain referenced node stubs

Queries:
  return node IDs
  use graph when current
  scan files when unparsed/stale
  parse hit files
  cap results

Editing:
  autosave to source file immediately

Commit:
  manual at repo root
  server JIT commit before pull (slice 2)
  desktop commit before push (slice 2)

Sync (slice 2):
  pull: server flush + JIT commit, client git pull, mark stale
  push: FF only, server work tree must be clean
  merge on client only

External changes:
  mark files/subtrees stale
  defer reparse
```

---

## The next concrete decision

Slice 1 scope: nail the **file node lifecycle** ([[doc/roadmap/workspace-scale-import.md]]). For example:

```text
discovered stub
  -> loaded from disk/db
  -> parsed into children
  -> edited
  -> serialized/autosaved
  -> externally changed
  -> marked stale
  -> reparsed
```

That lifecycle defines metadata and commands for slice 1. Slice 2 adds pull/push on top once `DataDir` and slice 1 behaviors exist.
# Workspace Scale Import

See also: [[doc/roadmap/workspace-scale-import-slice1-plan]], [[doc/roadmap/workspace-scale-file-and-db-management]], [[doc/roadmap/git-sync-gateway]], [[doc/roadmap/workspace-format-amb]], [[doc/roadmap/workspace-format-md]], [[doc/roadmap/workspace-format-plain]], [[doc/roadmap/workspace-format-code]]

Sequencing: **Slice 1** (outliner ↔ files on one machine) then **Slice 2** (git pull/push to a desktop clone). Slice 1 implementation lock: [[doc/roadmap/workspace-scale-import-slice1-plan]]. Slice 2 implementation lock: [[doc/roadmap/workspace-scale-import-slice2-plan]]; protocol detail in [[doc/roadmap/git-sync-gateway]].

# Slice 1: repo file-tree browsing + on-demand parse/edit for individual files

Authoritative detail for ownership-derived paths, shallow sync rules, metadata, commands, and tests: [[doc/roadmap/workspace-scale-import-slice1-plan]]. This section keeps the product overview.

Not full repo-scale querying, not stale reconciliation after pull, not multi-client graph merge yet.

## What it gives you

The user can:

1. Attach/import a git repo root.
2. See the repo’s directory/file tree in the outline.
3. Ignore `.git` and gitignored files.
4. Expand a file node.
5. On expansion, parse that one file into child nodes.
6. Edit those children.
7. Autosave writes the source file.
8. Run manual `git status` / `commit` from the repo root.

That already delivers the core promise:

> “I can browse and edit a real repo through the outliner.”

## What it avoids for now

Defer:

- full content indexing,
- repo-wide graph queries,
- stale reparse sophistication,
- annotation migration,
- client LRU,
- partial hydration,
- multi-client merge handling,
- branch switching,
- git object model,
- server-wide memory management beyond not parsing everything.

## Minimal state model

For this slice, you only need:

```text
Special File / Directory node:
  owner chain defines path
  kind = file or directory
  repo_root_id
  mtime
  parsed: bool
  stale: bool
```

Maybe also:

```text
hash
size
ignored/skipped marker
format hint
```

But you can start without all of it if mtime is enough.

## Key behavior

### Repo attach/import

Create owned graph stubs for the **directory structure and file nodes only**, under Workspace owners, including ROOT, or Directory owners that mirror disk.

Do not parse file contents.

```text
repo root
  src/
    main.cs
    utils.cs
  README.md
  package.json
```

At this point, files are graph nodes whose path is derived from the Workspace owner chain, including ROOT, plus Directory ownership.

### Expand file

When the user expands a file node:

```text
if parsed == false:
    read file from disk
    parse file
    create child nodes
    parsed = true
    stale = false
```

Then normal outliner behavior takes over.

### Edit child node

When a parsed child changes:

```text
serialize containing file node
write file to disk
update file mtime/hash
stale = false
```

### Repo commit

At repo root:

```text
git status
git add relevant files
git commit
```

You can keep this crude initially.

## Why this slice is valuable

It tests the highest-risk assumptions early:

1. **Does repo-as-outline feel good?**
2. **Does on-demand file parsing feel transparent enough?**
3. **Does autosave-to-file work reliably?**
4. **Does your serializer tolerate real source files?**
5. **Does the UI remain responsive with a medium repo when only tree nodes are loaded?**

Those are more important than query/indexing at first.

## I would include one small stale feature

Even in slice 1, add this:

On file expansion, compare current disk mtime/hash to stored mtime/hash.

```text
if parsed && disk newer:
    mark stale
    prompt or button: "Reparse from disk"
```

Do not auto-reconcile yet.

That gives you safety if someone edits the file outside the outliner.

## Success criterion

This slice is successful if you can:

- attach a medium repo,
- browse the tree,
- open/edit/save a handful of files,
- commit changes,
- restart the server,
- reopen the repo,
- and nothing surprising happens.

That is a useful product even before repo-wide search or advanced sync exists.

# Slice 2: git sync to desktop (git gateway)

See [[doc/roadmap/git-sync-gateway.md]] for protocol, credentials, and server module boundaries.

## What it adds to slice 1

- Same repo at `{DataDir}/@{label}/` on the server and a local clone via desktop workspace mapping.
- **Pull:** server JIT commit if dirty, then `git pull origin`; client merge; stale/reparse on changed files.
- **Push:** `git push origin`; server accepts only fast-forward when its working tree is clean.
- Stock git on desktop; dumb git gateway on the server — no server-side merge.

## Prerequisite

Slice 1 behaviors (tree, autosave, local commit, stale on expand). Stage 7 `DataDir` live-save is implemented ([[doc/current/workspace-stage-plan.md]] §7). Slice 2 does not replace HTTP graph sync; it is explicit coarse file sync between machines.
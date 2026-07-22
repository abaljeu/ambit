# Workspace Scale Import

See also: [[doc/roadmap/workspace-scale-file-and-db-management.md]], [[workspace-file-sync]], [[doc/roadmap/workspace-format-amb.md]], [[doc/roadmap/workspace-format-md.md]], [[doc/roadmap/workspace-format-plain.md]], [[doc/roadmap/workspace-format-code.md]]

Worksets: **disk-to-graph stub reconciliation**, **expand-to-parse and freshness UI**, and **workspace file sync**. Transport direction: [[workspace-file-sync]]; canonical Lazy Load project: [[lazy-load]].

## Repo file-tree browsing + on-demand parse/edit for individual files

Not full repo-scale querying, not advanced freshness reconciliation, and not multi-client graph merge. Coarse tree sync uses last-write-wins WebDAV in scope ([[workspace-file-sync]]); live graph editing stays on HTTP change batches.

## What it gives you

The user can:

1. Map a local folder to a workspace label (and/or browse server `DataDir` tree stubs).
2. See the directory/file tree in the outline.
3. Ignore `.git` and gitignored files (via `git check-ignore` on sync).
4. Expand a file node.
5. On expansion, parse that one file into child nodes.
6. Edit those children.
7. Autosave writes the source file.
8. Push / Pull scoped trees between desktop and server when needed.

That already delivers the core promise:

> “I can browse and edit a real file tree through the outliner.”

## What it avoids for now

Defer:

- full content indexing,
- repo-wide graph queries,
- advanced freshness/reparse handling,
- annotation migration,
- client LRU,
- partial hydration,
- multi-client graph merge (out of scope),
- mirror-delete / conflict UI on file sync,
- git object model in the outline,
- server-wide memory management beyond not parsing everything.

## Minimal state model

For repo browsing and on-demand parse, you only need:

```text
File node:
  path
  is_directory / is_file
  repo_root_id
  mtime
  parsed: bool
  freshness: current | unparsed | client_older | client_newer
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

Create graph nodes for the **directory structure and file nodes only**.

Do not parse file contents.

```text
repo root
  src/
    main.cs
    utils.cs
  README.md
  package.json
```

At this point, files are just graph nodes pointing at paths.

### Expand file

When the user expands a file node:

```text
if parsed == false:
    read file from disk
    parse file
    create child nodes
    parsed = true
    freshness = current
```

Then normal outliner behavior takes over.

### Edit child node

When a parsed child changes:

```text
serialize containing file node
write file to disk
update file mtime/hash
freshness = current
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

## Include a minimal freshness check

Include this in expand-to-parse and freshness UI:

On file expansion, compare current disk mtime/hash to the server metadata.

```text
if parsed && disk differs:
    show client_older or client_newer
    prompt or button: "Reparse from disk"
```

Do not auto-reconcile yet.

That gives you safety if someone edits the file outside the outliner.

## Success criterion

This workset is successful if you can:

- attach a medium repo,
- browse the tree,
- open/edit/save a handful of files,
- commit changes,
- restart the server,
- reopen the repo,
- and nothing surprising happens.

That is a useful product even before repo-wide search or advanced sync exists.

## Workspace file sync to desktop

See [[workspace-file-sync]] for WebDAV Class 1, server finish-commit, and `git check-ignore` for `.gitignore`.

## What file sync adds

- Same tree at `DataDir/{label}/` on the server and a mapped local folder (need not be a git clone).
- **Pull:** PROPFIND + GET under scope into the mapped root. Freshness display responds afterward under [[lazy-load]] and treats matching client/server files as current.
- **Push:** WebDAV PUT/MKCOL under scope; server finish-commit via WorkspaceGit; then Lazy Load stub reconcile.
- Desktop uses `git` only for ignore on Push; transport is not pack protocol.

## Boundary

`DataDir` live-save is implemented ([[doc/current/workspace-stage-plan.md]] §7). File sync does not require expand-to-parse / freshness UI, and it does not replace HTTP graph sync; it is explicit coarse file sync between machines. Stub reconciliation after server tree commit is implemented by Lazy Load ([[lazy-load]]); the target trigger is WebDAV push + finish-commit.
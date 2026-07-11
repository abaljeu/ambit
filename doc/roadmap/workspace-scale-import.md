# Workspace Scale Import

See also: [[doc/roadmap/workspace-scale-file-and-db-management.md]], [[doc/roadmap/git-sync-gateway.md]], [[doc/roadmap/workspace-format-amb.md]], [[doc/roadmap/workspace-format-md.md]], [[doc/roadmap/workspace-format-plain.md]], [[doc/roadmap/workspace-format-code.md]]

Worksets: **disk-to-graph stub reconciliation**, **expand-to-parse and freshness UI**, and **Git workspace transport**. Git protocol: [[git-sync-gateway]]; completed transport record: [[workspace-scale-import-slice2-plan]]; canonical Lazy Load project: [[lazy-load]].

## Repo file-tree browsing + on-demand parse/edit for individual files

Not full repo-scale querying, not advanced freshness reconciliation, and not multi-client graph merge. Workspace Git concurrency is handled by fast-forward-only push: a non-current client is rejected and must pull/merge locally before retrying.

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
- advanced freshness/reparse handling,
- annotation migration,
- client LRU,
- partial hydration,
- multi-client graph merge (out of scope; Git push rejects stale/non-FF clients),
- branch switching,
- git object model,
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

## Git workspace transport to desktop

See [[git-sync-gateway]] for protocol, credentials, and locked decisions (including **reject-dirty** push). Completed G0–G7 implementation record: [[workspace-scale-import-slice2-plan]].

## What Git transport adds

- Same repo at `DataDir/{label}/` on the server and a local clone via desktop workspace mapping.
- **Pull:** server JIT commit if dirty, then `git pull ambit`; client merge. Freshness display responds afterward under [[lazy-load]] and treats matching client/server files as current.
- **Push:** `git push ambit`; server accepts only fast-forward when its working tree is clean (**reject-dirty** — no JIT commit on push).
- Stock git on desktop; smart HTTPS git gateway on the server — no server-side merge. Remote name is **`ambit`**.

## Boundary

`DataDir` live-save is implemented ([[doc/current/workspace-stage-plan.md]] §7). Git transport does not require disk-to-graph reconciliation or expand-to-parse / freshness UI, and it does not replace HTTP graph sync; it is explicit coarse file sync between machines. Create-only response after successful receive is implemented by Lazy Load ([[lazy-load]]).
# Workspace Scale Import

See also: [[doc/roadmap/workspace-scale-file-and-db-management.md]]

A good first valuable slice would be:

# Slice 1: repo file-tree browsing + on-demand parse/edit for individual files

Not full repo-scale querying, not stale reconciliation, not multi-client yet.

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
File node:
  path
  is_directory / is_file
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
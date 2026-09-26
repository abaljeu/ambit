# 11 — Migrate Bullet, Included, and bootstrap wants

**Type:** coding
**Status:** blocked
**Blocked by:** [07 — Expand Want and edges/Nodes package](07-expand-want-and-edges-nodes-package.md)

## Context

Bullet, Included, and SiteMap still read Node children and `childrenStatus`. After expand, `Graph.childMap` and visible-closure bootstrap exist beside the old lists. This batch switches those readers so an Unloaded Node shows a hollow-circle Bullet until Children arrive, and Fold still bounds Included.

## What to build

The hollow-circle Bullet follows absent `childMap` or Unparsed. Included walks `childMap` and honors Fold. Production bootstrap uses visible-closure. No new per-Node loading Status. No new command that names Nodes.

### 1. Bullet

Module [Bullet](../arch.md). Story paths 10, 11, 12, 26, 34.

1. [ ] Absent key is hollow Unloaded — 10.2, 24.3: indicator reads absent `childMap` as Unloaded
2. [ ] Unparsed stays hollow — 11.1: Unparsed keeps the same hollow-circle Bullet
3. [ ] Children arrive — 26.1: after install, chevron or solid replaces hollow when Loaded and not Unparsed
4. [ ] Unloaded is not empty — 34.1: absent key never renders as a Loaded leaf
5. [ ] No new loading Status — 12.2: residency stays `childMap` plus Unparsed

### 2. Included

Module [Included](../arch.md). Story path 33.

1. [ ] Walk childMap — 4.2.2: `expand` walks `childMap`, not Node.children
2. [ ] Honor Fold — 33.1: stop at folded children

### 3. Graph childMap and bootstrap

Modules [Graph childMap](../arch.md), [ResidentProjection](../arch.md). Story paths 2, 8, 32.

1. [ ] Production bootstrap — 8.1: first paint uses visible-closure, not complete Workspace
2. [ ] Not a complete Workspace — 8.2: do not call complete-ROOT `rootBootstrapGraph` as the production scope
3. [ ] No new named-Node command — 32.1: silent wants are enough

## See also

[Browser residency architecture](../arch.md), [spec.md](../spec.md) Solution **Hollow-circle Bullet**

## Comments

- 2026-09-26: Filed via `/to-tickets`. Migrate batch. Blocked by expand.

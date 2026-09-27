# 11 — Pick git or desk for plain Load and Save

**Status:** defined
**Blocked by:** None — can start immediately

## Context

A person runs plain Load or Save on a Workspace. The Workspace work tree can have a git remote or no remote. Ambit must choose the Server-git path or the existing desk path without an allowlist, a special label, or another stored setting.

## What to build

### 1. PathPick

Build the pure **PathPick** module from the Module map in [[../arch.md]]. This module supports the **Every Workspace may connect**, **Server-git when a remote exists**, **Plain Load prefers git**, and **Plain Save prefers git** Story paths. It makes only the path choice; later tickets connect that choice to Load and Save.

- [ ] 2.1.1 No durable path state — PathPick stores no state.
- [ ] 2.2.1 Choose git for a remote — `choose` returns Git when `remoteExists` is true.
- [ ] 2.2.1 Choose desk without a remote — `choose` returns Desk when `remoteExists` is false.
- [ ] 2.2.2 No extra gate — The choice does not use an allowlist, Workspace name list, special label, or Server branch map.
- [ ] 2.3.1 Pure choice seam — Tests prove both results without a git process or another dependency.

## See also

- [github-transport architecture](../arch.md)
- [01 — Which Workspaces and remotes](01-which-workspace-labels-and-remotes.md)

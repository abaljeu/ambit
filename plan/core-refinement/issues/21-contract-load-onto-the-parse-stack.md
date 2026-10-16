# 21 — Contract Load onto the Parse stack

**Status:** `defined`
**Type:** coding
**Blocked by:** [26 — Revisit WebDAV parse](../../github-transport/issues/26-revisit-webdav-parse.md)
**Trackers:** architecture, user manual

## Context

Web desk Load of a Workspace Node, Directory Node, or File Node already posts through mailbox Load. A client hop remains for desktop-mapped Load and for the git Load after-step. The git Load after-step is already done. Desktop-mapped Load is still open.

## What to build

The remaining client hop retires. Load of a Workspace Node, Directory Node, or File Node uses the Parse stack.

### Contract

1. [ ] **Mailbox Load stays** — Web desk Load of a Workspace Node, Directory Node, or File Node posts through mailbox Load.
2. [ ] **Client hop retires** — The remaining client hop for desktop-mapped Load and for the git Load after-step is gone.
3. [ ] **Stack is the Load path** — Those Loads use the Parse stack.

### Obligations

1. [ ] **Parse and persist claim** — The Parse and persist claim shows Load on the Parse stack and shows the client hop as retired.
2. [ ] **Load test** — A test shows desktop-mapped Load and the git Load after-step use the Parse stack and do not use the old client hop.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documenattion)), deleting things no longer true and checking off things newly achieved.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement map](plan/core-refinement/map.md)
- [core-refinement](plan/core-refinement/project.md)
- [26 — Revisit WebDAV parse](../../github-transport/issues/26-revisit-webdav-parse.md)
- [22 — Drop the Browser git Load after-step](plan/github-transport/issues/22-drop-browser-git-load-after-step.md)
- [Parse and persist](doc/current/parse-persist.md)

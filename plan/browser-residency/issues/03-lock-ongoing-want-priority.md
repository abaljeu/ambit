# 03 — Lock ongoing want priority and when wants are attached

**Type:** grilling
**Status:** needs-info
**Blocked by:** None

## 1. Question

When does the Browser attach wants to post-Event and Poll, and what happens when the Want list is empty?

Ongoing priority is already locked: (1) Included Nodes that miss Children, (2) those Children. Reserved Nodes and Zoom ancestors are bootstrap, not a third ongoing tier. Auto wants need no click and no command.

Lock:

1. **Attachment moment** — Does every post-Event and every Poll carry the current Want list, or only some of those doors?
2. **Empty wants** — Encoding is locked on [01 — Lock Sync want + edges/Nodes package shape](01-lock-sync-want-package-shape.md): always send `want`; empty compose is `want: []`. Remaining: when no Included Node misses Children, does the Browser still attach that empty Want on every door, or skip some doors?
3. **Cadence** — Is Poll the steady want door, with post-Event as the after-Change door, or do both always carry the same Want? What stops a tight Poll loop from repeating a Want the Server already answered?

## Comments

- 2026-09-26: [01 — Lock Sync want + edges/Nodes package shape](01-lock-sync-want-package-shape.md) is done. Empty-Want encoding is `want: []`. This ticket still locks attachment and cadence.

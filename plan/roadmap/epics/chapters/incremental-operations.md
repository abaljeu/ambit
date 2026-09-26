# Chapter: Incremental operations

**Part of:** [[plan/roadmap/epics/robust-outliner.md]]
**Blocked by:** [[actors-supported.md]].

## Context

A person opens the Browser on a Graph that is already large on the Server. Load of a Workspace into the Browser is one shot. A hang then forces a full redo.

File on disk becomes Graph on the Server. Workspace Download and Workspace Upload move file bytes. They must stay file operations. The Browser must not Parse from those operations.

## Goal

The Browser starts small. It grows only from want. An Unloaded Node shows a hollow circle Bullet. The person opens that Node, or Find picks a hit that is not Resident, and the Browser then Fetches that want.

The Server Parse Actor turns file-shaped disk into Graph and emits Changes. Post-Event and Poll carry those Changes plus the Nodes the Browser wants. File-shaped file→Graph stays on the Server. Workspace Download and Workspace Upload stay file operations. They do not Parse on the Browser or the App.

Graph→Browser is visible-closure: the Browser holds the Included context and the path that frames it, not the whole Server Graph.

## Required for done

- [ ] Browser start is small, not a full Workspace Graph.
- [ ] Growth is want-driven: expand a hollow-circle Bullet, or Fetch when Find picks a hit that is not yet Resident.
- [ ] Server Parse Actor emits Changes. File-shaped reconcile (disk → Graph) stays on the Server and stays file-shaped.
- [ ] Post-Event and Poll carry Changes plus the wanted Nodes.
- [ ] Workspace Download and Workspace Upload stay file operations. Those operations do not Parse on the Browser or the App.
- [ ] Graph→Browser is visible-closure.

## Notes

- Search hydration is in scope: when Find picks a hit that is not yet Resident, Fetch those Nodes before navigate.
- Conflict resolution is already implemented. Do not re-plan it. Strike it as a todo wherever a leftover plan still lists it.
- Do not plan: document partition / membership partition; cache leases; coalesce; LRU / pinning; IndexedDB; per-document versions or patches as delivery; server eviction; partial-residency live-save; parent-index rebuild; client or App stub creation as the Upload structure; client Parse from Upload or Download.
- Full Workspace Upload/Load redesign stays parked relative to the Solid core bar until the Core and Actors Chapters are met. That redesign is not this Chapter. File-channel Upload/Download lives on [[automatic-upload-and-download.md]].
- [[plan/selective-client-loading/project.md]] is a prior client-only slice (complete Workspace, server fully Resident). This Chapter is the later want-driven visible-closure shape and may replace that slice's granularity.
- 2026-09-26 grill folded the useful remainder of leftover on-demand residency and Server file-shaped reconcile into this Chapter. Do not revive the deleted extras.

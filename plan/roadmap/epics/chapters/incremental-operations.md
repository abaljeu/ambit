# Chapter: Incremental operations

**Part of:** [[plan/roadmap/epics/robust-outliner.md]]
**Blocked by:** [[actors-supported.md]].

## Context

A person opens the Browser on a Graph that is already large on the Server. Load of a Workspace into the Browser is one shot. A hang then forces a full redo.

File on disk becomes Graph on the Server. Workspace Download and Workspace Upload move file bytes. They must stay file operations. The Browser must not Parse from those operations.

## Goal

The Browser starts small. It holds visible-closure: the Included context and the path that frames it, not the whole Server Graph.

The Browser grows by auto wants. One Want lists visible Nodes that miss Children, then their Children, then the grandchildren. An Unloaded Node shows a hollow-circle Bullet until that fill. Auto wants need no click and no command. Commands come later.

When Find picks a hit that is not Resident, the Browser Fetches those Nodes before navigate.

Post-Event and Poll carry Changes plus the Nodes the Browser wants.

## Required for done

- [ ] [[plan/browser-residency/project.md]] — visible-closure; auto wants (visible Nodes that miss Children, then their Children, then the grandchildren); hollow until fill; search hydration; post-Event and Poll carry Changes plus wanted Nodes
- [ ] [[plan/parse-thread/project.md]] — continuous Server Parse thread; file-shaped disk→Graph; priority from Browser wants; emits Changes
- [ ] [[plan/transport-layer/project.md]] — file transit stays here; no new transit Project

## Notes

- File transit remains [[plan/transport-layer/project.md]]. That Project will evolve an Actor linked to file transport. Do not create a new transit Project.
- Search hydration is in Browser residency: when Find picks a hit that is not yet Resident, Fetch those Nodes before navigate.
- Server search is present work: [[plan/online-search/project.md]]. Claims: [[doc/current/search-actor.md]] and [[doc/current/query-actor.md]]. It is not a later item of this Chapter.
- Conflict resolution is already implemented. Do not re-plan it. Strike it as a todo wherever a leftover plan still lists it.
- Do not plan: document partition / membership partition; cache leases; coalesce; LRU / pinning; IndexedDB; per-document versions or patches as delivery; server eviction; partial-residency live-save; parent-index rebuild; client or App stub creation as the Upload structure; client Parse from Upload or Download.
- Full Workspace Upload/Load redesign stays parked relative to the Solid core bar until the Core and Actors Chapters are met. That redesign is not this Chapter. File-channel Upload/Download lives on [[automatic-upload-and-download.md]].
- [[plan/selective-client-loading/project.md]] is done (prior whole-Workspace slice). Successor is [[plan/browser-residency/project.md]].
- 2026-09-26 grill folded the useful remainder of leftover on-demand residency into this Chapter. Project homes locked the same day.

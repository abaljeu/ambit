# Projects overview

| Project | Stage | Summary |
| --- | --- | --- |
| [Bullet tip times](bullet-tip-times/) | charting | Which non-obvious time facts a node-marker tooltip should show (update, workspace, server, last-sync) with de-dup, timezone, and availability rules; open questions unresolved. |
| [Download no-parse fix](download-no-parse-fix/) | charting | Download stamp-align must not require Parse; SetUpdateTime is exempt from the unparsed-document gate. |
| [Parse load demote](parse-load-demote/) | charting | Empty stub directory; stage unknown until contents land. |
| [RowView / FocusView layout vs behavior](rowview-layout-behavior/) | charting | Separate layout from behavior inside RowView/FocusView; plan complete, waits on the split-view-by-concern refactor landing. |
| [ChildNode drop ref](childnode-drop-ref/) | spec | Progressive removal of ChildNode.ref; Node.owner + Op.SetOwner sole ownership source; Loaded-scope membership seam; ordered id-only ChildNode retained. |
| [Expression Language](expression-language/) | tickets | Comprehensive spec at spec.md — eleven implementation tickets (15–25) from eval core through ch.11 regression; frontier 15 and 16 on `w/expr`. |
| [Client start time](client-start-time/) | active | Cache-first boot tickets 01–07 implemented (IndexedDB snapshot plus Change log, fold then first paint, boot Poll, truncation, optional bootstrapHash); HITL pending. |
| [Event-sourced ops](event-sourced-ops/) | active | One semantic standard for how any Actor's Change enters a Graph — implementation issues (`issues/01`–`15`) through wire migration, Actor spine, recovery, and permanent global history; charting docs in overview, architecture, and details. |
| [Large-node cursor perf](large-node-cursor-perf/) | active | Selection-only planPatchDOM/patchDOM fast path + O(1) SiteEntry.childIndex implemented; delete-children cost analysis in delete-children-cost.md (no delete fix yet). |
| [Owner-edge database repair](owner-edge-db-repair/) | active | Extend DbAgent startup sweep: ACID repair of `node_children` into a ROOT-owned tree; GC unreachable; promote a Ref when a reachable node has no owner. |
| [Selective client loading](selective-client-loading/) | active | Client-partial residency with explicit Load and Unloaded/Loaded child lists; spec ready-for-agent, implementation issues in flight. |
| [Auto-download persisted files](auto-download-persisted-files/) | blocked | Auto-download and filesize peels delivered; four runtime checks are tabled until the user decides to resume HITL verification. |
| [Fix large change apply budget](fix-large-change-apply-budget/) | done | Nested parse-tail Replace apply stayed on append fast-path; optimized Op.apply / Graph.replace validation so LargeChangeApplyTests stay under 300ms without raising the guard. |
| [Fix SetText SYSTEM css resilience](fix-settext-system-css-resilience/) | done | Cold bootstrap seeds Unparsed File stubs; SetText resilience Parses then edits; DocumentPersistenceTests assert Unparsed File on cold load. |
| [Glossary: Directory File](glossary-directory-file/) | done | Directory File glossary + isMarker/related API renames toward Directory File language. |
| [Load status phases](load-status-phases/) | done | Make Load's three stages visible: Uploading (desktop push), Parsing (disk parse/reconcile), Loading (graph fetch); web skips Uploading. |
| [Login / context restore](login-context-restore/) | done | Auth and cold-reload HITLs passed; owning Workspace and prior Zoom restore after iOS unload. |
| [Node-bullet tooltip](node-bullet-tooltip/) | done | Bullet hover tip (native title) listing self-gated Node facts — Guid tail, residency, workspace path + local desktop path, Update Time (local tz), CSS classes. Shared `bulletTip` + Client `nodeBullet`/CSS rename + local-time formatter + `VM.workspaceRoots` local-path line; delivered, committed, and browser-verified on 2026-08-16. |
| [Relaxed concurrency](relaxed-concurrency/) | done | Build-upon layer — verified Graph/Ops facts, shared rejections, frontier D–F; merge implementation and active standard are [[event-sourced-ops/overview.md]]. |
| [Search zoom select](search-zoom-select/) | done | Restore prior Find zoom framing; on no-children parent fallback, select the search target. |
| [Work board audit](work-board-audit/) | done | Audit WORK claims against source, tests, history, and durable HITL evidence. |

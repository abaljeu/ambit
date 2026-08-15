# Projects overview

| Project | Stage | Summary |
| --- | --- | --- |
| [Bullet tip times](bullet-tip-times/) | charting | Which non-obvious time facts a node-marker tooltip should show (update, workspace, server, last-sync) with de-dup, timezone, and availability rules; open questions unresolved. |
| [Parse load demote](parse-load-demote/) | charting | Empty stub directory; stage unknown until contents land. |
| [RowView / FocusView layout vs behavior](rowview-layout-behavior/) | charting | Separate layout from behavior inside RowView/FocusView; plan complete, waits on the split-view-by-concern refactor landing. |
| [ChildNode drop ref](childnode-drop-ref/) | spec | Progressive removal of ChildNode.ref; Node.owner + Op.SetOwner sole ownership source; Loaded-scope membership seam; ordered id-only ChildNode retained. |
| [Auto-download persisted files](auto-download-persisted-files/) | active | Auto-download into mapped folder implemented; Standards filesize peels delivered (ViewModelSync + UpdateWorkspace* splits; App timer-only). HITL verify remains. |
| [Owner-edge database repair](owner-edge-db-repair/) | active | Extend DbAgent startup sweep: ACID repair of `node_children` into a ROOT-owned tree; GC unreachable; promote a Ref when a reachable node has no owner. |
| [Selective client loading](selective-client-loading/) | active | Client-partial residency with explicit Load and Unloaded/Loaded child lists; spec ready-for-agent, implementation issues in flight. |
| [Fix large change apply budget](fix-large-change-apply-budget/) | done | Nested parse-tail Replace apply stayed on append fast-path; optimized Op.apply / Graph.replace validation so LargeChangeApplyTests stay under 300ms without raising the guard. |
| [Fix SetText SYSTEM css resilience](fix-settext-system-css-resilience/) | done | Cold bootstrap seeds Unparsed File stubs; SetText resilience Parses then edits; DocumentPersistenceTests assert Unparsed File on cold load. |
| [Glossary: Directory File](glossary-directory-file/) | done | Directory File glossary + isMarker/related API renames toward Directory File language. |
| [Load status phases](load-status-phases/) | done | Make Load's three stages visible: Uploading (desktop push), Parsing (disk parse/reconcile), Loading (graph fetch); web skips Uploading. |
| [Login / context restore](login-context-restore/) | done | Auth and cold-reload HITLs passed; owning Workspace and prior Zoom restore after iOS unload. |
| [Node-bullet tooltip](node-bullet-tooltip/) | done | Bullet hover tip (native title) listing self-gated Node facts — Guid tail, residency, workspace path + local desktop path, Update Time (local tz), CSS classes. Shared `bulletTip` + Client `nodeBullet`/CSS rename + local-time formatter + `VM.workspaceRoots` local-path line; delivered and committed. |

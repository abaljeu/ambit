# Projects overview

| Project | Stage | Summary |
| --- | --- | --- |
| [Bullet tip times](bullet-tip-times/) | charting | Which non-obvious time facts a node-marker tooltip should show (update, workspace, server, last-sync) with de-dup, timezone, and availability rules; open questions unresolved. |
| [RowView / FocusView layout vs behavior](rowview-layout-behavior/) | charting | Separate layout from behavior inside RowView/FocusView; plan complete, waits on the split-view-by-concern refactor landing. |
| [ChildNode drop ref](childnode-drop-ref/) | spec | Progressive removal of ChildNode.ref; Node.owner + Op.SetOwner sole ownership source; Loaded-scope membership seam; ordered id-only ChildNode retained. |
| [Auto-download persisted files](auto-download-persisted-files/) | active | Auto-download into mapped folder implemented; Standards filesize peels delivered (ViewModelSync + UpdateWorkspace* splits; App timer-only). HITL verify remains. |
| [Selective client loading](selective-client-loading/) | active | Client-partial residency with explicit Load and Unloaded/Loaded child lists; spec ready-for-agent, implementation issues in flight. |
| [Node-bullet tooltip](node-bullet-tooltip/) | done | Bullet hover tip (native title) listing self-gated Node facts — Guid tail, residency, workspace path + local desktop path, Update Time (local tz), CSS classes. Shared `bulletTip` + Client `nodeBullet`/CSS rename + local-time formatter + `VM.workspaceRoots` local-path line; delivered and committed. |

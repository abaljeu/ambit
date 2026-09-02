# Projects overview

| Project | Stage | Summary |
| --- | --- | --- |
| [Architecture](architecture/) | charting | A browsable description of how Gambol is coded and how it runs. |
| [Bullet tip times](bullet-tip-times/) | charting | Which non-obvious time facts a node-marker tooltip should show (update, workspace, server, last-sync) with de-dup, timezone, and availability rules; open questions unresolved. |
| [Document formats](document-formats/) | charting | Remaining document formats (including XML read/write) after the workspace file model baseline. |
| [Download no-parse fix](download-no-parse-fix/) | charting | Download stamp-align must not require Parse; SetUpdateTime is exempt from the unparsed-document gate. |
| [End-user wiki](end-user-wiki/) | charting | A browsable wiki that describes the software for people who use it. |
| [Graph view](graph-view/) | charting | Radial focus-centric tree view with Ref edges as annulus overlay; portals and optional satellite radials for off-subtree links. |
| [llm-connector](llm-connector/) | charting | Run `?` with included context; LLM reply as Owned children; long-running Actor. |
| [Marketing wiki](marketing-wiki/) | charting | A GitLab-level browsable wiki of uses; not a campaign. |
| [Parse load demote](parse-load-demote/) | charting | Empty stub directory; stage unknown until contents land. |
| [RowView / FocusView layout vs behavior](rowview-layout-behavior/) | charting | Separate layout from behavior inside RowView/FocusView; plan complete, waits on the split-view-by-concern refactor landing. |
| [Transport layer](transport-layer/) | charting | Cross-cutting transport layer — inbound, outbound, and round-trip patterns for moving information between outside sources and the Graph while Graph stays authority; Parse/Persist as the shared text-processing unit; module contract for connector Actors; `plan` until promoted to `doc/`. |
| [Roadmap](roadmap/map.md) [pinned] | steering | Standing goto for what to work on next; groups Epics by Stage; Chapter plus Required for done gate Epic completion. |
| [ChildNode drop ref](childnode-drop-ref/) | spec | Progressive removal of ChildNode.ref; Node.owner + Op.SetOwner sole ownership source; Loaded-scope membership seam; ordered id-only ChildNode retained. |
| [Debug reload](debug-reload/) | tickets | Tell a person on watch how to load debug modules and how to pick up an esbuild rebuild with a hard-reload of the Browser. |
| [Work board cleanup](work-board-cleanup/) | tickets | Retire the live WORK.md board; work lives in each plan/ project's issues/. |
| [Client start time](client-start-time/) | active | On App refresh after a prior Session, the Browser shows the Graph from a local IndexedDB snapshot plus stored Changes, then does a Poll, so the user does not wait for `/state` while a blank screen or Loading... is visible. |
| [Daily git save](daily-git-save/) | active | The Server saves Graph documents in App DataDir. Commit that directory each day so the operator can recover those files from git without a manual commit. |
| [Delete Ref](delete-ref/) | active | A person uses a Ref in Children to link to a Node Owned elsewhere in the Graph; this Project makes Delete unlink that appearance from Children and leave the Node in place, and makes Delete of an Owned Node with a self-Ref finish: the command must not hang and must not promote the self-Ref. |
| [Event-sourced ops](event-sourced-ops/) | active | Give one semantic standard for how an Actor's Change enters a Graph so every Actor uses the same path and concurrent work merges instead of being refused. |
| [Expression Language](expression-language/) | active | Specify and implement a Prolog-like Expression language in Amble. The language extends path references with a left-to-right word pipeline over the Graph and yields Node, text, and number Answers for Find. |
| [Git protocol](git-protocol/) | active | Give this repo one git procedure: ordinary commits on **dev**, merge to **ready** after **Agent-done**, squash to **master**; other instructions point at the skill and do not copy it. |
| [Large-node cursor perf](large-node-cursor-perf/) | active | When one Node has a large Children list in the SiteMap, make Selection, Focus, and delete among the Children stay fast in the Browser. |
| [Owner-edge database repair](owner-edge-db-repair/) | active | Persisted Owned Children can fail to be a tree. After Server restart, every surviving non-ROOT Node has exactly one Owned parent that reaches ROOT, unreachable Nodes are deleted, and a reachable Node with no Owned parent has a Ref promoted to Owned, durable with no History Change. |
| [Selective client loading](selective-client-loading/) | active | Give the Browser a Graph that starts with only the Workspace Nodes needed for ROOT and restored navigation, grow residency only through explicit Load, and keep the Server Graph fully Resident and authoritative. |
| [Auto-download persisted files](auto-download-persisted-files/) | blocked | Auto-download and filesize peels delivered; four runtime checks are tabled until the user decides to resume HITL verification. |
| [Fix large change apply budget](fix-large-change-apply-budget/) | done | Nested parse-tail Replace apply stayed on append fast-path; optimized Op.apply / Graph.replace validation so LargeChangeApplyTests stay under 300ms without raising the guard. |
| [Fix SetText SYSTEM css resilience](fix-settext-system-css-resilience/) | done | Cold bootstrap seeds Unparsed File stubs; SetText resilience Parses then edits; DocumentPersistenceTests assert Unparsed File on cold load. |
| [Glossary: Directory File](glossary-directory-file/) | done | Directory File glossary + isMarker/related API renames toward Directory File language. |
| [Load status phases](load-status-phases/) | done | Make Load's three stages visible: Uploading (desktop push), Parsing (disk parse/reconcile), Loading (graph fetch); web skips Uploading. |
| [Login / context restore](login-context-restore/) | done | Auth and cold-reload HITLs passed; owning Workspace and prior Zoom restore after iOS unload. |
| [Node-bullet tooltip](node-bullet-tooltip/) | done | Bullet hover tip (native title) listing self-gated Node facts — Guid tail, residency, workspace path + local desktop path, Update Time (local tz), CSS classes. Shared `bulletTip` + Client `nodeBullet`/CSS rename + local-time formatter + `VM.workspaceRoots` local-path line; delivered, committed, and browser-verified on 2026-08-16. |
| [Relaxed concurrency](relaxed-concurrency/) | done | Build-upon layer — verified Graph/Ops facts, shared rejections, frontier D–F; merge implementation and active standard are [[plan/event-sourced-ops/overview.md]]. |
| [Search zoom select](search-zoom-select/) | done | Restore prior Find zoom framing; on no-children parent fallback, select the search target. |

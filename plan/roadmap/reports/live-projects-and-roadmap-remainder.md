# Live feature-set Projects and roadmap remainder

Research for [[plan/roadmap/issues/02-inventory-live-projects-and-roadmap-remainder.md]]. Facts only. Branch at write: `w/roadmap`. Sources: [[plan/index.md]], each cited `project.md` / `map.md` / `spec.md`, [[doc/index.md]], [[doc/README.md]], [[doc/current/]], [[doc/roadmap/]].

## 1. Live feature-set Projects

Scope: every `plan/*/project.md` except [[plan/done/]] and except [[plan/roadmap/]] (Roadmap is steering, not a feature-set Project). Overview table: [[plan/index.md]].

### Charting

- **Architecture** — Stage `charting`. Summary: a browsable description of how Gambol is coded and how it runs. [[plan/architecture/project.md]]. Destination ([[plan/architecture/map.md]]): how it is coded and how it runs (processes, layers, data flow); not user how-to and not use-case marketing. Charted 2026-08-29 from the Roadmap.
- **Bullet tip times** — Stage `charting`. Summary: which non-obvious time facts a node-marker tooltip should show (update, workspace, server, last-sync) with de-dup, timezone, and availability rules; open questions unresolved. [[plan/bullet-tip-times/project.md]]. Destination ([[plan/bullet-tip-times/map.md]]): resolve every non-obvious requirement for displaying time facts on the Bullet hover tip so `/to-spec` can specify that portion; map Status is Parked (2026-08-08).
- **Download no-parse fix** — Stage `charting`. Summary: Download stamp-align must not require Parse; SetUpdateTime is exempt from the unparsed-document gate. [[plan/download-no-parse-fix/project.md]]. No `map.md` or `spec.md`.
- **End-user wiki** — Stage `charting`. Summary: a browsable wiki that describes the software for people who use it. [[plan/end-user-wiki/project.md]]. Destination ([[plan/end-user-wiki/map.md]]): describe the software for operators (what it is, Graph/Node in use, App and Browser). Charted 2026-08-29 from the Roadmap.
- **Marketing wiki** — Stage `charting`. Summary: a GitLab-level browsable wiki of uses; not a campaign. [[plan/marketing-wiki/project.md]]. Destination ([[plan/marketing-wiki/map.md]]): uses (who it is for, jobs, situations); GitLab-level browse quality; no campaign. Charted 2026-08-29 from the Roadmap.
- **Parse load demote** — Stage `charting`. Summary: empty stub directory; stage unknown until contents land. [[plan/parse-load-demote/project.md]]. No `map.md` or `spec.md` (directory has `git.md` and empty `issues/` only).
- **RowView / FocusView layout vs behavior** — Stage `charting`. Summary: separate layout from behavior inside RowView/FocusView; plan complete, waits on the split-view-by-concern refactor landing. [[plan/rowview-layout-behavior/project.md]]. No `map.md` or `spec.md` (plan lives in [[plan/rowview-layout-behavior/plan.md]]).

### Spec

- **ChildNode drop ref** — Stage `spec`. Summary: progressive removal of ChildNode.ref; Node.owner + Op.SetOwner sole ownership source; Loaded-scope membership seam; ordered id-only ChildNode retained. [[plan/childnode-drop-ref/project.md]] also states “Tabled. This idea complicates without sufficient value.” Destination ([[plan/childnode-drop-ref/map.md]]): ChildNode stays as ordered id-only edges; Node.owner sole in-memory ownership; Loaded-scope seam; Op.SetOwner; progressive slices until edge `ref` is gone from the type and live paths.

### Active

- **Client start time** — Stage `active`. Summary: cache-first boot tickets 01–07 implemented (IndexedDB snapshot plus Change log, fold then first paint, boot Poll, truncation, optional bootstrapHash); HITL pending. [[plan/client-start-time/project.md]]. No `map.md` or `spec.md`.
- **Daily git save** — Stage `active`. Summary: once per UTC day after listen and DbAgent ready, sequential commitAll of DataDir and immediate child repos; stamp SYSTEM/gambol.git-save-day only on full success. [[plan/daily-git-save/project.md]]. No `map.md` or `spec.md`.
- **Event-sourced ops** — Stage `active`. Summary: one semantic standard for how any Actor's Change enters a Graph — implementation issues (`issues/01`–`15`) through wire migration, Actor spine, recovery, and permanent global history; charting docs in overview, architecture, and details. [[plan/event-sourced-ops/project.md]]. No project-root `map.md` or `spec.md` (objective text in [[plan/event-sourced-ops/overview.md]]).
- **Expression Language** — Stage `active`. Summary: Spec locked; tickets 15–26 implemented on `w/expr`; Chapter 11 harness still omits `section` / `subsection` rows (covered in ExprSectionTests). [[plan/expression-language/project.md]]. Destination ([[plan/expression-language/map.md]]): a hand-off spec for syntax, evaluation semantics, and the first primitive catalog (implementation called a later effort in that map). Spec file: [[plan/expression-language/spec.md]] (hand-off specification; Status not a Stage line).
- **Large-node cursor perf** — Stage `active`. Summary: Selection-only planPatchDOM/patchDOM fast path + O(1) SiteEntry.childIndex implemented; delete-children cost analysis in delete-children-cost.md (no delete fix yet). [[plan/large-node-cursor-perf/project.md]]. No `map.md` or `spec.md`.
- **Owner-edge database repair** — Stage `active`. Summary: Extend DbAgent startup sweep: ACID repair of `node_children` into a ROOT-owned tree; GC unreachable; promote a Ref when a reachable node has no owner. [[plan/owner-edge-db-repair/project.md]]. Destination ([[plan/owner-edge-db-repair/spec.md]], Status ready-for-agent): fold ownership repair into DbAgent startup projection maintenance so after commit every surviving non-ROOT node has exactly one owner row, ROOT has none, and every Owned chain is acyclic and reaches ROOT.
- **Selective client loading** — Stage `active`. Summary: Client-partial residency with explicit Load and Unloaded/Loaded child lists; spec ready-for-agent, implementation issues in flight. [[plan/selective-client-loading/project.md]]. Destination ([[plan/selective-client-loading/map.md]]): resolve every product/domain/architecture decision needed for a complete selective client loading specification (implementation outside that map). Spec ([[plan/selective-client-loading/spec.md]], Status ready-for-agent): client starts with ROOT (and at most one restored Workspace), grows residency only via explicit Load, with Unloaded vs Loaded child lists while the server stays fully resident.

### Blocked

- **Auto-download persisted files** — Stage `blocked`. Summary: Auto-download and filesize peels delivered; four runtime checks are tabled until the user decides to resume HITL verification. [[plan/auto-download-persisted-files/project.md]]. No `map.md` or `spec.md`.

### Done (still under `plan/`, not yet in `plan/done/`)

- **Delete Ref** — Stage `done`. Summary: Delete of any Ref unlinks that appearance; the Owned Node stays. Repro: Ref to Workspaces. [[plan/delete-ref/project.md]]. No `map.md` or `spec.md`.
- **Fix large change apply budget** — Stage `done`. Summary: Nested parse-tail Replace apply stayed on append fast-path; optimized Op.apply / Graph.replace validation so LargeChangeApplyTests stay under 300ms without raising the guard. [[plan/fix-large-change-apply-budget/project.md]]. No `map.md` or `spec.md`.
- **Fix SetText SYSTEM css resilience** — Stage `done`. Summary: Cold bootstrap seeds Unparsed File stubs; SetText resilience Parses then edits; DocumentPersistenceTests assert Unparsed File on cold load. [[plan/fix-settext-system-css-resilience/project.md]]. No `map.md` or `spec.md`.
- **Glossary: Directory File** — Stage `done`. Summary: Directory File glossary + isMarker/related API renames toward Directory File language. [[plan/glossary-directory-file/project.md]]. No `map.md` or `spec.md`.
- **Load status phases** — Stage `done`. Summary: Make Load's three stages visible: Uploading (desktop push), Parsing (disk parse/reconcile), Loading (graph fetch); web skips Uploading. [[plan/load-status-phases/project.md]]. No `map.md` or `spec.md`.
- **Login / context restore** — Stage `done`. Summary: Auth and cold-reload HITLs passed; owning Workspace and prior Zoom restore after iOS unload. [[plan/login-context-restore/project.md]]. Destination ([[plan/login-context-restore/map.md]]): after iOS unloads Safari with tabs open, cold reload returns authenticated with previously active Workspace Loaded and Zoom restored (Refresh-parity).
- **Node-bullet tooltip** — Stage `done`. Summary: Bullet hover tip (native title) listing self-gated Node facts; delivered and browser-verified on 2026-08-16. [[plan/node-bullet-tooltip/project.md]]. Spec ([[plan/node-bullet-tooltip/spec.md]], Status ready-for-agent): hover Bullet shows Guid tail, residency, workspace path, Update Time, CSS classes; time model beyond Update Time deferred to bullet-tip-times.
- **Relaxed concurrency** — Stage `done`. Summary: Build-upon layer — verified Graph/Ops facts, shared rejections, frontier D–F; merge implementation and active standard are [[plan/event-sourced-ops/overview.md]]. [[plan/relaxed-concurrency/project.md]]. Map ([[plan/relaxed-concurrency/map.md]]): records verified facts and frontier D–F; active concurrency standard lives in event-sourced-ops. Spec ([[plan/relaxed-concurrency/spec.md]], Status superseded): do not implement from this file.
- **Search zoom select** — Stage `done`. Summary: Restore prior Find zoom framing; on no-children parent fallback, select the search target. [[plan/search-zoom-select/project.md]]. Spec ([[plan/search-zoom-select/spec.md]], Stage done): Find pick restores prior zoom framing; leaf fallback selects the search target.
- **Work board audit** — Stage `done`. Summary: Audit WORK claims against source, tests, history, and durable HITL evidence. [[plan/work-board-audit/project.md]]. No `map.md` or `spec.md`.

### Index note

[[plan/index.md]] also lists **Roadmap** at Stage `steering` (excluded here as non–feature-set). Expression Language summary text in the index differs slightly from [[plan/expression-language/project.md]] (index: tickets 15–22, next ticket 23; project.md: tickets 15–26 implemented). This report cites `project.md` for Stage and Summary.

## 2. `doc/roadmap/*.md` vs `doc/current/`

Classification uses [[doc/index.md]] Development Sequence terms where that index speaks, else the roadmap file's own Status plus whether [[doc/current/]] (or [[doc/README.md]] / [[doc/index.md]] Current Features) treats the topic as a current baseline. Authority rule: [[doc/README.md]] — when roadmap disagrees with current, current wins.

### Accomplished / superseded inventory (current owns the topic)

- [[doc/roadmap/database-migration.md]] — Status superseded; authority [[doc/current/persistence-model.md]] and [[doc/reference/postgres-environments.md]]. **Implemented** (moved).
- [[doc/roadmap/persistence-vs-domain-model.md]] — Status superseded; authority [[doc/current/persistence-model.md]]. **Implemented** (moved).
- [[doc/roadmap/postgres-environments.md]] — Status superseded; authority [[doc/reference/postgres-environments.md]] (reference, not current/; [[doc/README.md]] lists it under Reference). **Implemented** (moved).
- [[doc/roadmap/postgres-migration.md]] — Status superseded; authority [[doc/current/persistence-model.md]]. **Implemented** (moved).
- [[doc/roadmap/postgres-roadmap.md]] — roadmap index; sections 0–2 marked `[x]` Implemented with pointers to [[doc/current/persistence-model.md]] and [[doc/reference/postgres-environments.md]]. Treat as **accomplished inventory** for those sections; unfinished sections listed in §3 below. [[doc/README.md]] still points here as persistence-focused roadmap index.
- [[doc/roadmap/cmd-last-result-format.md]] — Status Implemented. No dedicated [[doc/current/]] baseline; [[doc/current/desktop-local-files.md]] and [[doc/current/workspace-local-mapping.md]] only mention `#cmd-last-result` as a surface. **Implemented** per its own Status (not listed under [[doc/index.md]] Current Features).
- [[doc/roadmap/workspace-name-immutable.md]] — Status Done. [[doc/current/workspace-graph.md]] states workspace names are immutable after creation / `Graph.setName` rejects Workspace rename. **Implemented**.
- [[doc/roadmap/workspace-name-verbatim.md]] — Status Slice A + B done. [[doc/current/workspace-stage-plan.md]] Stage 7 path uses folder name equals workspace label verbatim. **Implemented** (slices A+B).
- [[doc/roadmap/workspace-upload-client-structure.md]] — Status Done. [[doc/index.md]] Workspace file sync “Last implemented” cites Desktop Upload client-first stubs. **Implemented**.

### Partial (current baseline exists; roadmap remainder still open)

- [[doc/roadmap/future-merge-sync.md]] — no Status line; [[doc/index.md]] Server-authoritative sync and merge is **Partial**; [[doc/current/sync-mvp.md]] is MVP without client-side merge; [[doc/current/persistence-model.md]] lists server-authoritative merge as Not implemented. **Partial** / merge still planned.
- [[doc/roadmap/lazy-load.md]] — Status Partial; [[doc/index.md]] Lazy Load and workspace source formats **Partial**. **Partial** (reconcile done; expand-to-parse and richer freshness planned — also [[doc/roadmap/workspaces-checklist.md]] unchecked items).
- [[doc/roadmap/parsefile-document-codec-import.md]] — Status: Core Unparsed wiring landed; verification and Current warm remain. No dedicated current/ feature page. **Partial**.
- [[doc/roadmap/parse-file-reconcile-current.md]] — Status: Server-apply ParseFile landed (Shared/Client/Desktop verified); Server.Tests rebuild note. **Partial** / largely landed per own Status.
- [[doc/roadmap/reference-expression-interpretation.md]] — no Status; [[doc/current/workspace-graph.md]] Reference expressions (baseline) cites this as target grammar and lists implemented anchors/steps vs Not implemented postfixes/command syntax. **Partial**.
- [[doc/roadmap/reference-expressions.md]] — Status Partially superseded; interpretation authority is reference-expression-interpretation; surrounding language still draft. **Partial**.
- [[doc/roadmap/revising-workspace-file-model.md]] — no Status; Current Truths overlap [[doc/current/workspace-graph.md]]; Target Concept / Documents still design. **Partial**.
- [[doc/roadmap/workspace-file-directory-placement.md]] — Status In progress; [[doc/current/workspace-graph.md]] states owner-chain placement and uniqueness and cites this plan. [[doc/roadmap/workspaces-checklist.md]] marks the checklist item `[x]`. **Partial** / in progress (roadmap Status still In progress).
- [[doc/roadmap/workspace-file-model.md]] — Status working draft; [[doc/index.md]] Workspace file model and persistence **Partial**; implemented behavior summarized in [[doc/current/workspace-graph.md]] / [[doc/current/workspace-stage-plan.md]]. **Partial**.
- [[doc/roadmap/workspace-file-persistence.md]] — Status Draft; [[doc/current/workspace-stage-plan.md]] Stage 7 core and Stage 8 `[x]` with remaining follow-ups (hard delete under TRASH; git persistence verification); [[doc/current/persistence-model.md]] still lists “Full per-document snapshot layout and incremental file writes” under Not implemented pointing here — contradiction between current docs; both cited. Treat topic as **Partial**.
- [[doc/roadmap/workspace-file-sync.md]] — Status Partial; [[doc/index.md]] Workspace file sync **Partial**; [[doc/current/desktop-local-files.md]] / [[doc/current/workspace-local-mapping.md]] cover mapping and Upload/Download surfaces. **Partial** (overwrite/freshness UI, mirror-delete / Class 2 still open per index and checklist).
- [[doc/roadmap/workspace-format-cstyle-braces.md]] — Status Implemented (first slice). No dedicated [[doc/current/]] page; [[doc/reference/formats/code-shape.md]] documents CStyle codec. **Partial** / first slice implemented (not a current/ baseline).
- [[doc/roadmap/workspace-format-dispatch.md]] — no Status; describes Amb/Plain with Xml planned; [[doc/reference/formats/code-shape.md]] documents live `DocumentFormat` dispatch including Md/CStyle. Roadmap text is stale relative to reference. **Partial**.
- [[doc/roadmap/workspace-scale-file-and-db-management.md]] — umbrella; rollout steps mix done and planned; [[doc/index.md]] points related work as Partial/Planned. **Partial**.
- [[doc/roadmap/workspace-scale-import.md]] — worksets include done reconcile and planned expand-to-parse; [[doc/index.md]] Lazy Load **Partial**. **Partial**.
- [[doc/roadmap/workspace-webdav.md]] — Status Partial; [[doc/index.md]] cites WebDAV under Partial file sync. **Partial**.
- [[doc/roadmap/workspaces.md]] — index of current baselines vs active roadmaps; not a feature claim itself. Points at [[doc/current/]] for baselines. **Index** (baselines implemented; linked active roadmaps partial/planned).
- [[doc/roadmap/workspaces-checklist.md]] — living checklist; many `[x]`, open: Overwrite policy, Expand-to-parse, Richer freshness metadata/UI, On-demand graph residency, Annotation migration. **Partial**.
- [[doc/roadmap/amble-run.md]] — Status In progress; [[doc/index.md]] Amble run **Evolving**. No [[doc/current/]] baseline. **Partial** / evolving (slice 1 defines AmbleRun; Client Run wiring and later slices open).

### Still planned (no current baseline for the remaining work)

- [[doc/roadmap/on-demand-graph-residency.md]] — Status Planned; [[doc/index.md]] On-demand graph residency **Planned**; [[doc/current/sync-mvp.md]] still full-graph bootstrap; [[doc/current/persistence-model.md]] marks target residency roadmap-only. **Planned**.
- [[doc/roadmap/node-kind-transform.md]] — Status Planned. Not covered as implemented in [[doc/current/]]. **Planned**.
- [[doc/roadmap/paste-document-codec-import.md]] — Status Draft plan (no implementation yet). **Planned**.
- [[doc/roadmap/language-syntax-and-semantics.md]] — draft Amble language beyond RefExpr; no Status; [[doc/index.md]] does not list a current Expression/Amble baseline under Current Features (Amble run is Evolving only). **Planned** / draft.
- [[doc/roadmap/workspace-format-amb.md]] — Status Draft (target design; Snapshot.fs called pre-workspace baseline). Related codecs exist in code/reference; this doc remains target design. **Planned** / draft relative to its Status.
- [[doc/roadmap/workspace-format-code.md]] — Status Draft. **Planned** / draft.
- [[doc/roadmap/workspace-format-md.md]] — Status Target design; [[doc/index.md]] Might be next under workspace file model does not list Md as current; cites XML as next format example. **Planned**.
- [[doc/roadmap/workspace-format-plain.md]] — Status Draft. **Planned** / draft (reference formats note Plain exists in code; this roadmap Status remains Draft).
- [[doc/roadmap/workspace-format-xml.md]] — Status Draft; [[doc/index.md]] Might be next: XML read/write. **Planned**.
- [[doc/roadmap/workspace-text-outline-conversion.md]] — Status Draft. **Planned** / draft (Stage plan cites it as full spec for Stage 7; Status still Draft).
- [[doc/roadmap/workspace-text-outline-conversion.md.md]] — empty file (0 bytes). No claim.

### Non-`.md` note

[[doc/roadmap/]] also contains `selective client loading.amb` (not `*.md`); not classified here. Related live Project: [[plan/selective-client-loading/]].

## 3. [[doc/roadmap/postgres-roadmap.md]] accomplished inventory and unfinished sections

Current docs agree that PostgreSQL persistence and environments are implemented baselines: [[doc/current/persistence-model.md]], [[doc/reference/postgres-environments.md]], [[doc/index.md]] Persistence (PostgreSQL + correlated files), [[doc/README.md]].

Marked accomplished in postgres-roadmap itself:

- §0 PostgreSQL as persistence back-end — `[x]` → [[doc/current/persistence-model.md]].
- §1 Establish a PostgreSQL server — `[x]` → [[doc/reference/postgres-environments.md]].
- §2 Drop file authority when DB is present — `[x]` → [[doc/current/persistence-model.md]] (also cites [[doc/roadmap/future-merge-sync.md]]).

Unfinished or only partially marked:

- §3 Server-authoritative merge — decision text; no `[x]`. Still open per [[doc/index.md]] Partial merge, [[doc/current/persistence-model.md]] Not implemented, [[doc/current/sync-mvp.md]] MVP without that merge. Sub-decisions: smart rebase / orphan rescue; rebase-style convergence; edit wins over delete; conflict marker nodes; no client-side rebase.
- §4 Robust client-server sync — decision text; no `[x]`. Overlaps MVP in [[doc/current/sync-mvp.md]] (batches, changeId, polling) but also specifies auto-retry, localStorage queue persistence, accepting full server result replacing local graph, and leaves IndexedDB/offline cache open. Not fully checked off in this file.
- §5 Client-side memory management (document-level loading) — target on-demand residency; authority [[doc/roadmap/on-demand-graph-residency.md]]. Not `[x]`. [[doc/index.md]] Planned; current sync still full-graph bootstrap.
- §6 Replication unit: whole documents — tied to on-demand residency; not `[x]`.
- §7 Desktop app with local webserver — Status `[~]` Partially implemented → [[doc/current/desktop-local-files.md]], [[doc/current/workspace-stage-plan.md]]. Listed Not implemented in postgres-roadmap: open in system explorer; startup workspace registration; full workspace filesystem API. Same gaps appear under [[doc/current/desktop-local-files.md]] Not implemented (and [[doc/current/workspace-local-mapping.md]] startup sync / automatic initial Download).

## 4. Roadmap remainder not already in `doc/current` (summary)

Topics still open after subtracting current baselines:

- Server-authoritative merge / conflict markers ([[doc/roadmap/future-merge-sync.md]], postgres-roadmap §3).
- On-demand / selective graph residency beyond full-graph sync ([[doc/roadmap/on-demand-graph-residency.md]], postgres-roadmap §5–6; live Project [[plan/selective-client-loading/]] is the client-only phase).
- Lazy Load expand-to-parse and richer freshness UI ([[doc/roadmap/lazy-load.md]], [[doc/roadmap/workspaces-checklist.md]], [[doc/roadmap/workspace-scale-import.md]]).
- Annotation migration ([[doc/roadmap/workspaces-checklist.md]], [[doc/roadmap/workspace-scale-file-and-db-management.md]]).
- Workspace file sync remainder: overwrite/freshness UI; mirror-delete / Class 2 ([[doc/index.md]], [[doc/roadmap/workspace-file-sync.md]], checklist Overwrite policy).
- Stage 7 follow-ups: hard delete under TRASH removes artifacts; git persistence verification ([[doc/current/workspace-stage-plan.md]]).
- Desktop gaps: open-in-explorer; startup workspace registration; full filesystem API; `open` capability ([[doc/current/desktop-local-files.md]], postgres-roadmap §7).
- Format targets still draft/planned in roadmap: XML, Markdown, code comment-refs, text-outline conversion Status Draft ([[doc/roadmap/workspace-format-xml.md]], [[doc/roadmap/workspace-format-md.md]], [[doc/roadmap/workspace-format-code.md]], [[doc/roadmap/workspace-text-outline-conversion.md]]); paste via document codec ([[doc/roadmap/paste-document-codec-import.md]]); Normal↔Directory promote/demote ([[doc/roadmap/node-kind-transform.md]]).
- Amble / expression language beyond RefExpr baseline ([[doc/roadmap/language-syntax-and-semantics.md]], [[doc/roadmap/amble-run.md]]; live Project [[plan/expression-language/]]).
- RefExpr postfixes and command/assignment syntax still Not implemented in [[doc/current/workspace-graph.md]].
- Persistence-model Not implemented list also names: external migration tooling beyond initSchema; removal of legacy FileAgent file-authority path ([[doc/current/persistence-model.md]]).

# Ambit Roadmap
Apply [[.agents/skills/wait-what/SKILL.md]] to this file.
Labels: wayfinder:map

Ambit's Key Value Proposition: Rapid understanding via dynamic organization.
Every development aims to leverage this.


## Destination

A standing Roadmap that answers “what should I work on next.” Completing it means completing the application. It groups **Epics** by Stage — marketable user end-goals, larger than a feature or interaction. Each Epic is a standing file under [[epics/]] until that goal is met. Feature-set Projects implement focused features that enable Epics; this map points at them and does not copy their specs. Sessions invoke those Projects, implement them, and promote committed facts into `doc/`. `doc/` describes the current program.


## Current strategy
(Mutable section. Replace this section when the near-term aim changes; do not append.)

Solid product core via [[epics/robust-outliner.md]] **Solid core** (four-call surface, ACID apply, managed actor pool). Grow by pull in / organize / send out. First inbound example: **Grok Bot / agent messages into the outline** (instead of generic chat UI). Mail and other connected channels come **after** that near-term agent inbound (see [[epics/operate-connected-channels.md]]).
=======

## Notes

- Two Project kinds only: this Roadmap, and feature-set Projects (existing `plan/<slug>/`). No Epic Project folders. Standing Epic files live in [[epics/]] (`epics/<slug>.md`), one file per Epic. User Epics list Chapter files under [[epics/chapters/]] (`epics/chapters/<slug>.md`), one file per Chapter. Chapters are not issues. [[issues/]] is wayfinder tickets only (this map’s decisions and tasks). The frontier does not scan `epics/`.
- Open Epics are listed under **Epics** below, grouped by Stage (override of “do not list open tickets”). Order inside a Stage does not matter.
- Standing Epic files stay open until the end-goal is met. An Epic has a Stage and, when it is a User Epic, an ordered list of **Chapters** (named beats: Visit Troy, see Circe). The Epic file names which Chapter is current and links the Chapter files. Each Chapter file follows [[epics/chapter-template.md]]: Context, Goal, Required for done. The checklist is wikilinks to Projects or issues. The Chapter does not own those items. Most often a Chapter focuses in one Project; several pointers are allowed when the end-goal needs them. Advancing a Chapter is charting those pieces on the Projects that own them. Charting / spec / tickets / active / blocked / done / dead stay on those Projects. This Roadmap is not a coding Project. `/to-tickets` subdivides a feature-set Project, not the Epic or Chapter list.
- Every classified feature-set Project (or a named part) sits on at least one Epic. **Chapter** checklists hold items that belong to a beat. **Required for done** holds the rest. No overlap. The Epic is not done until every Chapter item and every Required item is done (or the named part). [[epics/organize-huge-outlines.md]], [[epics/robust-outliner.md]], and [[epics/process-improvement.md]] exist to home Projects; they have only Required for done. Sessions do not present them as uncharted Chapters. User Epics reference Organize Huge Outlines for scaling; first use does not wait on it. Wiki portions about this Epic are Required on that Epic; the whole wiki Project is not.
- [[plan/childnode-drop-ref/project.md]] is dead (retired without delivery). [[plan/work-board-cleanup/project.md]] is done (accomplished). Process work homes on [[epics/process-improvement.md]]. Debug reload homes on [[epics/robust-outliner.md]] as architecture documentation.
- `plan` is steering. `doc/` is committed facts (target). `doc/roadmap` is subsumed by references from this and other `plan` Projects — not a second project home. [[doc/roadmap/postgres-roadmap.md]] is accomplished inventory.
- The live work board [[WORK.md]] is retired. Start from an Epic. A User Epic may have a current Chapter; that Chapter points at Projects or issues, and those carry Stage/Status. A Developer Epic has no Chapters: start from its live Required for done. [[plan/index.md]] is a stage table, not a second work clock. This map does not copy project specs.
- Feature-set agent-done promotes spec into `doc/` (current or history as fit). This Roadmap groups Epics; it does not copy specs.
- [[doc/index.md]] is the Feature index of the current program. This map is the goto for what to work on next.
- This Roadmap’s Stage is **steering**. Feature-set Projects and Epics share the other Stage words; an Epic must not use steering. A User Epic has Stage, Chapters, and Required for done. A Developer Epic has Stage and Required for done only. A Chapter file has Context, Goal, and Required for done (pointer checklist).
- Skills: wayfinder, grilling, domain-modeling, project-work, maintain-doc-currency. Implementation skills run on feature-set Projects.
- Work on **dev**; promote finished work to **ready**. Plan by default on this map except when invoking a named feature-set Project. Do not write per-project git notes.
- Epics are parallel (order inside a Stage does not rank them). Continue from memories of recent work: that Epic, its current Chapter (or Developer Required live items), then pointed Project/issue status. Present that path as choices. Do not auto-pick. Do not offer Developer Epic Chapters; there are none.
- Documentation wikis: [[plan/end-user-wiki/map.md]], [[plan/marketing-wiki/map.md]], [[plan/architecture/map.md]]. An Epic is not done until the wiki portions about that Epic are done. Architecture’s remainder is also Required on [[epics/robust-outliner.md]].
- [[epics/robust-outliner.md]] **Solid core** records the shape: four-call surface, ACID apply, managed actor pool (not process crash isolation); file mode view-only. Other work posts Changes ([[plan/event-sourced-ops/overview.md]]). Same Epic aims at incremental operations (modest send, then more); Workspace upload and Browser Load are current counterexamples.

## Epics

Grouped by Stage. Order inside a Stage does not matter.

### charting

- [[epics/work-with-text-files-from-anywhere.md]] — Work with my documents from anywhere (current: [[epics/chapters/automatic-upload-and-download.md]])
- [[epics/build-or-explore-a-wiki.md]] — Build or explore a wiki (current: [[epics/chapters/markdown-codec.md]])
- [[epics/create-and-publish-web-pages.md]] — Create and publish web pages (current: [[epics/chapters/html-codec.md]])
- [[epics/manage-a-project.md]] — Manage a project (current: [[epics/chapters/status.md]])
- [[epics/operate-a-pkm.md]] — Operate a PKM (current: [[epics/chapters/find-what-i-wrote.md]])
- [[epics/agent-chat-managed-context.md]] — Agent chat with managed context (current: [[epics/chapters/ask-from-what-i-see.md]])
- [[epics/operate-connected-channels.md]] — Operate connected channels (no Chapter yet)
- [[epics/organize-huge-outlines.md]] — Organize Huge Outlines (Developer Epic; no Chapter to chart)
- [[epics/robust-outliner.md]] — Robust outliner (Developer Epic; no Chapter to chart)
- [[epics/process-improvement.md]] — Process improvement (Developer Epic; no Chapter to chart)

## Decisions so far

- [Inventory live Projects and roadmap remainder](plan/roadmap/issues/02-inventory-live-projects-and-roadmap-remainder.md) — [[plan/roadmap/reports/live-projects-and-roadmap-remainder.md]]: 16 non-done feature-set Projects (includes End-user wiki, Architecture, Marketing wiki); postgres-roadmap §0–2 done, §3–7 still open.
- [Name and order the first Epics](plan/roadmap/issues/01-name-and-order-first-epics.md) — five User Epics grouped by Stage (all charting); outline capture already met; wiki folders stay Projects.
- [Retire index Development Sequence](plan/roadmap/issues/03-retire-index-development-sequence.md) — [[doc/index.md]] is the Feature index; leftovers went to existing Projects plus [[plan/document-formats/map.md]]; desktop mapping is Current.
- [Chart chapters for Agent chat with managed context](plan/roadmap/issues/04-chart-agent-chat-managed-context.md) — five Chapters; current Ask from what I see; new Project plus `?` on expression-language and eso 07/09.
- [Create Ask from what I see Project](plan/roadmap/issues/05-create-ask-from-what-i-see-project.md) — [[plan/llm-connector/project.md]]; `?` recognition is [[plan/expression-language/issues/33-recognize-ask-run-statement.md]].
- [Chart chapters for Work with my text files from anywhere](plan/roadmap/issues/06-chart-work-with-text-files-from-anywhere.md) — title Work with my documents from anywhere; five Chapters; current Automatic upload and download; wiki issues plus [[plan/roadmap/issues/07-chart-automatic-upload-and-download.md]].
- [Chart chapters for Create and publish web pages](plan/roadmap/issues/08-chart-create-and-publish-web-pages.md) — four Chapters; current HTML codec; codec on document-formats; In-app styling, Public URL, and Published-page CSS have no Project yet.
- [Chart chapters for Build or explore a wiki](plan/roadmap/issues/09-chart-build-or-explore-a-wiki.md) — two Chapters; current Markdown codec; `.md` on document-formats; Public URL (md as HTML) has no Project yet.
- [Chart chapters for Manage a project](plan/roadmap/issues/10-chart-manage-a-project.md) — two Chapters; current Status; Date is set/find/compare with no forced meaning; no owning Project yet.
- [Home every Project on an Epic](plan/roadmap/issues/11-home-every-project-on-an-epic.md) — coverage via Homed Projects; new Developer Epics Organize Huge Outlines and Robust outliner; ChildNode drop ref tabled; work-board-cleanup on this Roadmap.
- 2026-09-04: Stage `dead` added (retired without delivery). ChildNode drop ref is dead. Work board cleanup is done. New Developer Epic [[epics/process-improvement.md]] homes [[plan/git-protocol/project.md]] (done). [[plan/debug-reload/project.md]] homes on Robust outliner as architecture documentation.
- [Required for Epic done vs Chapter, no overlap](plan/roadmap/issues/12-required-for-epic-done.md) — Chapter vs Required for done; wiki portions about this Epic gate this Epic.
- [Grill Cursor-repo to Ambit LLM use onto Epics](plan/roadmap/issues/13-grill-cursor-repo-to-ambit-llm-use.md) — one Chapter **Ambit keeps consistency with desktop repo for Agentic work** on [[epics/agent-chat-managed-context.md]]; depends on **Ask from what I see** and documents auto-upload/download; **Agent** in [[CONTEXT.md]].
- Work board retired 2026-09-02. No live [[WORK.md]]. Discovery is [[plan/index.md]] plus issue Status plus wayfinder frontier. Recorded here; no Roadmap issue file. Cleanup Project: [[plan/work-board-cleanup/project.md]] (rename of work-board-audit).
- Operate connected channels User Epic from locked definition (2026-09-03). Connect plus operate; mail is first channel in framing, not the title. No Chapter file yet. Recorded here; no Roadmap issue file.

## Not yet specified

- Chart Chapters for [[epics/operate-connected-channels.md]] (mail first in framing). Home connector Projects when they exist. [[plan/transport-layer/project.md]] stays the Project pattern; PKM still lists it as a dependency.
- Pointer charting for Public URL, Published-page CSS, and In-app styling (web pages) and for wiki Public URL (no owning Project yet).
- Pointer charting for Status and Date on [[epics/manage-a-project.md]] (no owning Project yet).
- Later Epics beyond the standing list.
- Which remaining Required-until-done items belong on a Chapter. Named parts of a Project still unspecified.
- How leftover `doc/roadmap` files become references vs history vs current.
- When the application is complete (User Epics plus any later).

## Out of scope

- A third Project kind (Epic Projects).
- Rewriting every `doc/` file in this charting session.
- Implementing feature slices on this map (those belong on feature-set Projects).
- Agent-instruction overhaul as the Roadmap sequence (except the steering stage added while charting).

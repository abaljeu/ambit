# Roadmap

Labels: wayfinder:map

## Destination

A standing Roadmap that answers “what should I work on next.” Completing it means completing the application. It groups **Epics** by Stage — marketable user end-goals, larger than a feature or interaction. Each Epic is a standing file under [[epics/]] until that goal is met. Feature-set Projects implement focused features that enable Epics; this map points at them and does not copy their specs. Sessions invoke those Projects, implement them, and promote committed facts into `doc/`. `doc/` describes the current program.

## Notes

- Two Project kinds only: this Roadmap, and feature-set Projects (existing `plan/<slug>/`). No Epic Project folders. Standing Epic files live in [[epics/]] (`epics/<slug>.md`), one file per Epic. Person-job Epics hold Chapters in that file. [[issues/]] is wayfinder tickets only (this map’s decisions and tasks). The frontier does not scan `epics/`.
- Open Epics are listed under **Epics** below, grouped by Stage (override of “do not list open tickets”). Order inside a Stage does not matter.
- Standing Epic files stay open until the end-goal is met. An Epic has a Stage and, when it is a person-job, an ordered list of **Chapters** (named beats: Visit Troy, see Circe). The file names which Chapter is current. Each Chapter follows the shape of [[plan/selective-client-loading/issues/17-represent-unloaded-child-lists-end-to-end.md]] raised one level: **What to build** names major features; the checklist is wikilinks to Projects or issues. The Chapter does not own those items. Most often a Chapter focuses in one Project; several pointers are allowed when the end-goal needs them. Advancing a Chapter is charting those pieces on the Projects that own them. Charting / spec / tickets / active / blocked / done stay on those Projects. This Roadmap is not a coding Project. `/to-tickets` subdivides a feature-set Project, not the Epic or Chapter list.
- Every classified feature-set Project (or a named part) sits on at least one Epic. **Chapter** checklists hold items that belong to a beat. **Required for done** holds the rest. No overlap. The Epic is not done until every Chapter item and every Required item is done (or the named part). [[epics/organize-huge-outlines.md]] and [[epics/robust-outliner.md]] exist to home Projects; they have only Required for done. Sessions do not present them as uncharted Chapters. Person-job Epics reference Organize Huge Outlines for scaling; first use does not wait on it. Wiki portions about this Epic are Required on that Epic; the whole wiki Project is not.
- [[plan/childnode-drop-ref/project.md]] is tabled; no Epic pointer. [[plan/work-board-audit/project.md]] served this Roadmap, not an Epic.
- `plan` is steering. `doc/` is committed facts (target). `doc/roadmap` is subsumed by references from this and other `plan` Projects — not a second project home. [[doc/roadmap/postgres-roadmap.md]] is accomplished inventory.
- [[WORK.md]] stays the low-level started/outstanding board. This map does not copy those lines.
- Feature-set agent-done promotes spec into `doc/` (current or history as fit). This Roadmap groups Epics; it does not copy specs.
- [[doc/index.md]] is the Feature index of the current program. This map is the goto for what to work on next.
- This Roadmap’s Stage is **steering**. Feature-set Projects and Epics share the other Stage words; an Epic must not use steering. A person-job Epic has Stage, Chapters, and Required for done. A home Epic has Stage and Required for done only. A Chapter’s body is **What to build** plus a pointer checklist.
- Skills: wayfinder, grilling, domain-modeling, project-work, maintain-doc-currency. Implementation skills run on feature-set Projects.
- Stay on `w/roadmap`. Plan by default on this map except when invoking a named feature-set Project.
- Sessions present multiple choice of takeable work (frontier tickets, uncharted person-job Epic Chapters, fog now specifiable). Do not auto-pick the first frontier ticket. Do not offer home-Epic Chapters; there are none.
- Documentation wikis: [[plan/end-user-wiki/map.md]], [[plan/marketing-wiki/map.md]], [[plan/architecture/map.md]]. An Epic is not done until the wiki portions about that Epic are done. Architecture’s remainder is also Required on [[epics/robust-outliner.md]].

## Epics

Grouped by Stage. Order inside a Stage does not matter.

### charting

- [[epics/work-with-text-files-from-anywhere.md]] — Work with my documents from anywhere (current: Automatic upload and download)
- [[epics/build-or-explore-a-wiki.md]] — Build or explore a wiki (current: Markdown codec)
- [[epics/create-and-publish-web-pages.md]] — Create and publish web pages (current: HTML codec)
- [[epics/manage-a-project.md]] — Manage a project (current: Status)
- [[epics/operate-a-pkm.md]] — Operate a PKM (current: Find what I wrote)
- [[epics/agent-chat-managed-context.md]] — Agent chat with managed context (current: Ask from what I see)
- [[epics/organize-huge-outlines.md]] — Organize Huge Outlines (home; no Chapter to chart)
- [[epics/robust-outliner.md]] — Robust outliner (home; no Chapter to chart)

## Decisions so far

- [Inventory live Projects and roadmap remainder](plan/roadmap/issues/02-inventory-live-projects-and-roadmap-remainder.md) — [[plan/roadmap/reports/live-projects-and-roadmap-remainder.md]]: 16 non-done feature-set Projects (includes End-user wiki, Architecture, Marketing wiki); postgres-roadmap §0–2 done, §3–7 still open.
- [Name and order the first Epics](plan/roadmap/issues/01-name-and-order-first-epics.md) — five person-jobs grouped by Stage (all charting); outline capture already met; wiki folders stay Projects.
- [Retire index Development Sequence](plan/roadmap/issues/03-retire-index-development-sequence.md) — [[doc/index.md]] is the Feature index; leftovers went to existing Projects plus [[plan/document-formats/map.md]]; desktop mapping is Current.
- [Chart chapters for Agent chat with managed context](plan/roadmap/issues/04-chart-agent-chat-managed-context.md) — five Chapters; current Ask from what I see; new Project plus `?` on expression-language and eso 07/09.
- [Create Ask from what I see Project](plan/roadmap/issues/05-create-ask-from-what-i-see-project.md) — [[plan/llm-connector/project.md]]; `?` recognition is [[plan/expression-language/issues/33-recognize-ask-run-statement.md]].
- [Chart chapters for Work with my text files from anywhere](plan/roadmap/issues/06-chart-work-with-text-files-from-anywhere.md) — title Work with my documents from anywhere; five Chapters; current Automatic upload and download; wiki issues plus [[plan/roadmap/issues/07-chart-automatic-upload-and-download.md]].
- [Chart chapters for Create and publish web pages](plan/roadmap/issues/08-chart-create-and-publish-web-pages.md) — four Chapters; current HTML codec; codec on document-formats; In-app styling, Public URL, and Published-page CSS have no Project yet.
- [Chart chapters for Build or explore a wiki](plan/roadmap/issues/09-chart-build-or-explore-a-wiki.md) — two Chapters; current Markdown codec; `.md` on document-formats; Public URL (md as HTML) has no Project yet.
- [Chart chapters for Manage a project](plan/roadmap/issues/10-chart-manage-a-project.md) — two Chapters; current Status; Date is set/find/compare with no forced meaning; no owning Project yet.
- [Home every Project on an Epic](plan/roadmap/issues/11-home-every-project-on-an-epic.md) — coverage via Homed Projects; new home Epics Organize Huge Outlines and Robust outliner; ChildNode drop ref tabled; work-board-audit on this Roadmap.
- [Required for Epic done vs Chapter, no overlap](plan/roadmap/issues/12-required-for-epic-done.md) — Chapter vs Required for done; wiki portions about this Epic gate this Epic.

## Not yet specified

- Chart connect Epic or home Epic for [[plan/transport-layer/project.md]] when connector Projects multiply beyond PKM dependency.
- Pointer charting for Public URL, Published-page CSS, and In-app styling (web pages) and for wiki Public URL (no owning Project yet).
- Pointer charting for Status and Date on [[epics/manage-a-project.md]] (no owning Project yet).
- Later Epics beyond the standing list.
- Which remaining Required-until-done items belong on a Chapter. Named parts of a Project still unspecified.
- How leftover `doc/roadmap` files become references vs history vs current.
- When the application is complete (person-job Epics plus any later).

## Out of scope

- A third Project kind (Epic Projects).
- Replacing [[WORK.md]].
- Rewriting every `doc/` file in this charting session.
- Implementing feature slices on this map (those belong on feature-set Projects).
- Agent-instruction overhaul as the Roadmap sequence (except the steering stage added while charting).

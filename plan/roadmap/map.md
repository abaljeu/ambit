# Ambit Roadmap

## Destination

Rapid understanding via dynamic organization.

The program, told through Epics. Epic shape: [[epic-context.md]]. Chapter shape: [[epics/chapter-template.md]]. Feature-set Project shape: [[../../context/project-context.md]].

Completing this Roadmap means completing the application. [[doc/index.md]] is the Feature index of the current program.

## Current strategy

(Mutable section. Replace this section when the near-term aim changes.)

Solid product core via [[epics/robust-outliner.md]]: **v1 built** ([[plan/core-creation/project.md]] Stage `done`; Chapter [[epics/chapters/initial-core.md]] met). **v2 live** Core-seam: [[plan/core-refinement/project.md]] ([[plan/core-refinement/arch.md]]); leftovers continue as pointers 08–17. Epic current Chapter: [[epics/chapters/actors-supported.md]]. Grow by pull in / organize / send out. First inbound Agent path [[plan/llm-connector/project.md]] is Stage done. Connected-channel first Chapter: **Ambit as bot DM channel** ([[epics/operate-connected-channels.md]], [[epics/chapters/ambit-as-bot-dm-channel.md]], [[plan/bot-channel/project.md]]); mail stays later. Present Find / Search work: [[plan/online-search/project.md]] (Ambot implement; Chapter home [[epics/chapters/find-what-i-wrote.md]]).

## Epics

- [[epics/work-with-text-files-from-anywhere.md]] — Work with my documents from anywhere (current: [[epics/chapters/automatic-upload-and-download.md]]; vision: [[epics/chapters/two-panel-view.md]])
- [[epics/build-or-explore-a-wiki.md]] — Build or explore a wiki (current: [[epics/chapters/markdown-codec.md]])
- [[epics/create-and-publish-web-pages.md]] — Create and publish web pages (current: [[epics/chapters/html-codec.md]])
- [[epics/manage-a-project.md]] — Manage a project (current: [[epics/chapters/status.md]])
- [[epics/operate-a-pkm.md]] — Operate a PKM (current: [[epics/chapters/find-what-i-wrote.md]])
- [[epics/agent-chat-managed-context.md]] — Agent chat with managed context (current: [[epics/chapters/ask-from-what-i-see.md]])
- [[epics/operate-connected-channels.md]] — Operate connected channels (current: [[epics/chapters/ambit-as-bot-dm-channel.md]])
- [[epics/organize-huge-outlines.md]] — Organize Huge Outlines (current: [[epics/chapters/huge-outline.md]])
- [[epics/robust-outliner.md]] — Robust outliner (current: [[epics/chapters/actors-supported.md]])
- [[epics/process-improvement.md]] — Process improvement (current: [[epics/chapters/improve-how-we-work.md]])

## Decisions so far

- 2026-10-07: Alan lock (Ambot2): [[plan/browser-residency/project.md]] is Stage done (migrate slice). Server Find is parked on [[plan/online-search/project.md]] (Chapter [[epics/chapters/find-what-i-wrote.md]]). [[plan/parse-thread/project.md]] stays on [[epics/chapters/automatic-parse.md]] and on [[epics/chapters/incremental-operations.md]]. Recorded here; no Roadmap issue file.
- 2026-10-07: Alan lock (Ambot2): [[plan/skills-review/project.md]] is on [[epics/process-improvement.md]], beside [[plan/planning-skills/project.md]]. [[plan/linux-server/project.md]] homes on that Epic as optional chart work and stays outside the committed list until Alan commits. Recorded here; no Roadmap issue file.
- 2026-10-07: Auto-upload as a sibling Project is withdrawn (Alan). Keep-files-current outbound is UPDATE = Persist (Graph → text) then push. [transport-layer](plan/transport-layer/project.md) owns that UPDATE chart as Desktop App sync (App folder ↔ Server DataDir; Upload and Download). GitHub stays the separate external remote. Chapter [Automatic upload and download](epics/chapters/automatic-upload-and-download.md). [07 — Chart Automatic upload and download onto Projects](issues/07-chart-automatic-upload-and-download.md) is done. It does not chart a new auto-upload Project. Recorded here; no new Roadmap issue.
- 2026-09-26: GitHub sourcing is Chapter [[epics/chapters/send-to-and-from-github.md]] on [[epics/work-with-text-files-from-anywhere.md]]; detail [[plan/github-transport/project.md]]. Keep-files-current stays [[epics/chapters/automatic-upload-and-download.md]]. [[epics/agent-chat-managed-context.md]] depends on Send to and from GitHub. It does not own that Chapter. Supersedes the 2026-09-01 home of desktop-repo consistency on Agent chat. Recorded here; no Roadmap issue file.
- 2026-09-19: Desktop App sync redesign (FETCH → ORGANIZE → UPDATE) folds into Chapter [[epics/chapters/automatic-upload-and-download.md]] on [[epics/work-with-text-files-from-anywhere.md]]; [[plan/transport-layer/project.md]] owns the redesign chart. GitHub stays a separate Chapter. Recorded here; no Roadmap issue file.
- 2026-09-26: Folded want-driven Browser residency, file-shaped reconcile, and search hydration from leftover on-demand residency into [[epics/chapters/incremental-operations.md]]. Deleted [[doc/roadmap/on-demand-graph-residency.md]] and the parse-load-demote stub. Other `doc/roadmap` files stay. Project homes: [[plan/browser-residency/project.md]], [[plan/parse-thread/project.md]], file transit remains [[plan/transport-layer/project.md]]. [[plan/selective-client-loading/project.md]] is done. Recorded here; no Roadmap issue file.
- [Inventory live Projects and roadmap remainder](plan/roadmap/issues/02-inventory-live-projects-and-roadmap-remainder.md) — [[plan/roadmap/reports/live-projects-and-roadmap-remainder.md]]: 16 non-done feature-set Projects (includes End-user wiki, Architecture, Marketing wiki); postgres-roadmap §0–2 done, §3–7 still open.
- [Name and order the first Epics](plan/roadmap/issues/01-name-and-order-first-epics.md) — five User Epics; outline capture already met; wiki folders stay Projects.
- [Retire index Development Sequence](plan/roadmap/issues/03-retire-index-development-sequence.md) — [[doc/index.md]] is the Feature index; leftovers went to existing Projects plus [[plan/document-formats/map.md]]; desktop mapping is Current.
- [Chart chapters for Agent chat with managed context](plan/roadmap/issues/04-chart-agent-chat-managed-context.md) — five Chapters; current Ask from what I see; llm-connector plus `?` on expression-language and the Core Actor spine.
- [Create Ask from what I see Project](plan/roadmap/issues/05-create-ask-from-what-i-see-project.md) — [[plan/llm-connector/project.md]]; `?` recognition is [[plan/expression-language/issues/33-recognize-ask-run-statement.md]].
- [Chart chapters for Work with my text files from anywhere](plan/roadmap/issues/06-chart-work-with-text-files-from-anywhere.md) — title Work with my documents from anywhere; current Automatic upload and download.
- [Chart chapters for Create and publish web pages](plan/roadmap/issues/08-chart-create-and-publish-web-pages.md) — four Chapters; current HTML codec; codec on document-formats.
- [Chart chapters for Build or explore a wiki](plan/roadmap/issues/09-chart-build-or-explore-a-wiki.md) — two Chapters; current Markdown codec; `.md` on document-formats.
- [Chart chapters for Manage a project](plan/roadmap/issues/10-chart-manage-a-project.md) — two Chapters; current Status; Date is set/find/compare.
- 2026-09-05: Chart Chapters on [[epics/robust-outliner.md]]. Current moved from [[epics/chapters/initial-core.md]] to [[epics/chapters/actors-supported.md]]. Core detail belongs to [[plan/core-creation/project.md]]. Recorded here; no Roadmap issue file.
- [Home every Project on an Epic](plan/roadmap/issues/11-home-every-project-on-an-epic.md) — coverage by Chapter Project lists; Organize Huge Outlines and Robust outliner added; ChildNode drop ref tabled; work-board-cleanup on this Roadmap.
- 2026-09-04: Stage `dead` added (retired without delivery). ChildNode drop ref is dead. Work board cleanup is done. [[epics/process-improvement.md]] places [[plan/git-protocol/project.md]] (done). [[plan/debug-reload/project.md]] sits on Robust outliner as architecture documentation.
- [Required for Epic done vs Chapter, no overlap](plan/roadmap/issues/12-required-for-epic-done.md) — Chapter Project lists hold the linkage. Wiki portions about an Epic sit on that Epic's Chapters.
- [Grill Cursor-repo to Ambit LLM use onto Epics](plan/roadmap/issues/13-grill-cursor-repo-to-ambit-llm-use.md) — home on [[epics/agent-chat-managed-context.md]]; depends on Ask from what I see and on automatic upload and download.
- Work board retired 2026-09-02. No live [[WORK.md]]. Cleanup Project: [[plan/work-board-cleanup/project.md]] (rename of work-board-audit).
- Operate connected channels from the locked definition (2026-09-03). Connect plus operate. Mail stays later. Chapter: [[epics/chapters/ambit-as-bot-dm-channel.md]].
- 2026-10-07: Alan approved: home [[plan/online-search/project.md]] on Chapter [[epics/chapters/find-what-i-wrote.md]] (Ambot keeps implement / server Search Want-fulfillment). Search hydration stays Browser residency. Leave `plan/online-search/` for its own sessions. Recorded here; no Roadmap issue file.
- 2026-10-07: Alan approved: [[plan/workspace-git/project.md]] Stage dead; prior home superseded by [[plan/github-transport/project.md]] / Chapter [[epics/chapters/send-to-and-from-github.md]]. Recorded here; no Roadmap issue file.
- 2026-10-07: Alan lock: solid core built via [[plan/core-creation/project.md]] (v1 implemented, Stage `done`); [[plan/core-refinement/project.md]] is v2. Chapter [[epics/chapters/initial-core.md]] met; current Chapter [[epics/chapters/actors-supported.md]]. Leftover core-creation issues continue as pointer tickets [[plan/core-refinement/issues/08-pointer-core-actor-pool.md]]–[[plan/core-refinement/issues/17-pointer-prove-testactor-hello.md]]. Recorded here; no Roadmap issue file.

## Not yet specified

- Further Chapters for [[epics/operate-connected-channels.md]] after [[epics/chapters/ambit-as-bot-dm-channel.md]] (mail still later). [[plan/transport-layer/project.md]] stays the Project pattern.
- A Project for Public URL, Published-page CSS, and In-app styling, and for wiki Public URL.
- A Project for Status and Date on [[epics/manage-a-project.md]].
- Cherry-pick Undo (invert a chosen ChangeEvent; skip Actor Events).
- Restore older file versions via actual git (from the commits already made on edits).
- Later Epics beyond the standing list.
- How leftover `doc/roadmap` files become references, history, or current.
- When the application is complete.

## Out of scope

- Epic Project folders.
- Rewriting every `doc/` file in the charting session.
- Implementing feature slices on this map. Those belong on feature-set Projects.

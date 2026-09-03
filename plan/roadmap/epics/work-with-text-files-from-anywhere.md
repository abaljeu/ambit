# Work with my documents from anywhere

Stage: charting

A person explores and works with their documents from any connected device, not only at one desk. The Browser or the App talks to the same Server. In the App a document on disk is a File Node. Graphic editing is out of scope.

Current chapter: [[chapters/automatic-upload-and-download.md]]

## Chapters

- [[chapters/automatic-upload-and-download.md]]
- [[chapters/automatic-parse.md]]
- [[chapters/markdown-styling.md]]
- [[chapters/embed-an-image.md]]
- [[chapters/tables.md]]

## Required for done

The Epic is not done until each item is done (or the named part).

Live:

- [ ] [[plan/selective-client-loading/project.md]]
- [ ] [[plan/client-start-time/project.md]] — primarily mobile; this Epic is a key component for [[manage-a-project.md]] and [[operate-a-pkm.md]]
- [ ] [[plan/daily-git-save/project.md]]
- [ ] [[plan/download-no-parse-fix/project.md]]
- [ ] [[plan/parse-load-demote/project.md]]
- [ ] [[plan/bullet-tip-times/project.md]]
- [ ] [[plan/document-formats/map.md]] — remainder (XML and other draft codecs); `.md` and HTML are on other Epics
- [ ] [[plan/end-user-wiki/issues/01-describe-documents-from-any-connected-device.md]] — end-user wiki portion for this Epic
- [ ] [[plan/marketing-wiki/issues/01-use-page-documents-from-any-connected-device.md]] — marketing wiki portion for this Epic

Done:

- [x] [[plan/load-status-phases/project.md]]
- [x] [[plan/login-context-restore/project.md]]
- [x] [[plan/node-bullet-tooltip/project.md]]
- [x] [[plan/relaxed-concurrency/project.md]]

## Notes

- [[plan/transport-layer/project.md]] cross-cutting pattern — disk Upload/Download is one transport instance (Parse/Persist); Google (Drive/Docs, example) is another inbound and likely later round-trip channel on the same contract (plan from Local Graph, Parse/Persist, emit Changes via ESO Actor). See [[plan/transport-layer/overview.md]], [[plan/transport-layer/map.md]].
- Scaling is [[organize-huge-outlines.md]]; first use does not wait on it.
- Outline capture (“get it out of my head”) is already met; this Epic is documents from anywhere.
- Chapters stay open-ended: add one when a specific document class or usability is named.
- Wiki write-up is not a Chapter. Portions are Required for done.
- A Google Doc (a document that is not a File) is potentially IN scope.
- Graphic editing is out of scope. A graphic file is not a document.
- English **document** vs **Document** (project) vs File Node: [[CONTEXT.md]].
- [[agent-chat-managed-context.md]] Chapter [[chapters/ambit-keeps-consistency-with-desktop-repo-for-agentic-work.md]] depends on this Epic’s [[chapters/automatic-upload-and-download.md]]. This Epic keeps that Chapter and those Projects. Mapping is Current ([[doc/current/workspace-local-mapping.md]]). Grill: [[plan/roadmap/issues/13-grill-cursor-repo-to-ambit-llm-use.md]].

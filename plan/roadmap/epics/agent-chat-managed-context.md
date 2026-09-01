# Agent chat with managed context

Stage: charting

A person chats with an Agent that provides managed context.

Current chapter: Ask from what I see

## Chapters

### Ask from what I see

**What to build:** Run `?` with a message and included context. The reply is Owned children of the focus Node. The call is a long-running Actor: launch, answers arrive while the person works, cancel stops a slow job.

**Blocked by:** None.

- [ ] [[plan/llm-connector/project.md]] — pack, LLM call, write-back
- [ ] [[plan/expression-language/issues/33-recognize-ask-run-statement.md]] — recognize `?` as a Run statement
- [ ] [[plan/event-sourced-ops/issues/07-generalized-server-actor-produce-path.md]]
- [ ] [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]]

### Ambit keeps consistency with desktop repo for Agentic work

**What to build:** Map a desktop repo folder to a Workspace Node in the App (Current). Automatic Upload and Download keep that folder consistent with the Server. The person works in the App and in the Browser. Ambit's Agent continues the Agentic work. Do not name the Agent after a vendor.

**Blocked by:** Ask from what I see; [[work-with-text-files-from-anywhere.md]] Automatic upload and download.

- [ ] [[plan/roadmap/issues/07-chart-automatic-upload-and-download.md]] — chart auto-upload (owned by the documents Chapter)
- [ ] [[plan/auto-download-persisted-files/project.md]] — auto-download (HITL tabled; owned by the documents Chapter)
- [ ] [[plan/client-start-time/project.md]] — primarily the Browser on a phone

### Talk again

**What to build:** Follow-up turns live in the Graph, not a one-shot `?`.

**Blocked by:** Ask from what I see.

### Change the Graph

**What to build:** The LLM authors Changes, not only reply text.

**Blocked by:** Ask from what I see.

### Query the Graph or the files

**What to build:** The LLM queries the Graph or the files behind File Nodes, not only included context.

**Blocked by:** Ask from what I see.

### Act through CLI

**What to build:** Work runs through CLI a long-running Actor. Not glossary Agent.

**Blocked by:** Change the Graph.

- [ ] [[plan/event-sourced-ops/details/actors-and-jobs.md]] — shell command as a later Actor

### Act through MCP

## Required for done

The Epic is not done until each item is done (or the named part).

Live:

- [ ] [[plan/expression-language/project.md]] — remainder beyond `?` recognition
- [ ] [[plan/event-sourced-ops/project.md]] — remainder beyond Actor issues on Chapters
- [ ] [[plan/end-user-wiki/map.md]] — portion for this Epic (not yet filed)
- [ ] [[plan/marketing-wiki/map.md]] — portion for this Epic (not yet filed)

## Notes

- [[plan/transport-layer/project.md]] cross-cutting pattern — agent Actor is an inbound transport leg (reply as Owned children); see [[plan/transport-layer/overview.md]], [[plan/transport-layer/map.md]].
- Scaling is [[organize-huge-outlines.md]]; first use does not wait on it.
- Desktop-repo use is Chapter **Ambit keeps consistency with desktop repo for Agentic work**. One Chapter on this Epic; Projects own the details. No Chapter order — only **Blocked by**. Current chapter stays **Ask from what I see** ([[plan/llm-connector/project.md]]). Mapping is Current ([[doc/current/workspace-local-mapping.md]]). Upload/Download stay on [[work-with-text-files-from-anywhere.md]]. **Talk again** stays a separate Chapter. **Agent** and **Agentic**: [[CONTEXT.md]]. Grill: [[plan/roadmap/issues/13-grill-cursor-repo-to-ambit-llm-use.md]]. Options history: [[plan/roadmap/reports/cursor-repo-to-ambit-mobile-grok.md]].
